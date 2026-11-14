using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.EventSub.Moderation;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	public class EventSubModerationContractTests
	{
		[Fact]
		public void BanEventBindsOfficialPayload()
		{
			const string json = @"{
				""user_id"": ""1234"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cooler_user"", ""broadcaster_user_name"": ""Cooler_User"",
				""moderator_user_id"": ""1339"", ""moderator_user_login"": ""mod_user"", ""moderator_user_name"": ""Mod_User"",
				""reason"": ""Offensive language"", ""banned_at"": ""2020-07-15T18:15:11.17106713Z"",
				""ends_at"": ""2020-07-15T18:16:11.17106713Z"", ""is_permanent"": false
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubModerationSerializerContext.Default.BanEvent);

			data.UserId.Should().Be("1234");
			data.UserLogin.Should().Be("cool_user");
			data.BroadcasterUserId.Should().Be("1337");
			data.ModeratorUserId.Should().Be("1339");
			data.ModeratorUserLogin.Should().Be("mod_user");
			data.Reason.Should().Be("Offensive language");
			data.BannedAt.Should().Be("2020-07-15T18:15:11.17106713Z");
			data.EndsAt.Should().Be("2020-07-15T18:16:11.17106713Z");
			data.IsPermanent.Should().BeFalse();
		}

		[Fact]
		public void UnbanEventBindsOfficialPayload()
		{
			const string json = @"{
				""user_id"": ""1234"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cooler_user"", ""broadcaster_user_name"": ""Cooler_User"",
				""moderator_user_id"": ""1339"", ""moderator_user_login"": ""mod_user"", ""moderator_user_name"": ""Mod_User""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubModerationSerializerContext.Default.UnbanEvent);

			data.UserId.Should().Be("1234");
			data.UserName.Should().Be("Cool_User");
			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserLogin.Should().Be("cooler_user");
			data.ModeratorUserId.Should().Be("1339");
			data.ModeratorUserName.Should().Be("Mod_User");
		}

		[Fact]
		public void ModerateEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""423374343"", ""broadcaster_user_login"": ""glowillig"", ""broadcaster_user_name"": ""glowillig"",
				""source_broadcaster_user_id"": ""41292030"", ""source_broadcaster_user_login"": ""adflynn404"", ""source_broadcaster_user_name"": ""adflynn404"",
				""moderator_user_id"": ""424596340"", ""moderator_user_login"": ""quotrok"", ""moderator_user_name"": ""quotrok"",
				""action"": ""warn"",
				""followers"": null, ""slow"": null, ""vip"": null, ""unvip"": null,
				""warn"": { ""user_id"": ""141981764"", ""user_login"": ""twitchdev"", ""user_name"": ""TwitchDev"", ""reason"": ""cut it out"", ""chat_rules_cited"": null },
				""unmod"": null, ""ban"": null, ""unban"": null, ""timeout"": null, ""untimeout"": null,
				""raid"": null, ""unraid"": null, ""delete"": null, ""automod_terms"": null, ""unban_request"": null,
				""shared_chat_ban"": null, ""shared_chat_unban"": null, ""shared_chat_timeout"": null,
				""shared_chat_untimeout"": null, ""shared_chat_delete"": null
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubModerationSerializerContext.Default.ModerationActionEvent);

			data.BroadcasterUserId.Should().Be("423374343");
			data.SourceBroadcasterUserId.Should().Be("41292030");
			data.SourceBroadcasterUserLogin.Should().Be("adflynn404");
			data.ModeratorUserId.Should().Be("424596340");
			data.Action.Should().Be("warn");
			data.Followers.Should().BeNull();
			data.Slow.Should().BeNull();
			data.Ban.Should().BeNull();
			data.Warn.Should().NotBeNull();
			data.Warn!.Value.UserId.Should().Be("141981764");
			data.Warn!.Value.UserLogin.Should().Be("twitchdev");
			data.Warn!.Value.Reason.Should().Be("cut it out");
			data.Warn!.Value.ChatRulesCited.Should().BeNull();
		}

		[Fact]
		public void ModeratorAddEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""user_id"": ""1234"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubModerationSerializerContext.Default.ModeratorEvent);

			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserLogin.Should().Be("cool_user");
			data.BroadcasterUserName.Should().Be("Cool_User");
			data.UserId.Should().Be("1234");
			data.UserLogin.Should().Be("cool_user");
			data.UserName.Should().Be("Cool_User");
		}

		[Fact]
		public void ModeratorRemoveEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""user_id"": ""1234"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubModerationSerializerContext.Default.ModeratorEvent);

			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserName.Should().Be("Cool_User");
			data.UserId.Should().Be("1234");
			data.UserLogin.Should().Be("cool_user");
		}

		[Fact]
		public void ShieldModeBeginEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""moderator_user_id"": ""9001"", ""moderator_user_login"": ""the_mod"", ""moderator_user_name"": ""The_Mod"",
				""started_at"": ""2022-07-26T17:00:03.17106713Z""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubModerationSerializerContext.Default.ShieldModeEvent);

			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserLogin.Should().Be("cool_user");
			data.ModeratorUserId.Should().Be("9001");
			data.ModeratorUserName.Should().Be("The_Mod");
			data.StartedAt.Should().Be("2022-07-26T17:00:03.17106713Z");
			data.EndedAt.Should().BeNull();
		}

		[Fact]
		public void ShieldModeEndEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""moderator_user_id"": ""9001"", ""moderator_user_login"": ""the_mod"", ""moderator_user_name"": ""The_Mod"",
				""ended_at"": ""2022-07-26T17:00:03.17106713Z""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubModerationSerializerContext.Default.ShieldModeEvent);

			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserName.Should().Be("Cool_User");
			data.ModeratorUserId.Should().Be("9001");
			data.ModeratorUserLogin.Should().Be("the_mod");
			data.EndedAt.Should().Be("2022-07-26T17:00:03.17106713Z");
			data.StartedAt.Should().BeNull();
		}
	}
}
