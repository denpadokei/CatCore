using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Services.Twitch;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	public class SharedChatTests
	{
		[Theory]
		[InlineData("shared_chat_raid", "raid")]
		[InlineData("shared_chat_resub", "resub")]
		[InlineData("shared_chat_sub_gift", "subgift")]
		[InlineData("shared_chat_community_sub_gift", "submysterygift")]
		[InlineData("shared_chat_announcement", "announcement")]
		[InlineData("sub_gift", "subgift")]
		[InlineData("resub", "resub")]
		[InlineData("", "usernotice")]
		[InlineData(null, "usernotice")]
		public void SharedChatNoticeTypesMapToLegacyMsgIds(string? noticeType, string expectedMsgId)
		{
			TwitchEventSubChatService.MapNoticeTypeToLegacyMsgId(noticeType).Should().Be(expectedMsgId);
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
		public void SharedChatMessageBindsSourceBroadcasterFields()
		{
			const string json = @"{
				""event"": {
					""broadcaster_user_id"": ""1337"",
					""broadcaster_user_login"": ""cool_user"",
					""broadcaster_user_name"": ""Cool_User"",
					""chatter_user_id"": ""9001"",
					""chatter_user_login"": ""viewer23"",
					""chatter_user_name"": ""Viewer23"",
					""message_id"": ""abc"",
					""color"": """",
					""message_type"": ""text"",
					""message"": { ""text"": ""hello"", ""fragments"": [ { ""type"": ""text"", ""text"": ""hello"" } ] },
					""source_broadcaster_user_id"": ""4242"",
					""source_broadcaster_user_login"": ""source_login"",
					""source_broadcaster_user_name"": ""Source_Name"",
					""source_message_id"": ""source-message-id"",
					""is_source_only"": true
				}
			}";

			var payload = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubChatMessagePayload);

			var ev = payload.Event;
			ev.SourceBroadcasterUserId.Should().Be("4242");
			ev.SourceBroadcasterUserLogin.Should().Be("source_login");
			ev.SourceBroadcasterUserName.Should().Be("Source_Name");
			ev.SourceMessageId.Should().Be("source-message-id");
			ev.IsSourceOnly.Should().BeTrue();
			ev.SourceBadges.Should().BeNull();
		}
	}
}
