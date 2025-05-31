using System.Collections.Generic;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Models.EventArgs;
using CatCore.Models.Shared;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
    internal sealed class TwitchEventSubServiceManager 
    {
        private readonly ILogger _logger;
        private readonly ThreadSafeRandomFactory _randomFactory;
        private readonly IKittenPlatformActiveStateManager _activeStateManager;
        private readonly ITwitchAuthService _twitchAuthService;
        private readonly ITwitchChannelManagementService _twitchChannelManagementService;

        private readonly Dictionary<string, TwitchEventSubWebSocketAgent> _activeEventSubConnections;

        public TwitchEventSubServiceManager(
            ILogger logger,
            ThreadSafeRandomFactory randomFactory,
            IKittenPlatformActiveStateManager activeStateManager,
            ITwitchAuthService twitchAuthService,
            ITwitchChannelManagementService twitchChannelManagementService)
        {
            _logger = logger;
            _randomFactory = randomFactory;
            _activeStateManager = activeStateManager;
            _twitchAuthService = twitchAuthService;
            _twitchChannelManagementService = twitchChannelManagementService;

            _twitchAuthService.OnCredentialsChanged += TwitchAuthServiceOnOnCredentialsChanged;
            _twitchChannelManagementService.ChannelsUpdated += TwitchChannelManagementServiceOnChannelsUpdated;

            _activeEventSubConnections = new Dictionary<string, TwitchEventSubWebSocketAgent>();
        }

        public Task Start()
        {
            foreach (var channelId in _twitchChannelManagementService.GetAllActiveChannelIds())
            {
                CreateEventSubAgent(channelId);
            }
            // EventSubではトピック登録の仕組みが異なる場合、ここで適宜初期化処理を追加

			return Task.CompletedTask;
		}

		public async Task Stop()
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

                CreateEventSubAgent(channelId);
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
                    CreateEventSubAgent(enabledChannel.Key);
                }
            }
        }

        private TwitchEventSubWebSocketAgent CreateEventSubAgent(string channelId)
        {
            var agent = new TwitchEventSubWebSocketAgent(
                _logger,
                //_randomFactory.CreateNewRandom(),
                _twitchAuthService,
                _activeStateManager,
                channelId
            );

            // 必要に応じてイベントハンドラを登録
            // agent.OnEventXxx += NotifyOnEventXxx;

            return _activeEventSubConnections[channelId] = agent;
        }

        private async Task DestroyEventSubAgent(string channelId, TwitchEventSubWebSocketAgent agent)
        {
            // 必要に応じてイベントハンドラを解除
            // agent.OnEventXxx -= NotifyOnEventXxx;

            await agent.DisposeAsync().ConfigureAwait(false);

            _activeEventSubConnections.Remove(channelId);
        }
    }
}