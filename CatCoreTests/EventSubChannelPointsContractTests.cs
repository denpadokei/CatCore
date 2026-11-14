using System.Text.Json;
using CatCore.Helpers.JSON;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Contract tests for the channel points EventSub payload models. They bind the official Twitch
	/// sample payloads and verify the exact JSON shapes the handler relies on.
	/// </summary>
	public class EventSubChannelPointsContractTests
	{
		[Fact]
		public void AutomaticRewardRedemptionEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""12826"", ""broadcaster_user_name"": ""Twitch"", ""broadcaster_user_login"": ""twitch"",
				""user_id"": ""141981764"", ""user_name"": ""TwitchDev"", ""user_login"": ""twitchdev"",
				""id"": ""f024099a-e0fe-4339-9a0a-a706fb59f353"",
				""reward"": { ""type"": ""send_highlighted_message"", ""channel_points"": 100, ""emote"": null },
				""message"": {
					""text"": ""Hello world! VoHiYo"",
					""fragments"": [
						{ ""type"": ""text"", ""text"": ""Hello world! "", ""emote"": null },
						{ ""type"": ""emote"", ""text"": ""VoHiYo"", ""emote"": { ""id"": ""81274"" } }
					]
				},
				""redeemed_at"": ""2024-08-12T21:14:34.260398045Z""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubChannelPointsSerializerContext.Default.AutomaticRewardRedemptionEvent);

			data.BroadcasterUserId.Should().Be("12826");
			data.BroadcasterUserLogin.Should().Be("twitch");
			data.UserId.Should().Be("141981764");
			data.UserLogin.Should().Be("twitchdev");
			data.Id.Should().Be("f024099a-e0fe-4339-9a0a-a706fb59f353");
			data.Reward.Type.Should().Be("send_highlighted_message");
			data.Reward.ChannelPoints.Should().Be(100);
			data.Reward.Emote.HasValue.Should().BeFalse();
			data.Message.HasValue.Should().BeTrue();
			data.Message.Value.Text.Should().Be("Hello world! VoHiYo");
			data.Message.Value.Fragments.Should().NotBeNull();
			data.Message.Value.Fragments!.Length.Should().Be(2);
			data.Message.Value.Fragments[0].Type.Should().Be("text");
			data.Message.Value.Fragments[0].Text.Should().Be("Hello world! ");
			data.Message.Value.Fragments[0].Emote.HasValue.Should().BeFalse();
			data.Message.Value.Fragments[1].Type.Should().Be("emote");
			data.Message.Value.Fragments[1].Text.Should().Be("VoHiYo");
			data.Message.Value.Fragments[1].Emote.Value.Id.Should().Be("81274");
			data.RedeemedAt.Should().Be("2024-08-12T21:14:34.260398045Z");
		}

		[Fact]
		public void CustomRewardRedemptionUpdateEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""17fa2df1-ad76-4804-bfa5-a40ef63efe63"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""user_id"": ""9001"", ""user_login"": ""cooler_user"", ""user_name"": ""Cooler_User"",
				""user_input"": ""pogchamp"",
				""status"": ""fulfilled"",
				""reward"": { ""id"": ""92af127c-7326-4483-a52b-b0da0be61c01"", ""title"": ""title"", ""cost"": 100, ""prompt"": ""reward prompt"" },
				""redeemed_at"": ""2020-07-15T17:16:03.17106713Z""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubChannelPointsSerializerContext.Default.CustomRewardRedemptionEvent);

			data.Id.Should().Be("17fa2df1-ad76-4804-bfa5-a40ef63efe63");
			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserName.Should().Be("Cool_User");
			data.UserId.Should().Be("9001");
			data.UserName.Should().Be("Cooler_User");
			data.UserInput.Should().Be("pogchamp");
			data.Status.Should().Be("fulfilled");
			data.Reward.Id.Should().Be("92af127c-7326-4483-a52b-b0da0be61c01");
			data.Reward.Title.Should().Be("title");
			data.Reward.Cost.Should().Be(100);
			data.Reward.Prompt.Should().Be("reward prompt");
			data.RedeemedAt.Should().Be("2020-07-15T17:16:03.17106713Z");
		}

		[Fact]
		public void CustomRewardAddEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""9001"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""is_enabled"": true, ""is_paused"": false, ""is_in_stock"": true,
				""title"": ""Cool Reward"", ""cost"": 100, ""prompt"": ""reward prompt"",
				""is_user_input_required"": true,
				""should_redemptions_skip_request_queue"": false,
				""cooldown_expires_at"": ""2019-11-16T10:11:12.634234626Z"",
				""redemptions_redeemed_current_stream"": 123,
				""max_per_stream"": { ""is_enabled"": true, ""value"": 1000 },
				""max_per_user_per_stream"": { ""is_enabled"": true, ""value"": 1000 },
				""global_cooldown"": { ""is_enabled"": true, ""seconds"": 1000 },
				""background_color"": ""#FA1ED2"",
				""image"": { ""url_1x"": ""https://static-cdn.jtvnw.net/image-1.png"", ""url_2x"": ""https://static-cdn.jtvnw.net/image-2.png"", ""url_4x"": ""https://static-cdn.jtvnw.net/image-4.png"" },
				""default_image"": { ""url_1x"": ""https://static-cdn.jtvnw.net/default-1.png"", ""url_2x"": ""https://static-cdn.jtvnw.net/default-2.png"", ""url_4x"": ""https://static-cdn.jtvnw.net/default-4.png"" }
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubChannelPointsSerializerContext.Default.CustomRewardEvent);

			data.Id.Should().Be("9001");
			data.BroadcasterUserId.Should().Be("1337");
			data.IsEnabled.Should().BeTrue();
			data.IsPaused.Should().BeFalse();
			data.IsInStock.Should().BeTrue();
			data.Title.Should().Be("Cool Reward");
			data.Cost.Should().Be(100);
			data.Prompt.Should().Be("reward prompt");
			data.IsUserInputRequired.Should().BeTrue();
			data.ShouldRedemptionsSkipRequestQueue.Should().BeFalse();
			data.CooldownExpiresAt.Should().Be("2019-11-16T10:11:12.634234626Z");
			data.RedemptionsRedeemedCurrentStream.Should().Be(123);
			data.MaxPerStream.IsEnabled.Should().BeTrue();
			data.MaxPerStream.Value.Should().Be(1000);
			data.MaxPerUserPerStream.IsEnabled.Should().BeTrue();
			data.MaxPerUserPerStream.Value.Should().Be(1000);
			data.GlobalCooldown.IsEnabled.Should().BeTrue();
			data.GlobalCooldown.Seconds.Should().Be(1000);
			data.BackgroundColor.Should().Be("#FA1ED2");
			data.Image.HasValue.Should().BeTrue();
			data.Image.Value.Url1x.Should().Be("https://static-cdn.jtvnw.net/image-1.png");
			data.Image.Value.Url2x.Should().Be("https://static-cdn.jtvnw.net/image-2.png");
			data.Image.Value.Url4x.Should().Be("https://static-cdn.jtvnw.net/image-4.png");
			data.DefaultImage.Url1x.Should().Be("https://static-cdn.jtvnw.net/default-1.png");
			data.DefaultImage.Url4x.Should().Be("https://static-cdn.jtvnw.net/default-4.png");
		}

		[Fact]
		public void CustomRewardRemoveEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""9001"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""is_enabled"": true, ""is_paused"": false, ""is_in_stock"": true,
				""title"": ""Cool Reward"", ""cost"": 100, ""prompt"": ""reward prompt"",
				""is_user_input_required"": true,
				""should_redemptions_skip_request_queue"": false,
				""cooldown_expires_at"": null,
				""redemptions_redeemed_current_stream"": null,
				""max_per_stream"": { ""is_enabled"": true, ""value"": 1000 },
				""max_per_user_per_stream"": { ""is_enabled"": true, ""value"": 1000 },
				""global_cooldown"": { ""is_enabled"": true, ""seconds"": 1000 },
				""background_color"": ""#FA1ED2"",
				""image"": { ""url_1x"": ""https://static-cdn.jtvnw.net/image-1.png"", ""url_2x"": ""https://static-cdn.jtvnw.net/image-2.png"", ""url_4x"": ""https://static-cdn.jtvnw.net/image-4.png"" },
				""default_image"": { ""url_1x"": ""https://static-cdn.jtvnw.net/default-1.png"", ""url_2x"": ""https://static-cdn.jtvnw.net/default-2.png"", ""url_4x"": ""https://static-cdn.jtvnw.net/default-4.png"" }
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubChannelPointsSerializerContext.Default.CustomRewardEvent);

			data.Id.Should().Be("9001");
			data.Title.Should().Be("Cool Reward");
			data.Cost.Should().Be(100);
			data.CooldownExpiresAt.Should().BeNull();
			data.RedemptionsRedeemedCurrentStream.Should().BeNull();
			data.MaxPerStream.Value.Should().Be(1000);
			data.GlobalCooldown.Seconds.Should().Be(1000);
			data.DefaultImage.Url1x.Should().Be("https://static-cdn.jtvnw.net/default-1.png");
		}

		[Fact]
		public void CustomRewardUpdateEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""9001"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""is_enabled"": false, ""is_paused"": true, ""is_in_stock"": false,
				""title"": ""Cool Reward"", ""cost"": 250, ""prompt"": ""updated prompt"",
				""is_user_input_required"": false,
				""should_redemptions_skip_request_queue"": true,
				""cooldown_expires_at"": ""2024-08-12T21:14:34.260398045Z"",
				""redemptions_redeemed_current_stream"": 7,
				""max_per_stream"": { ""is_enabled"": false, ""value"": 0 },
				""max_per_user_per_stream"": { ""is_enabled"": true, ""value"": 5 },
				""global_cooldown"": { ""is_enabled"": false, ""seconds"": 0 },
				""background_color"": ""#00FF00"",
				""image"": null,
				""default_image"": { ""url_1x"": ""https://static-cdn.jtvnw.net/default-1.png"", ""url_2x"": ""https://static-cdn.jtvnw.net/default-2.png"", ""url_4x"": ""https://static-cdn.jtvnw.net/default-4.png"" }
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubChannelPointsSerializerContext.Default.CustomRewardEvent);

			data.Id.Should().Be("9001");
			data.IsEnabled.Should().BeFalse();
			data.IsPaused.Should().BeTrue();
			data.IsInStock.Should().BeFalse();
			data.Cost.Should().Be(250);
			data.Prompt.Should().Be("updated prompt");
			data.IsUserInputRequired.Should().BeFalse();
			data.ShouldRedemptionsSkipRequestQueue.Should().BeTrue();
			data.CooldownExpiresAt.Should().Be("2024-08-12T21:14:34.260398045Z");
			data.RedemptionsRedeemedCurrentStream.Should().Be(7);
			data.MaxPerStream.IsEnabled.Should().BeFalse();
			data.MaxPerUserPerStream.Value.Should().Be(5);
			data.GlobalCooldown.IsEnabled.Should().BeFalse();
			data.BackgroundColor.Should().Be("#00FF00");
			data.Image.HasValue.Should().BeFalse();
			data.DefaultImage.Url2x.Should().Be("https://static-cdn.jtvnw.net/default-2.png");
		}
	}
}
