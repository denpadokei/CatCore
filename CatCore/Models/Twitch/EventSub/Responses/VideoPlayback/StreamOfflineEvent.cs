using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.EventSub.Responses.VideoPlayback
{
    public class StreamOfflineEvent
    {
        [JsonPropertyName("broadcaster_user_id")]
        public string BroadcasterUserId { get; set; } = default!;
        [JsonPropertyName("broadcaster_user_login")]
        public string BroadcasterUserLogin { get; set; } = default!;
        [JsonPropertyName("broadcaster_user_name")]
        public string BroadcasterUserName { get; set; } = default!;
    }
}