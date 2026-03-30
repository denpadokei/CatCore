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
using CatCore.Models.Twitch.PubSub.Responses;
using CatCore.Models.Twitch.PubSub.Responses.ChannelPointsChannelV1;
using CatCore.Models.Twitch.PubSub.Responses.Polls;
using CatCore.Models.Twitch.PubSub.Responses.Predictions;
using CatCore.Models.Twitch.PubSub.Responses.VideoPlayback;
using CatCore.Models.Twitch.Shared;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using CatCore.Services.Twitch.Media;
using Serilog;
using PredictionBadge = CatCore.Models.Twitch.PubSub.Responses.Predictions.Badge;
using PredictionUser = CatCore.Models.Twitch.PubSub.Responses.Predictions.User;
using RewardUser = CatCore.Models.Twitch.PubSub.Responses.ChannelPointsChannelV1.User;

namespace CatCore.Services.Twitch
{
	internal sealed class TwitchEventSubChatService : ITwitchIrcService, ITwitchPubSubServiceManager
	{
		private const string TWITCH_EVENTSUB_ENDPOINT = "wss://eventsub.wss.twitch.tv/ws";

		// EventSub subscription types for chat
		private const string SUB_TYPE_CHANNEL_CHAT_MESSAGE = "channel.chat.message";
		private const string SUB_TYPE_CHANNEL_CHAT_NOTIFICATION = "channel.chat.notification";
		private const string SUB_TYPE_CHANNEL_CHAT_MESSAGE_DELETE = "channel.chat.message_delete";
		private const string SUB_TYPE_CHANNEL_CHAT_CLEAR = "channel.chat.clear";
		private const string SUB_TYPE_CHANNEL_CHAT_SETTINGS_UPDATE = "channel.chat_settings.update";
		private const string SUB_TYPE_STREAM_ONLINE = "stream.online";
		private const string SUB_TYPE_STREAM_OFFLINE = "stream.offline";
		private const string SUB_TYPE_CHANNEL_AD_BREAK_BEGIN = "channel.ad_break.begin";
		private const string SUB_TYPE_CHANNEL_FOLLOW = "channel.follow";
		private const string SUB_TYPE_CHANNEL_POLL_BEGIN = "channel.poll.begin";
		private const string SUB_TYPE_CHANNEL_POLL_PROGRESS = "channel.poll.progress";
		private const string SUB_TYPE_CHANNEL_POLL_END = "channel.poll.end";
		private const string SUB_TYPE_CHANNEL_PREDICTION_BEGIN = "channel.prediction.begin";
		private const string SUB_TYPE_CHANNEL_PREDICTION_PROGRESS = "channel.prediction.progress";
		private const string SUB_TYPE_CHANNEL_PREDICTION_LOCK = "channel.prediction.lock";
		private const string SUB_TYPE_CHANNEL_PREDICTION_END = "channel.prediction.end";
		private const string SUB_TYPE_CHANNEL_REWARD_REDEEM = "channel.channel_points_custom_reward_redemption.add";
		private const string EVENTSUB_VERSION = "1";
		private const string EVENTSUB_VERSION_FOLLOW = "2";
		private static readonly TimeSpan VIEW_COUNT_POLL_INTERVAL = TimeSpan.FromSeconds(30);
		private const int HELIX_USER_IDS_PER_REQUEST_LIMIT = 100;

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
		private readonly ConcurrentDictionary<Action<string, ViewCountUpdate>, bool> _viewCountCallbackRegistrations = new();

		private readonly SemaphoreSlim _connectionLockerSemaphoreSlim = new(1, 1);
		private readonly object _viewCountPollingStateLock = new();
		private CancellationTokenSource? _viewCountPollingCancellationTokenSource;
		private Task? _viewCountPollingTask;

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

		event Action<string, ViewCountUpdate> ITwitchPubSubServiceManager.OnViewCountUpdated
		{
			add
			{
				if (_viewCountCallbackRegistrations.TryAdd(value, false))
				{
					TryStartViewCountPollingIfNeeded();
				}
				else
				{
					_logger.Warning("Callback was already registered for EventHandler {Name}", nameof(ITwitchPubSubServiceManager.OnViewCountUpdated));
				}
			}
			remove
			{
				if (_viewCountCallbackRegistrations.TryRemove(value, out _) && _viewCountCallbackRegistrations.IsEmpty)
				{
					_ = StopViewCountPollingIfRunning();
				}
			}
		}

		public event Action<string, StreamUp>? OnStreamUp;
		public event Action<string, StreamDown>? OnStreamDown;
		public event Action<string, Commercial>? OnCommercial;
		public event Action<string, Follow>? OnFollow;
		public event Action<string, PollData>? OnPoll;
		public event Action<string, PredictionData>? OnPrediction;
		public event Action<string, RewardRedeemedData>? OnRewardRedeemed;

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

		Task ITwitchPubSubServiceManager.Start()
		{
			return StartInternal();
		}

		Task ITwitchPubSubServiceManager.Stop()
		{
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
			await StopViewCountPollingIfRunning().ConfigureAwait(false);

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

				var metadata = JsonSerializer.Deserialize(metadataElement, TwitchEventSubSerializerContext.Default.EventSubMetadata);

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
				TryStartViewCountPollingIfNeeded();

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

				// Establish a new connection using the reconnect URL, then drop the old one per EventSub reconnect semantics.
				_ = Task.Run(async () =>
				{
					try
					{
						// Connect to the new URL (_reconnectUrl is already set, StartInternal will use it)
						await StartInternal().ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						_logger.Warning(ex, "Failed to start new EventSub session during session_reconnect");
					}

					try
					{
						// Disconnect old connection after attempting to bring up the new one
						await _kittenWebSocketProvider.Disconnect("session_reconnect").ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						_logger.Warning(ex, "Failed to disconnect old EventSub session during session_reconnect");
					}
				});
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
				case SUB_TYPE_STREAM_ONLINE:
					HandleStreamOnline(rawMessage);
					break;
				case SUB_TYPE_STREAM_OFFLINE:
					HandleStreamOffline(rawMessage);
					break;
				case SUB_TYPE_CHANNEL_AD_BREAK_BEGIN:
					HandleCommercial(rawMessage);
					break;
				case SUB_TYPE_CHANNEL_FOLLOW:
					HandleFollow(rawMessage);
					break;
				case SUB_TYPE_CHANNEL_POLL_BEGIN:
				case SUB_TYPE_CHANNEL_POLL_PROGRESS:
				case SUB_TYPE_CHANNEL_POLL_END:
					HandlePoll(rawMessage);
					break;
				case SUB_TYPE_CHANNEL_PREDICTION_BEGIN:
				case SUB_TYPE_CHANNEL_PREDICTION_PROGRESS:
				case SUB_TYPE_CHANNEL_PREDICTION_LOCK:
				case SUB_TYPE_CHANNEL_PREDICTION_END:
					HandlePrediction(rawMessage);
					break;
				case SUB_TYPE_CHANNEL_REWARD_REDEEM:
					HandleRewardRedeem(rawMessage);
					break;
				default:
					_logger.Verbose("Received unknown subscription type: {SubscriptionType}", metadata.SubscriptionType);
					break;
			}
		}

		private void HandleStreamOnline(string rawMessage)
		{
			try
			{
				using var jsonDocument = JsonDocument.Parse(rawMessage);
				var ev = jsonDocument.RootElement.GetProperty("payload").GetProperty("event");
				if (!TryGetString(ev, "broadcaster_user_id", out var channelId))
				{
					return;
				}

				var startedAt = TryGetDateTime(ev, "started_at") ?? DateTimeOffset.UtcNow;
				OnStreamUp?.Invoke(channelId, new StreamUp(ToLegacyServerTimeRaw(startedAt), 0));
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to handle stream.online");
			}
		}

		private void HandleStreamOffline(string rawMessage)
		{
			try
			{
				using var jsonDocument = JsonDocument.Parse(rawMessage);
				var ev = jsonDocument.RootElement.GetProperty("payload").GetProperty("event");
				if (!TryGetString(ev, "broadcaster_user_id", out var channelId))
				{
					return;
				}

				OnStreamDown?.Invoke(channelId, new StreamDown(ToLegacyServerTimeRaw(DateTimeOffset.UtcNow)));
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to handle stream.offline");
			}
		}

		private void HandleCommercial(string rawMessage)
		{
			try
			{
				using var jsonDocument = JsonDocument.Parse(rawMessage);
				var ev = jsonDocument.RootElement.GetProperty("payload").GetProperty("event");
				if (!TryGetString(ev, "broadcaster_user_id", out var channelId))
				{
					return;
				}

				var startedAt = TryGetDateTime(ev, "started_at") ?? DateTimeOffset.UtcNow;
				OnCommercial?.Invoke(channelId, new Commercial(ToLegacyServerTimeRaw(startedAt), TryGetUInt(ev, "duration_seconds")));
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to handle channel.ad_break.begin");
			}
		}

		private void HandleFollow(string rawMessage)
		{
			try
			{
				using var jsonDocument = JsonDocument.Parse(rawMessage);
				var ev = jsonDocument.RootElement.GetProperty("payload").GetProperty("event");
				if (!TryGetString(ev, "broadcaster_user_id", out var channelId) ||
				    !TryGetString(ev, "user_id", out var userId) ||
				    !TryGetString(ev, "user_login", out var userLogin) ||
				    !TryGetString(ev, "user_name", out var userName))
				{
					return;
				}

				OnFollow?.Invoke(channelId, new Follow(userId, userLogin, userName));
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to handle channel.follow");
			}
		}

		private void HandlePoll(string rawMessage)
		{
			try
			{
				using var jsonDocument = JsonDocument.Parse(rawMessage);
				var ev = jsonDocument.RootElement.GetProperty("payload").GetProperty("event");
				if (!TryGetString(ev, "broadcaster_user_id", out var channelId))
				{
					return;
				}

				var choices = new List<PollChoice>();
				uint totalVoters = 0;
				if (ev.TryGetProperty("choices", out var choicesElement) && choicesElement.ValueKind == JsonValueKind.Array)
				{
					foreach (var choice in choicesElement.EnumerateArray())
					{
						var bitsVotes = TryGetUInt(choice, "bits_votes");
						var channelPointsVotes = TryGetUInt(choice, "channel_points_votes");
						var votes = TryGetUInt(choice, "votes");
						totalVoters += votes;
						choices.Add(new PollChoice(
							TryGetString(choice, "id", out var choiceId) ? choiceId : string.Empty,
							TryGetString(choice, "title", out var choiceTitle) ? choiceTitle : string.Empty,
							new Votes(votes, bitsVotes, channelPointsVotes, votes),
							new Tokens(bitsVotes, channelPointsVotes),
							votes));
					}
				}

				var settings = new PollSettings(
					new PollSettingsEntry(false, null),
					new PollSettingsEntry(false, null),
					new PollSettingsEntry(false, null),
					new PollSettingsEntry(TryGetBool(ev, "bits_voting_enabled"), TryGetNullableUInt(ev, "bits_per_vote")),
					new PollSettingsEntry(TryGetBool(ev, "channel_points_voting_enabled"), TryGetNullableUInt(ev, "channel_points_per_vote")));

				var pollData = new PollData(
					TryGetString(ev, "id", out var pollId) ? pollId : string.Empty,
					channelId,
					channelId,
					TryGetString(ev, "title", out var pollTitle) ? pollTitle : string.Empty,
					TryGetString(ev, "started_at", out var startedAtRaw) ? startedAtRaw : string.Empty,
					TryGetString(ev, "ended_at", out var endedAtRaw) ? endedAtRaw : string.Empty,
					string.Empty,
					TryGetUInt(ev, "duration_seconds"),
					settings,
					ParsePollStatus(TryGetString(ev, "status", out var statusRaw) ? statusRaw : null),
					choices,
					new Votes(totalVoters, 0, 0, totalVoters),
					new Tokens(0, 0),
					totalVoters,
					0,
					null,
					null,
					null);

				OnPoll?.Invoke(channelId, pollData);
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to handle channel.poll.*");
			}
		}

		private void HandlePrediction(string rawMessage)
		{
			try
			{
				using var jsonDocument = JsonDocument.Parse(rawMessage);
				var ev = jsonDocument.RootElement.GetProperty("payload").GetProperty("event");
				if (!TryGetString(ev, "broadcaster_user_id", out var channelId))
				{
					return;
				}

				var outcomes = new List<Outcome>();
				if (ev.TryGetProperty("outcomes", out var outcomesElement) && outcomesElement.ValueKind == JsonValueKind.Array)
				{
					foreach (var outcome in outcomesElement.EnumerateArray())
					{
						var topPredictors = new List<TopPredictor>();
						if (outcome.TryGetProperty("top_predictors", out var topPredictorsElement) && topPredictorsElement.ValueKind == JsonValueKind.Array)
						{
							foreach (var predictor in topPredictorsElement.EnumerateArray())
							{
								var used = TryGetUInt(predictor, "channel_points_used");
								var won = TryGetUInt(predictor, "channel_points_won");
								topPredictors.Add(new TopPredictor(
									Guid.NewGuid().ToString("N"),
									TryGetString(ev, "id", out var eventIdRaw) ? eventIdRaw : string.Empty,
									TryGetString(outcome, "id", out var outcomeIdRaw) ? outcomeIdRaw : string.Empty,
									channelId,
									used,
									DateTime.UtcNow,
									DateTime.UtcNow,
									TryGetString(predictor, "user_id", out var predictorId) ? predictorId : string.Empty,
									new PredictorResult(won > 0 ? "WIN" : "UNKNOWN", won, true),
									TryGetString(predictor, "user_name", out var predictorName) ? predictorName : string.Empty));
							}
						}

						outcomes.Add(new Outcome(
							TryGetString(outcome, "id", out var outcomeId) ? outcomeId : string.Empty,
							TryGetString(outcome, "color", out var color) ? color : string.Empty,
							TryGetString(outcome, "title", out var outcomeTitle) ? outcomeTitle : string.Empty,
							TryGetUInt(outcome, "channel_points"),
							TryGetUInt(outcome, "users"),
							topPredictors,
							new PredictionBadge(string.Empty, string.Empty)));
					}
				}

				var createdBy = new PredictionUser("user", channelId, TryGetString(ev, "broadcaster_user_name", out var broadcasterName) ? broadcasterName : string.Empty, null);
				var endedAt = TryGetDateTime(ev, "ended_at")?.UtcDateTime;
				var lockedAt = TryGetDateTime(ev, "locks_at")?.UtcDateTime;
				var predictionData = new PredictionData(
					TryGetString(ev, "id", out var predictionId) ? predictionId : string.Empty,
					channelId,
					TryGetString(ev, "title", out var predictionTitle) ? predictionTitle : string.Empty,
					(TryGetDateTime(ev, "created_at") ?? DateTimeOffset.UtcNow).UtcDateTime,
					createdBy,
					endedAt,
					endedAt.HasValue ? createdBy : (PredictionUser?)null,
					lockedAt,
					lockedAt.HasValue ? createdBy : (PredictionUser?)null,
					outcomes,
					TryGetUInt(ev, "prediction_window_seconds"),
					ParsePredictionStatus(TryGetString(ev, "status", out var statusRaw) ? statusRaw : null),
					TryGetString(ev, "winning_outcome_id", out var winningOutcomeId) ? winningOutcomeId : null);

				OnPrediction?.Invoke(channelId, predictionData);
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to handle channel.prediction.*");
			}
		}

		private void HandleRewardRedeem(string rawMessage)
		{
			try
			{
				using var jsonDocument = JsonDocument.Parse(rawMessage);
				var ev = jsonDocument.RootElement.GetProperty("payload").GetProperty("event");
				if (!TryGetString(ev, "broadcaster_user_id", out var channelId) || !ev.TryGetProperty("reward", out var rewardElement))
				{
					return;
				}

				var user = new RewardUser(
					TryGetString(ev, "user_id", out var userId) ? userId : string.Empty,
					TryGetString(ev, "user_login", out var userLogin) ? userLogin : string.Empty,
					TryGetString(ev, "user_name", out var userName) ? userName : string.Empty);

				var reward = new Reward(
					TryGetString(rewardElement, "id", out var rewardId) ? rewardId : string.Empty,
					channelId,
					TryGetString(rewardElement, "title", out var rewardTitle) ? rewardTitle : string.Empty,
					TryGetString(rewardElement, "prompt", out var prompt) ? prompt : string.Empty,
					(int)TryGetUInt(rewardElement, "cost"),
					false,
					false,
					new object(),
					new DefaultImage(string.Empty, string.Empty, string.Empty),
					"#000000",
					true,
					false,
					true,
					new MaxPerStream(false, 0),
					false,
					string.Empty,
					DateTimeOffset.UtcNow,
					new MaxPerUserPerStream(false, 0),
					new GlobalCooldown(false, 0),
					null);

				var data = new RewardRedeemedData(
					TryGetString(ev, "id", out var redemptionId) ? redemptionId : string.Empty,
					user,
					channelId,
					TryGetString(ev, "redeemed_at", out var redeemedAtRaw) ? redeemedAtRaw : DateTimeOffset.UtcNow.ToString("O"),
					reward,
					TryGetString(ev, "status", out var status) ? status : "fulfilled");

				OnRewardRedeemed?.Invoke(channelId, data);
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to handle channel.channel_points_custom_reward_redemption.add");
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

			var loggedInUserId = _loggedInUser.Value.UserId;
			var subscriptionRequests = new List<(string type, string version, Dictionary<string, string> condition)>
			{
				(SUB_TYPE_CHANNEL_CHAT_MESSAGE, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId }, { "user_id", loggedInUserId } }),
				(SUB_TYPE_CHANNEL_CHAT_NOTIFICATION, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId }, { "user_id", loggedInUserId } }),
				(SUB_TYPE_CHANNEL_CHAT_MESSAGE_DELETE, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId }, { "user_id", loggedInUserId } }),
				(SUB_TYPE_CHANNEL_CHAT_CLEAR, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId }, { "user_id", loggedInUserId } }),
				(SUB_TYPE_CHANNEL_CHAT_SETTINGS_UPDATE, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId }, { "user_id", loggedInUserId } }),
				(SUB_TYPE_STREAM_ONLINE, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId } }),
				(SUB_TYPE_STREAM_OFFLINE, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId } }),
				(SUB_TYPE_CHANNEL_AD_BREAK_BEGIN, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId } }),
				(SUB_TYPE_CHANNEL_FOLLOW, EVENTSUB_VERSION_FOLLOW, new Dictionary<string, string> { { "broadcaster_user_id", channelId }, { "moderator_user_id", loggedInUserId } }),
				(SUB_TYPE_CHANNEL_POLL_BEGIN, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId } }),
				(SUB_TYPE_CHANNEL_POLL_PROGRESS, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId } }),
				(SUB_TYPE_CHANNEL_POLL_END, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId } }),
				(SUB_TYPE_CHANNEL_PREDICTION_BEGIN, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId } }),
				(SUB_TYPE_CHANNEL_PREDICTION_PROGRESS, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId } }),
				(SUB_TYPE_CHANNEL_PREDICTION_LOCK, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId } }),
				(SUB_TYPE_CHANNEL_PREDICTION_END, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId } }),
				(SUB_TYPE_CHANNEL_REWARD_REDEEM, EVENTSUB_VERSION, new Dictionary<string, string> { { "broadcaster_user_id", channelId } })
			};

			var subscriptionIds = new List<string>();
			var allSucceeded = true;
			foreach (var request in subscriptionRequests)
			{
				var subscriptionId = await _twitchHelixApiService.CreateEventSubSubscription(request.type, request.version, request.condition, _sessionId).ConfigureAwait(false);
				if (subscriptionId != null)
				{
					subscriptionIds.Add(subscriptionId);
				}
				else
				{
					_logger.Warning("Failed to create EventSub subscription type {SubscriptionType} for channel {ChannelId}", request.type, channelId);
					allSucceeded = false;
				}
			}

			if (subscriptionIds.Count > 0 && allSucceeded)
			{
				// Only delete existing subscriptions and replace when ALL new ones were created successfully,
				// to avoid silently reducing coverage for the channel on partial failure.
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
			else if (subscriptionIds.Count == 0)
			{
				_logger.Warning("Failed to create EventSub subscriptions for channel {ChannelId}. AttemptedTypes={AttemptedTypes}", channelId, string.Join(",", subscriptionRequests.Select(x => x.type)));
			}
			else
			{
				// Partial failure: clean up the newly created subscriptions to avoid leaks.
				_logger.Warning("Partial failure creating EventSub subscriptions for channel {ChannelId}. Rolling back {Count} created subscriptions.", channelId, subscriptionIds.Count);
				foreach (var subscriptionId in subscriptionIds)
				{
					var deleted = await _twitchHelixApiService.DeleteEventSubSubscription(subscriptionId).ConfigureAwait(false);
					if (!deleted)
					{
						_logger.Warning("Failed to delete EventSub subscription {SubscriptionId} during rollback for channel {ChannelId}", subscriptionId, channelId);
					}
				}
			}
		}

		private static string ToLegacyServerTimeRaw(DateTimeOffset value)
		{
			var unixMicros = value.ToUnixTimeMilliseconds() * 1000L;
			return $"{unixMicros / 1000000}.{unixMicros % 1000000:D6}";
		}

		private static bool TryGetString(JsonElement element, string propertyName, out string value)
		{
			if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
			{
				value = property.GetString()!;
				return true;
			}

			value = string.Empty;
			return false;
		}

		private static bool TryGetBool(JsonElement element, string propertyName)
		{
			return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.True;
		}

		private static uint TryGetUInt(JsonElement element, string propertyName)
		{
			if (!element.TryGetProperty(propertyName, out var property))
			{
				return 0;
			}

			if (property.ValueKind == JsonValueKind.Number && property.TryGetUInt32(out var number))
			{
				return number;
			}

			return 0;
		}

		private static uint? TryGetNullableUInt(JsonElement element, string propertyName)
		{
			if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
			{
				return null;
			}

			if (property.ValueKind == JsonValueKind.Number && property.TryGetUInt32(out var number))
			{
				return number;
			}

			return null;
		}

		private static DateTimeOffset? TryGetDateTime(JsonElement element, string propertyName)
		{
			if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
			{
				return null;
			}

			var raw = property.GetString();
			return DateTimeOffset.TryParse(raw, out var parsed) ? parsed : null;
		}

		private static PollStatus ParsePollStatus(string? status)
		{
			return status?.ToLowerInvariant() switch
			{
				"active" => PollStatus.Active,
				"completed" => PollStatus.Completed,
				"terminated" => PollStatus.Terminated,
				"archived" => PollStatus.Archived,
				"moderated" => PollStatus.Moderated,
				_ => PollStatus.Invalid
			};
		}

		private static PredictionStatus ParsePredictionStatus(string? status)
		{
			return status?.ToLowerInvariant() switch
			{
				"active" => PredictionStatus.Active,
				"resolved" => PredictionStatus.Resolved,
				"canceled" => PredictionStatus.Cancelled,
				"cancelled" => PredictionStatus.Cancelled,
				"locked" => PredictionStatus.Locked,
				_ => PredictionStatus.Active
			};
		}

		private void TryStartViewCountPollingIfNeeded()
		{
			lock (_viewCountPollingStateLock)
			{
				if (_viewCountPollingCancellationTokenSource != null || _viewCountCallbackRegistrations.IsEmpty)
				{
					return;
				}

				if (_loggedInUser == null || !_activeStateManager.GetState(PlatformType.Twitch))
				{
					return;
				}

				_viewCountPollingCancellationTokenSource = new CancellationTokenSource();
				_viewCountPollingTask = Task.Run(() => RunViewCountPollingLoop(_viewCountPollingCancellationTokenSource.Token));
			}
		}

		private async Task StopViewCountPollingIfRunning()
		{
			CancellationTokenSource? cancellationTokenSource;
			Task? pollingTask;

			lock (_viewCountPollingStateLock)
			{
				cancellationTokenSource = _viewCountPollingCancellationTokenSource;
				pollingTask = _viewCountPollingTask;
				_viewCountPollingCancellationTokenSource = null;
				_viewCountPollingTask = null;
			}

			if (cancellationTokenSource == null)
			{
				return;
			}

			cancellationTokenSource.Cancel();
			try
			{
				if (pollingTask != null)
				{
					await pollingTask.ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException)
			{
				// Expected while stopping
			}
			finally
			{
				cancellationTokenSource.Dispose();
			}
		}

		private async Task RunViewCountPollingLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					await PollViewCountsOnce(cancellationToken).ConfigureAwait(false);
					await Task.Delay(VIEW_COUNT_POLL_INTERVAL, cancellationToken).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception ex)
				{
					_logger.Warning(ex, "Failed to poll Twitch viewer counts");
					await Task.Delay(VIEW_COUNT_POLL_INTERVAL, cancellationToken).ConfigureAwait(false);
				}
			}
		}

		private async Task PollViewCountsOnce(CancellationToken cancellationToken)
		{
			if (_viewCountCallbackRegistrations.IsEmpty)
			{
				return;
			}

			var channelIds = _channelSubscriptionIds.Keys.ToArray();
			if (channelIds.Length == 0)
			{
				return;
			}

			var serverTimeRaw = ToLegacyServerTimeRaw(DateTimeOffset.UtcNow);
			for (var offset = 0; offset < channelIds.Length; offset += HELIX_USER_IDS_PER_REQUEST_LIMIT)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var chunkSize = Math.Min(HELIX_USER_IDS_PER_REQUEST_LIMIT, channelIds.Length - offset);
				var userIds = new string[chunkSize];
				Array.Copy(channelIds, offset, userIds, 0, chunkSize);

				var streamResponse = await _twitchHelixApiService.GetStreams(userIds: userIds, cancellationToken: cancellationToken).ConfigureAwait(false);
				if (streamResponse == null)
				{
					continue;
				}

				foreach (var stream in streamResponse.Value.Data)
				{
					var viewCountUpdate = new ViewCountUpdate(serverTimeRaw, stream.ViewerCount);
					foreach (var callback in _viewCountCallbackRegistrations.Keys)
					{
						callback(stream.UserId, viewCountUpdate);
					}
				}
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
