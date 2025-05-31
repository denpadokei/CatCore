using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.EventSub.Responses
{
    public class ChannelPointsRedeemEvent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;
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
        [JsonPropertyName("user_input")]
        public string UserInput { get; set; } = default!;
        [JsonPropertyName("reward")]
        public ChannelPointsReward Reward { get; set; } = default!;
        [JsonPropertyName("redeemed_at")]
        public string RedeemedAt { get; set; } = default!;
        [JsonPropertyName("status")]
        public string Status { get; set; } = default!;
    }

    public class ChannelPointsReward
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;
        [JsonPropertyName("title")]
        public string Title { get; set; } = default!;
        [JsonPropertyName("cost")]
        public int Cost { get; set; }
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = default!;
    }
}