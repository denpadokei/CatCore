using System.Text.Json;
using CatCore.Helpers.JSON;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	public class EventSubChannelActivityContractTests
	{
		[Fact]
		public void BitsUseEventBindsOfficialPayload()
		{
			const string json = @"{
				""user_id"": ""1234"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cooler_user"", ""broadcaster_user_name"": ""Cooler_User"",
				""bits"": 2, ""type"": ""cheer"", ""power_up"": null, ""custom_power_up"": null,
				""message"": {
					""text"": ""cheer1 hi cheer1"",
					""fragments"": [
						{ ""type"": ""cheermote"", ""text"": ""cheer1"", ""cheermote"": { ""prefix"": ""cheer"", ""bits"": 1, ""tier"": 1 }, ""emote"": null },
						{ ""type"": ""text"", ""text"": "" hi "", ""cheermote"": null, ""emote"": null }
					]
				}
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubChannelActivitySerializerContext.Default.BitsUseEvent);

			data.UserId.Should().Be("1234");
			data.UserLogin.Should().Be("cool_user");
			data.BroadcasterUserId.Should().Be("1337");
			data.Bits.Should().Be(2);
			data.Type.Should().Be("cheer");
			data.PowerUp.Should().BeNull();
			data.CustomPowerUp.Should().BeNull();

			data.Message.Should().NotBeNull();
			data.Message!.Value.Text.Should().Be("cheer1 hi cheer1");
			data.Message!.Value.Fragments.Should().HaveCount(2);
			data.Message!.Value.Fragments![0].Type.Should().Be("cheermote");
			data.Message!.Value.Fragments![0].Text.Should().Be("cheer1");
			data.Message!.Value.Fragments![0].Cheermote.Should().NotBeNull();
			data.Message!.Value.Fragments![0].Cheermote!.Value.Prefix.Should().Be("cheer");
			data.Message!.Value.Fragments![0].Cheermote!.Value.Bits.Should().Be(1);
			data.Message!.Value.Fragments![0].Cheermote!.Value.Tier.Should().Be(1);
			data.Message!.Value.Fragments![0].Emote.Should().BeNull();
			data.Message!.Value.Fragments![1].Type.Should().Be("text");
			data.Message!.Value.Fragments![1].Text.Should().Be(" hi ");
		}

		[Fact]
		public void CheerEventBindsOfficialPayload()
		{
			const string json = @"{
				""is_anonymous"": false,
				""user_id"": ""1234"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cooler_user"", ""broadcaster_user_name"": ""Cooler_User"",
				""message"": ""pogchamp"", ""bits"": 1000
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubChannelActivitySerializerContext.Default.CheerEvent);

			data.IsAnonymous.Should().BeFalse();
			data.UserId.Should().Be("1234");
			data.UserLogin.Should().Be("cool_user");
			data.UserName.Should().Be("Cool_User");
			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserLogin.Should().Be("cooler_user");
			data.Message.Should().Be("pogchamp");
			data.Bits.Should().Be(1000);
		}

		[Fact]
		public void CustomPowerUpRedemptionEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""17fa2df1-ad76-4804-bfa5-a40ef63efe63"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""user_id"": ""9001"", ""user_login"": ""cooler_user"", ""user_name"": ""Cooler_User"",
				""user_input"": ""pogchamp"", ""status"": ""unfulfilled"",
				""custom_power_up"": { ""id"": ""92af127c-7326-4483-a52b-b0da0be61c01"", ""title"": ""title"", ""bits"": 100, ""prompt"": ""Power-up prompt"" },
				""redeemed_at"": ""2026-05-01T17:16:03.17106713Z""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubChannelActivitySerializerContext.Default.CustomPowerUpRedemptionEvent);

			data.Id.Should().Be("17fa2df1-ad76-4804-bfa5-a40ef63efe63");
			data.BroadcasterUserId.Should().Be("1337");
			data.UserId.Should().Be("9001");
			data.UserLogin.Should().Be("cooler_user");
			data.UserInput.Should().Be("pogchamp");
			data.Status.Should().Be("unfulfilled");
			data.CustomPowerUp.Id.Should().Be("92af127c-7326-4483-a52b-b0da0be61c01");
			data.CustomPowerUp.Title.Should().Be("title");
			data.CustomPowerUp.Bits.Should().Be(100);
			data.CustomPowerUp.Prompt.Should().Be("Power-up prompt");
			data.RedeemedAt.Should().Be("2026-05-01T17:16:03.17106713Z");
		}

		[Fact]
		public void RaidEventBindsOfficialPayload()
		{
			const string json = @"{
				""from_broadcaster_user_id"": ""1234"", ""from_broadcaster_user_login"": ""cool_user"", ""from_broadcaster_user_name"": ""Cool_User"",
				""to_broadcaster_user_id"": ""1337"", ""to_broadcaster_user_login"": ""cooler_user"", ""to_broadcaster_user_name"": ""Cooler_User"",
				""viewers"": 9001
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubChannelActivitySerializerContext.Default.RaidEvent);

			data.FromBroadcasterUserId.Should().Be("1234");
			data.FromBroadcasterUserLogin.Should().Be("cool_user");
			data.FromBroadcasterUserName.Should().Be("Cool_User");
			data.ToBroadcasterUserId.Should().Be("1337");
			data.ToBroadcasterUserLogin.Should().Be("cooler_user");
			data.ToBroadcasterUserName.Should().Be("Cooler_User");
			data.Viewers.Should().Be(9001);
		}

		[Fact]
		public void ChannelUpdateEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""title"": ""Best Stream Ever"", ""language"": ""en"",
				""category_id"": ""12453"", ""category_name"": ""Grand Theft Auto"",
				""content_classification_labels"": [ ""MatureGame"" ]
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubChannelActivitySerializerContext.Default.ChannelUpdateEvent);

			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserLogin.Should().Be("cool_user");
			data.Title.Should().Be("Best Stream Ever");
			data.Language.Should().Be("en");
			data.CategoryId.Should().Be("12453");
			data.CategoryName.Should().Be("Grand Theft Auto");
			data.ContentClassificationLabels.Should().HaveCount(1);
			data.ContentClassificationLabels![0].Should().Be("MatureGame");
		}

		[Fact]
		public void ShoutoutCreatedEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""to_broadcaster_user_id"": ""1234"", ""to_broadcaster_user_login"": ""cooler_user"", ""to_broadcaster_user_name"": ""Cooler_User"",
				""moderator_user_id"": ""9001"", ""moderator_user_login"": ""coolmod"", ""moderator_user_name"": ""CoolMod"",
				""viewer_count"": 860, ""started_at"": ""2022-07-26T17:00:03.17106713Z"",
				""cooldown_ends_at"": ""2022-07-26T22:00:03.17106713Z"", ""target_cooldown_ends_at"": ""2022-07-27T22:00:03.17106713Z""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubChannelActivitySerializerContext.Default.ShoutoutCreatedEvent);

			data.BroadcasterUserId.Should().Be("1337");
			data.ToBroadcasterUserId.Should().Be("1234");
			data.ToBroadcasterUserLogin.Should().Be("cooler_user");
			data.ModeratorUserId.Should().Be("9001");
			data.ModeratorUserLogin.Should().Be("coolmod");
			data.ModeratorUserName.Should().Be("CoolMod");
			data.ViewerCount.Should().Be(860);
			data.StartedAt.Should().Be("2022-07-26T17:00:03.17106713Z");
			data.CooldownEndsAt.Should().Be("2022-07-26T22:00:03.17106713Z");
			data.TargetCooldownEndsAt.Should().Be("2022-07-27T22:00:03.17106713Z");
		}

		[Fact]
		public void ShoutoutReceivedEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""626262"", ""broadcaster_user_name"": ""SandySanderman"", ""broadcaster_user_login"": ""sandysanderman"",
				""from_broadcaster_user_id"": ""12345"", ""from_broadcaster_user_name"": ""SimplySimple"", ""from_broadcaster_user_login"": ""simplysimple"",
				""viewer_count"": 860, ""started_at"": ""2022-07-26T17:00:03.17106713Z""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubChannelActivitySerializerContext.Default.ShoutoutReceivedEvent);

			data.BroadcasterUserId.Should().Be("626262");
			data.BroadcasterUserName.Should().Be("SandySanderman");
			data.FromBroadcasterUserId.Should().Be("12345");
			data.FromBroadcasterUserLogin.Should().Be("simplysimple");
			data.ViewerCount.Should().Be(860);
			data.StartedAt.Should().Be("2022-07-26T17:00:03.17106713Z");
		}
	}
}
