using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
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

		public Task<Stream?> GetTokensByAuthorizationCode(string authorizationCode, string redirectUrl)
		{
			return PostFormExpectStreamInternal(new Dictionary<string, string>
			{
				["client_id"] = Environment.GetEnvironmentVariable("Twitch_CatCore_ClientId") ?? string.Empty,
				["client_secret"] = Environment.GetEnvironmentVariable("Twitch_CatCore_ClientSecret") ?? string.Empty,
				["code"] = authorizationCode,
				["grant_type"] = "authorization_code",
				["redirect_uri"] = redirectUrl
			});
		}

		public Task<Stream?> RefreshTokens(string refreshToken)
		{
			if (string.IsNullOrWhiteSpace(refreshToken))
			{
				return Task.FromResult<Stream?>(null);
			}

			return PostFormExpectStreamInternal(new Dictionary<string, string>
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
		private async Task<Stream?> PostFormExpectStreamInternal(Dictionary<string, string> parameters)
		{
			using var content = new FormUrlEncodedContent(parameters);
			using var responseMessage = await _authClient
				.PostAsync("token", content)
				.ConfigureAwait(false);

			if (!responseMessage.IsSuccessStatusCode)
			{
				return null;
			}

			var memoryStream = new MemoryStream();
			await responseMessage.Content.CopyToAsync(memoryStream).ConfigureAwait(false);
			memoryStream.Seek(0, SeekOrigin.Begin);

			return memoryStream;
		}
	}
}
