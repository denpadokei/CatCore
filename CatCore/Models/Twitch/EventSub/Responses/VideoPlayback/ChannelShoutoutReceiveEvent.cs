using System;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.EventSub.Responses.VideoPlayback
{
    /// <summary>
    /// EventSub: channel.shoutout.receive のイベントデータ
    /// 参考: https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelshoutoutreceive
    /// </summary>
    public class ChannelShoutoutReceiveEvent
    {
        [JsonPropertyName("broadcaster_user_id")]
        public string BroadcasterUserId { get; set; } = default!;

        [JsonPropertyName("broadcaster_user_login")]
        public string BroadcasterUserLogin { get; set; } = default!;

        [JsonPropertyName("broadcaster_user_name")]
        public string BroadcasterUserName { get; set; } = default!;

        [JsonPropertyName("from_broadcaster_user_id")]
        public string FromBroadcasterUserId { get; set; } = default!;

        [JsonPropertyName("from_broadcaster_user_login")]
        public string FromBroadcasterUserLogin { get; set; } = default!;

        [JsonPropertyName("from_broadcaster_user_name")]
        public string FromBroadcasterUserName { get; set; } = default!;

        [JsonPropertyName("viewer_count")]
        public int ViewerCount { get; set; }

        [JsonPropertyName("started_at")]
        public DateTimeOffset StartedAt { get; set; }
    }
}