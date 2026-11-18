using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Models.Api.Requests;
using CatCore.Models.Api.Responses;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services
{
	internal sealed class KittenApiService : IKittenApiService
	{
		/// <summary>Authority the internal portal is served on, e.g. http://localhost:8338.</summary>
		private static readonly string PortalAuthority = new Uri(ConstantsBase.InternalApiServerUri).GetLeftPart(UriPartial.Authority);

		private readonly ILogger _logger;
		private readonly IKittenSettingsService _settingsService;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly ITwitchChannelManagementService _twitchChannelManagementService;
		private readonly ITwitchHelixApiService _helixApiService;
		private readonly Version _libraryVersion;

		private HttpListener? _listener;
		private string? _webSitePage;
		private string? _csrfToken;

		public KittenApiService(ILogger logger, IKittenSettingsService settingsService, ITwitchAuthService twitchAuthService, ITwitchChannelManagementService twitchChannelManagementService,
			ITwitchHelixApiService helixApiService, Version libraryVersion)
		{
			_logger = logger;
			_settingsService = settingsService;
			_twitchAuthService = twitchAuthService;
			_twitchChannelManagementService = twitchChannelManagementService;
			_helixApiService = helixApiService;
			_libraryVersion = libraryVersion;
		}

		public async Task Initialize()
		{
			if (_webSitePage == null)
			{
				// The CSRF token is generated once per CatCore startup and embedded in the portal
				// page; state-changing endpoints only accept requests that echo it back.
				_csrfToken = PortalRequestGuard.CreateCsrfToken();
				_webSitePage = await BuildWebSitePage(_libraryVersion, _csrfToken).ConfigureAwait(false);
			}

			_logger.Information("Purring up internal webserver");

			_listener = new HttpListener {Prefixes = {ConstantsBase.InternalApiServerUri}};

			try
			{
				_listener.Start();

				_ = Task.Run(async () =>
				{
					while (true)
					{
						try
						{
							var context = await _listener.GetContextAsync().ConfigureAwait(false);
							await HandleContext(context).ConfigureAwait(false);
						}
						catch (Exception e)
						{
							_logger.Error(e, "An error occured while trying to handle an incoming request");
						}
					}

					// ReSharper disable once FunctionNeverReturns
				});

				_logger.Information("Internal webserver has been purred up");
			}
			catch (Exception e)
			{
				_logger.Error(e, "The portal webpage is most likely not available because an error occurred while trying to purr up the internal webserver");
			}
		}

		/// <summary>
		/// Builds the portal page with the library version and the given CSRF token filled in.
		/// Internal so the placeholder replacement can be unit tested.
		/// </summary>
		internal static async Task<string> BuildWebSitePage(Version libraryVersion, string csrfToken)
		{
			using var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(CatCore)}.Resources.index.html")!);
			var pageBuilder = new StringBuilder(await reader.ReadToEndAsync().ConfigureAwait(false));
			pageBuilder.Replace("{libVersion}", libraryVersion.ToString(3));
			pageBuilder.Replace(PortalRequestGuard.CsrfTokenPlaceholder, csrfToken);
			return pageBuilder.ToString();
		}

		private async Task HandleContext(HttpListenerContext ctx)
		{
			var request = ctx.Request;
			using var response = ctx.Response;
			try
			{
				var requestHandled = false;

#if DEBUG
				_logger.Debug("New incoming request {Method} {RequestUrl}", request.HttpMethod, request.Url.AbsoluteUri);
#endif

				if (request.Url.AbsolutePath.StartsWith("/api"))
				{
					requestHandled = await HandleApiRequest(request, response).ConfigureAwait(false);
				}
				else if (request.Url.AbsolutePath == "/" && request.HttpMethod == "GET")
				{
					var data = Encoding.UTF8.GetBytes(_webSitePage!);
					response.ContentEncoding = Encoding.UTF8;
					response.ContentLength64 = data.Length;
					response.ContentType = "text/html";
					await response.OutputStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);

					requestHandled = true;
				}

				if (!requestHandled)
				{
					_logger.Warning("{Method} {RequestUrl} went unhandled", request.HttpMethod, request.Url.AbsoluteUri);

					response.StatusCode = 404;
				}
#if DEBUG
				else
				{
					_logger.Debug("Successfully handled {Method} {RequestUrl}", request.HttpMethod, request.Url.AbsoluteUri);
				}
#endif
			}
			catch (Exception e)
			{
				_logger.Error(e, "Something went wrong while trying to handle an incoming request");
				response.StatusCode = 500;
			}
		}

		private Task<bool> HandleApiRequest(HttpListenerRequest request, HttpListenerResponse response)
		{
			return request.Url.Segments.ElementAtOrDefault(2) switch
			{
				"twitch/" => HandleTwitchApiRequests(request, response),
				"global/" => HandleGlobalApiRequest(request, response),
				_ => Task.FromResult(false)
			};
		}

		// ReSharper disable once CognitiveComplexity
		private async Task<bool> HandleTwitchApiRequests(HttpListenerRequest request, HttpListenerResponse response)
		{
			var segment = request.Url.Segments.ElementAtOrDefault(3);
			switch (segment)
			{
				case "login":
				case "logout":
					return await HandleTwitchStateChangingRequest(segment, request, response).ConfigureAwait(false);
				case "authcode_callback" when request.HttpMethod == "GET":
					string? code = null;
					string? state = null;
					var queryString = request.Url.Query;
					if (!string.IsNullOrEmpty(queryString))
					{
						foreach (var parameterPair in queryString.Substring(1).Split(new[] {'&'}, StringSplitOptions.RemoveEmptyEntries))
						{
							var kvp = parameterPair.Split('=');
							if (kvp.Length < 2)
							{
								continue;
							}

							switch (kvp[0])
							{
								case "code":
									code = kvp[1];
									break;
								case "state":
									state = kvp[1];
									break;
							}
						}
					}

					string? postAuthorizationRedirectUri = null;
					if (code != null)
					{
						postAuthorizationRedirectUri = await _twitchAuthService
							.GetTokensByAuthorizationCode(code, request.Url.GetLeftPart(UriPartial.Path), state)
							.ConfigureAwait(false);
					}

					response.Redirect(postAuthorizationRedirectUri ?? request.Url.GetLeftPart(UriPartial.Authority));

					return true;
				case "state" when request.HttpMethod == "GET":
					response.ContentEncoding = Encoding.UTF8;
					response.ContentType = "application/json";

					var pendingDeviceAuthorization = _twitchAuthService.PendingDeviceAuthorization;
					// Do not validate or refresh the old token pair while a device login is pending:
					// the portal polls this endpoint every few seconds and must not trigger a
					// validate + refresh cycle (including the back-end timeout) on every poll.
					var loggedInUserInfo = pendingDeviceAuthorization != null
						? _twitchAuthService.FetchLoggedInUserInfo()
						: await _twitchAuthService.FetchLoggedInUserInfoWithRefresh().ConfigureAwait(false);
					var userInfos = loggedInUserInfo != null
						? await _twitchChannelManagementService.GetAllChannelsEnriched().ConfigureAwait(false)
						: null;

					await JsonSerializer
						.SerializeAsync(response.OutputStream, new TwitchStateResponseDto(_twitchAuthService.TokenIsValid, loggedInUserInfo, userInfos, _settingsService.Config.TwitchConfig,
							pendingDeviceAuthorization, _twitchAuthService.LocalAuthorizationError))
						.ConfigureAwait(false);

					return true;
				case "state" when request.HttpMethod == "POST":
					var twitchStateRequestDto = await JsonSerializer.DeserializeAsync<TwitchStateRequestDto>(request.InputStream).ConfigureAwait(false);
					using (_settingsService.ChangeTransaction())
					{
						var twitchConfig = _settingsService.Config.TwitchConfig;
						if (_twitchAuthService.TokenIsValid)
						{
							_twitchChannelManagementService.UpdateChannels(twitchStateRequestDto.SelfEnabled, twitchStateRequestDto.AdditionalChannelsData);
						}

						twitchConfig.ParseBttvEmotes = twitchStateRequestDto.ParseBttvEmotes;
						twitchConfig.ParseFfzEmotes = twitchStateRequestDto.ParseFfzEmotes;
						twitchConfig.ParseTwitchEmotes = twitchStateRequestDto.ParseTwitchEmotes;
						twitchConfig.ParseCheermotes = twitchStateRequestDto.ParseCheermotes;
					}

					return true;
				case "channels" when request.HttpMethod == "GET":
					var query = request.QueryString["query"];
					var directChannelNameSearch = await _helixApiService.FetchUserInfo(loginNames: new []{query});
					var searchQueryChannels = await _helixApiService.SearchChannels(query).ConfigureAwait(false);

					var channelQueryData = new List<TwitchChannelQueryData>();
					if (directChannelNameSearch != null && directChannelNameSearch.Value.Data.Any())
					{
						channelQueryData.Add(new TwitchChannelQueryData(directChannelNameSearch.Value.Data.First()));
					}

					if (searchQueryChannels != null)
					{
						channelQueryData.AddRange(searchQueryChannels.Value.Data
							.Select(channelData => new TwitchChannelQueryData(channelData))
							.Except(channelQueryData)
							.OrderBy(x => x.DisplayName.Length)
							.ThenBy(x => x.DisplayName));
					}

					response.ContentEncoding = Encoding.UTF8;
					response.ContentType = "application/json";
					await JsonSerializer.SerializeAsync(response.OutputStream, channelQueryData).ConfigureAwait(false);

					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Handles the state-changing Twitch portal endpoints (login and logout). Both change the
		/// authorization state, so they only accept POST requests that present the per-startup CSRF
		/// token of the portal, mirroring the defense strategy of the OAuth state validation.
		/// </summary>
		private async Task<bool> HandleTwitchStateChangingRequest(string? segment, HttpListenerRequest request, HttpListenerResponse response)
		{
			var rejectionStatusCode = PortalRequestGuard.EvaluateStateChangingRequest(segment, request.HttpMethod, _csrfToken,
				request.Headers[PortalRequestGuard.CsrfTokenHeaderName], request.Headers["Origin"], request.Headers["Referer"],
				request.Url.GetLeftPart(UriPartial.Authority), PortalAuthority);

			if (rejectionStatusCode == 405)
			{
				_logger.Warning("Rejected {Method} {RequestPath}: the Twitch login and logout endpoints only accept POST requests",
					request.HttpMethod, request.Url.AbsolutePath);
				response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
				response.AddHeader("Allow", "POST");
				return true;
			}

			if (rejectionStatusCode == 403)
			{
				// Never log header values here: the CSRF token must not end up in log files.
				_logger.Warning("Rejected {Method} {RequestPath}: the request did not present the CSRF token of the local portal or did not originate from it",
					request.HttpMethod, request.Url.AbsolutePath);
				response.StatusCode = (int)HttpStatusCode.Forbidden;
				return true;
			}

			if (segment == "login")
			{
				var loginUrl = await _twitchAuthService
					.GetLoginUrl($"{request.Url.GetLeftPart(UriPartial.Authority)}/api/twitch/authcode_callback")
					.ConfigureAwait(false);

				// Answer with the login URL as JSON instead of a redirect: the portal sends the CSRF
				// token in a custom header through fetch, and a cross-origin redirect to Twitch could
				// not be followed from JavaScript.
				response.ContentEncoding = Encoding.UTF8;
				response.ContentType = "application/json";
				await JsonSerializer.SerializeAsync(response.OutputStream, new TwitchLoginResponseDto(loginUrl)).ConfigureAwait(false);
				return true;
			}

			var revoked = await _twitchAuthService.RevokeTokens().ConfigureAwait(false);

			response.Redirect(revoked
				? request.Url.GetLeftPart(UriPartial.Authority)
				: $"{request.Url.GetLeftPart(UriPartial.Authority)}?twitchLogoutError=revoke_failed");

			return true;
		}

		private async Task<bool> HandleGlobalApiRequest(HttpListenerRequest request, HttpListenerResponse response)
		{
			switch (request.Url.Segments.ElementAtOrDefault(3))
			{
				case "state" when request.HttpMethod == "GET":
					response.ContentEncoding = Encoding.UTF8;
					response.ContentType = "application/json";
					await JsonSerializer.SerializeAsync(response.OutputStream, new GlobalStateResponseDto(_settingsService.Config.GlobalConfig)).ConfigureAwait(false);

					return true;
				case "state" when request.HttpMethod == "POST":
					var globalStateRequestDto = await JsonSerializer.DeserializeAsync<GlobalStateRequestDto>(request.InputStream).ConfigureAwait(false);
					using (_settingsService.ChangeTransaction())
					{
						var globalConfig = _settingsService.Config.GlobalConfig;
						globalConfig.LaunchInternalApiOnStartup = globalStateRequestDto.LaunchInternalApiOnStartup;
						globalConfig.LaunchWebPortalOnStartup = globalStateRequestDto.LaunchWebPortalOnStartup;
						globalConfig.HandleEmojis = globalStateRequestDto.ParseEmojis;
					}

					return true;
				default:
					return false;
			}
		}
	}
}