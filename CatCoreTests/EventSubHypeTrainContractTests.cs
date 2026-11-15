using System.Text.Json;
using CatCore.Helpers.JSON;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Contract tests for the hype train EventSub payloads, using the official Twitch sample payloads.
	/// </summary>
	public class EventSubHypeTrainContractTests
	{
		[Fact]
		public void BeginEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""1b0AsbInCHZW2SQFQkCzqN07Ib2"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""total"": 137, ""progress"": 137, ""goal"": 500,
				""top_contributions"": [ { ""user_id"": ""123"", ""user_login"": ""pogchamp"", ""user_name"": ""PogChamp"", ""type"": ""bits"", ""total"": 50 } ],
				""shared_train_participants"": null,
				""level"": 1,
				""started_at"": ""2020-07-15T17:16:03.17106713Z"",
				""expires_at"": ""2020-07-15T17:16:11.17106713Z"",
				""is_shared_train"": false,
				""type"": ""regular"",
				""all_time_high_level"": 4,
				""all_time_high_total"": 2845
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubHypeTrainSerializerContext.Default.HypeTrainEvent);

			data.Id.Should().Be("1b0AsbInCHZW2SQFQkCzqN07Ib2");
			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserLogin.Should().Be("cool_user");
			data.BroadcasterUserName.Should().Be("Cool_User");
			data.Total.Should().Be(137);
			data.Progress.Should().Be(137);
			data.Goal.Should().Be(500);
			data.Level.Should().Be(1);
			data.Type.Should().Be("regular");
			data.IsSharedTrain.Should().BeFalse();
			data.AllTimeHighLevel.Should().Be(4);
			data.AllTimeHighTotal.Should().Be(2845);
			data.StartedAt.Should().Be("2020-07-15T17:16:03.17106713Z");
			data.ExpiresAt.Should().Be("2020-07-15T17:16:11.17106713Z");
			data.EndedAt.Should().BeNull();
			data.CooldownEndsAt.Should().BeNull();
			data.SharedTrainParticipants.Should().BeNull();

			data.TopContributions.Should().HaveCount(1);
			var contribution = data.TopContributions![0];
			contribution.UserId.Should().Be("123");
			contribution.UserLogin.Should().Be("pogchamp");
			contribution.UserName.Should().Be("PogChamp");
			contribution.Type.Should().Be("bits");
			contribution.Total.Should().Be(50);
		}

		[Fact]
		public void ProgressEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""1b0AsbInCHZW2SQFQkCzqN07Ib2"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""total"": 137, ""progress"": 137, ""goal"": 500,
				""top_contributions"": [ { ""user_id"": ""123"", ""user_login"": ""pogchamp"", ""user_name"": ""PogChamp"", ""type"": ""bits"", ""total"": 50 } ],
				""shared_train_participants"": null,
				""level"": 1,
				""started_at"": ""2020-07-15T17:16:03.17106713Z"",
				""expires_at"": ""2020-07-15T17:16:11.17106713Z"",
				""is_shared_train"": false,
				""type"": ""regular""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubHypeTrainSerializerContext.Default.HypeTrainEvent);

			data.Id.Should().Be("1b0AsbInCHZW2SQFQkCzqN07Ib2");
			data.BroadcasterUserId.Should().Be("1337");
			data.Total.Should().Be(137);
			data.Progress.Should().Be(137);
			data.Goal.Should().Be(500);
			data.Level.Should().Be(1);
			data.Type.Should().Be("regular");
			data.IsSharedTrain.Should().BeFalse();
			data.AllTimeHighLevel.Should().BeNull();
			data.AllTimeHighTotal.Should().BeNull();
			data.StartedAt.Should().Be("2020-07-15T17:16:03.17106713Z");
			data.ExpiresAt.Should().Be("2020-07-15T17:16:11.17106713Z");
			data.EndedAt.Should().BeNull();
			data.SharedTrainParticipants.Should().BeNull();

			data.TopContributions.Should().HaveCount(1);
			data.TopContributions![0].Total.Should().Be(50);
		}

		[Fact]
		public void EndEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""1b0AsbInCHZW2SQFQkCzqN07Ib2"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""total"": 137,
				""top_contributions"": [ { ""user_id"": ""123"", ""user_login"": ""pogchamp"", ""user_name"": ""PogChamp"", ""type"": ""bits"", ""total"": 50 } ],
				""shared_train_participants"": null,
				""level"": 1,
				""started_at"": ""2020-07-15T17:16:03.17106713Z"",
				""ended_at"": ""2020-07-15T17:16:11.17106713Z"",
				""cooldown_ends_at"": ""2020-07-16T17:16:11.17106713Z"",
				""is_shared_train"": false,
				""type"": ""regular""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubHypeTrainSerializerContext.Default.HypeTrainEvent);

			data.Id.Should().Be("1b0AsbInCHZW2SQFQkCzqN07Ib2");
			data.BroadcasterUserId.Should().Be("1337");
			data.Total.Should().Be(137);
			data.Level.Should().Be(1);
			data.Type.Should().Be("regular");
			data.IsSharedTrain.Should().BeFalse();
			data.Progress.Should().BeNull();
			data.Goal.Should().BeNull();
			data.ExpiresAt.Should().BeNull();
			data.AllTimeHighLevel.Should().BeNull();
			data.AllTimeHighTotal.Should().BeNull();
			data.StartedAt.Should().Be("2020-07-15T17:16:03.17106713Z");
			data.EndedAt.Should().Be("2020-07-15T17:16:11.17106713Z");
			data.CooldownEndsAt.Should().Be("2020-07-16T17:16:11.17106713Z");
			data.SharedTrainParticipants.Should().BeNull();

			data.TopContributions.Should().HaveCount(1);
			data.TopContributions![0].Type.Should().Be("bits");
			data.TopContributions![0].Total.Should().Be(50);
		}
	}
}
