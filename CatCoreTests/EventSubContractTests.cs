using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.EventSub;
using CatCore.Models.Twitch.Helix.Requests;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Contract tests for the EventSub payload models and the Helix EventSub request/response DTOs.
	/// They describe the payload shapes Twitch sends and verify that CatCore's models bind them
	/// without relying on the transport/service implementation.
	/// </summary>
	public class EventSubContractTests
	{
		[Fact]
		public void MetadataParsesNanosecondTimestampAndRoutingFields()
		{
			const string json = @"{
				""message_id"": ""96a3f3b5-5dec-4eed-908e-e11ee657416c"",
				""message_type"": ""notification"",
				""message_timestamp"": ""2023-07-19T14:56:51.634234626Z"",
				""subscription_type"": ""channel.chat.message"",
				""subscription_version"": ""1""
			}";

			var metadata = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubMetadata);

			metadata.MessageId.Should().Be("96a3f3b5-5dec-4eed-908e-e11ee657416c");
			metadata.MessageType.Should().Be("notification");
			metadata.SubscriptionType.Should().Be("channel.chat.message");
			metadata.SubscriptionVersion.Should().Be("1");
			metadata.MessageTimestamp.Should().BeAfter(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));
		}

		[Fact]
		public void SessionWelcomePayloadParses()
		{
			const string json = @"{
				""session"": {
					""id"": ""AQoQILE98gtqShGmLD7AM6yJThAB"",
					""status"": ""connected"",
					""keepalive_timeout_seconds"": 10,
					""reconnect_url"": null,
					""created_at"": ""2023-07-19T14:56:51.616329898Z""
				}
			}";

			var payload = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubSessionPayload);

			payload.Session.Id.Should().Be("AQoQILE98gtqShGmLD7AM6yJThAB");
			payload.Session.Status.Should().Be("connected");
			payload.Session.KeepaliveTimeoutSeconds.Should().Be(10);
			payload.Session.ReconnectUrl.Should().BeNull();
		}

		[Fact]
		public void ChatMessagePayloadParsesFragmentsBadgesCheerAndReply()
		{
			const string json = @"{
				""event"": {
					""broadcaster_user_id"": ""1337"",
					""broadcaster_user_login"": ""cool_user"",
					""broadcaster_user_name"": ""Cool_User"",
					""chatter_user_id"": ""9001"",
					""chatter_user_login"": ""viewer23"",
					""chatter_user_name"": ""Viewer23"",
					""message_id"": ""d62235c8-47ff-a4f4-84e8-5a29a65a9c03"",
					""color"": ""#1E90FF"",
					""message_type"": ""text"",
					""badges"": [ { ""set_id"": ""subscriber"", ""id"": ""12"", ""info"": ""24"" } ],
					""cheer"": { ""bits"": 100 },
					""reply"": {
						""parent_message_id"": ""parent-id"",
						""parent_message_body"": ""parent body"",
						""parent_user_id"": ""42"",
						""parent_user_login"": ""parent_login"",
						""parent_user_name"": ""Parent_Name""
					},
					""message"": {
						""text"": ""Cheer100 Kappa"",
						""fragments"": [
							{ ""type"": ""text"", ""text"": ""Cheer100 "" },
							{ ""type"": ""cheermote"", ""text"": ""Cheer100"", ""cheermote"": { ""bits"": 100, ""tier"": 1 } },
							{ ""type"": ""emote"", ""text"": ""Kappa"", ""emote"": { ""id"": ""25"", ""emote_set_id"": ""0"", ""owner_id"": ""1"", ""format"": [ ""static"" ] } }
						]
					}
				}
			}";

			var payload = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubChatMessagePayload);
			var ev = payload.Event;

			ev.BroadcasterUserId.Should().Be("1337");
			ev.ChatterUserId.Should().Be("9001");
			ev.ChatterUserName.Should().Be("Viewer23");
			ev.MessageId.Should().Be("d62235c8-47ff-a4f4-84e8-5a29a65a9c03");
			ev.Color.Should().Be("#1E90FF");
			ev.MessageType.Should().Be("text");
			ev.Badges.Should().HaveCount(1);
			ev.Badges[0].SetId.Should().Be("subscriber");
			ev.Badges[0].Info.Should().Be("24");
			ev.Cheer.Should().NotBeNull();
			ev.Cheer!.Value.Bits.Should().Be(100);
			ev.Reply.Should().NotBeNull();
			ev.Reply!.Value.ParentMessageId.Should().Be("parent-id");
			ev.Reply!.Value.ParentUserLogin.Should().Be("parent_login");

			ev.Message.Text.Should().Be("Cheer100 Kappa");
			ev.Message.Fragments.Should().HaveCount(3);
			ev.Message.Fragments[0].Type.Should().Be("text");
			ev.Message.Fragments[1].Cheermote.Should().NotBeNull();
			ev.Message.Fragments[1].Cheermote!.Value.Bits.Should().Be(100);
			ev.Message.Fragments[2].Emote.Should().NotBeNull();
			ev.Message.Fragments[2].Emote!.Value.Id.Should().Be("25");
			ev.Message.Fragments[2].Emote!.Value.Format.Should().Contain("static");
		}

		[Fact]
		public void ChatNotificationPayloadParsesCommonNoticeObjects()
		{
			const string json = @"{
				""event"": {
					""broadcaster_user_id"": ""1337"",
					""broadcaster_user_login"": ""cool_user"",
					""broadcaster_user_name"": ""Cool_User"",
					""chatter_user_id"": ""9001"",
					""chatter_user_login"": ""viewer23"",
					""chatter_user_name"": ""Viewer23"",
					""notice_type"": ""resub"",
					""system_message"": ""Viewer23 subscribed at Tier 2."",
					""message"": { ""text"": """", ""fragments"": [] },
					""sub"": { ""sub_tier"": ""2000"", ""duration_months"": 1, ""is_gift"": false },
					""resub"": {
						""cumulative_months"": 12,
						""duration_months"": 1,
						""streak_months"": null,
						""sub_tier"": ""2000"",
						""is_streak"": true
					},
					""raid"": { ""user_id"": ""777"", ""user_login"": ""raider_login"", ""user_name"": ""Raider_Name"", ""viewer_count"": 42 },
					""announcement"": { ""color"": ""PRIMARY"" },
					""bits_badge_tier"": { ""tier"": 1000 }
				}
			}";

			var payload = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubChatNotificationPayload);
			var ev = payload.Event;

			ev.NoticeType.Should().Be("resub");
			ev.SystemMessage.Should().Be("Viewer23 subscribed at Tier 2.");
			ev.Sub.Should().NotBeNull();
			ev.Sub!.Value.SubTier.Should().Be("2000");
			ev.Resub.Should().NotBeNull();
			ev.Resub!.Value.CumulativeMonths.Should().Be(12);
			ev.Resub!.Value.StreakMonths.Should().BeNull();
			ev.Raid.Should().NotBeNull();
			ev.Raid!.Value.ViewerCount.Should().Be(42);
			ev.Announcement.Should().NotBeNull();
			ev.Announcement!.Value.Color.Should().Be("PRIMARY");
			ev.BitsBadgeTier.Should().NotBeNull();
			ev.BitsBadgeTier!.Value.Tier.Should().Be(1000);
		}

		[Fact]
		public void ChatClearPayloadParses()
		{
			const string json = @"{
				""event"": {
					""broadcaster_user_id"": ""1337"",
					""broadcaster_user_login"": ""cool_user"",
					""broadcaster_user_name"": ""Cool_User""
				}
			}";

			var payload = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubChatClearPayload);

			payload.Event.BroadcasterUserId.Should().Be("1337");
			payload.Event.BroadcasterUserLogin.Should().Be("cool_user");
		}

		[Fact]
		public void ChatMessageDeletePayloadParses()
		{
			const string json = @"{
				""event"": {
					""broadcaster_user_id"": ""1337"",
					""broadcaster_user_login"": ""cool_user"",
					""broadcaster_user_name"": ""Cool_User"",
					""target_user_id"": ""9001"",
					""target_user_login"": ""viewer23"",
					""target_user_name"": ""Viewer23"",
					""message_id"": ""deleted-message-id""
				}
			}";

			var payload = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubChatMessageDeletePayload);

			payload.Event.TargetUserId.Should().Be("9001");
			payload.Event.TargetUserLogin.Should().Be("viewer23");
			payload.Event.MessageId.Should().Be("deleted-message-id");
		}

		[Fact]
		public void ChatSettingsPayloadParsesNonNullSettings()
		{
			const string json = @"{
				""event"": {
					""broadcaster_user_id"": ""1337"",
					""broadcaster_user_login"": ""cool_user"",
					""broadcaster_user_name"": ""Cool_User"",
					""emote_mode"": true,
					""follower_mode"": true,
					""follower_mode_duration_minutes"": 30,
					""slow_mode"": true,
					""slow_mode_wait_seconds"": 10,
					""subscriber_mode"": false,
					""unique_chat_mode"": true
				}
			}";

			var payload = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubChatSettingsPayload);

			payload.Event.EmoteMode.Should().BeTrue();
			payload.Event.FollowerMode.Should().BeTrue();
			payload.Event.FollowerModeDurationMinutes.Should().Be(30);
			payload.Event.SlowMode.Should().BeTrue();
			payload.Event.SlowModeWaitSeconds.Should().Be(10);
			payload.Event.SubscriberMode.Should().BeFalse();
			payload.Event.UniqueChatMode.Should().BeTrue();
		}

		[Fact]
		public void SendChatMessageRequestSerializesHelixContract()
		{
			var request = new SendChatMessageRequestDto
			{
				BroadcasterId = "111",
				SenderId = "222",
				Message = "hello world"
			};

			var json = JsonSerializer.Serialize(request, TwitchHelixSerializerContext.Default.SendChatMessageRequestDto);

			json.Should().Contain("\"broadcaster_id\":\"111\"");
			json.Should().Contain("\"sender_id\":\"222\"");
			json.Should().Contain("\"message\":\"hello world\"");
			json.Should().NotContain("reply_parent_message_id");
		}

		[Fact]
		public void EventSubSubscriptionRequestSerializesHelixContract()
		{
			var request = new EventSubSubscriptionRequestDto
			{
				Type = "channel.chat.message",
				Version = "1",
				Condition = new Dictionary<string, string>
				{
					{ "broadcaster_user_id", "111" },
					{ "user_id", "222" }
				},
				Transport = new EventSubTransportDto
				{
					Method = "websocket",
					SessionId = "session-1"
				}
			};

			var json = JsonSerializer.Serialize(request, TwitchHelixSerializerContext.Default.EventSubSubscriptionRequestDto);

			json.Should().Contain("\"type\":\"channel.chat.message\"");
			json.Should().Contain("\"version\":\"1\"");
			json.Should().Contain("\"broadcaster_user_id\":\"111\"");
			json.Should().Contain("\"user_id\":\"222\"");
			json.Should().Contain("\"method\":\"websocket\"");
			json.Should().Contain("\"session_id\":\"session-1\"");
		}

		[Fact]
		public void EventSubSubscriptionResponseDeserializes()
		{
			const string json = @"{
				""data"": [
					{
						""id"": ""sub-1"",
						""type"": ""channel.chat.message"",
						""version"": ""1"",
						""status"": ""enabled"",
						""condition"": { ""broadcaster_user_id"": ""111"", ""user_id"": ""222"" },
						""transport"": { ""method"": ""websocket"", ""session_id"": ""session-1"" },
						""created_at"": ""2023-11-06T18:11:47.4922535Z"",
						""cost"": 0
					}
				],
				""total"": 1,
				""total_cost"": 0,
				""max_total_cost"": 10
			}";

			var response = JsonSerializer.Deserialize(json, TwitchHelixSerializerContext.Default.EventSubSubscriptionResponseDto);

			response.Data.Should().HaveCount(1);
			response.Data[0].Id.Should().Be("sub-1");
			response.Data[0].Type.Should().Be("channel.chat.message");
			response.Data[0].Status.Should().Be("enabled");
			response.Data[0].Condition["user_id"].Should().Be("222");
			response.Data[0].Transport["session_id"].Should().Be("session-1");
			response.Total.Should().Be(1);
			response.MaxTotalCost.Should().Be(10);
		}

		[Fact]
		public void EventSubSerializerContextCoversAllPayloadTypes()
		{
			var context = TwitchEventSubSerializerContext.Default;
			var payloadTypes = new[]
			{
				typeof(EventSubMetadata),
				typeof(EventSubSessionPayload),
				typeof(EventSubChatMessagePayload),
				typeof(EventSubChatNotificationPayload),
				typeof(EventSubChatClearPayload),
				typeof(EventSubChatMessageDeletePayload),
				typeof(EventSubChatSettingsPayload)
			};

			foreach (var payloadType in payloadTypes)
			{
				context.GetTypeInfo(payloadType).Should().NotBeNull($"the source-generated context must cover {payloadType.Name}");
			}
		}
	}
}
