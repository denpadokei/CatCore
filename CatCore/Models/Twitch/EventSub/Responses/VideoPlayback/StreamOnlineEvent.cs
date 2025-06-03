using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.EventSub.Responses.VideoPlayback
{
    public class StreamOnlineEvent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;
        [JsonPropertyName("broadcaster_user_id")]
        public string BroadcasterUserId { get; set; } = default!;
        [JsonPropertyName("broadcaster_user_login")]
        public string BroadcasterUserLogin { get; set; } = default!;
        [JsonPropertyName("broadcaster_user_name")]
        public string BroadcasterUserName { get; set; } = default!;
        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;
        [JsonPropertyName("started_at")]
        public string StartedAt { get; set; } = default!;
    }
}