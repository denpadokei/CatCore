using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Models.Twitch.EventSub.Responses;
using CatCore.Models.Twitch.EventSub.Responses.VideoPlayback;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal sealed class TwitchEventSubWebSocketAgent : IAsyncDisposable
	{
		private const string EVENTSUB_WS_ENDPOINT = "wss://eventsub.wss.twitch.tv/ws";
		private readonly ILogger _logger;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly IKittenPlatformActiveStateManager _activeStateManager;
		private readonly string _channelId;
		private readonly IKittenWebSocketProvider _webSocketProvider;
		private WebSocketConnection? _webSocketConnection;
		private string? _sessionId;
		private bool _sessionIdReceived;
		private readonly CancellationTokenSource _cts = new();

		public event Action<string, StreamOnlineEvent>? OnStreamOnline;
		public event Action<string, StreamOfflineEvent>? OnStreamOffline;
		public event Action<string, ChannelFollowEvent>? OnChannelFollow;
		public event Action<string, ChannelSubscribeEvent>? OnChannelSubscribe;
		public event Action<string, ChannelPointsRedeemEvent>? OnChannelPointsRedeem;
		public event Action<string, ChannelPredictionBeginEvent>? OnPredictionBegin;

		public TwitchEventSubWebSocketAgent(
			ILogger logger,
			ITwitchAuthService twitchAuthService,
			IKittenPlatformActiveStateManager activeStateManager,
			string channelId)
		{
			_logger = logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, $"{typeof(TwitchEventSubWebSocketAgent).FullName} ({channelId})");
			_twitchAuthService = twitchAuthService;
			_activeStateManager = activeStateManager;
			_channelId = channelId;
			_webSocketProvider = new KittenWebSocketProvider(_logger);
		}

		public async Task StartAsync()
		{
			if (_webSocketProvider.IsConnected)
			{
				return;
			}

			_webSocketProvider.ConnectHappened -= ConnectHappenedHandler;
			_webSocketProvider.ConnectHappened += ConnectHappenedHandler;

			_webSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
			_webSocketProvider.DisconnectHappened += DisconnectHappenedHandler;

			_webSocketProvider.MessageReceived -= MessageReceivedHandler;
			_webSocketProvider.MessageReceived += MessageReceivedHandler;

			_sessionIdReceived = false;
			_sessionId = null;

			await _webSocketProvider.Connect(EVENTSUB_WS_ENDPOINT).ConfigureAwait(false);
		}

		private Task ConnectHappenedHandler(WebSocketConnection webSocketConnection)
		{
			_webSocketConnection = webSocketConnection;
			_logger.Information("Twitch EventSub WebSocket connected.");
			// 最初のメッセージで session_id を取得するため、MessageReceivedHandler で処理
			return Task.CompletedTask;
		}

		private async Task DisconnectHappenedHandler()
		{
			_webSocketConnection = null;
			_logger.Warning("Twitch EventSub WebSocket disconnected.");
			await Task.CompletedTask;
		}

		private async Task MessageReceivedHandler(WebSocketConnection webSocketConnection, string receivedMessage)
		{
			try
			{
				using var doc = JsonDocument.Parse(receivedMessage);
				var root = doc.RootElement;

				// 最初のメッセージで session_id を取得
				if (!_sessionIdReceived && root.TryGetProperty("payload", out var initialPayload) &&
					initialPayload.TryGetProperty("session", out var session) &&
					session.TryGetProperty("id", out var sessionIdElement))
				{
					_sessionId = sessionIdElement.GetString();
					_sessionIdReceived = true;
					_logger.Information($"Twitch EventSub WebSocket session started: {_sessionId}");

					await RegisterEventSubSubscriptions().ConfigureAwait(false);
					return;
				}

				// 通常のEventSub通知
				if (root.TryGetProperty("metadata", out var metadata))
				{
					var messageType = metadata.GetProperty("message_type").GetString();
					if (messageType == "notification")
					{
						var eventPayload = root.GetProperty("payload");
						var subscription = eventPayload.GetProperty("subscription");
						var eventType = subscription.GetProperty("type").GetString();
						var eventData = eventPayload.GetProperty("event");

						switch (eventType)
						{
							case "stream.online":
								{
									var data = eventData.Deserialize<StreamOnlineEvent>();
									if (data != null)
									{
										OnStreamOnline?.Invoke(_channelId, data);
									}

									break;
								}
							case "stream.offline":
								{
									var data = eventData.Deserialize<StreamOfflineEvent>();
									if (data != null)
									{
										OnStreamOffline?.Invoke(_channelId, data);
									}

									break;
								}
							case "channel.follow":
								{
									var data = eventData.Deserialize<ChannelFollowEvent>();
									if (data != null)
									{
										OnChannelFollow?.Invoke(_channelId, data);
									}

									break;
								}
							case "channel.subscribe":
								{
									var data = eventData.Deserialize<ChannelSubscribeEvent>();
									if (data != null)
									{
										OnChannelSubscribe?.Invoke(_channelId, data);
									}

									break;
								}
							case "channel.channel_points_custom_reward_redemption.add":
								{
									var data = eventData.Deserialize<ChannelPointsRedeemEvent>();
									if (data != null)
									{
										OnChannelPointsRedeem?.Invoke(_channelId, data);
									}

									break;
								}
							case "channel.prediction.begin":
								{
									var data = eventData.Deserialize<ChannelPredictionBeginEvent>();
									if (data != null)
									{
										OnPredictionBegin?.Invoke(_channelId, data);
									}

									break;
								}
							default:
								_logger.Warning("Unhandled EventSub event type: {EventType}", eventType);
								break;
						}
					}
					// 他の message_type（session_keepalive, revocation, reconnect等）も必要に応じて処理
				}
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Failed to parse EventSub WebSocket message");
			}
		}

		private async Task RegisterEventSubSubscriptions()
		{
			// 必要なイベントごとにサブスクリプションを作成
			// ここでは stream.online のみ例示
			var accessToken = _twitchAuthService.AccessToken;
			var clientId = await GetClientIdAsync();
			var userId = await GetUserIdAsync();

			using var httpClient = new System.Net.Http.HttpClient();
			var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://api.twitch.tv/helix/eventsub/subscriptions");
			request.Headers.Add("Authorization", $"Bearer {accessToken}");
			request.Headers.Add("Client-Id", clientId);
			request.Content = new System.Net.Http.StringContent(JsonSerializer.Serialize(new
			{
				type = "stream.online",
				version = "1",
				condition = new { broadcaster_user_id = userId },
				transport = new
				{
					method = "websocket",
					session_id = _sessionId
				}
			}), Encoding.UTF8, "application/json");

			var response = await httpClient.SendAsync(request);
			if (!response.IsSuccessStatusCode)
			{
				_logger.Error("Failed to register EventSub subscription: {Status} {Reason}", response.StatusCode, response.ReasonPhrase);
			}
		}

		private async Task<string> GetClientIdAsync()
		{
			var info = await _twitchAuthService.FetchLoggedInUserInfoWithRefresh();
			return info?.ClientId ?? throw new InvalidOperationException("ClientId not available");
		}

		private async Task<string> GetUserIdAsync()
		{
			var info = await _twitchAuthService.FetchLoggedInUserInfoWithRefresh();
			return info?.UserId ?? throw new InvalidOperationException("UserId not available");
		}

		public async ValueTask DisposeAsync()
		{
			_cts.Cancel();
			if (_webSocketProvider.IsConnected)
			{
				await _webSocketProvider.Disconnect("Disposing").ConfigureAwait(false);
			}
			_webSocketProvider.ConnectHappened -= ConnectHappenedHandler;
			_webSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
			_webSocketProvider.MessageReceived -= MessageReceivedHandler;
			_cts.Dispose();
		}
	}
}