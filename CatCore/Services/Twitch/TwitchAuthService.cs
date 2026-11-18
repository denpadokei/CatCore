using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Helpers.JSON;
using CatCore.Models.Credentials;
using CatCore.Models.Twitch.OAuth;
using CatCore.Services.Interfaces;
using CatCore.Shared.Models.Twitch.OAuth;
using CatCore.Services.Twitch.Interfaces;
using Polly;
using Polly.Retry;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal sealed class TwitchAuthService : KittenCredentialsProvider<TwitchCredentials>, ITwitchAuthService
	{
		private const string SERVICE_TYPE = nameof(Twitch);
		private const string TWITCH_AUTH_BASEURL = "https://id.twitch.tv/oauth2/";
		private const string DEVICE_CODE_GRANT_TYPE = "urn:ietf:params:oauth:grant-type:device_code";
		private const string DEVICE_AUTHORIZATION_PENDING = "authorization_pending";
		private const string DEVICE_SLOW_DOWN = "slow_down";
		private const string DEVICE_VERIFICATION_BASEURL = "https://www.twitch.tv/activate?public=true&device-code=";
		private const int DEVICE_CODE_DEFAULT_EXPIRES_IN_SECONDS = 1800;
		private const int DEVICE_CODE_DEFAULT_INTERVAL_SECONDS = 5;
		private const int DEVICE_CODE_SLOW_DOWN_INCREMENT_SECONDS = 5;

		private static readonly TimeSpan VALIDATION_INTERVAL = TimeSpan.FromHours(1);
		private static readonly TimeSpan FORCED_REFRESH_OFFSET = TimeSpan.FromMinutes(4);
		private static readonly TimeSpan FAILED_REFRESH_RETRY_DELAY = TimeSpan.FromMinutes(1);
		private static readonly TimeSpan AUTH_BACKEND_PROBE_TIMEOUT = TimeSpan.FromSeconds(5);
		private static readonly TimeSpan AUTH_BACKEND_REQUEST_TIMEOUT = TimeSpan.FromSeconds(20);
		private static readonly TimeSpan DEVICE_AUTHORIZATION_REQUEST_TIMEOUT = TimeSpan.FromSeconds(10);
		private const int FAILED_REFRESH_RETRY_MAX_MINUTES = 60;

		private readonly AsyncRetryPolicy<HttpResponseMessage> _exceptionRetryPolicy;

		private readonly SemaphoreSlim _refreshLocker = new(1, 1);
		private readonly SemaphoreSlim _loggedInUserUpdateLocker = new(1, 1);
		private readonly SemaphoreSlim _deviceAuthorizationStartLocker = new(1, 1);

		/// <summary>
		/// Scopes that CatCore cannot work without. A token that is missing one of them is rejected.
		/// </summary>
		private readonly string[] _twitchAuthorizationScope =
		{
			"bits:read",
			"chat:edit",
			"chat:read",
			"user:read:chat",
			"user:write:chat",
			"user:bot",
			"channel:bot",
			"channel:manage:broadcast",
			"channel:manage:polls",
			"channel:manage:predictions",
			"channel:manage:raids",
			"channel:manage:redemptions",
			"channel:moderate",
			"channel:read:ads",
			"channel:read:chat_settings",
			"channel:read:subscriptions",
			"moderator:manage:announcements",
			"moderator:manage:banned_users",
			"moderator:manage:chat_messages",
			"moderator:manage:chat_settings",
			"moderator:read:chat_settings",
			"moderator:read:followers",
			"user:manage:chat_color",
			"user:read:follows"
		};

		/// <summary>
		/// Scopes that unlock the optional EventSub subscription types (hype train, goals, charity,
		/// moderation tooling, ...). They are requested during authorization, but a token that is
		/// missing one of them is still valid: the matching subscriptions are simply not created.
		/// </summary>
		private readonly string[] _twitchOptionalAuthorizationScope =
		{
			"channel:read:charity",
			"channel:read:goals",
			"channel:read:hype_train",
			"channel:read:vips",
			"moderation:read",
			"moderator:manage:automod",
			"moderator:manage:automod_settings",
			"moderator:read:blocked_terms",
			"moderator:read:moderators",
			"moderator:read:shield_mode",
			"moderator:read:shoutouts",
			"moderator:read:suspicious_users",
			"moderator:read:unban_requests",
			"moderator:read:vips",
			"moderator:read:warnings",
			"user:read:whispers"
		};

		private readonly ILogger _logger;
		private readonly ConstantsBase _constants;
		private readonly HttpClient _twitchAuthClient;

		/// <summary>
		/// Client for the CatCore auth back-end. <c>null</c> when no usable back-end URI is
		/// configured, in which case CatCore operates in local-only authorization mode.
		/// </summary>
		private readonly HttpClient? _catCoreAuthClient;

		private readonly object _validationLoopStateLock = new();
		private readonly object _forcedRefreshStateLock = new();
		private readonly object _deviceAuthorizationStateLock = new();
		private readonly object _authorizationStateLock = new();

		private ValidationResponse? _loggedInUser;
		private AuthenticationStatus _status;
		private CancellationTokenSource? _validationCancellationTokenSource;
		private Task? _validationTask;
		private CancellationTokenSource? _forcedRefreshCancellationTokenSource;
		private Task? _forcedRefreshTask;
		private bool _isInitialized;
		private bool _scopeMismatchDetected;
		private int _consecutiveForcedRefreshFailures;

		private PendingDeviceAuthorizationState? _pendingDeviceAuthorization;
		private CancellationTokenSource? _deviceAuthorizationCancellationTokenSource;
		private string? _lastDeviceAuthorizationError;
		private string? _pendingAuthorizationState;

		protected override string ServiceType => SERVICE_TYPE;

		public string? AccessToken => Credentials.AccessToken;

		public bool HasTokens => !string.IsNullOrWhiteSpace(AccessToken) && !string.IsNullOrWhiteSpace(Credentials.RefreshToken);

		/// <remark>
		/// Consider token as not valid anymore when it has less than 5 minutes remaining
		/// </remark>
		public bool TokenIsValid => Credentials.ValidUntil > DateTimeOffset.Now.AddMinutes(5);

		public AuthenticationStatus Status
		{
			get => _status;
			private set
			{
				if (_status == value)
				{
					return;
				}

				_status = value;
				OnAuthenticationStatusChanged?.Invoke(_status);
			}
		}

		public event Action<AuthenticationStatus>? OnAuthenticationStatusChanged;

		public string? LocalAuthorizationError
		{
			get
			{
				lock (_deviceAuthorizationStateLock)
				{
					return _lastDeviceAuthorizationError;
				}
			}
		}

		public TwitchDeviceAuthorizationInfo? PendingDeviceAuthorization
		{
			get
			{
				lock (_deviceAuthorizationStateLock)
				{
					var pendingDeviceAuthorization = _pendingDeviceAuthorization;
					if (pendingDeviceAuthorization == null || pendingDeviceAuthorization.ExpiresAt <= DateTimeOffset.Now)
					{
						return null;
					}

					return pendingDeviceAuthorization.ToInfo();
				}
			}
		}

		public TwitchAuthService(ILogger logger, IKittenPathProvider kittenPathProvider, ConstantsBase constants, Version libraryVersion)
			: this(logger, kittenPathProvider, constants, libraryVersion, null)
		{
		}

		internal TwitchAuthService(ILogger logger, IKittenPathProvider kittenPathProvider, ConstantsBase constants, Version libraryVersion, HttpMessageHandler? httpMessageHandler)
			: base(logger, kittenPathProvider)
		{
			_logger = logger;
			_constants = constants;

			var userAgent = $"{nameof(CatCore)}/{libraryVersion.ToString(3)}";

			_twitchAuthClient = CreateTwitchAuthClient(userAgent, httpMessageHandler);
			_catCoreAuthClient = TryCreateCatCoreAuthClient(constants.CatCoreAuthServerUri, userAgent, httpMessageHandler);

			if (_catCoreAuthClient == null)
			{
				_logger.Warning("No usable CatCore auth back-end URI is configured; Twitch authorization will happen locally through the device code flow, which does not require a client secret");
			}

			_exceptionRetryPolicy = Policy<HttpResponseMessage>
				.Handle<HttpRequestException>()
				.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds((1 << (retryAttempt - 1)) * 500));
		}

		private static HttpClient CreateTwitchAuthClient(string userAgent, HttpMessageHandler? httpMessageHandler)
		{
			HttpClient twitchAuthClient;
#if !RELEASE
			twitchAuthClient = httpMessageHandler != null
				? new HttpClient(httpMessageHandler)
				: new HttpClient(new HttpClientHandler { Proxy = SharedProxyProvider.PROXY });
#else
			twitchAuthClient = httpMessageHandler != null
				? new HttpClient(httpMessageHandler)
				: new HttpClient();
#endif
			twitchAuthClient.BaseAddress = new Uri(TWITCH_AUTH_BASEURL, UriKind.Absolute);
			twitchAuthClient.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent);

			return twitchAuthClient;
		}

		private static HttpClient? TryCreateCatCoreAuthClient(string? serverUri, string userAgent, HttpMessageHandler? httpMessageHandler)
		{
			if (string.IsNullOrWhiteSpace(serverUri) || !Uri.TryCreate(serverUri, UriKind.Absolute, out var baseAddress))
			{
				return null;
			}

			HttpClient catCoreAuthClient;
#if !RELEASE
			catCoreAuthClient = httpMessageHandler != null
				? new HttpClient(httpMessageHandler)
				: new HttpClient(new HttpClientHandler { Proxy = SharedProxyProvider.PROXY });
#else
			catCoreAuthClient = httpMessageHandler != null
				? new HttpClient(httpMessageHandler)
				: new HttpClient();
#endif
			catCoreAuthClient.BaseAddress = baseAddress;
			catCoreAuthClient.Timeout = AUTH_BACKEND_REQUEST_TIMEOUT;
			catCoreAuthClient.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent);

			return catCoreAuthClient;
		}

		/// <summary>
		/// Initializes the Twitch authentication service and starts the background validation loop.
		/// </summary>
		/// <remarks>
		/// This method is idempotent and can be safely called multiple times. On the first call it
		/// creates the internal cancellation token source and starts the validation loop that keeps
		/// authentication state up to date. Subsequent calls are ignored once initialization has
		/// completed. Call this during application startup or when the <c>ITwitchAuthService</c>
		/// instance is first created to ensure that token validation runs in the background.
		/// </remarks>
		public void Initialize()
		{
			lock (_validationLoopStateLock)
			{
				if (_isInitialized)
				{
					return;
				}

				_isInitialized = true;
				_validationCancellationTokenSource = new CancellationTokenSource();
				var validationTask = Task.Run(() => RunValidationLoop(_validationCancellationTokenSource.Token));
				_validationTask = validationTask.ContinueWith(t =>
				{
					// Ensure any unhandled exceptions from the validation loop are observed and logged
					if (t.IsFaulted && t.Exception != null)
					{
						_logger.Error(t.Exception, "Scheduled Twitch token validation loop encountered an unhandled exception");
					}
				}, TaskContinuationOptions.OnlyOnFaulted);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ValidationResponse? FetchLoggedInUserInfo()
		{
			return _loggedInUser;
		}

		private async Task RunValidationLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					await ValidateCurrentSession().ConfigureAwait(false);
					await Task.Delay(VALIDATION_INTERVAL, cancellationToken).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception ex)
				{
					_logger.Warning(ex, "Scheduled Twitch token validation loop failed");
					await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
				}
			}
		}

		private async Task ValidateCurrentSession()
		{
			if (!HasTokens)
			{
				CancelForcedRefresh();
				return;
			}

			try
			{
				_logger.Information("Running scheduled Twitch token validation");
				await FetchLoggedInUserInfoWithRefresh().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Scheduled Twitch token validation failed");
				ScheduleForcedRefresh(FAILED_REFRESH_RETRY_DELAY);
			}
		}

		private void ScheduleForcedRefresh(TimeSpan? delayOverride = null)
		{
			if (string.IsNullOrWhiteSpace(Credentials.RefreshToken) || Credentials.ValidUntil == null)
			{
				CancelForcedRefresh();
				return;
			}

			var delay = delayOverride ?? (Credentials.ValidUntil.Value - DateTimeOffset.Now - FORCED_REFRESH_OFFSET);
			if (delay < TimeSpan.Zero)
			{
				delay = TimeSpan.Zero;
			}

			CancellationTokenSource? previousCancellationTokenSource;
			CancellationTokenSource cancellationTokenSource;
			lock (_forcedRefreshStateLock)
			{
				previousCancellationTokenSource = _forcedRefreshCancellationTokenSource;
				cancellationTokenSource = new CancellationTokenSource();
				_forcedRefreshCancellationTokenSource = cancellationTokenSource;
				_forcedRefreshTask = Task.Run(() => RunForcedRefreshSchedule(delay, cancellationTokenSource.Token));
				_ = _forcedRefreshTask.ContinueWith(t =>
				{
					if (t.IsFaulted && t.Exception != null)
					{
						_logger.Error(t.Exception, "Scheduled Twitch token refresh encountered an unhandled exception");
					}
				}, TaskContinuationOptions.OnlyOnFaulted);
			}

			previousCancellationTokenSource?.Cancel();

			_logger.Information("Scheduled forced Twitch token refresh in {Delay}", delay.ToString("g"));
		}

		private void CancelForcedRefresh()
		{
			CancellationTokenSource? cancellationTokenSource;
			lock (_forcedRefreshStateLock)
			{
				cancellationTokenSource = _forcedRefreshCancellationTokenSource;
				_forcedRefreshCancellationTokenSource = null;
				_forcedRefreshTask = null;
			}

			if (cancellationTokenSource == null)
			{
				return;
			}

			cancellationTokenSource.Cancel();
			// Do not dispose here: a task started with this token may still be reading
			// IsCancellationRequested on exit.
		}

		private async Task RunForcedRefreshSchedule(TimeSpan delay, CancellationToken cancellationToken)
		{
			try
			{
				if (delay > TimeSpan.Zero)
				{
					await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
				}

				_logger.Information("Running scheduled Twitch token refresh");
				if (!await RefreshTokens().ConfigureAwait(false) && !cancellationToken.IsCancellationRequested)
				{
					_consecutiveForcedRefreshFailures++;
					var retryDelay = TimeSpan.FromMinutes(Math.Min(FAILED_REFRESH_RETRY_MAX_MINUTES, Math.Pow(2, Math.Min(_consecutiveForcedRefreshFailures, 6))));
					_logger.Warning("Scheduled Twitch token refresh failed. Retrying in {Delay} (attempt {Attempt})", retryDelay.ToString("g"), _consecutiveForcedRefreshFailures);
					ScheduleForcedRefresh(retryDelay);
				}
				else
				{
					_consecutiveForcedRefreshFailures = 0;
				}
			}
			catch (OperationCanceledException)
			{
				// Expected when rescheduling or clearing the current forced refresh task
			}
			catch (ObjectDisposedException)
			{
				// The refresh may have permanently invalidated its own cancellation source
			}
		}

		// ReSharper disable once CognitiveComplexity
		public async Task<ValidationResponse?> FetchLoggedInUserInfoWithRefresh()
		{
			if (HasTokens)
			{
				if (_scopeMismatchDetected)
				{
					_logger.Verbose("Twitch token is missing required authorization scopes; skipping validation until the user re-authorizes");
					return null;
				}

				if (TokenIsValid && _loggedInUser != null)
				{
					return _loggedInUser;
				}

				using var _ = await Synchronization.LockAsync(_loggedInUserUpdateLocker);

				if (TokenIsValid && _loggedInUser != null)
				{
					return _loggedInUser;
				}

				if (Status == AuthenticationStatus.Unauthorized)
				{
					Status = AuthenticationStatus.Initializing;
				}

				try
				{
					var validateAccessToken = await ValidateAccessToken(Credentials, false).ConfigureAwait(false);
					_logger.Information("Validated token: Is valid: {IsValid}, Is refreshable: {IsRefreshable}, Scopes: {Scopes}",
						validateAccessToken != null && TokenIsValid,
						Credentials.RefreshToken != null,
						validateAccessToken?.Scopes == null ? "<none>" : string.Join(",", validateAccessToken.Value.Scopes));
					if (validateAccessToken == null || !TokenIsValid)
					{
						_logger.Information("Refreshing tokens");
						await RefreshTokens(force: true).ConfigureAwait(false);
					}

					return _loggedInUser;
				}
				catch (HttpRequestException ex)
				{
					_logger.Error(ex, "An error occurred while trying to validate/refresh the Twitch tokens. Make sure an active internet connection is available");
				}
			}
			else
			{
				_logger.Verbose("No Twitch Credentials present");
			}

			return null;
		}

		public string AuthorizationUrl(string redirectUrl)
		{
			var state = CreateAuthorizationState();
			lock (_authorizationStateLock)
			{
				_pendingAuthorizationState = state;
			}

			var scopes = _twitchAuthorizationScope.Concat(_twitchOptionalAuthorizationScope);
			return $"{TWITCH_AUTH_BASEURL}authorize" +
			       $"?client_id={_constants.TwitchClientId}" +
			       $"&redirect_uri={Uri.EscapeDataString(redirectUrl)}" +
			       "&response_type=code" +
			       "&force_verify=true" +
			       $"&scope={string.Join(" ", scopes)}" +
			       $"&state={state}";
		}

		private static string CreateAuthorizationState()
		{
			var stateBytes = new byte[16];
			using (var randomNumberGenerator = RandomNumberGenerator.Create())
			{
				randomNumberGenerator.GetBytes(stateBytes);
			}

			return BitConverter.ToString(stateBytes).Replace("-", string.Empty).ToLowerInvariant();
		}

		private bool TryConsumeAuthorizationState(string? state)
		{
			lock (_authorizationStateLock)
			{
				var expectedState = _pendingAuthorizationState;
				if (string.IsNullOrWhiteSpace(expectedState) || !string.Equals(expectedState, state, StringComparison.Ordinal))
				{
					// Keep the pending state: a forged or accidental callback must not invalidate
					// the login the user actually started.
					return false;
				}

				_pendingAuthorizationState = null;
				return true;
			}
		}

		/// <summary>
		/// Returns the URL the browser has to visit to authorize CatCore. The authorization code
		/// grant flow through the CatCore auth back-end is used when the back-end is reachable.
		/// Otherwise CatCore falls back to Twitch's device code grant flow, which is a public client
		/// flow and therefore does not need a client secret or the CatCore auth back-end at all.
		/// </summary>
		public async Task<string> GetLoginUrl(string redirectUrl)
		{
			if (await IsCatCoreAuthBackendReachable().ConfigureAwait(false))
			{
				_logger.Information("Using the CatCore auth back-end for the Twitch authorization");
				return AuthorizationUrl(redirectUrl);
			}

			_logger.Warning("The CatCore auth back-end is unreachable; falling back to the local Twitch device code authorization flow, which does not require a client secret");
			var deviceVerificationUri = await StartDeviceCodeAuthorization().ConfigureAwait(false);
			return deviceVerificationUri ?? AuthorizationUrl(redirectUrl);
		}

		/// <summary>
		/// Exchanges an authorization code for tokens through the CatCore auth back-end. When the
		/// back-end is unavailable, a local device code authorization is started instead and the
		/// browser is redirected to Twitch's activation page.
		/// </summary>
		/// <returns>The URL to redirect the browser to after the callback, or <c>null</c> to return to the portal.</returns>
		public async Task<string?> GetTokensByAuthorizationCode(string authorizationCode, string redirectUrl, string? state)
		{
			if (!TryConsumeAuthorizationState(state))
			{
				_logger.Warning("Rejecting the Twitch authorization callback because the OAuth state is missing or does not match the state of the login that was started");
				return null;
			}

			_logger.Information("Exchanging authorization code for credentials using secure CatCore auth back-end");

			var backendOutcome = await ExchangeAuthorizationCodeThroughBackend(authorizationCode, redirectUrl).ConfigureAwait(false);
			if (backendOutcome == BackendCallOutcome.Rejected)
			{
				_logger.Warning("The CatCore auth back-end rejected the authorization code; the user has to start a new login");
				return null;
			}

			if (backendOutcome == BackendCallOutcome.Unknown)
			{
				_logger.Error("The authorization code exchange did not finish within the timeout. The code may already be consumed; ask the user to try again");
				return null;
			}

			if (backendOutcome == BackendCallOutcome.Success)
			{
				return null;
			}

			_logger.Warning("The CatCore auth back-end is unreachable; falling back to the local Twitch device code authorization flow");
			return await StartDeviceCodeAuthorization().ConfigureAwait(false);
		}

		private async Task<BackendCallOutcome> ExchangeAuthorizationCodeThroughBackend(string authorizationCode, string redirectUrl)
		{
			if (_catCoreAuthClient == null)
			{
				return BackendCallOutcome.Unavailable;
			}

			try
			{
				using var content = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					["code"] = authorizationCode,
					["redirect_uri"] = redirectUrl
				});
				using var responseMessage = await _catCoreAuthClient
					.PostAsync(BuildAuthBackendUri("api/twitch/authorize"), content)
					.ConfigureAwait(false);

				if (!responseMessage.IsSuccessStatusCode)
				{
					// Any non-success response means that the authorization code route could not
					// complete. A 4xx is a permanent rejection; don't convert it into a second
					// authorization flow. 5xx/408/429/transport errors are treated as unavailable.
					_logger.Warning("Exchanging the authorization code for credentials resulted in non-success status code: {StatusCode}", responseMessage.StatusCode);
					return IsBackendPermanentFailureStatusCode(responseMessage.StatusCode)
						? BackendCallOutcome.Rejected
						: BackendCallOutcome.Unavailable;
				}

				var authorizationResponse = await responseMessage.Content.ReadFromJsonAsync(TwitchAuthSerializerContext.Default.AuthorizationResponse).ConfigureAwait(false);
				if (string.IsNullOrWhiteSpace(authorizationResponse.AccessToken) || string.IsNullOrWhiteSpace(authorizationResponse.RefreshToken))
				{
					_logger.Warning("The CatCore auth back-end returned an incomplete token pair");
					return BackendCallOutcome.Unavailable;
				}

				var newCredentials = new TwitchCredentials(authorizationResponse, TwitchTokenSources.AuthBackend);
				// Persist first: the authorization code has already been consumed, so losing the
				// tokens because a subsequent validation call fails would force a new login.
				_scopeMismatchDetected = false;
				if (!UpdateCredentials(newCredentials))
				{
					_logger.Error("Could not persist the Twitch credentials obtained from the authorization code. This session will be lost on restart");
				}

				try
				{
					await ValidateAccessToken(newCredentials, resetDataOnFailure: false).ConfigureAwait(false);
				}
				catch (Exception ex) when (ex is HttpRequestException || ex is OperationCanceledException || ex is JsonException)
				{
					// The exchange itself succeeded, so do not start a second authorization flow
					// just because validating the freshly issued token failed temporarily.
					_logger.Warning(ex, "The Twitch tokens were obtained, but validating them failed temporarily. The validation loop will retry");
				}

				return BackendCallOutcome.Success;
			}
			catch (HttpRequestException ex)
			{
				_logger.Warning(ex, "An error occurred while trying to exchange the authorization code through the CatCore auth back-end");
				return BackendCallOutcome.Unavailable;
			}
			catch (OperationCanceledException ex)
			{
				_logger.Warning(ex, "Exchanging the authorization code through the CatCore auth back-end timed out; the code may already be consumed");
				return BackendCallOutcome.Unknown;
			}
			catch (JsonException ex)
			{
				_logger.Warning(ex, "The CatCore auth back-end returned a response that could not be deserialized");
				return BackendCallOutcome.Unavailable;
			}
		}

		private string BuildAuthBackendUri(string relativePath)
		{
			// The BaseAddress is already validated and normalized by TryCreateCatCoreAuthClient.
			return new Uri(_catCoreAuthClient!.BaseAddress!, relativePath).AbsoluteUri;
		}

		private async Task<bool> IsCatCoreAuthBackendReachable()
		{
			if (_catCoreAuthClient == null)
			{
				return false;
			}

			try
			{
				using var cancellationTokenSource = new CancellationTokenSource(AUTH_BACKEND_PROBE_TIMEOUT);
				// The authorize endpoint answers with a 400 when the required query parameters are
				// missing, which still proves that the function is deployed and reachable.
				using var requestMessage = new HttpRequestMessage(HttpMethod.Post, BuildAuthBackendUri("api/twitch/authorize"));
				using var responseMessage = await _catCoreAuthClient
					.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token)
					.ConfigureAwait(false);

				if (IsAuthBackendUnavailableStatusCode(responseMessage.StatusCode))
				{
					_logger.Warning("The CatCore auth back-end probe returned status code {StatusCode}", responseMessage.StatusCode);
					return false;
				}

				return true;
			}
			catch (HttpRequestException ex)
			{
				_logger.Warning(ex, "The CatCore auth back-end is unreachable");
				return false;
			}
			catch (OperationCanceledException ex)
			{
				_logger.Warning(ex, "The CatCore auth back-end did not answer within {Timeout}", AUTH_BACKEND_PROBE_TIMEOUT.ToString("g"));
				return false;
			}
		}

		private static bool IsAuthBackendUnavailableStatusCode(HttpStatusCode statusCode)
		{
			return (int)statusCode >= 500
			       || statusCode == HttpStatusCode.NotFound
			       || statusCode == HttpStatusCode.RequestTimeout
			       || (int)statusCode == 429;
		}

		private static bool IsTransientValidationFailure(HttpStatusCode statusCode)
		{
			return (int)statusCode >= 500 || statusCode == HttpStatusCode.RequestTimeout || (int)statusCode == 429;
		}

		private static bool IsBackendPermanentFailureStatusCode(HttpStatusCode statusCode)
		{
			// The auth back-end signals a rejected code/token with 400/401/403. A 404 means the
			// route is not deployed or the URI is misconfigured, which is an availability problem
			// and must not discard otherwise valid credentials.
			return statusCode == HttpStatusCode.BadRequest
			       || statusCode == HttpStatusCode.Unauthorized
			       || statusCode == HttpStatusCode.Forbidden;
		}

		/// <summary>
		/// Starts a Twitch device code grant authorization. This is a public client flow: it only
		/// uses the client id and never a client secret, so it works without the CatCore auth
		/// back-end and without embedding any secret in the library.
		/// </summary>
		/// <returns>The Twitch activation URL to send the user to, or <c>null</c> when the flow could not be started.</returns>
		private async Task<string?> StartDeviceCodeAuthorization()
		{
			// Serialize the start so a double click cannot create two device codes and two pollers.
			await _deviceAuthorizationStartLocker.WaitAsync().ConfigureAwait(false);
			try
			{
				lock (_deviceAuthorizationStateLock)
				{
					if (_pendingDeviceAuthorization != null && _pendingDeviceAuthorization.ExpiresAt > DateTimeOffset.Now)
					{
						return _pendingDeviceAuthorization.VerificationUri;
					}
				}

				// Switching to the local flow invalidates any authorization-code flow that was
				// started earlier and never completed.
				ClearPendingAuthorizationState();
				CancelPendingDeviceAuthorization();
				lock (_deviceAuthorizationStateLock)
				{
					_lastDeviceAuthorizationError = null;
				}

				try
				{
					using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{TWITCH_AUTH_BASEURL}device")
					{
						Content = new FormUrlEncodedContent(new Dictionary<string, string>
						{
							["client_id"] = _constants.TwitchClientId,
							["scopes"] = string.Join(" ", _twitchAuthorizationScope.Concat(_twitchOptionalAuthorizationScope))
						})
					};
					// Bound this request: the local API handles requests sequentially, so a stalled
					// device authorization must not block the portal for HttpClient's default timeout.
					using var requestCancellationTokenSource = new CancellationTokenSource(DEVICE_AUTHORIZATION_REQUEST_TIMEOUT);
					using var responseMessage = await _twitchAuthClient
						.SendAsync(requestMessage, requestCancellationTokenSource.Token)
						.ConfigureAwait(false);

					if (!responseMessage.IsSuccessStatusCode)
					{
						_logger.Warning("Starting the local Twitch device code authorization resulted in non-success status code: {StatusCode}", responseMessage.StatusCode);
						return null;
					}

					var deviceAuthorization = await responseMessage.Content.ReadFromJsonAsync(TwitchAuthSerializerContext.Default.DeviceAuthorizationResponse).ConfigureAwait(false);
					if (string.IsNullOrWhiteSpace(deviceAuthorization.DeviceCode) || string.IsNullOrWhiteSpace(deviceAuthorization.UserCode))
					{
						_logger.Warning("The local Twitch device code authorization response did not contain a usable device or user code");
						return null;
					}

					var verificationUri = IsTrustedDeviceVerificationUri(deviceAuthorization.VerificationUri)
						? deviceAuthorization.VerificationUri
						: DEVICE_VERIFICATION_BASEURL + Uri.EscapeDataString(deviceAuthorization.UserCode);
					var expiresInSeconds = deviceAuthorization.ExpiresInRaw > 0 ? deviceAuthorization.ExpiresInRaw : DEVICE_CODE_DEFAULT_EXPIRES_IN_SECONDS;
					var intervalSeconds = deviceAuthorization.IntervalRaw > 0 ? deviceAuthorization.IntervalRaw : DEVICE_CODE_DEFAULT_INTERVAL_SECONDS;
					var pendingDeviceAuthorization = new PendingDeviceAuthorizationState(deviceAuthorization.DeviceCode, deviceAuthorization.UserCode, verificationUri,
						DateTimeOffset.Now.AddSeconds(expiresInSeconds), intervalSeconds);

					var cancellationTokenSource = new CancellationTokenSource();
					lock (_deviceAuthorizationStateLock)
					{
						_pendingDeviceAuthorization = pendingDeviceAuthorization;
						_deviceAuthorizationCancellationTokenSource = cancellationTokenSource;
					}

					_ = Task.Run(() => PollDeviceCodeAuthorization(pendingDeviceAuthorization, cancellationTokenSource));

					_logger.Information("Started the local Twitch device code authorization flow. User code: {UserCode}", pendingDeviceAuthorization.UserCode);
					return verificationUri;
				}
				catch (HttpRequestException ex)
				{
					_logger.Warning(ex, "An error occurred while trying to start the local Twitch device code authorization");
					return null;
				}
				catch (OperationCanceledException ex)
				{
					_logger.Warning(ex, "Starting the local Twitch device code authorization timed out");
					return null;
				}
				catch (JsonException ex)
				{
					_logger.Warning(ex, "The local Twitch device code authorization response could not be deserialized");
					return null;
				}
			}
			finally
			{
				_deviceAuthorizationStartLocker.Release();
			}
		}

		/// <summary>
		/// Only the official Twitch activation pages are allowed as redirect targets. The URI is
		/// supplied by Twitch, but validating it keeps a malformed or compromised response from
		/// sending the browser (or the portal link) to an arbitrary host.
		/// </summary>
		private static bool IsTrustedDeviceVerificationUri(string? verificationUri)
		{
			return Uri.TryCreate(verificationUri, UriKind.Absolute, out var uri)
			       && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
			       && (string.Equals(uri.Host, "twitch.tv", StringComparison.OrdinalIgnoreCase)
			           || string.Equals(uri.Host, "www.twitch.tv", StringComparison.OrdinalIgnoreCase));
		}

		private async Task PollDeviceCodeAuthorization(PendingDeviceAuthorizationState pendingDeviceAuthorization, CancellationTokenSource cancellationTokenSource)
		{
			var cancellationToken = cancellationTokenSource.Token;
			var pollInterval = TimeSpan.FromSeconds(Math.Max(1, pendingDeviceAuthorization.IntervalSeconds));
			var lastTransientError = "none";

			try
			{
				_logger.Information("Waiting for the user to authorize CatCore through the local Twitch device code flow");

				while (!cancellationToken.IsCancellationRequested && DateTimeOffset.Now < pendingDeviceAuthorization.ExpiresAt)
				{
					await Task.Delay(pollInterval, cancellationToken).ConfigureAwait(false);

					var pollResult = await PollDeviceCodeTokens(pendingDeviceAuthorization.DeviceCode, cancellationToken).ConfigureAwait(false);
					switch (pollResult.Outcome)
					{
						case DeviceCodePollOutcome.Authorized:
							if (!pollResult.Tokens.HasValue)
							{
								_logger.Warning("The local Twitch device code authorization completed without a usable token response");
								return;
							}

							lock (_deviceAuthorizationStateLock)
							{
								// A newer login attempt may have replaced this flow while the request was in flight.
								if (!ReferenceEquals(_pendingDeviceAuthorization, pendingDeviceAuthorization))
								{
									return;
								}
							}

							var newCredentials = new TwitchCredentials(pollResult.Tokens.Value, TwitchTokenSources.LocalDeviceCode);
							// Persist immediately. Twitch invalidates the previous refresh token as soon
							// as it issues a new one, so losing the new pair would force a new login.
							_scopeMismatchDetected = false;
							lock (_deviceAuthorizationStateLock)
							{
								_lastDeviceAuthorizationError = null;
							}

							if (!UpdateCredentials(newCredentials))
							{
								_logger.Error("Could not persist the locally obtained Twitch credentials. This session will be lost on restart");
							}

							_logger.Information("The local Twitch device code authorization completed successfully");

							try
							{
								await ValidateAccessToken(newCredentials, resetDataOnFailure: false).ConfigureAwait(false);
							}
							catch (Exception ex) when (ex is HttpRequestException || ex is OperationCanceledException || ex is JsonException)
							{
								_logger.Warning(ex, "The local Twitch tokens were obtained, but validating them failed temporarily. The validation loop will retry");
							}

							return;
						case DeviceCodePollOutcome.AuthorizationPending:
							continue;
						case DeviceCodePollOutcome.SlowDown:
						case DeviceCodePollOutcome.TransientFailure:
							// A slow-down request or a temporary network error must not kill the flow
							// while the device code is still valid; back off but keep polling.
							lastTransientError = pollResult.ErrorMessage ?? "temporary failure";
							pollInterval = TimeSpan.FromSeconds(Math.Min(pollInterval.TotalSeconds + DEVICE_CODE_SLOW_DOWN_INCREMENT_SECONDS, 60));
							_logger.Verbose("Backing off the local Twitch device code polling to {Interval} after a temporary failure or slow-down request", pollInterval);
							continue;
						default:
							lock (_deviceAuthorizationStateLock)
							{
								_lastDeviceAuthorizationError = pollResult.ErrorMessage ?? "the authorization was rejected";
							}

							_logger.Warning("The local Twitch device code authorization failed: {Error}", pollResult.ErrorMessage);
							return;
					}
				}

				lock (_deviceAuthorizationStateLock)
				{
					_lastDeviceAuthorizationError = $"the authorization did not complete in time (last polling error: {lastTransientError})";
				}

				_logger.Warning("The local Twitch device code authorization expired: {Error}", _lastDeviceAuthorizationError);
			}
			catch (OperationCanceledException)
			{
				// Expected when a new login is started, the user logs out, or the service shuts down
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "The local Twitch device code authorization poller stopped unexpectedly");
			}
			finally
			{
				CompletePendingDeviceAuthorization(pendingDeviceAuthorization);
				cancellationTokenSource.Dispose();
			}
		}

		private async Task<DeviceCodePollResult> PollDeviceCodeTokens(string deviceCode, CancellationToken cancellationToken)
		{
			try
			{
				using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{TWITCH_AUTH_BASEURL}token")
				{
					Content = new FormUrlEncodedContent(new Dictionary<string, string>
					{
						["client_id"] = _constants.TwitchClientId,
						["device_code"] = deviceCode,
						["grant_type"] = DEVICE_CODE_GRANT_TYPE
					})
				};
				using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				requestTimeout.CancelAfter(DEVICE_AUTHORIZATION_REQUEST_TIMEOUT);
				using var responseMessage = await _twitchAuthClient.SendAsync(requestMessage, requestTimeout.Token).ConfigureAwait(false);
				var responseBody = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

				if (responseMessage.IsSuccessStatusCode)
				{
					var authorizationResponse = JsonSerializer.Deserialize(responseBody, TwitchAuthSerializerContext.Default.AuthorizationResponse);
					return string.IsNullOrWhiteSpace(authorizationResponse.AccessToken)
					       || string.IsNullOrWhiteSpace(authorizationResponse.RefreshToken)
						? DeviceCodePollResult.Failed("The token response did not contain a complete token pair")
						: DeviceCodePollResult.Authorized(authorizationResponse);
				}

				if (TryReadDeviceCodeErrorMessage(responseBody, out var errorMessage))
				{
					switch (errorMessage)
					{
						case DEVICE_AUTHORIZATION_PENDING:
							return DeviceCodePollResult.Pending();
						case DEVICE_SLOW_DOWN:
							return DeviceCodePollResult.SlowDown();
					}

					if ((int)responseMessage.StatusCode >= 500
					    || responseMessage.StatusCode == HttpStatusCode.RequestTimeout
					    || (int)responseMessage.StatusCode == 429)
					{
						return DeviceCodePollResult.TransientFailure($"Twitch returned status code {responseMessage.StatusCode}");
					}

					return DeviceCodePollResult.Failed(errorMessage!);
				}

				if ((int)responseMessage.StatusCode >= 500
				    || responseMessage.StatusCode == HttpStatusCode.RequestTimeout
				    || (int)responseMessage.StatusCode == 429)
				{
					return DeviceCodePollResult.TransientFailure($"Twitch returned status code {responseMessage.StatusCode}");
				}

				return DeviceCodePollResult.Failed($"Twitch returned status code {responseMessage.StatusCode}");
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex) when (ex is HttpRequestException || ex is JsonException || ex is OperationCanceledException)
			{
				// Temporary transport, timeout or body errors: the device code may still be usable.
				_logger.Warning("Temporary failure while polling Twitch for the device code authorization: {Message}", ex.Message);
				return DeviceCodePollResult.TransientFailure(ex.Message);
			}
		}

		private static bool TryReadDeviceCodeErrorMessage(string responseBody, out string? errorMessage)
		{
			errorMessage = null;
			try
			{
				var errorResponse = JsonSerializer.Deserialize(responseBody, TwitchAuthSerializerContext.Default.OAuthErrorResponse);
				errorMessage = !string.IsNullOrWhiteSpace(errorResponse.Message) ? errorResponse.Message : errorResponse.Error;
				return !string.IsNullOrWhiteSpace(errorMessage);
			}
			catch (JsonException)
			{
				return false;
			}
		}

		private void CompletePendingDeviceAuthorization(PendingDeviceAuthorizationState pendingDeviceAuthorization)
		{
			lock (_deviceAuthorizationStateLock)
			{
				if (!ReferenceEquals(_pendingDeviceAuthorization, pendingDeviceAuthorization))
				{
					return;
				}

				_pendingDeviceAuthorization = null;
				_deviceAuthorizationCancellationTokenSource = null;
			}
		}

		private void CancelPendingDeviceAuthorization()
		{
			CancellationTokenSource? cancellationTokenSource;
			lock (_deviceAuthorizationStateLock)
			{
				_pendingDeviceAuthorization = null;
				cancellationTokenSource = _deviceAuthorizationCancellationTokenSource;
				_deviceAuthorizationCancellationTokenSource = null;
			}

			// Do not dispose the source here: the polling task owns it and disposes it when it ends.
			cancellationTokenSource?.Cancel();
		}

		public async Task<ValidationResponse?> ValidateAccessToken(TwitchCredentials credentials, bool resetDataOnFailure = true)
		{
			// Serialize the validation result with token rotation so a validation that started before a
			// refresh cannot overwrite the freshly rotated credentials.
			using var _ = await Synchronization.LockAsync(_refreshLocker).ConfigureAwait(false);
			return await ValidateAccessTokenCore(credentials, resetDataOnFailure).ConfigureAwait(false);
		}

		private async Task<ValidationResponse?> ValidateAccessTokenCore(TwitchCredentials credentials, bool resetDataOnFailure = true)
		{
			if (string.IsNullOrWhiteSpace(credentials.AccessToken))
			{
				return null;
			}

			using var responseMessage = await _exceptionRetryPolicy
				.ExecuteAsync(async () =>
				{
					using var requestMessage = new HttpRequestMessage(HttpMethod.Get, TWITCH_AUTH_BASEURL + "validate");
					requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);
					return await _twitchAuthClient.SendAsync(requestMessage).ConfigureAwait(false);
				})
				.ConfigureAwait(false);

			if (!responseMessage.IsSuccessStatusCode)
			{
				var isTransient = IsTransientValidationFailure(responseMessage.StatusCode);
				if (resetDataOnFailure || !isTransient)
				{
					if (resetDataOnFailure)
					{
						UpdateCredentials(TwitchCredentials.Empty());
						_loggedInUser = null;
						CancelForcedRefresh();
					}

					Status = AuthenticationStatus.Unauthorized;
				}
				else
				{
					// A fresh token pair is already persisted: a temporary /validate failure must
					// not report a logout or stop the refresh schedule.
					_logger.Warning("Twitch token validation returned a temporary status {StatusCode}; keeping the current session", responseMessage.StatusCode);
					ScheduleForcedRefresh();
				}

				return null;
			}

			var validationResponse = await responseMessage.Content.ReadFromJsonAsync(TwitchAuthSerializerContext.Default.ValidationResponse).ConfigureAwait(false);
			if (validationResponse.Scopes == null)
			{
				if (resetDataOnFailure)
				{
					UpdateCredentials(TwitchCredentials.Empty());
					_loggedInUser = null;
					CancelForcedRefresh();
				}

				Status = AuthenticationStatus.Unauthorized;
				_logger.Warning("Twitch token validation returned no scope information");
				return null;
			}

			var missingScopes = _twitchAuthorizationScope.Where(requiredScope => !validationResponse.Scopes.Contains(requiredScope)).ToArray();
			if (missingScopes.Length > 0)
			{
				// Do not discard the stored credentials on a scope mismatch. The refresh token is still
				// valid and can be used to recover, and wiping it makes post-upgrade diagnosis impossible.
				// Consumers observe AuthenticationStatus.Unauthorized and the portal can prompt for
				// re-authorization; validation is short-circuited until new credentials are provided.
				_scopeMismatchDetected = true;
				CancelForcedRefresh();

				Status = AuthenticationStatus.Unauthorized;
				_logger.Warning("Twitch token is missing required authorization scopes. Missing={MissingScopes}; Current={CurrentScopes}",
					string.Join(",", missingScopes),
					string.Join(",", validationResponse.Scopes));
				return null;
			}

			_scopeMismatchDetected = false;

			if (!string.Equals(Credentials.AccessToken, credentials.AccessToken, StringComparison.Ordinal))
			{
				_logger.Verbose("Ignoring a stale token validation result because the current credentials changed while validating");
				return _loggedInUser;
			}

			_loggedInUser = validationResponse;

			UpdateCredentials(credentials.ValidUntil!.Value > validationResponse.ExpiresIn
				? new TwitchCredentials(credentials.AccessToken, credentials.RefreshToken, validationResponse.ExpiresIn, credentials.TokenSource)
				: credentials);
			ScheduleForcedRefresh();

			Status = AuthenticationStatus.Authenticated;

			return _loggedInUser;
		}

		public async Task<bool> RefreshTokens(bool force = false)
		{
			using var _ = await Synchronization.LockAsync(_refreshLocker).ConfigureAwait(false);
			if (string.IsNullOrWhiteSpace(Credentials.RefreshToken))
			{
				return false;
			}

			if (!force && TokenIsValid)
			{
				return true;
			}

			if (Credentials.IsLocalDeviceCodeToken)
			{
				_logger.Information("Refreshing tokens locally using the public client device code grant credentials");
				if (await RefreshTokensLocally().ConfigureAwait(false))
				{
					return true;
				}

				if (_catCoreAuthClient == null)
				{
					return false;
				}

				_logger.Warning("The local Twitch token refresh was rejected; trying the CatCore auth back-end as a last resort");
				var fallbackOutcome = await RefreshTokensThroughBackend().ConfigureAwait(false);
				if (fallbackOutcome == BackendCallOutcome.Rejected)
				{
					MarkReauthorizationRequired();
				}

				return fallbackOutcome == BackendCallOutcome.Success;
			}

			_logger.Information("Refreshing tokens using secure CatCore auth back-end");
			var backendOutcome = await RefreshTokensThroughBackend().ConfigureAwait(false);
			if (backendOutcome == BackendCallOutcome.Success)
			{
				return true;
			}

			if (backendOutcome == BackendCallOutcome.Rejected)
			{
				MarkReauthorizationRequired();
				return false;
			}

			_logger.Warning("The CatCore auth back-end could not refresh the tokens ({Outcome}). Attempting a local refresh without a client secret", backendOutcome);
			if (await RefreshTokensLocally().ConfigureAwait(false))
			{
				return true;
			}

			_logger.Error("Refreshing the Twitch tokens failed both through the CatCore auth back-end and locally. Re-authorize to obtain a locally refreshable token, or restore connectivity to the CatCore auth back-end");
			return false;
		}

		private void MarkReauthorizationRequired()
		{
			_logger.Error("The refresh token was rejected permanently; the user has to authorize CatCore again");
			// Keep the stored token pair. The back-end can only tell us that a refresh was
			// rejected, not whether the underlying token is still usable; destroying the only
			// local copy would turn a transient back-end problem into a permanent logout.
			_loggedInUser = null;
			_scopeMismatchDetected = false;
			CancelForcedRefresh();
			Status = AuthenticationStatus.Unauthorized;
		}

		private async Task<BackendCallOutcome> RefreshTokensThroughBackend()
		{
			if (_catCoreAuthClient == null)
			{
				return BackendCallOutcome.Unavailable;
			}

			try
			{
				var refreshTokenBeforeRefresh = Credentials.RefreshToken;
				if (string.IsNullOrWhiteSpace(refreshTokenBeforeRefresh))
				{
					return BackendCallOutcome.Unavailable;
				}

				using var responseMessage = await _exceptionRetryPolicy.ExecuteAsync(async () =>
					{
						using var content = new FormUrlEncodedContent(new Dictionary<string, string>
						{
							["refresh_token"] = refreshTokenBeforeRefresh!
						});
						return await _catCoreAuthClient.PostAsync(BuildAuthBackendUri("api/twitch/refresh"), content).ConfigureAwait(false);
					})
					.ConfigureAwait(false);

				if (!responseMessage.IsSuccessStatusCode)
				{
					_logger.Warning("Refreshing tokens through the CatCore auth back-end resulted in non-success status code: {StatusCode}", responseMessage.StatusCode);
					return IsBackendPermanentFailureStatusCode(responseMessage.StatusCode)
						? BackendCallOutcome.Rejected
						: BackendCallOutcome.Unavailable;
				}

				var authorizationResponse = await responseMessage.Content.ReadFromJsonAsync(TwitchAuthSerializerContext.Default.AuthorizationResponse).ConfigureAwait(false);
				if (string.IsNullOrWhiteSpace(authorizationResponse.AccessToken) || string.IsNullOrWhiteSpace(authorizationResponse.RefreshToken))
				{
					_logger.Error("The CatCore auth back-end returned an incomplete token pair during refresh");
					return BackendCallOutcome.Unavailable;
				}

				if (!string.Equals(Credentials.RefreshToken, refreshTokenBeforeRefresh, StringComparison.Ordinal))
				{
					// A login completed while this refresh was in flight. The newer token pair wins;
					// applying the stale refresh result would log the user out again.
					_logger.Verbose("Ignoring a stale token refresh result because the credentials changed while refreshing");
					return BackendCallOutcome.Success;
				}

				var refreshedCredentials = new TwitchCredentials(authorizationResponse, TwitchTokenSources.AuthBackend);
				// Persist the rotated token pair immediately. Twitch may invalidate the previous refresh token
				// as soon as it issues a new one, so losing the new pair would permanently log the user out.
				if (!UpdateCredentials(refreshedCredentials))
				{
					_logger.Error("Could not persist the refreshed Twitch credentials. The new token pair only exists in memory and will be lost on restart");
					return BackendCallOutcome.Unavailable;
				}

				// Validation is best-effort: a temporary /validate failure must not wipe or reject
				// the freshly rotated token pair.
				await ValidateAccessTokenCore(refreshedCredentials, resetDataOnFailure: false).ConfigureAwait(false);
				return BackendCallOutcome.Success;
			}
			catch (HttpRequestException ex)
			{
				_logger.Warning(ex, "An error occurred while trying to refresh tokens through the CatCore auth back-end");
				return BackendCallOutcome.Unavailable;
			}
			catch (OperationCanceledException ex)
			{
				_logger.Warning(ex, "Refreshing tokens through the CatCore auth back-end timed out");
				return BackendCallOutcome.Unavailable;
			}
			catch (JsonException ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to deserialize the refresh tokens body from the CatCore auth back-end");
				return BackendCallOutcome.Unavailable;
			}
		}

		/// <summary>
		/// Refreshes a token pair directly against Twitch without a client secret. This only works
		/// for public client credentials, i.e. credentials obtained through the device code grant
		/// flow. It is the offline fallback for token rotation when the CatCore auth back-end is
		/// unreachable.
		/// </summary>
		private async Task<bool> RefreshTokensLocally()
		{
			var refreshToken = Credentials.RefreshToken;
			if (string.IsNullOrWhiteSpace(refreshToken))
			{
				return false;
			}

			_logger.Information("Refreshing Twitch tokens locally without a client secret");
			try
			{
				using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{TWITCH_AUTH_BASEURL}token")
				{
					Content = new FormUrlEncodedContent(new Dictionary<string, string>
					{
						["client_id"] = _constants.TwitchClientId,
						["grant_type"] = "refresh_token",
						["refresh_token"] = refreshToken!
					})
				};
				using var responseMessage = await _twitchAuthClient.SendAsync(requestMessage).ConfigureAwait(false);

				if (!responseMessage.IsSuccessStatusCode)
				{
					_logger.Warning("The local Twitch token refresh was rejected with status code {StatusCode}. This token pair can only be refreshed through the CatCore auth back-end", responseMessage.StatusCode);
					return false;
				}

				var authorizationResponse = await responseMessage.Content.ReadFromJsonAsync(TwitchAuthSerializerContext.Default.AuthorizationResponse).ConfigureAwait(false);
				if (string.IsNullOrWhiteSpace(authorizationResponse.AccessToken) || string.IsNullOrWhiteSpace(authorizationResponse.RefreshToken))
				{
					_logger.Error("Twitch returned an incomplete token pair during the local refresh");
					return false;
				}

				if (!string.Equals(Credentials.RefreshToken, refreshToken, StringComparison.Ordinal))
				{
					// A login completed while this refresh was in flight; keep the newer token pair.
					_logger.Verbose("Ignoring a stale local token refresh result because the credentials changed while refreshing");
					return true;
				}

				var refreshedCredentials = new TwitchCredentials(authorizationResponse, TwitchTokenSources.LocalDeviceCode);
				if (!UpdateCredentials(refreshedCredentials))
				{
					_logger.Error("Could not persist the locally refreshed Twitch credentials. The new token pair only exists in memory and will be lost on restart");
					return false;
				}

				await ValidateAccessTokenCore(refreshedCredentials, resetDataOnFailure: false).ConfigureAwait(false);
				return true;
			}
			catch (HttpRequestException ex)
			{
				_logger.Warning(ex, "An error occurred while trying to refresh the Twitch tokens locally");
				return false;
			}
			catch (TaskCanceledException ex)
			{
				_logger.Warning(ex, "The local Twitch token refresh timed out or was canceled");
				return false;
			}
			catch (JsonException ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to deserialize the local Twitch token refresh response");
				return false;
			}
		}

		public async Task<bool> RevokeTokens()
		{
			CancelPendingDeviceAuthorization();
			ClearPendingAuthorizationState();

			var refreshToken = Credentials.RefreshToken;
			if (string.IsNullOrWhiteSpace(refreshToken))
			{
				return false;
			}

			try
			{
				using var responseMessage = await _exceptionRetryPolicy
					.ExecuteAsync(async () =>
					{
						using var content = new FormUrlEncodedContent(new Dictionary<string, string>
						{
							["client_id"] = _constants.TwitchClientId,
							["token"] = refreshToken!
						});
						return await _twitchAuthClient.PostAsync($"{TWITCH_AUTH_BASEURL}revoke", content).ConfigureAwait(false);
					})
					.ConfigureAwait(false);

				if (!responseMessage.IsSuccessStatusCode)
				{
					_logger.Warning("Twitch token revocation failed with status code {StatusCode}; the token may still be valid on Twitch's side", responseMessage.StatusCode);
				}

				if (!UpdateCredentials(TwitchCredentials.Empty()))
				{
					_logger.Error("Could not persist the local logout. The credentials may reappear after a restart");
				}

				_loggedInUser = null;
				_scopeMismatchDetected = false;
				CancelForcedRefresh();

				return responseMessage.IsSuccessStatusCode;
			}
			catch (JsonException ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to deserialize the revoke tokens body");

				return false;
			}
		}

		private void ClearPendingAuthorizationState()
		{
			lock (_authorizationStateLock)
			{
				_pendingAuthorizationState = null;
			}
		}

		private enum BackendCallOutcome
		{
			Success,
			Rejected,
			Unavailable,
			Unknown
		}

		private enum DeviceCodePollOutcome
		{
			Authorized,
			AuthorizationPending,
			SlowDown,
			TransientFailure,
			Failed
		}

		private readonly struct DeviceCodePollResult
		{
			public DeviceCodePollOutcome Outcome { get; }
			public AuthorizationResponse? Tokens { get; }
			public string? ErrorMessage { get; }

			private DeviceCodePollResult(DeviceCodePollOutcome outcome, AuthorizationResponse? tokens, string? errorMessage)
			{
				Outcome = outcome;
				Tokens = tokens;
				ErrorMessage = errorMessage;
			}

			public static DeviceCodePollResult Authorized(AuthorizationResponse tokens) =>
				new(DeviceCodePollOutcome.Authorized, tokens, null);

			public static DeviceCodePollResult Pending() =>
				new(DeviceCodePollOutcome.AuthorizationPending, null, null);

			public static DeviceCodePollResult SlowDown() =>
				new(DeviceCodePollOutcome.SlowDown, null, null);

			public static DeviceCodePollResult TransientFailure(string errorMessage) =>
				new(DeviceCodePollOutcome.TransientFailure, null, errorMessage);

			public static DeviceCodePollResult Failed(string errorMessage) =>
				new(DeviceCodePollOutcome.Failed, null, errorMessage);
		}

		private sealed class PendingDeviceAuthorizationState
		{
			public string DeviceCode { get; }
			public string UserCode { get; }
			public string VerificationUri { get; }
			public DateTimeOffset ExpiresAt { get; }
			public int IntervalSeconds { get; }

			public PendingDeviceAuthorizationState(string deviceCode, string userCode, string verificationUri, DateTimeOffset expiresAt, int intervalSeconds)
			{
				DeviceCode = deviceCode;
				UserCode = userCode;
				VerificationUri = verificationUri;
				ExpiresAt = expiresAt;
				IntervalSeconds = intervalSeconds;
			}

			public TwitchDeviceAuthorizationInfo ToInfo() => new(UserCode, VerificationUri, ExpiresAt);
		}
	}
}
