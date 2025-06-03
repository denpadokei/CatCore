using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Models.EventArgs;
using CatCore.Models.Shared;
using CatCore.Models.Twitch.EventSub.Responses;
using CatCore.Models.Twitch.EventSub.Responses.VideoPlayback;
using CatCore.Models.Twitch.PubSub;
using CatCore.Models.Twitch.PubSub.Responses.VideoPlayback;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal sealed partial class TwitchEventSubServiceManager : ITwitchEventSubServiceManager
	{
		private readonly ILogger _logger;
		private readonly IKittenPlatformActiveStateManager _activeStateManager;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly ITwitchChannelManagementService _twitchChannelManagementService;

		private readonly Dictionary<string, TwitchEventSubWebSocketAgent> _activeEventSubConnections;
		private readonly HashSet<string> _topicsWithRegisteredCallbacks = new();
		private readonly SemaphoreSlim _topicRegistrationLocker = new SemaphoreSlim(1, 1);

		public TwitchEventSubServiceManager(
			ILogger logger,
			IKittenPlatformActiveStateManager activeStateManager,
			ITwitchAuthService twitchAuthService,
			ITwitchChannelManagementService twitchChannelManagementService)
		{
			_logger = logger;
			_activeStateManager = activeStateManager;
			_twitchAuthService = twitchAuthService;
			_twitchChannelManagementService = twitchChannelManagementService;

			_twitchAuthService.OnCredentialsChanged += TwitchAuthServiceOnOnCredentialsChanged;
			_twitchChannelManagementService.ChannelsUpdated += TwitchChannelManagementServiceOnChannelsUpdated;

			_activeEventSubConnections = new Dictionary<string, TwitchEventSubWebSocketAgent>();
		}

		private void RegisterTopicWhenNeeded(string topic)
		{
			using var _ = Synchronization.Lock(_topicRegistrationLocker);
			if (!_topicsWithRegisteredCallbacks.Add(topic))
			{
				_logger.Warning("Topic was already requested by previous callbacks");
				return;
			}
			SendListenRequestToAgentsInternal(topic);
		}

		private void SendListenRequestToAgentsInternal(string topic)
		{
			var selfUserId = _twitchAuthService.FetchLoggedInUserInfo()?.UserId;
			if (CanRegisterTopicOnAllChannels(topic))
			{
				foreach (var twitchPubSubServiceExperimentalAgent in _activeEventSubConnections)
				{
					twitchPubSubServiceExperimentalAgent.Value.StartAsync().ConfigureAwait(false);
				}
			}
			else if (selfUserId != null && _activeEventSubConnections.TryGetValue(selfUserId, out var selfPubSubServiceExperimentalAgent))
			{
				selfPubSubServiceExperimentalAgent.StartAsync().ConfigureAwait(false);
			}
		}

		async Task ITwitchEventSubServiceManager.Start()
		{
			foreach (var channelId in _twitchChannelManagementService.GetAllActiveChannelIds())
			{
				CreateEventSubAgent(channelId);
			}
			using var _ = await Synchronization.LockAsync(_topicRegistrationLocker);
			foreach (var connect in _activeEventSubConnections.Values)
			{
				var __ = connect.StartAsync().ConfigureAwait(false);
			}
		}

		async Task ITwitchEventSubServiceManager.Stop()
		{
			foreach (var kvp in _activeEventSubConnections)
			{
				await DestroyEventSubAgent(kvp.Key, kvp.Value).ConfigureAwait(false);
			}
		}

		private void TwitchAuthServiceOnOnCredentialsChanged()
		{
			if (!_twitchAuthService.HasTokens || !_activeStateManager.GetState(PlatformType.Twitch))
			{
				return;
			}

			foreach (var channelId in _twitchChannelManagementService.GetAllActiveChannelIds())
			{
				if (_activeEventSubConnections.ContainsKey(channelId))
				{
					continue;
				}

				_ = CreateEventSubAgent(channelId);
			}
		}

		private async void TwitchChannelManagementServiceOnChannelsUpdated(object sender, TwitchChannelsUpdatedEventArgs args)
		{
			if (_activeStateManager.GetState(PlatformType.Twitch))
			{
				foreach (var disabledChannel in args.DisabledChannels)
				{
					if (_activeEventSubConnections.TryGetValue(disabledChannel.Key, out var agent))
					{
						await DestroyEventSubAgent(disabledChannel.Key, agent).ConfigureAwait(false);
					}
				}

				foreach (var enabledChannel in args.EnabledChannels)
				{
					_ = CreateEventSubAgent(enabledChannel.Key);
				}
			}
		}

		private TwitchEventSubWebSocketAgent CreateEventSubAgent(string channelId)
		{
			var agent = new TwitchEventSubWebSocketAgent(
				_logger,
				_twitchAuthService,
				_activeStateManager,
				channelId
			);

			// 必要に応じてイベントハンドラを登録
			agent.OnStreamOnline += NotifyOnStreamUp;
			agent.OnStreamOffline += NotifyOnStreamDown;
			agent.OnChannelFollow += NotifyOnFollow;
			agent.OnChannelPointsRedeem += NotifyOnChannelPointsRedeem;
			agent.OnPredictionBegin += NotifyOnPredictionBegin;

			// --- 追加: チャット・シャウトアウト関連イベント ---
			agent.OnChatMessage += NotifyOnChatMessage;
			agent.OnChatMessageDelete += NotifyOnChatMessageDelete;
			agent.OnShoutoutCreate += NotifyOnShoutoutCreate;
			agent.OnShoutoutReceive += NotifyOnShoutoutReceive;

			return _activeEventSubConnections[channelId] = agent;
		}

		private async Task DestroyEventSubAgent(string channelId, TwitchEventSubWebSocketAgent agent)
		{
			// 必要に応じてイベントハンドラを解除
			agent.OnStreamOnline -= NotifyOnStreamUp;
			agent.OnStreamOffline -= NotifyOnStreamDown;
			agent.OnChannelFollow -= NotifyOnFollow;
			agent.OnChannelPointsRedeem -= NotifyOnChannelPointsRedeem;
			agent.OnPredictionBegin -= NotifyOnPredictionBegin;

			// --- 追加: チャット・シャウトアウト関連イベント ---
			agent.OnChatMessage -= NotifyOnChatMessage;
			agent.OnChatMessageDelete -= NotifyOnChatMessageDelete;
			agent.OnShoutoutCreate -= NotifyOnShoutoutCreate;
			agent.OnShoutoutReceive -= NotifyOnShoutoutReceive;

			await agent.DisposeAsync().ConfigureAwait(false);

			_ = _activeEventSubConnections.Remove(channelId);
		}
	}
}