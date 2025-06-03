using System;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.EventSub.Responses.VideoPlayback
{
    /// <summary>
    /// EventSub: channel.chat.message_delete のイベントデータ
    /// 参考: https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelchatmessagedelete
    /// </summary>
    public class ChannelChatMessageDeleteEvent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;

        [JsonPropertyName("broadcaster_user_id")]
        public string BroadcasterUserId { get; set; } = default!;

        [JsonPropertyName("broadcaster_user_login")]
        public string BroadcasterUserLogin { get; set; } = default!;

        [JsonPropertyName("broadcaster_user_name")]
        public string BroadcasterUserName { get; set; } = default!;

        [JsonPropertyName("chatter_user_id")]
        public string ChatterUserId { get; set; } = default!;

        [JsonPropertyName("chatter_user_login")]
        public string ChatterUserLogin { get; set; } = default!;

        [JsonPropertyName("chatter_user_name")]
        public string ChatterUserName { get; set; } = default!;

        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = default!;

        [JsonPropertyName("message")]
        public string Message { get; set; } = default!;

        [JsonPropertyName("deleted_by")]
        public string DeletedBy { get; set; } = default!;

        [JsonPropertyName("deleted_at")]
        public DateTimeOffset DeletedAt { get; set; }
    }
}