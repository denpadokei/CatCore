using System.Collections.Generic;
using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.EventSub;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Twitch;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	public class SharedChatTests
	{
		// A notice that happened in another channel of a shared chat is delivered over IRC as
		// msg-id=sharedchatnotice with the original notice type in source-msg-id.
		[Theory]
		[InlineData("shared_chat_raid", "sharedchatnotice")]
		[InlineData("shared_chat_resub", "sharedchatnotice")]
		[InlineData("shared_chat_sub_gift", "sharedchatnotice")]
		[InlineData("shared_chat_community_sub_gift", "sharedchatnotice")]
		[InlineData("shared_chat_announcement", "sharedchatnotice")]
		[InlineData("sub_gift", "subgift")]
		[InlineData("resub", "resub")]
		[InlineData("", "usernotice")]
		[InlineData(null, "usernotice")]
		public void NoticeTypesMapToLegacyMsgIds(string? noticeType, string expectedMsgId)
		{
			TwitchEventSubChatService.MapNoticeTypeToLegacyMsgId(noticeType).Should().Be(expectedMsgId);
		}

		[Theory]
		[InlineData("raid", "raid")]
		[InlineData("resub", "resub")]
		[InlineData("sub_gift", "subgift")]
		[InlineData("community_sub_gift", "submysterygift")]
		[InlineData("gift_paid_upgrade", "giftpaidupgrade")]
		[InlineData("prime_paid_upgrade", "primepaidupgrade")]
		[InlineData("pay_it_forward", "paysforward")]
		[InlineData("announcement", "announcement")]
		[InlineData("bits_badge_tier", "bitsbadgetier")]
		[InlineData("charity_donation", "charitydonation")]
		[InlineData("watch_streak", "watchstreak")]
		[InlineData(null, "usernotice")]
		public void UnderlyingNoticeTypesMapToLegacyMsgIds(string? noticeType, string expectedMsgId)
		{
			TwitchEventSubChatService.MapUnderlyingNoticeTypeToLegacyMsgId(noticeType).Should().Be(expectedMsgId);
		}

		[Fact]
		public void SharedChatNotificationBindsSourceAndNoticeObjects()
		{
			const string json = @"{
				""event"": {
					""broadcaster_user_id"": ""1337"",
					""broadcaster_user_login"": ""cool_user"",
					""broadcaster_user_name"": ""Cool_User"",
					""chatter_user_id"": ""9001"",
					""chatter_user_login"": ""viewer23"",
					""chatter_user_name"": ""Viewer23"",
					""notice_type"": ""shared_chat_raid"",
					""message"": { ""text"": """", ""fragments"": [] },
					""source_broadcaster_user_id"": ""4242"",
					""source_broadcaster_user_login"": ""source_login"",
					""source_broadcaster_user_name"": ""Source_Name"",
					""source_message_id"": ""source-message-id"",
					""source_badges"": [ { ""set_id"": ""moderator"", ""id"": ""1"", ""info"": null } ],
					""is_source_only"": false,
					""shared_chat_raid"": {
						""user_id"": ""777"",
						""user_login"": ""raider_login"",
						""user_name"": ""Raider_Name"",
						""viewer_count"": 42,
						""profile_image_url"": ""https://example.invalid/avatar.png""
					},
					""shared_chat_resub"": {
						""duration_months"": 1,
						""cumulative_months"": 12,
						""sub_tier"": ""1000"",
						""is_gift"": true,
						""gifter_user_login"": ""gifter_login"",
						""gifter_user_name"": ""Gifter_Name""
					}
				}
			}";

			var payload = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubChatNotificationPayload);

			var ev = payload.Event;
			ev.NoticeType.Should().Be("shared_chat_raid");
			ev.SourceBroadcasterUserId.Should().Be("4242");
			ev.SourceBroadcasterUserLogin.Should().Be("source_login");
			ev.SourceBroadcasterUserName.Should().Be("Source_Name");
			ev.SourceMessageId.Should().Be("source-message-id");
			ev.SourceBadges.Should().NotBeNull();
			ev.SourceBadges!.Count.Should().Be(1);
			ev.IsSourceOnly.Should().BeFalse();

			ev.SharedChatRaid.Should().NotBeNull();
			ev.SharedChatRaid!.Value.UserLogin.Should().Be("raider_login");
			ev.SharedChatRaid!.Value.ViewerCount.Should().Be(42);
			ev.SharedChatRaid!.Value.ProfileImageUrl.Should().Be("https://example.invalid/avatar.png");

			ev.SharedChatResub.Should().NotBeNull();
			ev.SharedChatResub!.Value.CumulativeMonths.Should().Be(12);
			ev.SharedChatResub!.Value.GifterUserLogin.Should().Be("gifter_login");
			ev.SharedChatSub.Should().BeNull();
		}

		[Fact]
		public void SharedChatRaidMetadataUsesSharedNoticeAndSourceTags()
		{
			const string json = @"{
				""event"": {
					""broadcaster_user_id"": ""1337"",
					""broadcaster_user_login"": ""cool_user"",
					""broadcaster_user_name"": ""Cool_User"",
					""chatter_user_id"": ""9001"",
					""chatter_user_login"": ""viewer23"",
					""chatter_user_name"": ""Viewer23"",
					""notice_type"": ""shared_chat_raid"",
					""message"": { ""text"": """", ""fragments"": [] },
					""source_broadcaster_user_id"": ""4242"",
					""source_message_id"": ""source-message-id"",
					""source_badges"": [
						{ ""set_id"": ""subscriber"", ""id"": ""12"", ""info"": ""24"" },
						{ ""set_id"": ""moderator"", ""id"": ""1"", ""info"": null }
					],
					""is_source_only"": false,
					""shared_chat_raid"": {
						""user_id"": ""777"",
						""user_login"": ""raider_login"",
						""user_name"": ""Raider_Name"",
						""viewer_count"": 42
					}
				}
			}";

			var payload = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubChatNotificationPayload);
			var metadata = TwitchEventSubChatService.BuildLegacyUserNoticeMetadataCore(payload.Event, "Raider_Name is raiding with a party of 42 viewers.");

			metadata[IrcMessageTags.MSG_ID].Should().Be("sharedchatnotice");
			metadata[IrcMessageTags.SOURCE_MSG_ID].Should().Be("raid");
			metadata[IrcMessageTags.SOURCE_ROOM_ID].Should().Be("4242");
			metadata[IrcMessageTags.SOURCE_ID].Should().Be("source-message-id");
			metadata[IrcMessageTags.SOURCE_BADGES].Should().Be("subscriber/12,moderator/1");
			metadata[IrcMessageTags.SOURCE_BADGE_INFO].Should().Be("subscriber/24");
			metadata.Should().NotContainKey(IrcMessageTags.SOURCE_ONLY);
			metadata[IrcMessageTags.MSG_PARAM_VIEWER_COUNT].Should().Be("42");
			metadata[IrcMessageTags.MSG_PARAM_LOGIN].Should().Be("raider_login");
			metadata[IrcMessageTags.MSG_PARAM_DISPLAY_NAME].Should().Be("Raider_Name");
		}

		[Fact]
		public void SharedChatSubGiftMetadataUsesUnderlyingSourceMsgId()
		{
			const string json = @"{
				""event"": {
					""broadcaster_user_id"": ""1337"",
					""broadcaster_user_login"": ""cool_user"",
					""broadcaster_user_name"": ""Cool_User"",
					""chatter_user_id"": ""9001"",
					""chatter_user_login"": ""viewer23"",
					""chatter_user_name"": ""Viewer23"",
					""notice_type"": ""shared_chat_sub_gift"",
					""message"": { ""text"": """", ""fragments"": [] },
					""shared_chat_sub_gift"": {
						""duration_months"": 1,
						""recipient_user_id"": ""4242"",
						""recipient_user_login"": ""recipient_login"",
						""recipient_user_name"": ""Recipient_Name"",
						""sub_tier"": ""1000"",
						""community_gift_id"": null
					}
				}
			}";

			var payload = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubChatNotificationPayload);
			var metadata = TwitchEventSubChatService.BuildLegacyUserNoticeMetadataCore(payload.Event, "Someone gifted a sub to Recipient_Name.");

			metadata[IrcMessageTags.MSG_ID].Should().Be("sharedchatnotice");
			metadata[IrcMessageTags.SOURCE_MSG_ID].Should().Be("subgift");
			metadata[IrcMessageTags.MSG_PARAM_SUB_PLAN].Should().Be("1000");
			metadata[IrcMessageTags.MSG_PARAM_RECIPIENT_DISPLAY_NAME].Should().Be("Recipient_Name");
		}

		[Fact]
		public void LocalNoticeDoesNotEmitSharedChatSourceMsgId()
		{
			const string json = @"{
				""event"": {
					""broadcaster_user_id"": ""1337"",
					""broadcaster_user_login"": ""cool_user"",
					""broadcaster_user_name"": ""Cool_User"",
					""chatter_user_id"": ""9001"",
					""chatter_user_login"": ""viewer23"",
					""chatter_user_name"": ""Viewer23"",
					""notice_type"": ""raid"",
					""message"": { ""text"": """", ""fragments"": [] },
					""raid"": { ""user_id"": ""777"", ""user_login"": ""raider_login"", ""user_name"": ""Raider_Name"", ""viewer_count"": 5 }
				}
			}";

			var payload = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubChatNotificationPayload);
			var metadata = TwitchEventSubChatService.BuildLegacyUserNoticeMetadataCore(payload.Event, "Raider_Name is raiding.");

			metadata[IrcMessageTags.MSG_ID].Should().Be("raid");
			metadata.Should().NotContainKey(IrcMessageTags.SOURCE_MSG_ID);
		}

		[Fact]
		public void SharedChatMessageSourceMetadataUsesOfficialTagNames()
		{
			var badges = new List<EventSubBadge>
			{
				new EventSubBadge { SetId = "subscriber", Id = "12", Info = "24" },
				new EventSubBadge { SetId = "moderator", Id = "1" }
			};

			var metadata = TwitchEventSubChatService.BuildSourceBroadcasterMetadata("4242", "source-message-id", badges, true);

			metadata.Should().NotBeNull();
			metadata![IrcMessageTags.SOURCE_ROOM_ID].Should().Be("4242");
			metadata[IrcMessageTags.SOURCE_ID].Should().Be("source-message-id");
			metadata[IrcMessageTags.SOURCE_BADGES].Should().Be("subscriber/12,moderator/1");
			metadata[IrcMessageTags.SOURCE_BADGE_INFO].Should().Be("subscriber/24");
			metadata[IrcMessageTags.SOURCE_ONLY].Should().Be("1");
		}

		[Fact]
		public void BuildSourceBroadcasterMetadataReturnsNullWithoutSourceFields()
		{
			TwitchEventSubChatService.BuildSourceBroadcasterMetadata(null, null, null, null).Should().BeNull();
		}
	}
}
