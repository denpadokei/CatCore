using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Helpers.JSON;
using CatCore.Models.EventArgs;
using CatCore.Models.Shared;
using CatCore.Models.Twitch;
using CatCore.Models.Twitch.EventSub;
using CatCore.Models.Twitch.IRC;
using CatCore.Models.Twitch.OAuth;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using CatCore.Services.Twitch.Media;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal sealed class TwitchEventSubChatService : ITwitchIrcService
	{
		private const string TWITCH_EVENTSUB_ENDPOINT = "wss://eventsub.wss.twitch.tv/ws";

		// EventSub subscription types for chat
		private const string SUB_TYPE_CHANNEL_CHAT_MESSAGE = "channel.chat.message";
		private const string SUB_TYPE_CHANNEL_CHAT_NOTIFICATION = "channel.chat.notification";
		private const string SUB_TYPE_CHANNEL_CHAT_MESSAGE_DELETE = "channel.chat.message_delete";
		private const string SUB_TYPE_CHANNEL_CHAT_CLEAR = "channel.chat.clear";
		private const string SUB_TYPE_CHANNEL_CHAT_SETTINGS_UPDATE = "channel.chat_settings.update";
		private const string EVENTSUB_VERSION = "1";

		private readonly ILogger _logger;
		private readonly IKittenWebSocketProvider _kittenWebSocketProvider;
		private readonly IKittenPlatformActiveStateManager _activeStateManager;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly ITwitchChannelManagementService _twitchChannelManagementService;
		private readonly ITwitchRoomStateTrackerService _roomStateTrackerService;
		private readonly ITwitchUserStateTrackerService _userStateTrackerService;
		private readonly TwitchEmoteDetectionHelper _twitchEmoteDetectionHelper;
		private readonly TwitchMediaDataProvider _twitchMediaDataProvider;
		private readonly TwitchHelixApiService _twitchHelixApiService;

		private string? _sessionId;
		private string? _reconnectUrl;
		private ValidationResponse? _loggedInUser;

		// channelId → subscriptionIds[]
		private readonly ConcurrentDictionary<string, List<string>> _channelSubscriptionIds;

		private readonly SemaphoreSlim _connectionLockerSemaphoreSlim = new(1, 1);

		public TwitchEventSubChatService(ILogger logger, IKittenWebSocketProvider kittenWebSocketProvider, IKittenPlatformActiveStateManager activeStateManager,
			ITwitchAuthService twitchAuthService, ITwitchChannelManagementService twitchChannelManagementService, ITwitchRoomStateTrackerService roomStateTrackerService,
			ITwitchUserStateTrackerService userStateTrackerService, TwitchEmoteDetectionHelper twitchEmoteDetectionHelper, TwitchMediaDataProvider twitchMediaDataProvider,
			TwitchHelixApiService twitchHelixApiService)
		{
			_logger = logger;
			_kittenWebSocketProvider = kittenWebSocketProvider;
			_activeStateManager = activeStateManager;
			_twitchAuthService = twitchAuthService;
			_twitchChannelManagementService = twitchChannelManagementService;
			_roomStateTrackerService = roomStateTrackerService;
			_userStateTrackerService = userStateTrackerService;
			_twitchEmoteDetectionHelper = twitchEmoteDetectionHelper;
			_twitchMediaDataProvider = twitchMediaDataProvider;
			_twitchHelixApiService = twitchHelixApiService;

			_twitchAuthService.OnCredentialsChanged += TwitchAuthServiceOnOnCredentialsChanged;
			_twitchChannelManagementService.ChannelsUpdated += TwitchChannelManagementServiceOnChannelsUpdated;

			_channelSubscriptionIds = new ConcurrentDictionary<string, List<string>>();
		}

		public event Action? OnChatConnected;
		public event Action<TwitchChannel>? OnJoinChannel;
		public event Action<TwitchChannel>? OnLeaveChannel;
		public event Action<TwitchChannel>? OnRoomStateChanged;
		public event Action<TwitchMessage>? OnMessageReceived;
		public event Action<TwitchChannel, string>? OnMessageDeleted;
		public event Action<TwitchChannel, string?>? OnChatCleared;

		public void SendMessage(TwitchChannel channel, string message)
		{
			if (_loggedInUser == null)
			{
				_logger.Warning("Cannot send message: not logged in");
				return;
			}

			_ = Task.Run(async () =>
			{
				var senderUserId = _loggedInUser?.UserId ?? string.Empty;
				if (senderUserId.Length == 0)
				{
					_logger.Warning("Cannot send message: user id is unavailable");
					return;
				}

				var success = await _twitchHelixApiService.SendChatMessage(channel.Id, senderUserId, message).ConfigureAwait(false);
				if (!success)
				{
					_logger.Warning("Failed to send message to channel {ChannelId}", channel.Id);
				}
			});
		}

		Task ITwitchIrcService.Start()
		{
			_logger.Verbose("Start requested by service manager");
			return StartInternal();
		}

		Task ITwitchIrcService.Stop()
		{
			_logger.Verbose("Stop requested by service manager");
			return StopInternal();
		}

		private async Task StartInternal()
		{
			using var _ = await Synchronization.LockAsync(_connectionLockerSemaphoreSlim);
			if (!_twitchAuthService.HasTokens)
			{
				return;
			}

			_loggedInUser = await _twitchAuthService.FetchLoggedInUserInfoWithRefresh().ConfigureAwait(false);
			if (_loggedInUser == null)
			{
				return;
			}

			_kittenWebSocketProvider.ConnectHappened -= ConnectHappenedHandler;
			_kittenWebSocketProvider.ConnectHappened += ConnectHappenedHandler;

			_kittenWebSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
			_kittenWebSocketProvider.DisconnectHappened += DisconnectHappenedHandler;

			_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;
			_kittenWebSocketProvider.MessageReceived += MessageReceivedHandler;

			var connectUrl = _reconnectUrl ?? TWITCH_EVENTSUB_ENDPOINT;
			await _kittenWebSocketProvider.Connect(connectUrl).ConfigureAwait(false);
		}

		private async Task StopInternal()
		{
			// Unsubscribe from all channels
			var channelIds = _channelSubscriptionIds.Keys.ToList();
			foreach (var channelId in channelIds)
			{
				await UnsubscribeFromChannelInternal(channelId).ConfigureAwait(false);
			}

			await _kittenWebSocketProvider.Disconnect().ConfigureAwait(false);

			_kittenWebSocketProvider.ConnectHappened -= ConnectHappenedHandler;
			_kittenWebSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
			_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;

			_loggedInUser = null;
			_sessionId = null;
			_reconnectUrl = null;
		}

		private async void TwitchAuthServiceOnOnCredentialsChanged()
		{
			if (_twitchAuthService.HasTokens)
			{
				if (_activeStateManager.GetState(PlatformType.Twitch))
				{
					_logger.Verbose("(Re)start requested by credential changes");
					await StartInternal().ConfigureAwait(false);
				}
			}
			else
			{
				await StopInternal().ConfigureAwait(false);
			}
		}

		private void TwitchChannelManagementServiceOnChannelsUpdated(object sender, TwitchChannelsUpdatedEventArgs e)
		{
			if (_activeStateManager.GetState(PlatformType.Twitch))
			{
				foreach (var disabledChannel in e.DisabledChannels)
				{
					_ = Task.Run(() => UnsubscribeFromChannelInternal(disabledChannel.Key));
				}

				foreach (var enabledChannel in e.EnabledChannels)
				{
					_ = Task.Run(() => SubscribeToChannelInternal(enabledChannel.Key, enabledChannel.Value));
				}
			}
		}

		private async Task ConnectHappenedHandler(WebSocketConnection webSocketConnection)
		{
			_logger.Verbose("EventSub WebSocket connect handler triggered");
		}

		private Task DisconnectHappenedHandler()
		{
			_reconnectUrl = null;

			return Task.CompletedTask;
		}

		private Task MessageReceivedHandler(WebSocketConnection webSocketConnection, string message)
		{
			MessageReceivedHandlerInternal(webSocketConnection, message);
			return Task.CompletedTask;
		}

		private void MessageReceivedHandlerInternal(WebSocketConnection webSocketConnection, string rawMessage)
		{
#if DEBUG
			_logger.Verbose("EventSub message received: {Message}", rawMessage);
#endif

			try
			{
				// Parse the top-level message to determine type
				using var jsonDocument = JsonDocument.Parse(rawMessage);
				var root = jsonDocument.RootElement;

				if (!root.TryGetProperty("metadata", out var metadataElement))
				{
					_logger.Warning("Received EventSub message without metadata");
					return;
				}

				using var metadataDocument = JsonDocument.Parse(metadataElement.GetRawText());
				var metadata = JsonSerializer.Deserialize(metadataDocument.RootElement, TwitchEventSubSerializerContext.Default.EventSubMetadata);

				HandleEventSubMessage(metadata, rawMessage);
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to parse EventSub message");
			}
		}

		private void HandleEventSubMessage(EventSubMetadata metadata, string rawMessage)
		{
			switch (metadata.MessageType)
			{
				case "session_welcome":
					HandleSessionWelcome(rawMessage);
					break;
				case "session_keepalive":
					// NOP
					break;
				case "session_reconnect":
					HandleSessionReconnect(rawMessage);
					break;
				case "notification":
					HandleNotification(metadata, rawMessage);
					break;
				case "revocation":
					HandleRevocation(rawMessage);
					break;
				default:
					_logger.Verbose("Received unknown EventSub message type: {MessageType}", metadata.MessageType);
					break;
			}
		}

		private void HandleSessionWelcome(string rawMessage)
		{
			try
			{
				using var jsonDocument = JsonDocument.Parse(rawMessage);
				var payload = JsonSerializer.Deserialize(
					jsonDocument.RootElement.GetProperty("payload"),
					TwitchEventSubSerializerContext.Default.EventSubSessionPayload);

				_sessionId = payload.Session.Id;
				_reconnectUrl = null;

				_logger.Information("EventSub session established. Session ID: {SessionId}", _sessionId);

				OnChatConnected?.Invoke();

				// Subscribe to all active channels
				_ = Task.Run(() => SubscribeToAllChannelsInternal());
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to handle session_welcome");
			}
		}

		private void HandleSessionReconnect(string rawMessage)
		{
			try
			{
				using var jsonDocument = JsonDocument.Parse(rawMessage);
				var payload = JsonSerializer.Deserialize(
					jsonDocument.RootElement.GetProperty("payload"),
					TwitchEventSubSerializerContext.Default.EventSubSessionPayload);

				_reconnectUrl = payload.Session.ReconnectUrl;

				_logger.Information("EventSub session reconnect requested. New URL: {ReconnectUrl}", _reconnectUrl);

				// Disconnect current connection (will trigger reconnect with new URL)
				_ = Task.Run(() => _kittenWebSocketProvider.Disconnect("session_reconnect"));
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to handle session_reconnect");
			}
		}

		private void HandleNotification(EventSubMetadata metadata, string rawMessage)
		{
			switch (metadata.SubscriptionType)
			{
				case SUB_TYPE_CHANNEL_CHAT_MESSAGE:
					HandleChatMessage(rawMessage);
					break;
				case SUB_TYPE_CHANNEL_CHAT_NOTIFICATION:
					HandleChatNotification(rawMessage);
					break;
				case SUB_TYPE_CHANNEL_CHAT_MESSAGE_DELETE:
					HandleMessageDelete(rawMessage);
					break;
				case SUB_TYPE_CHANNEL_CHAT_CLEAR:
					HandleChatClear(rawMessage);
					break;
				case SUB_TYPE_CHANNEL_CHAT_SETTINGS_UPDATE:
					HandleSettingsUpdate(rawMessage);
					break;
				default:
					_logger.Verbose("Received unknown subscription type: {SubscriptionType}", metadata.SubscriptionType);
					break;
			}
		}

		private void HandleChatMessage(string rawMessage)
		{
			try
			{
				using var jsonDocument = JsonDocument.Parse(rawMessage);
				var payload = JsonSerializer.Deserialize(
					jsonDocument.RootElement.GetProperty("payload"),
					TwitchEventSubSerializerContext.Default.EventSubChatMessagePayload);

				var ev = payload.Event;
				var channel = new TwitchChannel(this, ev.BroadcasterUserId, ev.BroadcasterUserLogin);

				// Build badges
				var badgeEntries = new List<IChatBadge>();
				foreach (var badge in ev.Badges ?? new List<EventSubBadge>())
				{
					var badgeIdentifier = $"{badge.SetId}/{badge.Id}";
					if (_twitchMediaDataProvider.TryGetBadge(badgeIdentifier, ev.BroadcasterUserId, out var badgeObj))
					{
						badgeEntries.Add(badgeObj!);
					}
				}

				// Build user
				var user = new TwitchUser(
					ev.ChatterUserId,
					ev.ChatterUserLogin,
					ev.ChatterUserName,
					ev.Color,
					ev.Badges?.Any(b => b.SetId == "moderator") ?? false,
					ev.Badges?.Any(b => b.SetId == "broadcaster") ?? false,
					ev.Badges?.Any(b => b.SetId == "subscriber" || b.SetId == "founder") ?? false,
					ev.Badges?.Any(b => b.SetId == "turbo") ?? false,
					ev.Badges?.Any(b => b.SetId == "vip") ?? false,
					badgeEntries.AsReadOnly()
				);

				// Extract emotes
				var emotes = _twitchEmoteDetectionHelper.ExtractEmoteInfoFromFragments(
					ev.Message.Text,
					ev.Message.Fragments,
					ev.BroadcasterUserId,
					(uint)(ev.Cheer?.Bits ?? 0)
				);

				// Determine if the logged-in user is mentioned
				var isMentioned = false;
				if (_loggedInUser != null && !string.IsNullOrEmpty(ev.Message.Text))
				{
					var mention = "@" + _loggedInUser.Value.LoginName;
					isMentioned = ev.Message.Text.IndexOf(mention, StringComparison.OrdinalIgnoreCase) >= 0;
				}

				// Build TwitchMessage
				var twitchMessage = new TwitchMessage(
					ev.MessageId,
					false,
					ev.MessageType == "ACTION",
					isMentioned,
					ev.Message.Text,
					user,
					channel,
					emotes.AsReadOnly(),
					null, // Metadata (not needed for EventSub)
					"PRIVMSG", // IRC type equivalent
					(uint)(ev.Cheer?.Bits ?? 0)
				);

				OnMessageReceived?.Invoke(twitchMessage);
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to handle channel.chat.message");
			}
		}

		private void HandleChatNotification(string rawMessage)
		{
			try
			{
				using var jsonDocument = JsonDocument.Parse(rawMessage);
				var payload = JsonSerializer.Deserialize(
					jsonDocument.RootElement.GetProperty("payload"),
					TwitchEventSubSerializerContext.Default.EventSubChatNotificationPayload);

				var ev = payload.Event;
				var channel = new TwitchChannel(this, ev.BroadcasterUserId, ev.BroadcasterUserLogin);

				// Build user
				var user = new TwitchUser(
					ev.ChatterUserId,
					ev.ChatterUserLogin,
					ev.ChatterUserName,
					"#ffffff",
					false,
					false,
					false,
					false,
					false,
					new ReadOnlyCollection<IChatBadge>(new List<IChatBadge>())
				);

				// Extract emotes
				var emotes = _twitchEmoteDetectionHelper.ExtractEmoteInfoFromFragments(
					ev.Message.Text,
					ev.Message.Fragments,
					ev.BroadcasterUserId,
					0
				);

				var twitchMessage = new TwitchMessage(
					Guid.NewGuid().ToString(),
					true, // IsSystemMessage
					false,
					false,
					ev.Message.Text,
					user,
					channel,
					emotes.AsReadOnly(),
					null,
					"USERNOTICE",
					0
				);

				OnMessageReceived?.Invoke(twitchMessage);
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to handle channel.chat.notification");
			}
		}

		private void HandleMessageDelete(string rawMessage)
		{
			try
			{
				using var jsonDocument = JsonDocument.Parse(rawMessage);
				var payload = JsonSerializer.Deserialize(
					jsonDocument.RootElement.GetProperty("payload"),
					TwitchEventSubSerializerContext.Default.EventSubChatMessageDeletePayload);

				var ev = payload.Event;
				var channel = new TwitchChannel(this, ev.BroadcasterUserId, ev.BroadcasterUserLogin);

				OnMessageDeleted?.Invoke(channel, ev.MessageId);
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to handle channel.chat.message_delete");
			}
		}

		private void HandleChatClear(string rawMessage)
		{
			try
			{
				using var jsonDocument = JsonDocument.Parse(rawMessage);
				var payload = JsonSerializer.Deserialize(
					jsonDocument.RootElement.GetProperty("payload"),
					TwitchEventSubSerializerContext.Default.EventSubChatClearPayload);

				var ev = payload.Event;
				var channel = new TwitchChannel(this, ev.BroadcasterUserId, ev.BroadcasterUserLogin);

				OnChatCleared?.Invoke(channel, null);
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to handle channel.chat.clear");
			}
		}

		private void HandleSettingsUpdate(string rawMessage)
		{
			try
			{
				using var jsonDocument = JsonDocument.Parse(rawMessage);
				var payload = JsonSerializer.Deserialize(
					jsonDocument.RootElement.GetProperty("payload"),
					TwitchEventSubSerializerContext.Default.EventSubChatSettingsPayload);

				var ev = payload.Event;

				// Build IRC-equivalent tags dictionary
				var tags = new Dictionary<string, string>();
				// Populate tags for both enabled and disabled states so room state transitions correctly.
				tags[IrcMessageTags.EMOTE_ONLY] = ev.EmoteMode ? "1" : "0";
				tags[IrcMessageTags.FOLLOWERS_ONLY] = ev.FollowerMode
					? ev.FollowerModeDurationMinutes.ToString()
					: "-1";
				tags[IrcMessageTags.SUBS_ONLY] = ev.SubscriberMode ? "1" : "0";
				tags[IrcMessageTags.R9_K] = ev.UniqueChatMode ? "1" : "0";
				tags[IrcMessageTags.SLOW] = ev.SlowMode
					? ev.SlowModeWaitSeconds.ToString()
					: "0";

				var roomStateDict = new ReadOnlyDictionary<string, string>(tags);
				_roomStateTrackerService.UpdateRoomState(ev.BroadcasterUserLogin, roomStateDict);

				var channel = new TwitchChannel(this, ev.BroadcasterUserId, ev.BroadcasterUserLogin);
				OnRoomStateChanged?.Invoke(channel);
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to handle channel.chat.settings.update");
			}
		}

		private void HandleRevocation(string rawMessage)
		{
			_logger.Warning("Received revocation message: {Message}", rawMessage);
		}

		private async Task SubscribeToAllChannelsInternal()
		{
			if (_sessionId == null)
			{
				return;
			}

			var activeChannels = _twitchChannelManagementService.GetAllActiveChannelsAsDictionary();
			var channelIds = _twitchChannelManagementService.GetAllActiveChannelIds();
			foreach (var channelId in channelIds)
			{
				if (!activeChannels.TryGetValue(channelId, out var channelName))
				{
					_logger.Warning("Active channel dictionary missing entry for channel ID {ChannelId} while subscribing to all channels.", channelId);
					continue;
				}

				await SubscribeToChannelInternal(channelId, channelName).ConfigureAwait(false);
			}
		}

		private async Task SubscribeToChannelInternal(string channelId, string channelName)
		{
			if (_sessionId == null)
			{
				_logger.Verbose("Cannot subscribe: session not established");
				return;
			}

			if (_loggedInUser == null)
			{
				_logger.Warning("Cannot subscribe to channel {ChannelId}: logged in user is unavailable", channelId);
				return;
			}

			var condition = new Dictionary<string, string>
			{
				{ "broadcaster_user_id", channelId },
				{ "user_id", _loggedInUser.Value.UserId }
			};
			var subscriptionTypes = new[] { SUB_TYPE_CHANNEL_CHAT_MESSAGE, SUB_TYPE_CHANNEL_CHAT_NOTIFICATION, SUB_TYPE_CHANNEL_CHAT_MESSAGE_DELETE, SUB_TYPE_CHANNEL_CHAT_CLEAR, SUB_TYPE_CHANNEL_CHAT_SETTINGS_UPDATE };

			var subscriptionIds = new List<string>();
			foreach (var subType in subscriptionTypes)
			{
				var subscriptionId = await _twitchHelixApiService.CreateEventSubSubscription(subType, EVENTSUB_VERSION, condition, _sessionId).ConfigureAwait(false);
				if (subscriptionId != null)
				{
					subscriptionIds.Add(subscriptionId);
				}
				else
				{
					_logger.Warning("Failed to create EventSub subscription type {SubscriptionType} for channel {ChannelId}", subType, channelId);
				}
			}

			if (subscriptionIds.Count > 0)
			{
				// Delete any existing subscriptions for this channel before replacing to avoid leaks.
				if (_channelSubscriptionIds.TryRemove(channelId, out var existingSubscriptionIds))
				{
					foreach (var existingId in existingSubscriptionIds)
					{
						await _twitchHelixApiService.DeleteEventSubSubscription(existingId).ConfigureAwait(false);
					}
				}

				_channelSubscriptionIds[channelId] = subscriptionIds;
				OnJoinChannel?.Invoke(new TwitchChannel(this, channelId, channelName));
			}
			else
			{
				_logger.Warning("Failed to create EventSub subscriptions for channel {ChannelId}. AttemptedTypes={AttemptedTypes}", channelId, string.Join(",", subscriptionTypes));
			}
		}

		private async Task UnsubscribeFromChannelInternal(string channelId)
		{
			if (!_channelSubscriptionIds.TryRemove(channelId, out var subscriptionIds))
			{
				return;
			}

			foreach (var subscriptionId in subscriptionIds)
			{
				await _twitchHelixApiService.DeleteEventSubSubscription(subscriptionId).ConfigureAwait(false);
			}

			// Get the channel name for the event
			var channelDictionary = _twitchChannelManagementService.GetAllActiveChannelsAsDictionary(includeSelfRegardlessOfState: true);
			if (channelDictionary.TryGetValue(channelId, out var channelName))
			{
				_roomStateTrackerService.UpdateRoomState(channelName, null);
				_userStateTrackerService.UpdateUserState(channelId, null);

				OnLeaveChannel?.Invoke(new TwitchChannel(this, channelId, channelName));
			}
		}
	}
}
