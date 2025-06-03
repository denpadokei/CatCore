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
		private const string TWITCH_AUTH_BASEURL = "https://id.twitch.tv/oauth2/";

		private readonly HttpClient _authClient;

		public TwitchAuthService(IHttpClientFactory httpClientFactory)
		{
			_authClient = httpClientFactory.CreateClient();
			_authClient.DefaultRequestVersion = HttpVersion.Version20;
			_authClient.BaseAddress = new Uri(TWITCH_AUTH_BASEURL, UriKind.Absolute);
			_authClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"{nameof(CatCore)}/1.0.0");
		}

		public async Task<Stream?> GetTokensByAuthorizationCode(string authorizationCode, string redirectUrl)
		{
			var clientId = Environment.GetEnvironmentVariable("Twitch_CatCore_ClientId");
			var clientSecret = Environment.GetEnvironmentVariable("Twitch_CatCore_ClientSecret");

			var requestUri = $"{TWITCH_AUTH_BASEURL}token";

			var content = new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("client_id", clientId ?? string.Empty),
				new KeyValuePair<string, string>("client_secret", clientSecret ?? string.Empty),
				new KeyValuePair<string, string>("code", authorizationCode),
				new KeyValuePair<string, string>("grant_type", "authorization_code"),
				new KeyValuePair<string, string>("redirect_uri", redirectUrl)
			});

			using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
			{
				Content = content
			};

			using var responseMessage = await _authClient
				.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
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

		public async Task<Stream?> GetAppTokens()
		{
			var requestUri = $"{TWITCH_AUTH_BASEURL}token";

			var clientId = Environment.GetEnvironmentVariable("Twitch_CatCore_ClientId");
			var clientSecret = Environment.GetEnvironmentVariable("Twitch_CatCore_ClientSecret");

			var content = new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("client_id", clientId ?? string.Empty),
				new KeyValuePair<string, string>("client_secret", clientSecret ?? string.Empty),
				new KeyValuePair<string, string>("grant_type", "client_credentials")
			});

			using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
			{
				Content = content
			};

			using var responseMessage = await _authClient
				.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
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

		public async Task<Stream?> RefreshTokens(string refreshToken)
		{
			if (string.IsNullOrWhiteSpace(refreshToken))
			{
				return null;
			}

			var clientId = Environment.GetEnvironmentVariable("Twitch_CatCore_ClientId");
			var clientSecret = Environment.GetEnvironmentVariable("Twitch_CatCore_ClientSecret");
			var requestUri = $"{TWITCH_AUTH_BASEURL}token";

			var content = new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("client_id", clientId ?? string.Empty),
				new KeyValuePair<string, string>("client_secret", clientSecret ?? string.Empty),
				new KeyValuePair<string, string>("grant_type", "refresh_token"),
				new KeyValuePair<string, string>("refresh_token", refreshToken)
			});

			using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
			{
				Content = content
			};

			using var responseMessage = await _authClient
				.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
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