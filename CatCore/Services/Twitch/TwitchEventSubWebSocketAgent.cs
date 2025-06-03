using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Models.Shared;
using CatCore.Models.Twitch.EventSub;
using CatCore.Models.Twitch.EventSub.Responses;
using CatCore.Models.Twitch.EventSub.Responses.VideoPlayback;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using DryIoc;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal sealed class TwitchEventSubWebSocketAgent : IAsyncDisposable
	{
		private const string EVENTSUB_WS_ENDPOINT = "wss://eventsub.wss.twitch.tv/ws";
		private const string EVENTSUB_CONDUITS_ENDPOINT = "https://api.twitch.tv/helix/eventsub/conduits";
		private const string EVENTSUB_SUBSCRIPTIONS_ENDPOINT = "https://api.twitch.tv/helix/eventsub/subscriptions";
		private readonly ILogger _logger;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly IKittenPlatformActiveStateManager _activeStateManager;
		private readonly string _channelId;
		private readonly IKittenWebSocketProvider _kittenWebSocketProvider;
		private readonly SemaphoreSlim _initSemaphoreSlim = new(1, 1);
		private readonly SemaphoreSlim _wsStateChangeSemaphoreSlim = new(1, 1);
		private WebSocketConnection? _webSocketConnection;
		private string? _sessionId;
		private string? _conduitId;
		private bool _sessionIdReceived;
		private readonly CancellationTokenSource _cts = new();

		public event Action<string, StreamOnlineEvent>? OnStreamOnline;
		public event Action<string, StreamOfflineEvent>? OnStreamOffline;
		public event Action<string, ChannelFollowEvent>? OnChannelFollow;
		public event Action<string, ChannelSubscribeEvent>? OnChannelSubscribe;
		public event Action<string, ChannelPointsRedeemEvent>? OnChannelPointsRedeem;
		public event Action<string, ChannelPredictionBeginEvent>? OnPredictionBegin;
		public event Action<string, ChannelChatMessageEvent>? OnChatMessage;
		public event Action<string, ChannelChatMessageDeleteEvent>? OnChatMessageDelete;
		public event Action<string, ChannelShoutoutCreateEvent>? OnShoutoutCreate;
		public event Action<string, ChannelShoutoutReceiveEvent>? OnShoutoutReceive;

		private ConcurrentDictionary<string, string> _registerdConduitEvent = new ConcurrentDictionary<string, string>();

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
			_kittenWebSocketProvider = new KittenWebSocketProvider(_logger);

			_twitchAuthService.OnCredentialsChanged += TwitchAuthServiceOnOnCredentialsChanged;
		}

		internal void RequestTopicListening(string topic)
		{
			_ = StartAsync().ConfigureAwait(false);
		}

		private async void TwitchAuthServiceOnOnCredentialsChanged()
		{
			if (_twitchAuthService.HasTokens)
			{
				if (_activeStateManager.GetState(PlatformType.Twitch))
				{
					await StartAsync().ConfigureAwait(false);
				}
			}
			else
			{
				await Stop().ConfigureAwait(false);
			}
		}

		public async Task StartAsync(bool force = false)
		{
			_logger.Information("Starting Twitch EventSub WebSocket (Conduit) for channel {ChannelId}", _channelId);
			if (!force && (_initSemaphoreSlim.CurrentCount == 0 || _kittenWebSocketProvider.IsConnected))
			{
				return;
			}
			var lockAcquired = false;
			try
			{
				if (!(lockAcquired = await _initSemaphoreSlim.WaitAsync(force ? -1 : 0).ConfigureAwait(false)))
				{
					return;
				}
				if (!_twitchAuthService.HasTokens)
				{
					return;
				}

				var loggedInUser = await _twitchAuthService.FetchLoggedInUserInfoWithRefresh().ConfigureAwait(false);
				if (loggedInUser == null)
				{
					return;
				}

				_kittenWebSocketProvider.ConnectHappened -= ConnectHappenedHandler;
				_kittenWebSocketProvider.ConnectHappened += ConnectHappenedHandler;

				_kittenWebSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
				_kittenWebSocketProvider.DisconnectHappened += DisconnectHappenedHandler;

				_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;
				_kittenWebSocketProvider.MessageReceived += MessageReceivedHandler;

				_sessionIdReceived = false;
				_sessionId = null;
				_conduitId = null;

				await RegisterConduitAndSubscriptionsStep1().ConfigureAwait(false);
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to acquire semaphore for Twitch EventSub WebSocket initialization.");
			}
			finally
			{
				if (!lockAcquired)
				{
					_initSemaphoreSlim.Release();
				}
			}
		}

		private async Task Stop(string? disconnectReason = null)
		{
			using var _ = await Synchronization.LockAsync(_wsStateChangeSemaphoreSlim).ConfigureAwait(false);

			await _kittenWebSocketProvider.Disconnect(disconnectReason).ConfigureAwait(false);

			_kittenWebSocketProvider.ConnectHappened -= ConnectHappenedHandler;
			_kittenWebSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
			_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;

			_sessionIdReceived = false;
			_sessionId = null;
			_conduitId = null;
		}

		private async Task ConnectHappenedHandler(WebSocketConnection webSocketConnection)
		{
			_webSocketConnection = webSocketConnection;
			_logger.Information("Twitch EventSub WebSocket connected (Conduit形式).");
			// 最初のメッセージで session_id を取得するため、MessageReceivedHandler で処理
			await Task.Yield(); // Ensure the connection is processed before any further actions
		}

		private async Task DisconnectHappenedHandler()
		{
			_webSocketConnection = null;
			_logger.Warning("Twitch EventSub WebSocket disconnected.");
			await Task.Yield(); // Ensure the disconnect is processed before any further actions
		}

		private async Task MessageReceivedHandler(WebSocketConnection webSocketConnection, string receivedMessage)
		{
			try
			{
				_logger.Information("Received message from Twitch EventSub WebSocket: {Message}", receivedMessage);
				using var doc = JsonDocument.Parse(receivedMessage);
				var root = doc.RootElement;

				// 最初のメッセージで session_id を取得
				if (!_sessionIdReceived
					&& root.TryGetProperty("metadata", out var initialMetadata)
					&& root.TryGetProperty("payload", out var initialPayload)
					&& initialPayload.TryGetProperty("session", out var session)
					&& session.TryGetProperty("id", out var sessionIdElement))
				{
					_sessionId = sessionIdElement.GetString();
					_sessionIdReceived = true;
					_logger.Information($"Twitch EventSub WebSocket session started: {_sessionId}");
					_ = RegisterConduitAndSubscriptionsStep2().ConfigureAwait(false);
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
							case EventSubTypes.STREAM_ONLINE:
								{
									var data = eventData.Deserialize<StreamOnlineEvent>();
									if (data != null)
									{
										OnStreamOnline?.Invoke(_channelId, data);
									}

									break;
								}
							case EventSubTypes.STREAM_OFFLINE:
								{
									var data = eventData.Deserialize<StreamOfflineEvent>();
									if (data != null)
									{
										OnStreamOffline?.Invoke(_channelId, data);
									}

									break;
								}
							case EventSubTypes.CHANNEL_FOLLOW:
								{
									var data = eventData.Deserialize<ChannelFollowEvent>();
									if (data != null)
									{
										OnChannelFollow?.Invoke(_channelId, data);
									}

									break;
								}
							case EventSubTypes.CHANNEL_SUBSCRIBE:
								{
									var data = eventData.Deserialize<ChannelSubscribeEvent>();
									if (data != null)
									{
										OnChannelSubscribe?.Invoke(_channelId, data);
									}

									break;
								}
							case EventSubTypes.CHANNEL_POINTS_REDEEM:
								{
									var data = eventData.Deserialize<ChannelPointsRedeemEvent>();
									if (data != null)
									{
										OnChannelPointsRedeem?.Invoke(_channelId, data);
									}

									break;
								}
							case EventSubTypes.CHANNEL_PREDICTION_BEGIN:
								{
									var data = eventData.Deserialize<ChannelPredictionBeginEvent>();
									if (data != null)
									{
										OnPredictionBegin?.Invoke(_channelId, data);
									}

									break;
								}
							case EventSubTypes.CHANNEL_CHAT_MESSAGE:
								{
									var data = eventData.Deserialize<ChannelChatMessageEvent>();
									if (data != null)
									{
										OnChatMessage?.Invoke(_channelId, data);
									}

									break;
								}
							case EventSubTypes.CHANNEL_CHAT_MESSAGE_DELETE:
								{
									var data = eventData.Deserialize<ChannelChatMessageDeleteEvent>();
									if (data != null)
									{
										OnChatMessageDelete?.Invoke(_channelId, data);
									}

									break;
								}
							case EventSubTypes.CHANNEL_SHOUTOUT_CREATE:
								{
									var data = eventData.Deserialize<ChannelShoutoutCreateEvent>();
									if (data != null)
									{
										OnShoutoutCreate?.Invoke(_channelId, data);
									}

									break;
								}
							case EventSubTypes.CHANNEL_SHOUTOUT_RECEIVE:
								{
									var data = eventData.Deserialize<ChannelShoutoutReceiveEvent>();
									if (data != null)
									{
										OnShoutoutReceive?.Invoke(_channelId, data);
									}

									break;
								}
							default:
								_logger.Warning("Unhandled EventSub event type: {EventType}", eventType);
								break;
						}
					}
					else if (messageType == "session_keepalive")
					{
						_logger.Debug("Received session_keepalive message from Twitch EventSub WebSocket.");
					}
					else if (messageType == "revocation")
					{
						_logger.Warning("Received revocation message from Twitch EventSub WebSocket. サブスクリプションが無効化されました。");
					}
					else if (messageType == "reconnect")
					{
						_logger.Information("Received reconnect message from Twitch EventSub WebSocket. 再接続を実施します。");
						if (_kittenWebSocketProvider.IsConnected)
						{
							await _kittenWebSocketProvider.Disconnect("Reconnect requested by Twitch").ConfigureAwait(false);
						}
						await Task.Delay(1000);
						await StartAsync().ConfigureAwait(false);
					}
					else if (messageType == "session_welcome")
					{
						_logger.Information("Received session_welcome message from Twitch EventSub WebSocket.");
						// 必要ならここで何か初期化処理を追加
					}
					else
					{
						_logger.Warning("Received unknown message_type from Twitch EventSub WebSocket: {MessageType}", messageType);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Failed to parse EventSub WebSocket message");
			}
		}

		private async Task<bool> RegisterConduitAndSubscriptionsStep1()
		{
			//var accessToken = _twitchAuthService.AccessToken;
			var appAccessToken = _twitchAuthService.AppAccessToken;
			var clientId = await GetClientIdAsync();
			var userId = await GetUserIdAsync();

			using var httpClient = new System.Net.Http.HttpClient();
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {appAccessToken}");
			httpClient.DefaultRequestHeaders.Add("Client-Id", clientId);
			// 1. 既存Conduitの取得
			if (string.IsNullOrEmpty(_conduitId))
			{
				var getConduitsResponse = await httpClient.GetAsync(EVENTSUB_SUBSCRIPTIONS_ENDPOINT);
				if (getConduitsResponse.IsSuccessStatusCode)
				{
					var json = await getConduitsResponse.Content.ReadAsStringAsync();
					_logger.Information("Fetched existing EventSub Conduits: {Json}", json);
					using var doc = JsonDocument.Parse(json);
					var data = doc.RootElement.GetProperty("data");
					if (data.GetArrayLength() > 0)
					{
						var transport = data[0].GetProperty("transport");
						_conduitId = transport.GetProperty("conduit_id").GetString();
						_logger.Information("Reusing existing EventSub Conduit: {ConduitId}", _conduitId);
						for (var i = 0; i < data.GetArrayLength(); i++)
						{
							var conduit = data[i];
							if (conduit.TryGetProperty("id", out var conduitIdElement) && conduit.TryGetProperty("type", out var typeElement))
							{
								if (string.IsNullOrEmpty(conduitIdElement.GetString())
									|| string.IsNullOrEmpty(typeElement.GetString()))
								{
									continue;
								}
								var chid = conduit.GetProperty("transport");
								if (chid.GetProperty("conduit_id").GetString() != _conduitId)
								{
									continue; // 現在のConduitと一致しないものはスキップ
								}
								_registerdConduitEvent.TryAdd(typeElement.GetString()!, conduitIdElement.GetString()!);
							}
						}
					}
				}
			}
			// 1. Conduitの作成または取得
			if (string.IsNullOrEmpty(_conduitId))
			{
				var createConduitPayload = new { shard_count = 1 };
				var createConduitRequest = new StringContent(JsonSerializer.Serialize(createConduitPayload),
						Encoding.UTF8,
						"application/json");
				var createConduitResponse = await httpClient.PostAsync(EVENTSUB_CONDUITS_ENDPOINT, createConduitRequest);
				if (!createConduitResponse.IsSuccessStatusCode)
				{
					_logger.Error("Failed to create EventSub Conduit: {Status} {Reason}", createConduitResponse.StatusCode, createConduitResponse.ReasonPhrase);
					_sessionId = null;
					_conduitId = null;
					_sessionIdReceived = false;
					_logger.Information("Retrying EventSub WebSocket connection...");
					return false;
				}
				using var conduitDoc = JsonDocument.Parse(await createConduitResponse.Content.ReadAsStringAsync());
				_conduitId = conduitDoc.RootElement.GetProperty("data")[0].GetProperty("id").GetString();
				_logger.Information("Created/Obtained EventSub Conduit: {ConduitId}", _conduitId);
			}
			// ここでWebSocketの接続が確立されていることを確認するため、セッションIDを取得する必要があります。
			if (string.IsNullOrEmpty(_sessionId))
			{
				await _kittenWebSocketProvider.Connect(EVENTSUB_WS_ENDPOINT);
			}
			return true;
		}

		public async Task RegisterConduitAndSubscriptionsStep2()
		{
			var appAccessToken = _twitchAuthService.AppAccessToken;
			var clientId = await GetClientIdAsync();
			var userId = await GetUserIdAsync();

			using var httpClient = new System.Net.Http.HttpClient();
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {appAccessToken}");
			httpClient.DefaultRequestHeaders.Add("Client-Id", clientId);

			// Conduitへの移送の割り当て
			var shardsContent = new
			{
				conduit_id = _conduitId,
				shards = new[]
				{
					new
					{
						id = 0,
						transport = new
						{
							method = "websocket",
							session_id = _sessionId // ここで session_id を指定
						}
					}
				}
			};
			var shardsRequest = new HttpRequestMessage(new HttpMethod("PATCH"), $"{EVENTSUB_CONDUITS_ENDPOINT}/shards")
			{
				Content = new StringContent(JsonSerializer.Serialize(shardsContent), Encoding.UTF8, "application/json")
			};

			var response = await httpClient.SendAsync(shardsRequest);
			if (!response.IsSuccessStatusCode)
			{
				_logger.Error("Failed to assign shards to EventSub Conduit: {Status} {Reason}", response.StatusCode, response.ReasonPhrase);
				_sessionId = null;
				_conduitId = null;
				_sessionIdReceived = false;
				_logger.Information("Retrying EventSub WebSocket connection...");
				return;
			}
			else
			{
				var res = await response.Content.ReadAsStringAsync();
				_logger.Information("Assigned shards to EventSub Conduit: {ConduitId}", _conduitId);
				_logger.Information("Response: {Response}", res);
			}

			await Task.Delay(1000); // Wait for WebSocket connection to stabilize

			// 2. Conduitにサブスクリプションを紐付け
			var eventTypes = new[]
			{
				EventSubTypes.STREAM_ONLINE,
				EventSubTypes.STREAM_OFFLINE,
				EventSubTypes.CHANNEL_FOLLOW,
				EventSubTypes.CHANNEL_SUBSCRIBE,
				EventSubTypes.CHANNEL_POINTS_REDEEM,
				EventSubTypes.CHANNEL_PREDICTION_BEGIN,
				EventSubTypes.CHANNEL_CHAT_MESSAGE,
				EventSubTypes.CHANNEL_CHAT_MESSAGE_DELETE,
				EventSubTypes.CHANNEL_SHOUTOUT_CREATE,
				EventSubTypes.CHANNEL_SHOUTOUT_RECEIVE
			};

			foreach (var eventType in eventTypes)
			{
				if (_registerdConduitEvent.ContainsKey(eventType))
				{
					continue; // 既に登録済みのイベントはスキップ
				}
				var condition = eventType switch
				{
					EventSubTypes.CHANNEL_CHAT_MESSAGE
					or EventSubTypes.CHANNEL_CHAT_MESSAGE_DELETE => new { broadcaster_user_id = userId, user_id = userId },
					EventSubTypes.CHANNEL_FOLLOW
					or EventSubTypes.CHANNEL_MODERATOR_ADD
					or EventSubTypes.CHANNEL_MODERATOR_REMOVE
					or EventSubTypes.CHANNEL_SHOUTOUT_CREATE
					or EventSubTypes.CHANNEL_SHOUTOUT_RECEIVE => CatCore.Models.Twitch.EventSub.EventSubConditions.ModeratorUserId(userId, userId),
					EventSubTypes.USER_AUTHORIZATION_GRANT or EventSubTypes.USER_AUTHORIZATION_REVOKE or EventSubTypes.USER_UPDATE => new { user_id = userId },
					EventSubTypes.CHANNEL_RAID => new { to_broadcaster_user_id = userId }, // 例: レイド受信
					_ => CatCore.Models.Twitch.EventSub.EventSubConditions.BroadcasterUserId(userId),
				};
				var payload = new
				{
					type = eventType,
					version = eventType switch
					{
						EventSubTypes.CHANNEL_FOLLOW => "2",
						_ => "1"
					},
					condition,
					transport = new
					{
						method = "conduit",
						conduit_id = _conduitId
					}
				};

				var request = new System.Net.Http.HttpRequestMessage(HttpMethod.Post, EVENTSUB_SUBSCRIPTIONS_ENDPOINT)
				{
					Content = new System.Net.Http.StringContent(
						JsonSerializer.Serialize(payload),
						Encoding.UTF8,
						"application/json")
				};

				response = await httpClient.SendAsync(request);
				if (!response.IsSuccessStatusCode)
				{
					_logger.Error("Failed to register EventSub subscription (Conduit): {Type} {Status} {Reason}", eventType, response.StatusCode, response.ReasonPhrase);
				}
				else
				{
					_logger.Information("Subscribed to EventSub (Conduit): {Type}", eventType);
				}
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

		private async Task DeleteSubscribe(string type)
		{
			var httpClient = new HttpClient();
			var clientId = await GetClientIdAsync();
			var appToken = _twitchAuthService.AppAccessToken;
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {appToken}");
			httpClient.DefaultRequestHeaders.Add("Client-Id", clientId);

			var h = new HttpRequestMessage(HttpMethod.Delete, $"{EVENTSUB_SUBSCRIPTIONS_ENDPOINT}?id={_registerdConduitEvent[type]}");
			var response = await httpClient.SendAsync(h);
			if (response.IsSuccessStatusCode)
			{
				_logger.Information("Unsubscribed from EventSub (Conduit): {Type}", type);
				_registerdConduitEvent.TryRemove(type, out _);
			}
			else
			{
				_logger.Error("Failed to unsubscribe from EventSub (Conduit): {Type} {Status} {Reason}", type, response.StatusCode, response.ReasonPhrase);
			}
		}

		public async ValueTask DisposeAsync()
		{
			_cts.Cancel();
			var subs = _registerdConduitEvent.Keys.ToArray();
			foreach (var item in subs)
			{
				await DeleteSubscribe(item).ConfigureAwait(false);
			}
			if (_kittenWebSocketProvider.IsConnected)
			{
				await Stop("Forced to go close").ConfigureAwait(false);
			}
			_kittenWebSocketProvider.ConnectHappened -= ConnectHappenedHandler;
			_kittenWebSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
			_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;
			_cts.Dispose();
		}
	}
}