using System.Text.Json;
using CatCore.Helpers.JSON;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Contract tests for the charity campaign EventSub payload models.
	/// They use the payload shapes Twitch documents for the channel.charity_campaign.* types.
	/// </summary>
	public class EventSubCharityContractTests
	{
		[Fact]
		public void DonationEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""a1b2c3-aabb-4455-d1e2f3"",
				""campaign_id"": ""123-abc-456-def"",
				""broadcaster_user_id"": ""123456"", ""broadcaster_user_name"": ""SunnySideUp"", ""broadcaster_user_login"": ""sunnysideup"",
				""user_id"": ""654321"", ""user_login"": ""generoususer1"", ""user_name"": ""GenerousUser1"",
				""charity_name"": ""Example name"",
				""charity_description"": ""Example description"",
				""charity_logo"": ""https://abc.cloudfront.net/ppgf/1000/100.png"",
				""charity_website"": ""https://www.example.com"",
				""amount"": { ""value"": 10000, ""decimal_places"": 2, ""currency"": ""USD"" }
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubCharitySerializerContext.Default.CharityDonationEvent);

			data.Id.Should().Be("a1b2c3-aabb-4455-d1e2f3");
			data.CampaignId.Should().Be("123-abc-456-def");
			// The donate payload names the broadcaster with the broadcaster_user_* fields.
			data.BroadcasterUserId.Should().Be("123456");
			data.BroadcasterUserLogin.Should().Be("sunnysideup");
			data.BroadcasterUserName.Should().Be("SunnySideUp");
			data.UserId.Should().Be("654321");
			data.UserLogin.Should().Be("generoususer1");
			data.UserName.Should().Be("GenerousUser1");
			data.CharityName.Should().Be("Example name");
			data.CharityDescription.Should().Be("Example description");
			data.CharityLogo.Should().Be("https://abc.cloudfront.net/ppgf/1000/100.png");
			data.CharityWebsite.Should().Be("https://www.example.com");
			data.Amount.Value.Should().Be(10000);
			data.Amount.DecimalPlaces.Should().Be(2);
			data.Amount.Currency.Should().Be("USD");
		}

		[Fact]
		public void ProgressEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""123-abc-456-def"",
				""broadcaster_id"": ""123456"", ""broadcaster_name"": ""SunnySideUp"", ""broadcaster_login"": ""sunnysideup"",
				""charity_name"": ""Example name"", ""charity_description"": ""Example description"",
				""charity_logo"": ""https://abc.cloudfront.net/ppgf/1000/100.png"", ""charity_website"": ""https://www.example.com"",
				""current_amount"": { ""value"": 260000, ""decimal_places"": 2, ""currency"": ""USD"" },
				""target_amount"":  { ""value"": 1500000, ""decimal_places"": 2, ""currency"": ""USD"" }
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubCharitySerializerContext.Default.CharityCampaignEvent);

			data.Id.Should().Be("123-abc-456-def");
			// The progress/start/stop payloads use broadcaster_* instead of broadcaster_user_*.
			data.BroadcasterId.Should().Be("123456");
			data.BroadcasterLogin.Should().Be("sunnysideup");
			data.BroadcasterName.Should().Be("SunnySideUp");
			data.CharityName.Should().Be("Example name");
			data.CurrentAmount.Value.Should().Be(260000);
			data.CurrentAmount.DecimalPlaces.Should().Be(2);
			data.CurrentAmount.Currency.Should().Be("USD");
			data.TargetAmount.Value.Should().Be(1500000);
			data.TargetAmount.Currency.Should().Be("USD");
			data.StartedAt.Should().BeNull();
			data.StoppedAt.Should().BeNull();
		}

		[Fact]
		public void StartEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""123-abc-456-def"",
				""broadcaster_id"": ""123456"", ""broadcaster_name"": ""SunnySideUp"", ""broadcaster_login"": ""sunnysideup"",
				""charity_name"": ""Example name"", ""charity_description"": ""Example description"",
				""charity_logo"": ""https://abc.cloudfront.net/ppgf/1000/100.png"", ""charity_website"": ""https://www.example.com"",
				""current_amount"": { ""value"": 260000, ""decimal_places"": 2, ""currency"": ""USD"" },
				""target_amount"":  { ""value"": 1500000, ""decimal_places"": 2, ""currency"": ""USD"" },
				""started_at"": ""2022-07-26T17:00:03.17106713Z""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubCharitySerializerContext.Default.CharityCampaignEvent);

			data.Id.Should().Be("123-abc-456-def");
			data.BroadcasterId.Should().Be("123456");
			data.CharityName.Should().Be("Example name");
			data.CurrentAmount.Value.Should().Be(260000);
			data.TargetAmount.Value.Should().Be(1500000);
			data.StartedAt.Should().Be("2022-07-26T17:00:03.17106713Z");
			data.StoppedAt.Should().BeNull();
		}

		[Fact]
		public void StopEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""123-abc-456-def"",
				""broadcaster_id"": ""123456"", ""broadcaster_name"": ""SunnySideUp"", ""broadcaster_login"": ""sunnysideup"",
				""charity_name"": ""Example name"", ""charity_description"": ""Example description"",
				""charity_logo"": ""https://abc.cloudfront.net/ppgf/1000/100.png"", ""charity_website"": ""https://www.example.com"",
				""current_amount"": { ""value"": 1450000, ""decimal_places"": 2, ""currency"": ""USD"" },
				""target_amount"":  { ""value"": 1500000, ""decimal_places"": 2, ""currency"": ""USD"" },
				""stopped_at"": ""2022-07-26T22:00:03.17106713Z""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubCharitySerializerContext.Default.CharityCampaignEvent);

			data.Id.Should().Be("123-abc-456-def");
			data.BroadcasterId.Should().Be("123456");
			data.BroadcasterLogin.Should().Be("sunnysideup");
			data.CurrentAmount.Value.Should().Be(1450000);
			data.CurrentAmount.DecimalPlaces.Should().Be(2);
			data.TargetAmount.Value.Should().Be(1500000);
			data.StoppedAt.Should().Be("2022-07-26T22:00:03.17106713Z");
			data.StartedAt.Should().BeNull();
		}
	}
}
