using System.Text.Json;
using CatCore.Helpers.JSON;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Contract tests for the channel goal EventSub payloads, using the official Twitch sample payloads.
	/// </summary>
	public class EventSubGoalContractTests
	{
		[Fact]
		public void BeginEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""12345-cool-event"",
				""broadcaster_user_id"": ""141981764"", ""broadcaster_user_name"": ""TwitchDev"", ""broadcaster_user_login"": ""twitchdev"",
				""type"": ""subscription"",
				""description"": ""Help me get partner!"",
				""current_amount"": 100,
				""target_amount"": 220,
				""started_at"": ""2021-07-15T17:16:03.17106713Z""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubGoalSerializerContext.Default.GoalEvent);

			data.Id.Should().Be("12345-cool-event");
			data.BroadcasterUserId.Should().Be("141981764");
			data.BroadcasterUserLogin.Should().Be("twitchdev");
			data.BroadcasterUserName.Should().Be("TwitchDev");
			data.Type.Should().Be("subscription");
			data.Description.Should().Be("Help me get partner!");
			data.CurrentAmount.Should().Be(100);
			data.TargetAmount.Should().Be(220);
			data.StartedAt.Should().Be("2021-07-15T17:16:03.17106713Z");
			data.IsAchieved.Should().BeNull();
			data.EndedAt.Should().BeNull();
		}

		[Fact]
		public void ProgressEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""12345-cool-event"",
				""broadcaster_user_id"": ""141981764"", ""broadcaster_user_name"": ""TwitchDev"", ""broadcaster_user_login"": ""twitchdev"",
				""type"": ""subscription"",
				""description"": ""Help me get partner!"",
				""current_amount"": 120,
				""target_amount"": 220,
				""started_at"": ""2021-07-15T17:16:03.17106713Z""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubGoalSerializerContext.Default.GoalEvent);

			data.Id.Should().Be("12345-cool-event");
			data.BroadcasterUserId.Should().Be("141981764");
			data.Type.Should().Be("subscription");
			data.Description.Should().Be("Help me get partner!");
			data.CurrentAmount.Should().Be(120);
			data.TargetAmount.Should().Be(220);
			data.StartedAt.Should().Be("2021-07-15T17:16:03.17106713Z");
			data.IsAchieved.Should().BeNull();
			data.EndedAt.Should().BeNull();
		}

		[Fact]
		public void EndEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""12345-cool-event"",
				""broadcaster_user_id"": ""141981764"", ""broadcaster_user_name"": ""TwitchDev"", ""broadcaster_user_login"": ""twitchdev"",
				""type"": ""subscription"",
				""description"": ""Help me get partner!"",
				""current_amount"": 180,
				""target_amount"": 220,
				""started_at"": ""2021-07-15T17:16:03.17106713Z"",
				""is_achieved"": false,
				""ended_at"": ""2020-07-16T17:16:03.17106713Z""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubGoalSerializerContext.Default.GoalEvent);

			data.Id.Should().Be("12345-cool-event");
			data.BroadcasterUserId.Should().Be("141981764");
			data.BroadcasterUserName.Should().Be("TwitchDev");
			data.Type.Should().Be("subscription");
			data.Description.Should().Be("Help me get partner!");
			data.CurrentAmount.Should().Be(180);
			data.TargetAmount.Should().Be(220);
			data.StartedAt.Should().Be("2021-07-15T17:16:03.17106713Z");
			data.IsAchieved.Should().BeFalse();
			data.EndedAt.Should().Be("2020-07-16T17:16:03.17106713Z");
		}
	}
}
