using CatCore.Services.Twitch;
using CatCore.Services.Twitch.Interfaces;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Verifies that the EventSub events are exposed through the same service instance that
	/// receives the EventSub notifications, so that the consumers actually receive the events.
	/// </summary>
	public class TwitchEventSubServiceRegistrationTests
	{
		[Fact]
		public void TheEventSubServiceIsImplementedByTheServiceThatReceivesTheNotifications()
		{
			typeof(TwitchEventSubChatService).Should().BeAssignableTo<ITwitchEventSubService>();
			typeof(TwitchEventSubChatService).Should().BeAssignableTo<ITwitchIrcService>();
			typeof(TwitchEventSubChatService).Should().BeAssignableTo<ITwitchPubSubServiceManager>();
		}

		[Fact]
		public void TheEventSubServiceExposesEveryEventFamily()
		{
			var eventSubService = new CatCore.Services.Twitch.EventSub.EventSubEventHub(new Serilog.LoggerConfiguration().CreateLogger());

			eventSubService.HypeTrain.Should().NotBeNull();
			eventSubService.Goal.Should().NotBeNull();
			eventSubService.Charity.Should().NotBeNull();
			eventSubService.User.Should().NotBeNull();
			eventSubService.Subscriptions.Should().NotBeNull();
			eventSubService.ChannelActivity.Should().NotBeNull();
			eventSubService.Moderation.Should().NotBeNull();
			eventSubService.ChannelPoints.Should().NotBeNull();
			eventSubService.AutoMod.Should().NotBeNull();
			eventSubService.Safety.Should().NotBeNull();
		}
	}
}
