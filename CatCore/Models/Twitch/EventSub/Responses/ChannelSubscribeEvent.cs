using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.EventSub.Responses
{
    public class ChannelSubscribeEvent
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = default!;
        [JsonPropertyName("user_login")]
        public string UserLogin { get; set; } = default!;
        [JsonPropertyName("user_name")]
        public string UserName { get; set; } = default!;
        [JsonPropertyName("broadcaster_user_id")]
        public string BroadcasterUserId { get; set; } = default!;
        [JsonPropertyName("broadcaster_user_login")]
        public string BroadcasterUserLogin { get; set; } = default!;
        [JsonPropertyName("broadcaster_user_name")]
        public string BroadcasterUserName { get; set; } = default!;
        [JsonPropertyName("tier")]
        public string Tier { get; set; } = default!;
        [JsonPropertyName("is_gift")]
        public bool IsGift { get; set; }
    }
}