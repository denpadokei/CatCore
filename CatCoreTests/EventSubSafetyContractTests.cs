using System.Linq;
using System.Text.Json;
using CatCore.Helpers.JSON;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Contract tests for the safety family EventSub models (suspicious users, unban requests, warnings).
	/// The JSON samples come from the Twitch EventSub reference documentation.
	/// </summary>
	public class EventSubSafetyContractTests
	{
		[Fact]
		public void SuspiciousUserMessageEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""moderator_user_id"": ""9001"", ""moderator_user_login"": ""the_mod"", ""moderator_user_name"": ""The_Mod"",
				""user_id"": ""1234"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User"",
				""low_trust_status"": ""active_monitoring"",
				""shared_ban_channel_ids"": [ ""1234"" ],
				""types"": [ ""manually_added"" ],
				""ban_evasion_evaluation"": ""likely"",
				""message"": {
					""message_id"": ""abc-123"",
					""text"": ""hello"",
					""fragments"": [ { ""type"": ""text"", ""text"": ""hello"", ""cheermote"": null, ""emote"": null } ]
				}
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubSafetySerializerContext.Default.SuspiciousUserMessageEvent);

			data.BroadcasterUserId.Should().Be("1337");
			data.ModeratorUserName.Should().Be("The_Mod");
			data.UserId.Should().Be("1234");
			data.LowTrustStatus.Should().Be("active_monitoring");
			data.SharedBanChannelIds.Should().ContainSingle().Which.Should().Be("1234");
			data.Types.Should().Contain("manually_added");
			data.BanEvasionEvaluation.Should().Be("likely");
			data.Message.Should().NotBeNull();
			data.Message!.Value.MessageId.Should().Be("abc-123");
			data.Message!.Value.Text.Should().Be("hello");
			data.Message!.Value.Fragments.Should().HaveCount(1);
			data.Message!.Value.Fragments[0].Type.Should().Be("text");
			data.Message!.Value.Fragments[0].Cheermote.Should().BeNull();
			data.Message!.Value.Fragments[0].Emote.Should().BeNull();
		}

		[Fact]
		public void SuspiciousUserUpdateEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""1050263435"", ""broadcaster_user_name"": ""77f111cbb75341449f5"", ""broadcaster_user_login"": ""77f111cbb75341449f5"",
				""moderator_user_id"": ""1050263436"", ""moderator_user_name"": ""29087e59dfc441968f6"", ""moderator_user_login"": ""29087e59dfc441968f6"",
				""user_id"": ""1050263437"", ""user_name"": ""06fbcc75952245c5a87"", ""user_login"": ""06fbcc75952245c5a87"",
				""low_trust_status"": ""restricted""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubSafetySerializerContext.Default.SuspiciousUserUpdateEvent);

			data.BroadcasterUserId.Should().Be("1050263435");
			data.BroadcasterUserLogin.Should().Be("77f111cbb75341449f5");
			data.ModeratorUserId.Should().Be("1050263436");
			data.ModeratorUserLogin.Should().Be("29087e59dfc441968f6");
			data.UserId.Should().Be("1050263437");
			data.UserName.Should().Be("06fbcc75952245c5a87");
			data.LowTrustStatus.Should().Be("restricted");
		}

		[Fact]
		public void UnbanRequestCreateEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""1234"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""user_id"": ""1234"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User"",
				""text"": ""Please unban me"",
				""created_at"": ""2022-07-26T17:00:03.17106713Z""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubSafetySerializerContext.Default.UnbanRequestEvent);

			data.Id.Should().Be("1234");
			data.BroadcasterUserId.Should().Be("1337");
			data.UserLogin.Should().Be("cool_user");
			data.Text.Should().Be("Please unban me");
			data.CreatedAt.Should().Be("2022-07-26T17:00:03.17106713Z");
			data.ModeratorUserId.Should().BeNull();
			data.ResolutionText.Should().BeNull();
			data.Status.Should().BeNull();
		}

		[Fact]
		public void UnbanRequestResolveEventBindsOfficialPayload()
		{
			const string json = @"{
				""id"": ""1234"",
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cool_user"", ""broadcaster_user_name"": ""Cool_User"",
				""moderator_user_id"": ""9001"", ""moderator_user_login"": ""the_mod"", ""moderator_user_name"": ""The_Mod"",
				""user_id"": ""1234"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User"",
				""resolution_text"": ""Unbanned"",
				""status"": ""approved""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubSafetySerializerContext.Default.UnbanRequestEvent);

			data.Id.Should().Be("1234");
			data.ModeratorUserId.Should().Be("9001");
			data.ModeratorUserLogin.Should().Be("the_mod");
			data.ModeratorUserName.Should().Be("The_Mod");
			data.UserName.Should().Be("Cool_User");
			data.ResolutionText.Should().Be("Unbanned");
			data.Status.Should().Be("approved");
			data.Text.Should().BeNull();
		}

		[Fact]
		public void WarningSendEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""423374343"", ""broadcaster_user_login"": ""glowillig"", ""broadcaster_user_name"": ""glowillig"",
				""moderator_user_id"": ""424596340"", ""moderator_user_login"": ""quotrok"", ""moderator_user_name"": ""quotrok"",
				""user_id"": ""141981764"", ""user_login"": ""twitchdev"", ""user_name"": ""TwitchDev"",
				""reason"": ""cut it out"", ""chat_rules_cited"": null
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubSafetySerializerContext.Default.WarningEvent);

			data.BroadcasterUserId.Should().Be("423374343");
			data.ModeratorUserId.Should().Be("424596340");
			data.ModeratorUserLogin.Should().Be("quotrok");
			data.UserId.Should().Be("141981764");
			data.UserName.Should().Be("TwitchDev");
			data.Reason.Should().Be("cut it out");
			data.ChatRulesCited.Should().BeNull();
		}

		[Fact]
		public void WarningAcknowledgeEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""423374343"", ""broadcaster_user_login"": ""glowillig"", ""broadcaster_user_name"": ""glowillig"",
				""user_id"": ""141981764"", ""user_login"": ""twitchdev"", ""user_name"": ""TwitchDev""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubSafetySerializerContext.Default.WarningEvent);

			data.BroadcasterUserId.Should().Be("423374343");
			data.BroadcasterUserLogin.Should().Be("glowillig");
			data.UserId.Should().Be("141981764");
			data.UserLogin.Should().Be("twitchdev");
			data.UserName.Should().Be("TwitchDev");
			data.ModeratorUserId.Should().BeNull();
			data.Reason.Should().BeNull();
		}
	}
}
