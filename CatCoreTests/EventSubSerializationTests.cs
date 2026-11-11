using System.Linq;
using System.Text.Json;
using CatCore.Helpers.JSON;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	public class EventSubSerializationTests
	{
		// Regression test for the session_reconnect payload: Twitch sends keepalive_timeout_seconds as
		// null in reconnect messages, so the DTO property must be nullable (a non-nullable int made every
		// reconnect message throw and disabled the whole reconnect flow).
		[Fact]
		public void SessionReconnectPayloadWithNullKeepaliveTimeoutDeserializes()
		{
			const string json = @"{
				""session"": {
					""id"": ""AQoQexAWVYKSTIu4ec_2VAxyuhAB"",
					""status"": ""reconnecting"",
					""keepalive_timeout_seconds"": null,
					""reconnect_url"": ""wss://eventsub.wss.twitch.tv/ws?session_id=test"",
					""connected_at"": ""2022-11-16T10:11:12.634234626Z""
				}
			}";

			var payload = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubSessionPayload);

			payload.Session.Id.Should().Be("AQoQexAWVYKSTIu4ec_2VAxyuhAB");
			payload.Session.Status.Should().Be("reconnecting");
			payload.Session.KeepaliveTimeoutSeconds.Should().BeNull();
			payload.Session.ReconnectUrl.Should().Be("wss://eventsub.wss.twitch.tv/ws?session_id=test");
		}

		// Regression test for channel.chat_settings.update: follower_mode_duration_minutes and
		// slow_mode_wait_time_seconds are nullable and lazy-initialized; the previous DTO used
		// non-nullable ints and a wrong JSON name (slow_mode_wait_seconds), which made every settings
		// event fail to deserialize and prevented room state from ever being updated.
		[Fact]
		public void ChatSettingsUpdatePayloadWithNullableFieldsDeserializes()
		{
			const string json = @"{
				""event"": {
					""broadcaster_user_id"": ""1337"",
					""broadcaster_user_login"": ""cool_user"",
					""broadcaster_user_name"": ""Cool_User"",
					""emote_mode"": false,
					""follower_mode"": false,
					""follower_mode_duration_minutes"": null,
					""slow_mode"": true,
					""slow_mode_wait_time_seconds"": 10,
					""subscriber_mode"": false,
					""unique_chat_mode"": false
				}
			}";

			var payload = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubChatSettingsPayload);

			payload.Event.FollowerMode.Should().BeFalse();
			payload.Event.FollowerModeDurationMinutes.Should().BeNull();
			payload.Event.SlowMode.Should().BeTrue();
			payload.Event.SlowModeWaitSeconds.Should().Be(10);
		}

		// Regression test for the gift-sub notice: the real JSON object is sub_gift, while the previous
		// DTO mapped gift_sub, so the object never bound. Also covers the notification identity fields
		// (color / badges / message_id) that were previously missing from the model.
		[Fact]
		public void ChatNotificationSubGiftBindsAndKeepsIdentityFields()
		{
			const string json = @"{
				""event"": {
					""broadcaster_user_id"": ""1337"",
					""broadcaster_user_login"": ""cool_user"",
					""broadcaster_user_name"": ""Cool_User"",
					""chatter_user_id"": ""9001"",
					""chatter_user_login"": ""viewer23"",
					""chatter_user_name"": ""Viewer23"",
					""chatter_is_anonymous"": false,
					""color"": ""#FF0000"",
					""badges"": [ { ""set_id"": ""moderator"", ""id"": ""1"", ""info"": null } ],
					""message_id"": ""d62235c8-47ff-a4f4-84e8-5a29a65a9c03"",
					""notice_type"": ""sub_gift"",
					""message"": { ""text"": """", ""fragments"": [] },
					""sub_gift"": {
						""duration_months"": 1,
						""cumulative_total"": null,
						""recipient_user_id"": ""4242"",
						""recipient_user_login"": ""recipient_login"",
						""recipient_user_name"": ""Recipient_Name"",
						""sub_tier"": ""1000"",
						""community_gift_id"": null
					}
				}
			}";

			var payload = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubChatNotificationPayload);

			payload.Event.NoticeType.Should().Be("sub_gift");
			payload.Event.Color.Should().Be("#FF0000");
			payload.Event.MessageId.Should().Be("d62235c8-47ff-a4f4-84e8-5a29a65a9c03");
			payload.Event.Badges.Should().NotBeNull();
			payload.Event.Badges!.Count.Should().Be(1);
			payload.Event.Badges!.Single().SetId.Should().Be("moderator");

			payload.Event.SubGift.Should().NotBeNull();
			var subGift = payload.Event.SubGift!.Value;
			subGift.RecipientUserLogin.Should().Be("recipient_login");
			subGift.RecipientUserName.Should().Be("Recipient_Name");
			subGift.SubTier.Should().Be("1000");
		}

		// The cheermote fragment shape (prefix/bits/tier) and the shared-chat source fields were missing
		// from the chat message model even though the emote extractor now consumes cheermote fragments.
		[Fact]
		public void ChatMessageCheermoteFragmentExposesPrefixAndBits()
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
					""cheer"": { ""bits"": 100 },
					""message"": {
						""text"": ""Cheer100"",
						""fragments"": [
							{
								""type"": ""cheermote"",
								""text"": ""Cheer100"",
								""cheermote"": { ""prefix"": ""cheer"", ""bits"": 100, ""tier"": 1 },
								""emote"": null
							}
						]
					}
				}
			}";

			var payload = JsonSerializer.Deserialize(json, TwitchEventSubSerializerContext.Default.EventSubChatMessagePayload);

			payload.Event.Cheer.Should().NotBeNull();
			payload.Event.Cheer!.Value.Bits.Should().Be(100);
			payload.Event.Message.Fragments.Count.Should().Be(1);

			var fragment = payload.Event.Message.Fragments[0];
			fragment.Type.Should().Be("cheermote");
			fragment.Cheermote.Should().NotBeNull();
			fragment.Cheermote!.Value.Prefix.Should().Be("cheer");
			fragment.Cheermote!.Value.Bits.Should().Be(100);
		}

		// Helix Send Chat Message returns 200 with is_sent/drop_reason; the previous implementation only
		// looked at the status code, so dropped messages were reported as sent.
		[Theory]
		[InlineData(true, null, null)]
		[InlineData(false, "automod_held", "Message held by AutoMod")]
		public void SendChatMessageResponseExposesIsSentAndDropReason(bool isSent, string? dropReasonCode, string? dropReasonMessage)
		{
			var dropReasonJson = dropReasonCode == null
				? "null"
				: $@"{{ ""code"": ""{dropReasonCode}"", ""message"": ""{dropReasonMessage}"" }}";

			var json = $@"{{ ""data"": [ {{ ""message_id"": ""message-id"", ""is_sent"": {(isSent ? "true" : "false")}, ""drop_reason"": {dropReasonJson} }} ] }}";

			var response = JsonSerializer.Deserialize(json, TwitchHelixSerializerContext.Default.SendChatMessageResponseDto);

			response.Data.Count.Should().Be(1);
			response.Data[0].MessageId.Should().Be("message-id");
			response.Data[0].IsSent.Should().Be(isSent);
			response.Data[0].DropReason?.Code.Should().Be(dropReasonCode);
		}
	}
}
