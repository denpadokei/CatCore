using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Models.EventArgs;
using CatCore.Models.Shared;
using CatCore.Models.Twitch;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using CatCore.Services.Twitch.Media;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal sealed class TwitchIrcService : ITwitchIrcService
	{
		private const string TWITCH_IRC_ENDPOINT = "wss://irc-ws.chat.twitch.tv:443";

		/// <remark>
		/// According to the official documentation, the rate limiting window interval is 30 seconds.
		/// However, due to delays in the connection/Twitch servers and this library being too precise time-wise,
		/// it might result in going over the rate limit again when it should have been reset.
		/// Resulting in a global temporary chat ban of 30 minutes, hence why we pick an internal time window of 32 seconds.
		/// </remark>
		private const long MESSAGE_SENDING_TIME_WINDOW_TICKS = 32 * TimeSpan.TicksPerSecond;

		private readonly ILogger _logger;
		private readonly IKittenWebSocketProvider _kittenWebSocketProvider;
		private readonly IKittenPlatformActiveStateManager _activeStateManager;
		private readonly IKittenSettingsService _settingsService;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly ITwitchChannelManagementService _twitchChannelManagementService;
		private readonly ITwitchRoomStateTrackerService _roomStateTrackerService;
		private readonly ITwitchUserStateTrackerService _userStateTrackerService;
		private readonly TwitchMediaDataProvider _twitchMediaDataProvider;

		private readonly char[] _ircMessageSeparator = { '\r', '\n' };

		private readonly Dictionary<string, string> _channelNameToChannelIdDictionary;

		private readonly ConcurrentQueue<(string channelId, string message)> _messageQueue;
		private readonly ConcurrentDictionary<string, long> _forcedSendChannelMessageSendDelays;
		private readonly List<long> _messageSendTimestamps;

		private readonly SemaphoreSlim _workerCanSleepSemaphoreSlim = new(1, 1);
		private readonly SemaphoreSlim _workerSemaphoreSlim = new(0, 1);

		private CancellationTokenSource? _messageQueueProcessorCancellationTokenSource;

		public TwitchIrcService(ILogger logger, IKittenWebSocketProvider kittenWebSocketProvider, IKittenPlatformActiveStateManager activeStateManager, IKittenSettingsService settingsService,
			ITwitchAuthService twitchAuthService, ITwitchChannelManagementService twitchChannelManagementService, ITwitchRoomStateTrackerService roomStateTrackerService,
			ITwitchUserStateTrackerService userStateTrackerService, TwitchMediaDataProvider twitchMediaDataProvider)
		{
			_logger = logger;
			_kittenWebSocketProvider = kittenWebSocketProvider;
			_activeStateManager = activeStateManager;
			_settingsService = settingsService;
			_twitchAuthService = twitchAuthService;
			_twitchChannelManagementService = twitchChannelManagementService;
			_roomStateTrackerService = roomStateTrackerService;
			_userStateTrackerService = userStateTrackerService;
			_twitchMediaDataProvider = twitchMediaDataProvider;

			_twitchAuthService.OnCredentialsChanged += TwitchAuthServiceOnOnCredentialsChanged;
			_twitchChannelManagementService.ChannelsUpdated += TwitchChannelManagementServiceOnChannelsUpdated;

			_channelNameToChannelIdDictionary = new Dictionary<string, string>();

			_messageQueue = new ConcurrentQueue<(string channelId, string message)>();
			_forcedSendChannelMessageSendDelays = new ConcurrentDictionary<string, long>();
			_messageSendTimestamps = new List<long>();
		}

		public event Action? OnChatConnected;
		public event Action<TwitchChannel>? OnJoinChannel;
		public event Action<TwitchChannel>? OnLeaveChannel;
		public event Action<TwitchChannel>? OnRoomStateChanged;
		public event Action<TwitchMessage>? OnMessageReceived;

		public void SendMessage(TwitchChannel channel, string message)
		{
			_workerCanSleepSemaphoreSlim.Wait();
			_messageQueue.Enqueue((channel.Id, $"@id={Guid.NewGuid().ToString()};{IrcMessageTags.ROOM_ID}={channel.Id} {IrcCommands.PRIVMSG} #{channel.Name} :{message}"));
			_ = _workerCanSleepSemaphoreSlim.Release();

			// Trigger re-activation of worker thread
			if (_workerSemaphoreSlim.CurrentCount == 0)
			{
				_ = _workerSemaphoreSlim.Release();
			}
		}

		async Task ITwitchIrcService.Start()
		{
			if (!_twitchAuthService.HasTokens || !_twitchAuthService.LoggedInUser.HasValue)
			{
				return;
			}

			if (!(_twitchAuthService.TokenIsValid || await _twitchAuthService.RefreshTokens().ConfigureAwait(false)))
			{
				return;
			}

			_kittenWebSocketProvider.ConnectHappened -= ConnectHappenedHandler;
			_kittenWebSocketProvider.ConnectHappened += ConnectHappenedHandler;

			_kittenWebSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
			_kittenWebSocketProvider.DisconnectHappened += DisconnectHappenedHandler;

			_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;
			_kittenWebSocketProvider.MessageReceived += MessageReceivedHandler;

			await _kittenWebSocketProvider.Connect(TWITCH_IRC_ENDPOINT).ConfigureAwait(false);
		}

		async Task ITwitchIrcService.Stop()
		{
			await _kittenWebSocketProvider.Disconnect("Requested by service manager").ConfigureAwait(false);

			_kittenWebSocketProvider.ConnectHappened -= ConnectHappenedHandler;
			_kittenWebSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
			_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;
		}

		private async void TwitchAuthServiceOnOnCredentialsChanged()
		{
			if (_twitchAuthService.HasTokens)
			{
				if (_activeStateManager.GetState(PlatformType.Twitch))
				{
					await ((ITwitchIrcService) this).Start().ConfigureAwait(false);
				}
			}
			else
			{
				await ((ITwitchIrcService) this).Stop().ConfigureAwait(false);
			}
		}

		private void TwitchChannelManagementServiceOnChannelsUpdated(object sender, TwitchChannelsUpdatedEventArgs e)
		{
			if (_activeStateManager.GetState(PlatformType.Twitch))
			{
				foreach (var disabledChannel in e.DisabledChannels)
				{
					_kittenWebSocketProvider.SendMessage($"PART #{disabledChannel.Value}");
				}

				foreach (var enabledChannel in e.EnabledChannels)
				{
					_channelNameToChannelIdDictionary[enabledChannel.Value] = enabledChannel.Key;
					_kittenWebSocketProvider.SendMessage($"JOIN #{enabledChannel.Value}");
				}
			}
		}

		private void ConnectHappenedHandler()
		{
			_kittenWebSocketProvider.SendMessage("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership");

			_kittenWebSocketProvider.SendMessage($"PASS oauth:{_twitchAuthService.AccessToken}");
			_kittenWebSocketProvider.SendMessage($"NICK {_twitchAuthService.LoggedInUser?.LoginName ?? "."}");
		}

		private void DisconnectHappenedHandler()
		{
			_messageQueueProcessorCancellationTokenSource?.Cancel();
			_messageQueueProcessorCancellationTokenSource = null;

			_channelNameToChannelIdDictionary.Clear();
		}

		private void MessageReceivedHandler(string message)
		{
			MessageReceivedHandlerInternal(message);
		}

		// TODO: Investigate possibility to split a message string into ReadOnlySpans<char> or ReadOnlyMemory<char> types instead of strings
		// This would prevents unnecessary heap allocations which might in turn improve the throughput
		// TODO: Remove debug stopwatches when said optimisation has been done
		private void MessageReceivedHandlerInternal(string rawMessage, bool sendBySelf = false)
		{
#if !RELEASE
			var stopwatch = new System.Diagnostics.Stopwatch();
			stopwatch.Start();
#endif

			var messages = rawMessage.Split(_ircMessageSeparator, StringSplitOptions.RemoveEmptyEntries);
			foreach (var messageInternal in messages)
			{
				// Handle IRC messages here
				IrcExtensions.ParseIrcMessage(messageInternal, out var tags, out var prefix, out var commandType, out var channelName, out var message);
#if DEBUG
				_logger.Verbose("{MessageTemplate}", messageInternal);

				_logger.Verbose("Tags count: {Tags}", tags?.Count.ToString() ?? "N/A");
				_logger.Verbose("Prefix: {Prefix}", prefix ?? "N/A");
				_logger.Verbose("CommandType: {CommandType}", commandType);
				_logger.Verbose("ChannelName: {ChannelName}", channelName ?? "N/A");
				_logger.Verbose("Message: {Message}", message ?? "N/A");
				_logger.Verbose("");
#endif

				HandleParsedIrcMessage(ref tags, ref prefix, ref commandType, ref channelName, ref message, sendBySelf);
			}

#if !RELEASE
			stopwatch.Stop();
			_logger.Information("Handling of {MessageCount} took {ElapsedTime} ticks", messages.Length, stopwatch.ElapsedTicks);
#endif
		}

		// ReSharper disable once CognitiveComplexity
		// ReSharper disable once CyclomaticComplexity
		private void HandleParsedIrcMessage(ref ReadOnlyDictionary<string, string>? messageMeta, ref string? prefix, ref string commandType, ref string? channelName, ref string? message,
			bool wasSendByLibrary)
		{
			// Command official documentation: https://datatracker.ietf.org/doc/html/rfc1459 and https://datatracker.ietf.org/doc/html/rfc2812
			// Command Twitch documentation: https://dev.twitch.tv/docs/irc/commands
			// CommandMeta documentation: https://dev.twitch.tv/docs/irc/tags

			switch (commandType)
			{
				case IrcCommands.PING:
					_kittenWebSocketProvider.SendMessage($"{IrcCommands.PONG} :{message!}");
					break;
				case IrcCommands.RPL_ENDOFMOTD:
					OnChatConnected?.Invoke();
					foreach (var channel in _twitchChannelManagementService.GetAllActiveChannelsAsDictionary())
					{
						_channelNameToChannelIdDictionary[channel.Value] = channel.Key;
						_kittenWebSocketProvider.SendMessage($"JOIN #{channel.Value}");
					}

					_messageQueueProcessorCancellationTokenSource?.Cancel();
					_messageQueueProcessorCancellationTokenSource = new CancellationTokenSource();

					_ = Task.Run(() => ProcessQueuedMessage(_messageQueueProcessorCancellationTokenSource.Token), _messageQueueProcessorCancellationTokenSource.Token);

					break;
				case IrcCommands.NOTICE:
					// MessageId for NOTICE documentation: https://dev.twitch.tv/docs/irc/msg-id
					switch (message)
					{
						case "Login authentication failed":
							_logger.Warning("Login failed. Error {ErrorMessage}", message);
							_ = _kittenWebSocketProvider.Disconnect(message).ConfigureAwait(false);
							break;
					}

					goto case IrcCommands.PRIVMSG;
				case TwitchIrcCommands.USERNOTICE:
				case IrcCommands.PRIVMSG:
					HandlePrivMessage(ref messageMeta, ref prefix, ref commandType, ref channelName, ref message, wasSendByLibrary);

					break;
				case IrcCommands.JOIN:
				{
					_ = prefix.ParsePrefix(out _, out _, out var username, out _);
					if (_twitchAuthService.LoggedInUser?.LoginName == username)
					{
						OnJoinChannel?.Invoke(new TwitchChannel(this, _channelNameToChannelIdDictionary[channelName!], channelName!));
					}

					break;
				}
				case IrcCommands.PART:
				{
					_ = prefix.ParsePrefix(out _, out _, out var username, out _);
					if (_twitchAuthService.LoggedInUser?.LoginName == username)
					{
						var channelId = _channelNameToChannelIdDictionary[channelName!];
						OnLeaveChannel?.Invoke(new TwitchChannel(this, channelId, channelName!));

						_ = _roomStateTrackerService.UpdateRoomState(channelName!, null);
						_userStateTrackerService.UpdateUserState(channelId, null);

						_channelNameToChannelIdDictionary.Remove(channelName!);
					}

					break;
				}
				case TwitchIrcCommands.ROOMSTATE:
				{
					_ = _roomStateTrackerService.UpdateRoomState(channelName!, messageMeta);

					OnRoomStateChanged?.Invoke(new TwitchChannel(this, _channelNameToChannelIdDictionary[channelName!], channelName!));

					break;
				}
				case TwitchIrcCommands.USERSTATE:
					_userStateTrackerService.UpdateUserState(_channelNameToChannelIdDictionary[channelName!], messageMeta);

					break;
				case TwitchIrcCommands.GLOBALUSERSTATE:
					_userStateTrackerService.UpdateGlobalUserState(messageMeta);

					break;
				case TwitchIrcCommands.CLEARCHAT:
					break;
				case TwitchIrcCommands.CLEARMSG:
					break;
				case TwitchIrcCommands.RECONNECT:
					_ = ((ITwitchIrcService) this).Start().ConfigureAwait(false);
					break;
				case TwitchIrcCommands.HOSTTARGET:
					// NOP
					// Consumers are already notified of this through a NOTICE message when the logged in user hosts/stops hosting a channel
					// Also doesn't cover when the user is the one getting hosted by another channel
					break;
			}
		}

		// ReSharper disable once CognitiveComplexity
		// ReSharper disable once CyclomaticComplexity
		private void HandlePrivMessage(ref ReadOnlyDictionary<string, string>? messageMeta, ref string? prefix, ref string commandType, ref string? channelName, ref string? message,
			bool wasSendByLibrary)
		{
			// Determine channelId
			var channelId = messageMeta != null && messageMeta.TryGetValue(IrcMessageTags.ROOM_ID, out var roomId)
				? roomId
				: _channelNameToChannelIdDictionary[channelName!];

			// Create Channel object
			var channel = new TwitchChannel(this, channelId, channelName!);

			var globalUserState = _userStateTrackerService.GlobalUserState;
			var userState = _userStateTrackerService.GetUserState(channelId);

			var selfDisplayName = globalUserState?.DisplayName ?? _twitchAuthService.LoggedInUser?.LoginName;

			_ = prefix.ParsePrefix(out var isServer, out _, out var username, out var hostname);

			string messageId;
			uint bits;
			TwitchUser twitchUser;
			if (wasSendByLibrary)
			{
				var badgeEntries = new List<IChatBadge>();
				if (globalUserState?.Badges != null)
				{
					foreach (var badgeIdentifier in globalUserState.Badges.Split(','))
					{
						if (_twitchMediaDataProvider.TryGetBadge(badgeIdentifier, channelId, out var badge))
						{
							badgeEntries.Add(badge!);
						}
					}
				}

				if (userState?.Badges != null)
				{
					foreach (var badgeIdentifier in userState.Badges.Split(','))
					{
						if (_twitchMediaDataProvider.TryGetBadge(badgeIdentifier, channelId, out var badge))
						{
							badgeEntries.Add(badge!);
						}
					}
				}

				twitchUser = new TwitchUser(globalUserState?.UserId ?? _twitchAuthService.LoggedInUser?.UserId ?? string.Empty,
					_twitchAuthService.LoggedInUser?.LoginName ?? string.Empty,
					selfDisplayName ?? string.Empty,
					globalUserState?.Color ?? "#ffffff",
					userState?.IsModerator ?? false,
					userState?.IsBroadcaster ?? false,
					userState?.IsSubscriber ?? false,
					userState?.IsTurbo ?? false,
					userState?.IsVip ?? false,
					badgeEntries.AsReadOnly());

				messageId = messageMeta![IrcMessageTags.ID]!;
				bits = 0;
			}
			else
			{
				string userId;
				string displayName;
				string color;

				bool isModerator;
				bool isBroadcaster;
				bool isSubscriber;
				bool isTurbo;
				bool isVip;

				List<IChatBadge> badgeEntries;

				if (messageMeta != null)
				{
					if (!messageMeta.TryGetValue(IrcMessageTags.USER_ID, out userId))
					{
						userId = string.Empty;
					}

					if (!messageMeta.TryGetValue(IrcMessageTags.DISPLAY_NAME, out displayName))
					{
						displayName = (bool) isServer! ? hostname! : username!;
					}

					if (!messageMeta.TryGetValue(IrcMessageTags.COLOR, out color))
					{
						color = "#ffffff";
					}

					if (messageMeta.TryGetValue(IrcMessageTags.BADGES, out var badgesString))
					{
						isModerator = badgesString.Contains("moderator/");
						isBroadcaster = badgesString.Contains("broadcaster/");
						isSubscriber = (badgesString.Contains("subscriber/")) || (badgesString.Contains("founder/"));
						isTurbo = badgesString.Contains("turbo/");
						isVip = badgesString.Contains("vip/");

						badgeEntries = new List<IChatBadge>();
						foreach (var badgeIdentifier in badgesString.Split(','))
						{
							if (_twitchMediaDataProvider.TryGetBadge(badgeIdentifier, channelId, out var badge))
							{
								badgeEntries.Add(badge!);
							}
						}
					}
					else
					{
						isModerator = false;
						isBroadcaster = false;
						isSubscriber = false;
						isTurbo = false;
						isVip = false;

						badgeEntries = new List<IChatBadge>(0);
					}

					messageId = messageMeta.TryGetValue(IrcMessageTags.ID, out var msgId) ? msgId : Guid.NewGuid().ToString();
					bits = messageMeta.TryGetValue(IrcMessageTags.BITS, out var bitsString) ? uint.Parse(bitsString) : 0;
				}
				else
				{
					userId = string.Empty;
					displayName = (bool) isServer! ? hostname! : username!;
					color = "#ffffff";
					isModerator = false;
					isBroadcaster = false;
					isSubscriber = false;
					isTurbo = false;
					isVip = false;
					badgeEntries = new List<IChatBadge>(0);

					messageId = Guid.NewGuid().ToString();
					bits = 0;
				}

				twitchUser = new TwitchUser(userId,
					(bool) isServer! ? hostname! : username!,
					displayName,
					color,
					isModerator,
					isBroadcaster,
					isSubscriber,
					isTurbo,
					isVip,
					badgeEntries.AsReadOnly()
				);
			}

			var isActionMessage = false;
			var isMentioned = false;
			if (message != null)
			{
				if (message.StartsWith("ACTION ", StringComparison.Ordinal))
				{
					isActionMessage = true;
					message = message.AsSpan().Slice(8, message.Length - 9).ToString();
				}

				if (selfDisplayName != null)
				{
					isMentioned = message.Contains($"@{selfDisplayName}");
				}
			}
			else
			{
				message = string.Empty;
			}

			var emotes = message.Length > 0 ? ExtractEmoteInfo(message, messageMeta, channelId, bits) : new List<IChatEmote>(0);

			// TODO: Implement emoji support
			OnMessageReceived?.Invoke(new TwitchMessage(
				messageId,
				commandType is IrcCommands.NOTICE or TwitchIrcCommands.USERNOTICE,
				isActionMessage,
				isMentioned,
				message,
				twitchUser,
				channel,
				emotes.AsReadOnly(),
				messageMeta,
				commandType,
				bits
			));
		}

		// TODO: Look into moving this logic into its own class
		private List<IChatEmote> ExtractEmoteInfo(string message, IReadOnlyDictionary<string, string>? messageMeta, string channelId, uint bits)
		{
			var emotes = new List<IChatEmote>();

			var twitchConfig = _settingsService.Config.TwitchConfig;
			if (twitchConfig.ParseTwitchEmotes && messageMeta != null)
			{
				ExtractTwitchEmotes(emotes, message, messageMeta);
			}

			if (_settingsService.Config.GlobalConfig.HandleEmojis)
			{
				ExtractEmojis(emotes, message);
			}

			ExtractOtherEmotes(emotes, message, channelId, twitchConfig.ParseCheermotes && bits > 0, twitchConfig.ParseBttvEmotes || twitchConfig.ParseFfzEmotes);

			return emotes;
		}

		private static void ExtractTwitchEmotes(List<IChatEmote> emotes, string message, IReadOnlyDictionary<string, string> messageMeta)
		{
			if (!messageMeta.TryGetValue(IrcMessageTags.EMOTES, out var emotesString))
			{
				return;
			}

			var emoteGroup = emotesString.Split('/');
			for (var i = 0; i < emoteGroup.Length; i++)
			{
				var emoteSet = emoteGroup[i].Split(':');
				var emoteId = emoteSet[0];

				var emotePlaceholders = emoteSet[1].Split(',');

				for (var j = 0; j < emotePlaceholders.Length; j++)
				{
					var emoteMeta = emotePlaceholders[j].Split('-');
					var emoteStart = int.Parse(emoteMeta[0]);
					var emoteEnd = int.Parse(emoteMeta[1]);

					emotes.Add(new TwitchEmote("TwitchEmote_" + emoteId, message.Substring(emoteStart, emoteEnd + 1 - emoteStart), emoteStart, emoteEnd,
						$"https://static-cdn.jtvnw.net/emoticons/v2/{emoteId}/static/dark/3.0"));
				}
			}
		}

		private static void ExtractEmojis(List<IChatEmote> emotes, string message)
		{
			for (var i = 0; i < message.Length; i++)
			{
				var foundEmojiLeaf = Twemoji.Emojis.EmojiReferenceData.LookupLeaf(message, i);
				if (foundEmojiLeaf != null)
				{
					emotes.Add(new Emoji(foundEmojiLeaf.Key, foundEmojiLeaf.Key, i, i + foundEmojiLeaf.Depth, foundEmojiLeaf.Url));
					i += foundEmojiLeaf.Depth;
				}
			}
		}

		// ReSharper disable once CognitiveComplexity
		private void ExtractOtherEmotes(List<IChatEmote> emotes, string message, string channelId, bool parseCheermotes, bool parseCustomEmotes)
		{
			if (!parseCheermotes && !parseCustomEmotes)
			{
				return;
			}

			void ExtractOtherEmotesInternal(int messageStartIndex, int messageEndIndex)
			{
				var currentWordBuilder = new StringBuilder();
				for (var i = messageStartIndex; i <= messageEndIndex; i++)
				{
					if (i == messageEndIndex || char.IsWhiteSpace(message[i]))
					{
						if (currentWordBuilder.Length <= 0)
						{
							continue;
						}

						var currentWord = currentWordBuilder.ToString();

						if (parseCustomEmotes && _twitchMediaDataProvider.TryGetThirdPartyEmote(currentWord, channelId, out var customEmote))
						{
							var startIndex = i - currentWord.Length;
							var endIndex = i - 1;

							emotes.Add(new TwitchEmote(customEmote!.Id, customEmote.Name, startIndex, endIndex, customEmote.Url, customEmote.IsAnimated));
						}
						else if (parseCheermotes && _twitchMediaDataProvider.TryGetCheermote(currentWord, channelId, out var emoteBits, out var cheermoteData))
						{
							var startIndex = i - currentWord.Length;
							var endIndex = i - 1;

							emotes.Add(new TwitchEmote(cheermoteData!.Id, cheermoteData.Name, startIndex, endIndex, cheermoteData.Url, cheermoteData.IsAnimated, emoteBits, cheermoteData.Color));
						}

						currentWordBuilder.Clear();
					}
					else
					{
						currentWordBuilder.Append(message[i]);
					}
				}
			}

			var orderedEmotesList = emotes.OrderBy(x => x.StartIndex).ToList();
			var loopStartIndex = 0;
			foreach (var referenceEmote in orderedEmotesList)
			{
				ExtractOtherEmotesInternal(loopStartIndex, referenceEmote.StartIndex - 1);

				loopStartIndex = referenceEmote.EndIndex + 2;
			}

			ExtractOtherEmotesInternal(loopStartIndex, message.Length);
		}

		// ReSharper disable once CognitiveComplexity
		private async Task ProcessQueuedMessage(CancellationToken cts)
		{
			long? GetTicksTillReset()
			{
				if (_messageQueue.IsEmpty)
				{
					return null;
				}

				var rateLimit = (int) GetRateLimit(_messageQueue.First().channelId);

				if (_messageSendTimestamps.Count < rateLimit)
				{
					return 0;
				}

				var ticksTillReset = _messageSendTimestamps[_messageSendTimestamps.Count - rateLimit] + MESSAGE_SENDING_TIME_WINDOW_TICKS - DateTime.UtcNow.Ticks;
				return ticksTillReset > 0 ? ticksTillReset : 0;
			}

			void UpdateRateLimitState()
			{
				while (_messageSendTimestamps.Count > 0 && DateTime.UtcNow.Ticks - _messageSendTimestamps.First() > MESSAGE_SENDING_TIME_WINDOW_TICKS)
				{
					_messageSendTimestamps.RemoveAt(0);
				}
			}

			bool CheckIfConsumable()
			{
				UpdateRateLimitState();

				return _messageQueue.TryPeek(out var queueEntry) && _messageSendTimestamps.Count < (int) GetRateLimit(queueEntry.channelId);
			}

			async Task HandleQueue()
			{
				while (_messageQueue.TryPeek(out var msg))
				{
					var rateLimit = GetRateLimit(msg.channelId);
					if (_messageSendTimestamps.Count >= (int) rateLimit)
					{
						_logger.Debug("Hit rate limit. Type {RateLimit}", rateLimit.ToString("G"));
						break;
					}

					if (_forcedSendChannelMessageSendDelays.TryGetValue(msg.channelId, out var ticksSinceLastChannelMessage))
					{
						var ticksTillReset = ticksSinceLastChannelMessage + (rateLimit == MessageSendingRateLimit.Relaxed ? 50 : 1250) * TimeSpan.TicksPerMillisecond - DateTime.UtcNow.Ticks;
						if (ticksTillReset > 0)
						{
							var msTillReset = (int) Math.Ceiling((double) ticksTillReset / TimeSpan.TicksPerMillisecond);
							_logger.Verbose("Delayed message sending, will send next message in {TimeTillReset}ms", msTillReset);
							await Task.Delay(msTillReset, CancellationToken.None).ConfigureAwait(false);
						}
					}

					_ = _messageQueue.TryDequeue(out msg);

					// Send message
					await _kittenWebSocketProvider.SendMessageInstant(msg.message).ConfigureAwait(false);

					// Forward to internal message-received handler
					MessageReceivedHandlerInternal(msg.message, true);

					var ticksNow = DateTime.UtcNow.Ticks;
					_messageSendTimestamps.Add(ticksNow);
					_forcedSendChannelMessageSendDelays[msg.channelId] = ticksNow;
				}
			}

			while (!cts.IsCancellationRequested)
			{
				await HandleQueue().ConfigureAwait(false);

				do
				{
					_logger.Verbose("Hibernating worker queue");

					await _workerCanSleepSemaphoreSlim.WaitAsync(CancellationToken.None).ConfigureAwait(false);
					var canConsume = !_messageQueue.IsEmpty;
					_ = _workerCanSleepSemaphoreSlim.Release();

					var remainingTicks = GetTicksTillReset();
					var autoReExecutionDelay = canConsume ? remainingTicks > 0 ? (int) Math.Ceiling((double) remainingTicks / TimeSpan.TicksPerMillisecond) : 0 : -1;
					_logger.Information("Auto re-execution delay: {AutoReExecutionDelay}ms", autoReExecutionDelay);

					_ = await Task.WhenAny(
							Task.Delay(autoReExecutionDelay, cts),
							_workerSemaphoreSlim.WaitAsync(cts))
						.ConfigureAwait(false);

					_logger.Verbose("Waking up worker queue");
				} while (!CheckIfConsumable() && !cts.IsCancellationRequested);
			}

			_logger.Warning("Stopped worker queue");
		}

		private MessageSendingRateLimit GetRateLimit(string channelId)
		{
			if (_twitchAuthService.LoggedInUser?.UserId == channelId)
			{
				return MessageSendingRateLimit.Relaxed;
			}

			var userState = _userStateTrackerService.GetUserState(channelId);
			if (userState != null && (userState.IsBroadcaster || userState.IsModerator))
			{
				return MessageSendingRateLimit.Relaxed;
			}

			return MessageSendingRateLimit.Normal;
		}
	}
}