using System;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.EventSub.Responses.VideoPlayback
{
    /// <summary>
    /// EventSub: channel.shoutout.create のイベントデータ
    /// 参考: https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelshoutoutcreate
    /// </summary>
    public class ChannelShoutoutCreateEvent
    {
        [JsonPropertyName("broadcaster_user_id")]
        public string BroadcasterUserId { get; set; } = default!;

        [JsonPropertyName("broadcaster_user_login")]
        public string BroadcasterUserLogin { get; set; } = default!;

        [JsonPropertyName("broadcaster_user_name")]
        public string BroadcasterUserName { get; set; } = default!;

        [JsonPropertyName("to_broadcaster_user_id")]
        public string ToBroadcasterUserId { get; set; } = default!;

        [JsonPropertyName("to_broadcaster_user_login")]
        public string ToBroadcasterUserLogin { get; set; } = default!;

        [JsonPropertyName("to_broadcaster_user_name")]
        public string ToBroadcasterUserName { get; set; } = default!;

        [JsonPropertyName("moderator_user_id")]
        public string ModeratorUserId { get; set; } = default!;

        [JsonPropertyName("moderator_user_login")]
        public string ModeratorUserLogin { get; set; } = default!;

        [JsonPropertyName("moderator_user_name")]
        public string ModeratorUserName { get; set; } = default!;

        [JsonPropertyName("started_at")]
        public DateTimeOffset StartedAt { get; set; }

        [JsonPropertyName("viewer_count")]
        public int ViewerCount { get; set; }
    }
}