using CatCore.Services.Twitch;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	public class EventSubRevocationTests
	{
		[Theory]
		[InlineData("authorization_revoked", (int)EventSubRevocationAction.ForceReauth)]
		[InlineData("user_removed", (int)EventSubRevocationAction.DetachChannel)]
		[InlineData("moderator_removed", (int)EventSubRevocationAction.DetachChannel)]
		[InlineData("chat_user_banned", (int)EventSubRevocationAction.DetachChannel)]
		[InlineData("version_removed", (int)EventSubRevocationAction.LogVersionRemoved)]
		[InlineData("websocket_disconnected", (int)EventSubRevocationAction.ResubscribeChannel)]
		[InlineData("unexpected_status", (int)EventSubRevocationAction.ResubscribeChannel)]
		public void ClassifyRevocationMapsStatusToAction(string status, int expectedAction)
		{
			TwitchEventSubChatService.ClassifyRevocation(status).Should().Be((EventSubRevocationAction)expectedAction);
		}

		[Fact]
		public void ClassifyRevocationFallsBackForMissingStatus()
		{
			TwitchEventSubChatService.ClassifyRevocation(null).Should().Be(EventSubRevocationAction.ResubscribeChannel);
		}
	}
}
