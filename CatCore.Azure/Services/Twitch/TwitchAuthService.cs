using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CatCore.Azure.Services.Twitch
{
	internal class TwitchAuthService
	{
		private readonly HttpClient _authClient;

		public TwitchAuthService(IHttpClientFactory httpClientFactory)
		{
			_authClient = httpClientFactory.CreateClient();
			_authClient.DefaultRequestVersion = HttpVersion.Version20;
			_authClient.BaseAddress = new Uri("https://id.twitch.tv/oauth2/", UriKind.Absolute);
			_authClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"{nameof(CatCore)}/1.0.0");
		}

		public Task<TwitchTokenResponse> GetTokensByAuthorizationCode(string authorizationCode, string redirectUrl)
		{
			return PostFormExpectTokenResponse(new Dictionary<string, string>
			{
				["client_id"] = Environment.GetEnvironmentVariable("Twitch_CatCore_ClientId") ?? string.Empty,
				["client_secret"] = Environment.GetEnvironmentVariable("Twitch_CatCore_ClientSecret") ?? string.Empty,
				["code"] = authorizationCode,
				["grant_type"] = "authorization_code",
				["redirect_uri"] = redirectUrl
			});
		}

		public Task<TwitchTokenResponse> RefreshTokens(string refreshToken)
		{
			if (string.IsNullOrWhiteSpace(refreshToken))
			{
				return Task.FromResult(TwitchTokenResponse.Failure(HttpStatusCode.BadRequest, "missing refresh_token"));
			}

			return PostFormExpectTokenResponse(new Dictionary<string, string>
			{
				["client_id"] = Environment.GetEnvironmentVariable("Twitch_CatCore_ClientId") ?? string.Empty,
				["client_secret"] = Environment.GetEnvironmentVariable("Twitch_CatCore_ClientSecret") ?? string.Empty,
				["grant_type"] = "refresh_token",
				["refresh_token"] = refreshToken
			});
		}

		/// <summary>
		/// Sends the token request with the client secret in the form body instead of the URL, so
		/// the secret cannot end up in outbound request logs or dependency telemetry.
		/// </summary>
		private async Task<TwitchTokenResponse> PostFormExpectTokenResponse(Dictionary<string, string> parameters)
		{
			using var content = new FormUrlEncodedContent(parameters);
			using var responseMessage = await _authClient
				.PostAsync("token", content)
				.ConfigureAwait(false);

			var responseBody = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
			if (!responseMessage.IsSuccessStatusCode)
			{
				return TwitchTokenResponse.Failure(responseMessage.StatusCode, ExtractErrorMessage(responseBody));
			}

			var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(responseBody));
			return TwitchTokenResponse.Success(memoryStream);
		}

		private static string? ExtractErrorMessage(string responseBody)
		{
			if (string.IsNullOrWhiteSpace(responseBody))
			{
				return null;
			}

			try
			{
				using var document = JsonDocument.Parse(responseBody);
				if (document.RootElement.TryGetProperty("message", out var message))
				{
					return message.GetString();
				}

				if (document.RootElement.TryGetProperty("error", out var error))
				{
					return error.GetString();
				}
			}
			catch (JsonException)
			{
				// Non-JSON error page; keep a short excerpt for diagnostics only.
			}

			return responseBody.Length <= 200 ? responseBody : null;
		}
	}
}
