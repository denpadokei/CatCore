using System;
using CatCore.Services.Twitch;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	public class EventSubRevocationTests
	{
		// Official EventSub revocation statuses are authorization_revoked, user_removed and
		// version_removed (see the WebSocket reference). Anything else must not force a re-subscribe.
		[Theory]
		[InlineData("authorization_revoked", (int)EventSubRevocationAction.ForceReauth)]
		[InlineData("user_removed", (int)EventSubRevocationAction.DetachChannel)]
		[InlineData("version_removed", (int)EventSubRevocationAction.LogVersionRemoved)]
		[InlineData("websocket_disconnected", (int)EventSubRevocationAction.Ignore)]
		[InlineData("unexpected_status", (int)EventSubRevocationAction.Ignore)]
		public void ClassifyRevocationMapsStatusToAction(string status, int expectedAction)
		{
			EventSubRevocationPolicy.Classify(status).Should().Be((EventSubRevocationAction)expectedAction);
		}

		[Fact]
		public void ClassifyRevocationIgnoresMissingStatus()
		{
			EventSubRevocationPolicy.Classify(null).Should().Be(EventSubRevocationAction.Ignore);
			EventSubRevocationPolicy.Classify(string.Empty).Should().Be(EventSubRevocationAction.Ignore);
		}

		[Fact]
		public void AuthorizationRefreshIsCoalescedWithinCooldownWindow()
		{
			var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
			var policy = new EventSubRevocationPolicy(() => now);

			policy.TryBeginAuthorizationRefresh().Should().BeTrue();

			now = now.AddSeconds(1);
			policy.TryBeginAuthorizationRefresh().Should().BeFalse();

			now = now.AddSeconds(20);
			policy.TryBeginAuthorizationRefresh().Should().BeFalse();
		}

		[Fact]
		public void AuthorizationRefreshIsAllowedAfterCooldownWindow()
		{
			var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
			var policy = new EventSubRevocationPolicy(() => now);

			policy.TryBeginAuthorizationRefresh().Should().BeTrue();

			now = now.Add(EventSubRevocationPolicy.AuthorizationRefreshCooldown);
			policy.TryBeginAuthorizationRefresh().Should().BeTrue();
		}
	}
}
