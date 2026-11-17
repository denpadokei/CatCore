using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CatCore;
using CatCore.Models.Api.Responses;
using CatCore.Models.Config;
using CatCore.Models.Credentials;
using CatCore.Models.Twitch.OAuth;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch;
using FluentAssertions;
using Serilog;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Tests for the local authorization fallback that keeps CatCore usable when the CatCore auth
	/// back-end is unreachable. The fallback uses Twitch's device code grant flow, which is a
	/// public client flow and must not send a client secret anywhere.
	/// </summary>
	public class TwitchAuthLocalFallbackTests : IDisposable
	{
		private const string AUTH_BACKEND_HOST = "catcore.test";
		private const string DEVICE_VERIFICATION_URI = "https://www.twitch.tv/activate?public=true&device-code=ABCD1234";

		private static readonly string[] RequiredScopes =
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

		private readonly string _dataPath = Path.Combine(Path.GetTempPath(), "CatCoreTests-" + Guid.NewGuid().ToString("N"));

		[Fact]
		public void StateDtoExposesTheLocalAuthorizationContractUsedByThePortal()
		{
			var dto = new TwitchStateResponseDto(false, null, null, new TwitchConfig(),
				new TwitchDeviceAuthorizationInfo("ABCD1234", DEVICE_VERIFICATION_URI, DateTimeOffset.Now.AddMinutes(10)));

			using var document = JsonDocument.Parse(JsonSerializer.Serialize(dto));
			var root = document.RootElement;

			root.GetProperty("LocalAuthorizationPending").GetBoolean().Should().BeTrue();
			root.GetProperty("LocalAuthorizationUserCode").GetString().Should().Be("ABCD1234");
			root.GetProperty("LocalAuthorizationVerificationUri").GetString().Should().Be(DEVICE_VERIFICATION_URI);
			root.TryGetProperty("LocalAuthorizationExpiresAt", out _).Should().BeTrue();
		}

		[Fact]
		public async Task GetLoginUrlFallsBackToTheLocalDeviceCodeFlowWhenTheAuthBackendIsUnreachable()
		{
			var handler = new StubHttpMessageHandler((request, _) =>
			{
				if (IsAuthBackendRequest(request))
				{
					throw new HttpRequestException("The CatCore auth back-end is down");
				}

				if (request.RequestUri!.AbsolutePath.EndsWith("/device", StringComparison.Ordinal))
				{
					return JsonResponse(DeviceAuthorizationJson());
				}

				return JsonResponse("{}");
			});
			var authService = CreateAuthService(handler);

			try
			{
				var loginUrl = await authService.GetLoginUrl("https://localhost/callback");

				loginUrl.Should().Be(DEVICE_VERIFICATION_URI);
				authService.PendingDeviceAuthorization.Should().NotBeNull();
				authService.PendingDeviceAuthorization!.Value.UserCode.Should().Be("ABCD1234");
				authService.PendingDeviceAuthorization!.Value.VerificationUri.Should().Be(DEVICE_VERIFICATION_URI);

				handler.Requests.Should().Contain(request => request.RequestUri!.AbsolutePath.EndsWith("/device", StringComparison.Ordinal));
				handler.Requests.Should().NotContain(request => request.RequestUri!.AbsoluteUri.Contains("client_secret", StringComparison.Ordinal));
				handler.RequestBodies.Should().NotContain(body => body.Contains("client_secret", StringComparison.Ordinal));
			}
			finally
			{
				await authService.RevokeTokens();
			}
		}

		[Fact]
		public async Task GetLoginUrlUsesTheAuthorizationCodeFlowWhenTheAuthBackendIsReachable()
		{
			var handler = new StubHttpMessageHandler((request, _) =>
			{
				if (IsAuthBackendRequest(request))
				{
					// The probe calls the authorize endpoint without parameters, which the function
					// answers with a 400. That still proves the back-end is reachable.
					return new HttpResponseMessage(HttpStatusCode.BadRequest);
				}

				throw new InvalidOperationException("The local device code flow must not start when the back-end is reachable");
			});
			var authService = CreateAuthService(handler);

			var loginUrl = await authService.GetLoginUrl("https://localhost/callback");

			loginUrl.Should().StartWith("https://id.twitch.tv/oauth2/authorize");
			loginUrl.Should().Contain("response_type=code");
			loginUrl.Should().Contain("client_id=test-client-id");
			authService.PendingDeviceAuthorization.Should().BeNull();

			handler.Requests.Single(request => IsAuthBackendRequest(request)).RequestUri!.AbsolutePath.Should().Be("/auth/api/twitch/authorize");
		}

		[Fact]
		public async Task AuthorizationCodeExchangeFallsBackToTheLocalDeviceCodeFlowWhenTheAuthBackendIsUnreachable()
		{
			var handler = new StubHttpMessageHandler((request, _) =>
			{
				if (IsAuthBackendRequest(request))
				{
					throw new HttpRequestException("The CatCore auth back-end is down");
				}

				if (request.RequestUri!.AbsolutePath.EndsWith("/device", StringComparison.Ordinal))
				{
					return JsonResponse(DeviceAuthorizationJson());
				}

				return JsonResponse("{}");
			});
			var authService = CreateAuthService(handler);

			try
			{
				var authorizationUrl = authService.AuthorizationUrl("https://localhost/api/twitch/authcode_callback");
				var state = ExtractState(authorizationUrl);
				var redirectUri = await authService.GetTokensByAuthorizationCode("code-1", "https://localhost/api/twitch/authcode_callback", state);

				redirectUri.Should().Be(DEVICE_VERIFICATION_URI);
				authService.PendingDeviceAuthorization.Should().NotBeNull();
			}
			finally
			{
				await authService.RevokeTokens();
			}
		}

		[Fact]
		public async Task AuthorizationCallbackRejectsAMissingOrMismatchedState()
		{
			var handler = new StubHttpMessageHandler((_, _) => throw new InvalidOperationException("No HTTP request may be made when the OAuth state is invalid"));
			var authService = CreateAuthService(handler);

			var authorizationUrl = authService.AuthorizationUrl("https://localhost/callback");
			var state = ExtractState(authorizationUrl);

			var missingStateResult = await authService.GetTokensByAuthorizationCode("code-1", "https://localhost/callback", null);

			authorizationUrl = authService.AuthorizationUrl("https://localhost/callback");
			var mismatchedStateResult = await authService.GetTokensByAuthorizationCode("code-1", "https://localhost/callback", state + "0");

			missingStateResult.Should().BeNull();
			mismatchedStateResult.Should().BeNull();
			handler.Requests.Should().BeEmpty();
		}

		[Fact]
		public async Task AuthorizationCallbackAcceptsTheMatchingStateAndUsesAFormBody()
		{
			var handler = new StubHttpMessageHandler((request, _) =>
			{
				if (IsAuthBackendRequest(request))
				{
					return JsonResponse(TokenJson("backend-access", "backend-refresh"));
				}

				if (request.RequestUri!.AbsolutePath.EndsWith("/validate", StringComparison.Ordinal))
				{
					return JsonResponse(ValidationJson());
				}

				return JsonResponse("{}");
			});
			var authService = CreateAuthService(handler);

			var authorizationUrl = authService.AuthorizationUrl("https://localhost/callback");
			var state = ExtractState(authorizationUrl);

			var redirectUri = await authService.GetTokensByAuthorizationCode("code-1", "https://localhost/callback", state);

			redirectUri.Should().BeNull();
			authService.AccessToken.Should().Be("backend-access");

			var backendRequest = handler.Requests.Single(request => IsAuthBackendRequest(request));
			backendRequest.RequestUri!.AbsolutePath.Should().Be("/auth/api/twitch/authorize");
			backendRequest.RequestUri.Query.Should().NotContain("code=");

			var backendRequestBody = handler.RequestBodies[handler.Requests.IndexOf(backendRequest)];
			backendRequestBody.Should().Contain("code=code-1");
			backendRequestBody.Should().Contain("redirect_uri=https%3A%2F%2Flocalhost%2Fcallback");
		}

		[Fact]
		public async Task BackendRefreshUsesAFormBodyAndNeverPutsTheRefreshTokenInTheUrl()
		{
			SeedCredentials("old-access", "old-refresh", DateTimeOffset.Now.AddMinutes(-10), TwitchTokenSources.AuthBackend);

			var handler = new StubHttpMessageHandler((request, _) =>
			{
				if (IsAuthBackendRequest(request))
				{
					return JsonResponse(TokenJson("backend-access", "backend-refresh"));
				}

				if (request.RequestUri!.AbsolutePath.EndsWith("/validate", StringComparison.Ordinal))
				{
					return JsonResponse(ValidationJson());
				}

				return new HttpResponseMessage(HttpStatusCode.OK);
			});
			var authService = CreateAuthService(handler);

			var refreshed = await authService.RefreshTokens(force: true);

			refreshed.Should().BeTrue();
			authService.AccessToken.Should().Be("backend-access");

			var backendRequest = handler.Requests.Single(request => IsAuthBackendRequest(request));
			backendRequest.RequestUri!.AbsolutePath.Should().Be("/auth/api/twitch/refresh");
			backendRequest.RequestUri.Query.Should().BeEmpty();

			var backendRequestBody = handler.RequestBodies[handler.Requests.IndexOf(backendRequest)];
			backendRequestBody.Should().Contain("refresh_token=old-refresh");
			backendRequestBody.Should().NotContain("client_secret");

			var storedCredentials = LoadStoredCredentials();
			storedCredentials.RefreshToken.Should().Be("backend-refresh");
			storedCredentials.TokenSource.Should().Be(TwitchTokenSources.AuthBackend);
		}

		[Fact]
		public async Task BackendRefreshRetriesTransientTransportErrors()
		{
			SeedCredentials("old-access", "old-refresh", DateTimeOffset.Now.AddMinutes(-10), TwitchTokenSources.AuthBackend);

			var backendRefreshAttempts = 0;
			var handler = new StubHttpMessageHandler((request, _) =>
			{
				if (IsAuthBackendRequest(request))
				{
					backendRefreshAttempts++;
					if (backendRefreshAttempts == 1)
					{
						throw new HttpRequestException("transient transport error");
					}

					return JsonResponse(TokenJson("backend-access", "backend-refresh"));
				}

				if (request.RequestUri!.AbsolutePath.EndsWith("/validate", StringComparison.Ordinal))
				{
					return JsonResponse(ValidationJson());
				}

				return new HttpResponseMessage(HttpStatusCode.OK);
			});
			var authService = CreateAuthService(handler);

			var refreshed = await authService.RefreshTokens(force: true);

			refreshed.Should().BeTrue();
			backendRefreshAttempts.Should().Be(2);
			authService.AccessToken.Should().Be("backend-access");
		}

		[Fact]
		public async Task RevokeTokensUsesAFormBodyAndNeverPutsTheTokenInTheUrl()
		{
			SeedCredentials("old-access", "old-refresh", DateTimeOffset.Now.AddHours(1), TwitchTokenSources.LocalDeviceCode);

			var handler = new StubHttpMessageHandler((request, _) =>
				request.RequestUri!.AbsolutePath.EndsWith("/revoke", StringComparison.Ordinal)
					? new HttpResponseMessage(HttpStatusCode.OK)
					: new HttpResponseMessage(HttpStatusCode.OK));
			var authService = CreateAuthService(handler);

			var revoked = await authService.RevokeTokens();

			revoked.Should().BeTrue();

			var revokeRequest = handler.Requests.Single(request => request.RequestUri!.AbsolutePath.EndsWith("/revoke", StringComparison.Ordinal));
			revokeRequest.RequestUri!.Query.Should().BeEmpty();

			var revokeRequestBody = handler.RequestBodies[handler.Requests.IndexOf(revokeRequest)];
			revokeRequestBody.Should().Contain("client_id=test-client-id");
			revokeRequestBody.Should().Contain("token=old-refresh");
			revokeRequestBody.Should().NotContain("client_secret");
		}

		[Theory]
		[InlineData("https://evil.example/activate?public=true&device-code=ABCD1234")]
		[InlineData("javascript:alert(1)")]
		public async Task DeviceAuthorizationFallsBackToTheOfficialActivationUriForAnUntrustedVerificationUri(string verificationUri)
		{
			var handler = new StubHttpMessageHandler((request, _) =>
			{
				if (IsAuthBackendRequest(request))
				{
					throw new HttpRequestException("The CatCore auth back-end is down");
				}

				if (request.RequestUri!.AbsolutePath.EndsWith("/device", StringComparison.Ordinal))
				{
					return JsonResponse(DeviceAuthorizationJson(verificationUri));
				}

				return JsonResponse("{}");
			});
			var authService = CreateAuthService(handler);

			try
			{
				var loginUrl = await authService.GetLoginUrl("https://localhost/callback");

				loginUrl.Should().Be(DEVICE_VERIFICATION_URI);
				authService.PendingDeviceAuthorization.Should().NotBeNull();
				authService.PendingDeviceAuthorization!.Value.VerificationUri.Should().Be(DEVICE_VERIFICATION_URI);
			}
			finally
			{
				await authService.RevokeTokens();
			}
		}

		[Fact]
		public async Task RefreshTokensUsesTheLocalEndpointForLocallyObtainedTokensAndNeverSendsAClientSecret()
		{
			SeedCredentials("old-access", "old-refresh", DateTimeOffset.Now.AddMinutes(-10), TwitchTokenSources.LocalDeviceCode);

			var handler = new StubHttpMessageHandler((request, _) =>
			{
				if (IsAuthBackendRequest(request))
				{
					throw new InvalidOperationException("A locally obtained token must be refreshed locally, not through the auth back-end");
				}

				if (request.RequestUri!.AbsolutePath.EndsWith("/token", StringComparison.Ordinal))
				{
					return JsonResponse(TokenJson("new-access", "new-refresh"));
				}

				if (request.RequestUri.AbsolutePath.EndsWith("/validate", StringComparison.Ordinal))
				{
					return JsonResponse(ValidationJson());
				}

				return new HttpResponseMessage(HttpStatusCode.OK);
			});
			var authService = CreateAuthService(handler);

			var refreshed = await authService.RefreshTokens(force: true);

			refreshed.Should().BeTrue();
			authService.AccessToken.Should().Be("new-access");
			handler.Requests.Should().NotContain(request => IsAuthBackendRequest(request));

			var tokenRequest = handler.Requests.Single(request => request.RequestUri!.AbsolutePath.EndsWith("/token", StringComparison.Ordinal));
			var tokenRequestBody = handler.RequestBodies[handler.Requests.IndexOf(tokenRequest)];
			tokenRequestBody.Should().Contain("grant_type=refresh_token");
			tokenRequestBody.Should().Contain("client_id=test-client-id");
			tokenRequestBody.Should().Contain("refresh_token=old-refresh");
			tokenRequestBody.Should().NotContain("client_secret");

			var storedCredentials = LoadStoredCredentials();
			storedCredentials.RefreshToken.Should().Be("new-refresh");
			storedCredentials.TokenSource.Should().Be(TwitchTokenSources.LocalDeviceCode);
		}

		[Fact]
		public async Task RefreshTokensFallsBackToALocalRefreshWhenTheAuthBackendIsUnreachable()
		{
			// Tokens obtained by an older CatCore version have no token source, so CatCore first
			// tries the auth back-end and must then fall back to a local, secret-less refresh.
			SeedCredentials("old-access", "old-refresh", DateTimeOffset.Now.AddMinutes(-10), null);

			var handler = new StubHttpMessageHandler((request, _) =>
			{
				if (IsAuthBackendRequest(request))
				{
					throw new HttpRequestException("The CatCore auth back-end is down");
				}

				if (request.RequestUri!.AbsolutePath.EndsWith("/token", StringComparison.Ordinal))
				{
					return JsonResponse(TokenJson("new-access", "new-refresh"));
				}

				if (request.RequestUri.AbsolutePath.EndsWith("/validate", StringComparison.Ordinal))
				{
					return JsonResponse(ValidationJson());
				}

				return new HttpResponseMessage(HttpStatusCode.OK);
			});
			var authService = CreateAuthService(handler);

			var refreshed = await authService.RefreshTokens(force: true);

			refreshed.Should().BeTrue();
			authService.AccessToken.Should().Be("new-access");

			var backendRequest = handler.Requests.First(request => IsAuthBackendRequest(request));
			backendRequest.RequestUri!.AbsolutePath.Should().Be("/auth/api/twitch/refresh");
			backendRequest.RequestUri.Query.Should().BeEmpty();
			handler.RequestBodies[handler.Requests.IndexOf(backendRequest)].Should().Contain("refresh_token=old-refresh");

			var storedCredentials = LoadStoredCredentials();
			storedCredentials.RefreshToken.Should().Be("new-refresh");
			storedCredentials.TokenSource.Should().Be(TwitchTokenSources.LocalDeviceCode);
		}

		[Fact]
		public async Task RefreshTokensKeepsTheStoredCredentialsWhenBothTheBackendAndTheLocalRefreshFail()
		{
			SeedCredentials("old-access", "old-refresh", DateTimeOffset.Now.AddMinutes(-10), null);

			var handler = new StubHttpMessageHandler((request, _) =>
			{
				if (IsAuthBackendRequest(request))
				{
					throw new HttpRequestException("The CatCore auth back-end is down");
				}

				if (request.RequestUri!.AbsolutePath.EndsWith("/token", StringComparison.Ordinal))
				{
					// A confidential client refresh token cannot be rotated without the client secret.
					return JsonResponse("{\"status\":400,\"message\":\"invalid client\"}", HttpStatusCode.BadRequest);
				}

				return new HttpResponseMessage(HttpStatusCode.OK);
			});
			var authService = CreateAuthService(handler);

			var refreshed = await authService.RefreshTokens(force: true);

			refreshed.Should().BeFalse();
			authService.HasTokens.Should().BeTrue();
			authService.AccessToken.Should().Be("old-access");

			// A failed refresh must never discard a still usable refresh token; the auth back-end
			// may be reachable again before the token expires.
			var storedCredentials = LoadStoredCredentials();
			storedCredentials.RefreshToken.Should().Be("old-refresh");
		}

		[Fact]
		public async Task DeviceCodePollingObtainsAndStoresTokensWithoutAClientSecret()
		{
			var tokenRequestCount = 0;
			var handler = new StubHttpMessageHandler((request, _) =>
			{
				if (IsAuthBackendRequest(request))
				{
					throw new HttpRequestException("The CatCore auth back-end is down");
				}

				if (request.RequestUri!.AbsolutePath.EndsWith("/device", StringComparison.Ordinal))
				{
					return JsonResponse(DeviceAuthorizationJson());
				}

				if (request.RequestUri.AbsolutePath.EndsWith("/token", StringComparison.Ordinal))
				{
					tokenRequestCount++;
					return tokenRequestCount == 1
						? JsonResponse("{\"status\":400,\"message\":\"authorization_pending\"}", HttpStatusCode.BadRequest)
						: JsonResponse(TokenJson("device-access", "device-refresh"));
				}

				if (request.RequestUri.AbsolutePath.EndsWith("/validate", StringComparison.Ordinal))
				{
					return JsonResponse(ValidationJson());
				}

				return new HttpResponseMessage(HttpStatusCode.OK);
			});
			var authService = CreateAuthService(handler);

			try
			{
				var loginUrl = await authService.GetLoginUrl("https://localhost/callback");
				loginUrl.Should().Be(DEVICE_VERIFICATION_URI);

				var timeout = DateTimeOffset.Now.AddSeconds(15);
				while (!authService.HasTokens && DateTimeOffset.Now < timeout)
				{
					await Task.Delay(100);
				}

				authService.HasTokens.Should().BeTrue();
				authService.AccessToken.Should().Be("device-access");
				authService.PendingDeviceAuthorization.Should().BeNull();

				var storedCredentials = LoadStoredCredentials();
				storedCredentials.RefreshToken.Should().Be("device-refresh");
				storedCredentials.TokenSource.Should().Be(TwitchTokenSources.LocalDeviceCode);

				handler.RequestBodies.Should().NotContain(body => body.Contains("client_secret", StringComparison.Ordinal));
				tokenRequestCount.Should().BeGreaterThan(1);
			}
			finally
			{
				await authService.RevokeTokens();
			}
		}

		private static string ExtractState(string authorizationUrl)
		{
			var match = Regex.Match(authorizationUrl, "[?&]state=([0-9a-f]+)");
			match.Success.Should().BeTrue();
			return match.Groups[1].Value;
		}

		private TwitchAuthService CreateAuthService(StubHttpMessageHandler handler)
		{
			Directory.CreateDirectory(_dataPath);
			return new TwitchAuthService(
				new LoggerConfiguration().CreateLogger(),
				new TestKittenPathProvider(_dataPath),
				new TestConstants(),
				new Version(2, 0, 0),
				handler);
		}

		private void SeedCredentials(string accessToken, string refreshToken, DateTimeOffset validUntil, string? tokenSource)
		{
			Directory.CreateDirectory(_dataPath);
			var credentials = new TwitchCredentials(accessToken, refreshToken, validUntil, tokenSource);
			File.WriteAllText(GetCredentialsFilePath(), JsonSerializer.Serialize(credentials, new JsonSerializerOptions { WriteIndented = true }));
		}

		private TwitchCredentials LoadStoredCredentials()
		{
			var json = File.ReadAllText(GetCredentialsFilePath());
			return JsonSerializer.Deserialize<TwitchCredentials>(json)!;
		}

		private string GetCredentialsFilePath() => Path.Combine(_dataPath, "CatCoreCredentials - Twitch.json");

		private static bool IsAuthBackendRequest(HttpRequestMessage request)
		{
			return string.Equals(request.RequestUri?.Host, AUTH_BACKEND_HOST, StringComparison.Ordinal);
		}

		private static string DeviceAuthorizationJson(string verificationUri = DEVICE_VERIFICATION_URI)
		{
			return JsonSerializer.Serialize(new
			{
				device_code = "device-code-1",
				user_code = "ABCD1234",
				verification_uri = verificationUri,
				expires_in = 900,
				interval = 1
			});
		}

		private static string TokenJson(string accessToken, string refreshToken)
		{
			return JsonSerializer.Serialize(new
			{
				access_token = accessToken,
				refresh_token = refreshToken,
				token_type = "bearer",
				scope = RequiredScopes,
				expires_in = 14400
			});
		}

		private static string ValidationJson()
		{
			return JsonSerializer.Serialize(new
			{
				client_id = "test-client-id",
				login = "tester",
				user_id = "12345",
				scopes = RequiredScopes,
				expires_in = 14400
			});
		}

		private static HttpResponseMessage JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
		{
			return new HttpResponseMessage(statusCode)
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
		}

		public void Dispose()
		{
			try
			{
				if (Directory.Exists(_dataPath))
				{
					Directory.Delete(_dataPath, recursive: true);
				}
			}
			catch (IOException)
			{
				// Best effort cleanup only.
			}
		}

		private sealed class StubHttpMessageHandler : HttpMessageHandler
		{
			private readonly Func<HttpRequestMessage, string, HttpResponseMessage> _handler;

			public List<HttpRequestMessage> Requests { get; } = new();
			public List<string> RequestBodies { get; } = new();

			public StubHttpMessageHandler(Func<HttpRequestMessage, string, HttpResponseMessage> handler)
			{
				_handler = handler;
			}

			protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				var requestBody = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
				Requests.Add(request);
				RequestBodies.Add(requestBody);

				return _handler(request, requestBody);
			}
		}

		private sealed class TestKittenPathProvider : IKittenPathProvider
		{
			public string DataPath { get; }

			public TestKittenPathProvider(string dataPath)
			{
				DataPath = dataPath;
			}
		}

		private sealed class TestConstants : ConstantsBase
		{
			internal override string TwitchClientId => "test-client-id";

			internal override string CatCoreAuthServerUri => $"https://{AUTH_BACKEND_HOST}/auth/";
		}
	}
}
