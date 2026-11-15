using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.EventSub.AutoMod;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Contract tests for the AutoMod and chat user message hold/update EventSub payload models.
	/// The JSON samples are the official Twitch documentation samples.
	/// </summary>
	public class EventSubAutoModContractTests
	{
		[Fact]
		public void MessageHoldEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""blah"", ""broadcaster_user_name"": ""blahblah"",
				""user_id"": ""4242"", ""user_login"": ""baduser"", ""user_name"": ""badbaduser"",
				""message_id"": ""bad-message-id"",
				""message"": {
					""text"": ""This is a bad message… pogchamp"",
					""fragments"": [
						{ ""type"": ""text"", ""text"": ""This is a bad message… "", ""cheermote"": null, ""emote"": null },
						{ ""type"": ""cheermote"", ""text"": ""pogchamp"", ""cheermote"": { ""prefix"": ""pogchamp"", ""bits"": 1000, ""tier"": 1 }, ""emote"": null }
					]
				},
				""reason"": ""automod"",
				""automod"": { ""category"": ""aggressive"", ""level"": 1, ""boundaries"": [ { ""start_pos"": 0, ""end_pos"": 10 }, { ""start_pos"": 20, ""end_pos"": 30 } ] },
				""blocked_term"": null,
				""held_at"": ""2022-12-02T15:00:00.00Z""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubAutoModSerializerContext.Default.AutoModMessageHoldEvent);

			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserLogin.Should().Be("blah");
			data.UserId.Should().Be("4242");
			data.UserLogin.Should().Be("baduser");
			data.MessageId.Should().Be("bad-message-id");
			data.Reason.Should().Be("automod");
			data.HeldAt.Should().Be("2022-12-02T15:00:00.00Z");

			data.Message.Text.Should().Be("This is a bad message… pogchamp");
			var fragments = data.Message.Fragments!;
			fragments.Should().HaveCount(2);
			fragments[0].Type.Should().Be("text");
			fragments[0].Text.Should().Be("This is a bad message… ");
			fragments[1].Type.Should().Be("cheermote");
			fragments[1].Cheermote.HasValue.Should().BeTrue();
			fragments[1].Cheermote.GetValueOrDefault().Prefix.Should().Be("pogchamp");
			fragments[1].Cheermote.GetValueOrDefault().Bits.Should().Be(1000);
			fragments[1].Cheermote.GetValueOrDefault().Tier.Should().Be(1);

			data.Automod.HasValue.Should().BeTrue();
			var automod = data.Automod.GetValueOrDefault();
			automod.Category.Should().Be("aggressive");
			automod.Level.Should().Be(1);
			automod.Boundaries.Should().HaveCount(2);
			automod.Boundaries![0].StartPos.Should().Be(0);
			automod.Boundaries![0].EndPos.Should().Be(10);
			automod.Boundaries![1].StartPos.Should().Be(20);
			automod.Boundaries![1].EndPos.Should().Be(30);

			data.BlockedTerm.HasValue.Should().BeFalse();
		}

		[Fact]
		public void MessageUpdateEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""blah"", ""broadcaster_user_name"": ""blahblah"",
				""moderator_user_id"": ""9001"", ""moderator_user_login"": ""the_mod"", ""moderator_user_name"": ""The_Mod"",
				""user_id"": ""4242"", ""user_login"": ""baduser"", ""user_name"": ""badbaduser"",
				""message_id"": ""bad-message-id"",
				""message"": {
					""text"": ""This is a bad message… pogchamp"",
					""fragments"": [
						{ ""type"": ""text"", ""text"": ""This is a bad message… "", ""cheermote"": null, ""emote"": null },
						{ ""type"": ""cheermote"", ""text"": ""pogchamp"", ""cheermote"": { ""prefix"": ""pogchamp"", ""bits"": 1000, ""tier"": 1 }, ""emote"": null }
					]
				},
				""reason"": ""automod"", ""automod"": null,
				""blocked_term"": { ""terms_found"": [ { ""term_id"": ""123"", ""owner_broadcaster_user_id"": ""1337"", ""owner_broadcaster_user_login"": ""blah"", ""owner_broadcaster_user_name"": ""blahblah"", ""boundary"": { ""start_pos"": 0, ""end_pos"": 30 } } ] },
				""status"": ""approved"", ""held_at"": ""2022-12-02T15:00:00.00Z""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubAutoModSerializerContext.Default.AutoModMessageUpdateEvent);

			data.BroadcasterUserId.Should().Be("1337");
			data.ModeratorUserId.Should().Be("9001");
			data.ModeratorUserLogin.Should().Be("the_mod");
			data.ModeratorUserName.Should().Be("The_Mod");
			data.UserId.Should().Be("4242");
			data.MessageId.Should().Be("bad-message-id");
			data.Reason.Should().Be("automod");
			data.Status.Should().Be("approved");
			data.HeldAt.Should().Be("2022-12-02T15:00:00.00Z");

			data.Message.Text.Should().Be("This is a bad message… pogchamp");
			data.Message.Fragments.Should().HaveCount(2);

			data.Automod.HasValue.Should().BeFalse();

			data.BlockedTerm.HasValue.Should().BeTrue();
			var termsFound = data.BlockedTerm.GetValueOrDefault().TermsFound!;
			termsFound.Should().HaveCount(1);
			termsFound[0].TermId.Should().Be("123");
			termsFound[0].OwnerBroadcasterUserId.Should().Be("1337");
			termsFound[0].OwnerBroadcasterUserLogin.Should().Be("blah");
			termsFound[0].OwnerBroadcasterUserName.Should().Be("blahblah");
			termsFound[0].Boundary.StartPos.Should().Be(0);
			termsFound[0].Boundary.EndPos.Should().Be(30);
		}

		[Fact]
		public void SettingsUpdateEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_login"": ""cooluser"", ""broadcaster_user_name"": ""CoolUser"",
				""moderator_user_id"": ""9001"", ""moderator_user_login"": ""coolmod"", ""moderator_user_name"": ""CoolMod"",
				""bullying"": 3, ""overall_level"": null, ""disability"": 3, ""race_ethnicity_or_religion"": 3,
				""misogyny"": 3, ""sexuality_sex_or_gender"": 3, ""aggression"": 3, ""sex_based_terms"": 3, ""swearing"": 0
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubAutoModSerializerContext.Default.AutoModSettingsEvent);

			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserLogin.Should().Be("cooluser");
			data.BroadcasterUserName.Should().Be("CoolUser");
			data.ModeratorUserId.Should().Be("9001");
			data.ModeratorUserLogin.Should().Be("coolmod");
			data.ModeratorUserName.Should().Be("CoolMod");
			data.Bullying.Should().Be(3);
			data.OverallLevel.Should().BeNull();
			data.Disability.Should().Be(3);
			data.RaceEthnicityOrReligion.Should().Be(3);
			data.Misogyny.Should().Be(3);
			data.SexualitySexOrGender.Should().Be(3);
			data.Aggression.Should().Be(3);
			data.SexBasedTerms.Should().Be(3);
			data.Swearing.Should().Be(0);
		}

		[Fact]
		public void TermsUpdateEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""1337"", ""broadcaster_user_name"": ""blah"", ""broadcaster_user_login"": ""blahblah"",
				""moderator_user_id"": ""9001"", ""moderator_user_login"": ""the_mod"", ""moderator_user_name"": ""The_Mod"",
				""action"": ""add_blocked"", ""from_automod"": true,
				""terms"": [ ""automodterm1"", ""automodterm2"", ""automodterm3"" ]
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubAutoModSerializerContext.Default.AutoModTermsEvent);

			data.BroadcasterUserId.Should().Be("1337");
			data.BroadcasterUserLogin.Should().Be("blahblah");
			data.ModeratorUserId.Should().Be("9001");
			data.ModeratorUserName.Should().Be("The_Mod");
			data.Action.Should().Be("add_blocked");
			data.FromAutomod.Should().BeTrue();
			data.Terms.Should().HaveCount(3);
			data.Terms[0].Should().Be("automodterm1");
			data.Terms[1].Should().Be("automodterm2");
			data.Terms[2].Should().Be("automodterm3");
		}

		[Fact]
		public void UserMessageHoldEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""123"", ""broadcaster_user_login"": ""bob"", ""broadcaster_user_name"": ""Bob"",
				""user_id"": ""456"", ""user_login"": ""tom"", ""user_name"": ""Tommy"",
				""message_id"": ""789"",
				""message"": {
					""text"": ""hey world"",
					""fragments"": [
						{ ""type"": ""emote"", ""text"": ""hey world"", ""cheermote"": null, ""emote"": { ""id"": ""foo"", ""emote_set_id"": ""7"" } },
						{ ""type"": ""cheermote"", ""text"": ""bye world"", ""cheermote"": { ""prefix"": ""prefix"", ""bits"": 100, ""tier"": 1 }, ""emote"": null },
						{ ""type"": ""text"", ""text"": ""surprise"", ""cheermote"": null, ""emote"": null }
					]
				}
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubAutoModSerializerContext.Default.ChatUserMessageHoldEvent);

			data.BroadcasterUserId.Should().Be("123");
			data.BroadcasterUserLogin.Should().Be("bob");
			data.BroadcasterUserName.Should().Be("Bob");
			data.UserId.Should().Be("456");
			data.UserLogin.Should().Be("tom");
			data.UserName.Should().Be("Tommy");
			data.MessageId.Should().Be("789");

			data.Message.Text.Should().Be("hey world");
			var fragments = data.Message.Fragments!;
			fragments.Should().HaveCount(3);

			fragments[0].Type.Should().Be("emote");
			fragments[0].Text.Should().Be("hey world");
			fragments[0].Emote.HasValue.Should().BeTrue();
			fragments[0].Emote.GetValueOrDefault().Id.Should().Be("foo");
			fragments[0].Emote.GetValueOrDefault().EmoteSetId.Should().Be("7");

			fragments[1].Type.Should().Be("cheermote");
			fragments[1].Text.Should().Be("bye world");
			fragments[1].Cheermote.HasValue.Should().BeTrue();
			fragments[1].Cheermote.GetValueOrDefault().Prefix.Should().Be("prefix");
			fragments[1].Cheermote.GetValueOrDefault().Bits.Should().Be(100);
			fragments[1].Cheermote.GetValueOrDefault().Tier.Should().Be(1);

			fragments[2].Type.Should().Be("text");
			fragments[2].Text.Should().Be("surprise");
			fragments[2].Emote.HasValue.Should().BeFalse();
		}

		[Fact]
		public void UserMessageUpdateEventBindsOfficialPayload()
		{
			const string json = @"{
				""broadcaster_user_id"": ""123"", ""broadcaster_user_login"": ""bob"", ""broadcaster_user_name"": ""Bob"",
				""user_id"": ""456"", ""user_login"": ""tom"", ""user_name"": ""Tommy"",
				""status"": ""approved"",
				""message_id"": ""789"",
				""message"": {
					""text"": ""hey world"",
					""fragments"": [
						{ ""type"": ""emote"", ""text"": ""hey world"", ""cheermote"": null, ""emote"": { ""id"": ""foo"", ""emote_set_id"": ""7"" } },
						{ ""type"": ""cheermote"", ""text"": ""bye world"", ""cheermote"": { ""prefix"": ""prefix"", ""bits"": 100, ""tier"": 1 }, ""emote"": null },
						{ ""type"": ""text"", ""text"": ""surprise"", ""cheermote"": null, ""emote"": null }
					]
				}
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubAutoModSerializerContext.Default.ChatUserMessageUpdateEvent);

			data.BroadcasterUserId.Should().Be("123");
			data.UserId.Should().Be("456");
			data.UserName.Should().Be("Tommy");
			data.MessageId.Should().Be("789");
			data.Status.Should().Be("approved");

			data.Message.Text.Should().Be("hey world");
			data.Message.Fragments.Should().HaveCount(3);
			data.Message.Fragments![0].Emote.GetValueOrDefault().Id.Should().Be("foo");
			data.Message.Fragments![1].Cheermote.GetValueOrDefault().Bits.Should().Be(100);
			data.Message.Fragments![2].Text.Should().Be("surprise");
		}
	}
}
