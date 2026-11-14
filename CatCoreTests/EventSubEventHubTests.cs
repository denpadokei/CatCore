using System.Threading.Tasks;
using CatCore.Models.Twitch.EventSub.HypeTrain;
using CatCore.Services.Twitch.EventSub;
using FluentAssertions;
using Serilog;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Tests for the EventSub event hub that routes the incoming notifications to the handlers.
	/// </summary>
	public class EventSubEventHubTests
	{
		private static EventSubEventHub CreateHub()
		{
			return new EventSubEventHub(new LoggerConfiguration().CreateLogger());
		}

		[Fact]
		public void HubKnowsTheSupportedSubscriptionTypes()
		{
			var hub = CreateHub();

			hub.CanHandle("channel.hype_train.begin", "2").Should().BeTrue();
			hub.CanHandle("channel.goal.begin", "1").Should().BeTrue();
			hub.CanHandle("channel.subscribe", "1").Should().BeTrue();
			hub.CanHandle("channel.ban", "1").Should().BeTrue();
			hub.CanHandle("channel.channel_points_custom_reward.add", "1").Should().BeTrue();
			hub.CanHandle("automod.message.hold", "2").Should().BeTrue();
			hub.CanHandle("channel.warning.send", "1").Should().BeTrue();
			hub.CanHandle("user.whisper.message", "1").Should().BeTrue();
			hub.CanHandle("channel.some.unknown.type", "1").Should().BeFalse();
		}

		[Fact]
		public void TheSubscriptionVersionIsTakenIntoAccount()
		{
			var hub = CreateHub();

			// The hype train is only subscribed in version 2, so a version 1 notification of the
			// same type must not be routed to the handler.
			hub.CanHandle("channel.hype_train.begin", "2").Should().BeTrue();
			hub.CanHandle("channel.hype_train.begin", "1").Should().BeFalse();
		}

		[Fact]
		public async Task HypeTrainNotificationRaisesTheMatchingEvent()
		{
			var hub = CreateHub();

			HypeTrainEvent? received = null;
			string? receivedChannelId = null;
			hub.HypeTrain.OnBegin += (channelId, data) =>
			{
				receivedChannelId = channelId;
				received = data;
			};

			const string message = @"{
				""metadata"": { ""subscription_type"": ""channel.hype_train.begin"", ""subscription_version"": ""2"" },
				""payload"": { ""event"": {
					""id"": ""1b0AsbInCHZW2SQFQkCzqN07Ib2"",
					""broadcaster_user_id"": ""1337"",
					""broadcaster_user_login"": ""cool_user"",
					""broadcaster_user_name"": ""Cool_User"",
					""total"": 137, ""progress"": 137, ""goal"": 500,
					""level"": 1, ""type"": ""regular"", ""is_shared_train"": false,
					""started_at"": ""2020-07-15T17:16:03.17106713Z"",
					""expires_at"": ""2020-07-15T17:16:11.17106713Z"",
					""top_contributions"": [ { ""user_id"": ""123"", ""user_login"": ""pogchamp"", ""user_name"": ""PogChamp"", ""type"": ""bits"", ""total"": 50 } ]
				} }
			}";

			hub.Handle("channel.hype_train.begin", "2", ExtractEventJson(message));

			received.Should().NotBeNull();
			receivedChannelId.Should().Be("1337");
			received!.Value.Id.Should().Be("1b0AsbInCHZW2SQFQkCzqN07Ib2");
			received.Value.Total.Should().Be(137);
			received.Value.Level.Should().Be(1);
			received.Value.TopContributions.Should().HaveCount(1);
			received.Value.TopContributions![0].UserLogin.Should().Be("pogchamp");
		}

		[Fact]
		public void UnknownSubscriptionTypeIsRejected()
		{
			var hub = CreateHub();

			hub.Invoking(hub => hub.Handle("channel.some.unknown.type", "1", "{}")).Should().Throw<System.InvalidOperationException>();
		}

		private static string ExtractEventJson(string message)
		{
			using var jsonDocument = System.Text.Json.JsonDocument.Parse(message);
			return jsonDocument.RootElement.GetProperty("payload").GetProperty("event").GetRawText();
		}
	}
}
