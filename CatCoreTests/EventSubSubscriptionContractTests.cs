using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.EventSub.Subscription;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Contract tests for the subscription and VIP EventSub payload models. The JSON samples are the
	/// official Twitch documentation payloads.
	/// </summary>
	public class EventSubSubscriptionContractTests
	{
		[Fact]
		public void SubscribeEventBindsOfficialPayload()
		{
			const string json = @"{
				""user_id"": ""1234"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cooler_user"", ""broadcaster_user_name"": ""Cooler_User"",
				""tier"": ""1000"", ""is_gift"": false
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubSubscriptionSerializerContext.Default.SubscriptionEvent);

			data.UserId.Should().Be("1234");
			data.UserLogin.Should().Be("cool_user");
			data.UserName.Should().Be("Cool_User");
			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserLogin.Should().Be("cooler_user");
			data.BroadcasterUserName.Should().Be("Cooler_User");
			data.Tier.Should().Be("1000");
			data.IsGift.Should().BeFalse();
		}

		[Fact]
		public void SubscriptionEndEventBindsOfficialPayload()
		{
			const string json = @"{
				""user_id"": ""1234"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cooler_user"", ""broadcaster_user_name"": ""Cooler_User"",
				""tier"": ""1000"", ""is_gift"": false
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubSubscriptionSerializerContext.Default.SubscriptionEndEvent);

			data.UserId.Should().Be("1234");
			data.UserLogin.Should().Be("cool_user");
			data.UserName.Should().Be("Cool_User");
			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserLogin.Should().Be("cooler_user");
			data.BroadcasterUserName.Should().Be("Cooler_User");
			data.Tier.Should().Be("1000");
			data.IsGift.Should().BeFalse();
		}

		[Fact]
		public void SubscriptionGiftEventBindsOfficialPayload()
		{
			const string json = @"{
				""user_id"": ""1234"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cooler_user"", ""broadcaster_user_name"": ""Cooler_User"",
				""total"": 2, ""tier"": ""1000"", ""cumulative_total"": 284, ""is_anonymous"": false
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubSubscriptionSerializerContext.Default.SubscriptionGiftEvent);

			data.UserId.Should().Be("1234");
			data.UserLogin.Should().Be("cool_user");
			data.UserName.Should().Be("Cool_User");
			data.BroadcasterUserId.Should().Be("1337");
			data.Total.Should().Be(2);
			data.Tier.Should().Be("1000");
			data.CumulativeTotal.Should().Be(284);
			data.IsAnonymous.Should().BeFalse();
		}

		[Fact]
		public void SubscriptionMessageEventBindsOfficialPayload()
		{
			const string json = @"{
				""user_id"": ""1234"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cooler_user"", ""broadcaster_user_name"": ""Cooler_User"",
				""tier"": ""1000"",
				""message"": { ""text"": ""Love the stream! FevziGG"", ""emotes"": [ { ""begin"": 23, ""end"": 30, ""id"": ""302976485"" } ] },
				""cumulative_months"": 15, ""streak_months"": 1, ""duration_months"": 6
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubSubscriptionSerializerContext.Default.SubscriptionMessageEvent);

			data.UserId.Should().Be("1234");
			data.UserLogin.Should().Be("cool_user");
			data.UserName.Should().Be("Cool_User");
			data.BroadcasterUserId.Should().Be("1337");
			data.Tier.Should().Be("1000");
			data.Message.Text.Should().Be("Love the stream! FevziGG");
			data.Message.Emotes.Should().NotBeNull();
			data.Message.Emotes.Should().HaveCount(1);
			data.Message.Emotes![0].Begin.Should().Be(23);
			data.Message.Emotes[0].End.Should().Be(30);
			data.Message.Emotes[0].Id.Should().Be("302976485");
			data.CumulativeMonths.Should().Be(15);
			data.StreakMonths.Should().Be(1);
			data.DurationMonths.Should().Be(6);
		}

		[Fact]
		public void VipAddedEventBindsOfficialPayload()
		{
			const string json = @"{
				""user_id"": ""1234"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cooler_user"", ""broadcaster_user_name"": ""Cooler_User""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubSubscriptionSerializerContext.Default.VipEvent);

			data.UserId.Should().Be("1234");
			data.UserLogin.Should().Be("cool_user");
			data.UserName.Should().Be("Cool_User");
			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserLogin.Should().Be("cooler_user");
			data.BroadcasterUserName.Should().Be("Cooler_User");
		}

		[Fact]
		public void VipRemovedEventBindsOfficialPayload()
		{
			const string json = @"{
				""user_id"": ""1234"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cooler_user"", ""broadcaster_user_name"": ""Cooler_User""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubSubscriptionSerializerContext.Default.VipEvent);

			data.UserId.Should().Be("1234");
			data.UserLogin.Should().Be("cool_user");
			data.UserName.Should().Be("Cool_User");
			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserLogin.Should().Be("cooler_user");
			data.BroadcasterUserName.Should().Be("Cooler_User");
		}
	}
}
