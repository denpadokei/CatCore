using System;
using CatCore;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch;
using FluentAssertions;
using Serilog;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Tests for the authorization scope handling of the Twitch auth service.
	/// </summary>
	public class TwitchAuthServiceScopeTests
	{
		private static TwitchAuthService CreateAuthService()
		{
			var logger = new LoggerConfiguration().CreateLogger();
			return new TwitchAuthService(logger, new TestKittenPathProvider(), new TestConstants(), new Version(2, 0, 0));
		}

		[Fact]
		public void AuthorizationUrlRequestsTheScopesOfTheOptionalEventSubTypes()
		{
			var authService = CreateAuthService();

			var url = authService.AuthorizationUrl("https://localhost/callback");

			url.Should().Contain("scope=");
			url.Should().Contain("channel:read:hype_train");
			url.Should().Contain("channel:read:goals");
			url.Should().Contain("channel:read:charity");
			url.Should().Contain("channel:read:vips");
			url.Should().Contain("moderation:read");
			url.Should().Contain("moderator:manage:automod");
			url.Should().Contain("moderator:manage:automod_settings");
			url.Should().Contain("moderator:read:blocked_terms");
			url.Should().Contain("moderator:read:moderators");
			url.Should().Contain("moderator:read:shield_mode");
			url.Should().Contain("moderator:read:shoutouts");
			url.Should().Contain("moderator:read:suspicious_users");
			url.Should().Contain("moderator:read:unban_requests");
			url.Should().Contain("moderator:read:vips");
			url.Should().Contain("moderator:read:warnings");
			url.Should().Contain("user:read:whispers");
		}

		[Fact]
		public void AuthorizationUrlStillRequestsTheRequiredScopes()
		{
			var authService = CreateAuthService();

			var url = authService.AuthorizationUrl("https://localhost/callback");

			url.Should().Contain("chat:read");
			url.Should().Contain("chat:edit");
			url.Should().Contain("bits:read");
			url.Should().Contain("channel:read:subscriptions");
			url.Should().Contain("channel:moderate");
		}

		private sealed class TestKittenPathProvider : IKittenPathProvider
		{
			public string DataPath => System.IO.Path.GetTempPath();
		}

		private sealed class TestConstants : ConstantsBase
		{
			internal override string TwitchClientId => "test-client-id";

			internal override string CatCoreAuthServerUri => "https://localhost/auth";
		}
	}
}
