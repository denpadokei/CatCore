using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.EventSub.ChannelPoints
{
	/// <summary>
	/// Event payload of channel.channel_points_automatic_reward_redemption.add (version 2).
	/// </summary>
	[PublicAPI]
	public readonly struct AutomaticRewardRedemptionEvent
	{
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		[JsonPropertyName("id")]
		public string Id { get; init; }

		[JsonPropertyName("reward")]
		public AutomaticReward Reward { get; init; }

		[JsonPropertyName("message")]
		public AutomaticRewardMessage? Message { get; init; }

		[JsonPropertyName("redeemed_at")]
		public string RedeemedAt { get; init; }
	}

	/// <summary>
	/// The automatically redeemed reward.
	/// </summary>
	[PublicAPI]
	public readonly struct AutomaticReward
	{
		[JsonPropertyName("type")]
		public string Type { get; init; }

		[JsonPropertyName("channel_points")]
		public int ChannelPoints { get; init; }

		[JsonPropertyName("emote")]
		public AutomaticRewardEmote? Emote { get; init; }
	}

	/// <summary>
	/// Emote attached to an automatically redeemed reward.
	/// </summary>
	[PublicAPI]
	public readonly struct AutomaticRewardEmote
	{
		[JsonPropertyName("id")]
		public string Id { get; init; }

		[JsonPropertyName("name")]
		public string Name { get; init; }
	}

	/// <summary>
	/// User supplied message of an automatically redeemed reward.
	/// </summary>
	[PublicAPI]
	public readonly struct AutomaticRewardMessage
	{
		[JsonPropertyName("text")]
		public string Text { get; init; }

		[JsonPropertyName("fragments")]
		public AutomaticRewardFragment[]? Fragments { get; init; }
	}

	/// <summary>
	/// A single fragment of an automatically redeemed reward message.
	/// </summary>
	[PublicAPI]
	public readonly struct AutomaticRewardFragment
	{
		[JsonPropertyName("type")]
		public string Type { get; init; }

		[JsonPropertyName("text")]
		public string Text { get; init; }

		[JsonPropertyName("emote")]
		public AutomaticRewardEmote? Emote { get; init; }
	}

	/// <summary>
	/// Event payload of channel.channel_points_custom_reward_redemption.update.
	/// </summary>
	[PublicAPI]
	public readonly struct CustomRewardRedemptionEvent
	{
		[JsonPropertyName("id")]
		public string Id { get; init; }

		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		[JsonPropertyName("user_input")]
		public string UserInput { get; init; }

		[JsonPropertyName("status")]
		public string Status { get; init; }

		[JsonPropertyName("reward")]
		public CustomRedemptionReward Reward { get; init; }

		[JsonPropertyName("redeemed_at")]
		public string RedeemedAt { get; init; }
	}

	/// <summary>
	/// The custom reward that was redeemed.
	/// </summary>
	[PublicAPI]
	public readonly struct CustomRedemptionReward
	{
		[JsonPropertyName("id")]
		public string Id { get; init; }

		[JsonPropertyName("title")]
		public string Title { get; init; }

		[JsonPropertyName("cost")]
		public int Cost { get; init; }

		[JsonPropertyName("prompt")]
		public string Prompt { get; init; }
	}

	/// <summary>
	/// Event payload shared by channel.channel_points_custom_reward.add, .update and .remove.
	/// </summary>
	[PublicAPI]
	public readonly struct CustomRewardEvent
	{
		[JsonPropertyName("id")]
		public string Id { get; init; }

		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("is_enabled")]
		public bool IsEnabled { get; init; }

		[JsonPropertyName("is_paused")]
		public bool IsPaused { get; init; }

		[JsonPropertyName("is_in_stock")]
		public bool IsInStock { get; init; }

		[JsonPropertyName("title")]
		public string Title { get; init; }

		[JsonPropertyName("cost")]
		public int Cost { get; init; }

		[JsonPropertyName("prompt")]
		public string Prompt { get; init; }

		[JsonPropertyName("is_user_input_required")]
		public bool IsUserInputRequired { get; init; }

		[JsonPropertyName("should_redemptions_skip_request_queue")]
		public bool ShouldRedemptionsSkipRequestQueue { get; init; }

		[JsonPropertyName("cooldown_expires_at")]
		public string? CooldownExpiresAt { get; init; }

		[JsonPropertyName("redemptions_redeemed_current_stream")]
		public int? RedemptionsRedeemedCurrentStream { get; init; }

		[JsonPropertyName("max_per_stream")]
		public RewardLimit MaxPerStream { get; init; }

		[JsonPropertyName("max_per_user_per_stream")]
		public RewardLimit MaxPerUserPerStream { get; init; }

		[JsonPropertyName("global_cooldown")]
		public RewardCooldown GlobalCooldown { get; init; }

		[JsonPropertyName("background_color")]
		public string BackgroundColor { get; init; }

		[JsonPropertyName("image")]
		public RewardImage? Image { get; init; }

		[JsonPropertyName("default_image")]
		public RewardImage DefaultImage { get; init; }
	}

	/// <summary>
	/// Redemption limit of a custom reward.
	/// </summary>
	[PublicAPI]
	public readonly struct RewardLimit
	{
		[JsonPropertyName("is_enabled")]
		public bool IsEnabled { get; init; }

		[JsonPropertyName("value")]
		public int Value { get; init; }
	}

	/// <summary>
	/// Cooldown of a custom reward.
	/// </summary>
	[PublicAPI]
	public readonly struct RewardCooldown
	{
		[JsonPropertyName("is_enabled")]
		public bool IsEnabled { get; init; }

		[JsonPropertyName("seconds")]
		public int Seconds { get; init; }
	}

	/// <summary>
	/// Image set of a custom reward.
	/// </summary>
	[PublicAPI]
	public readonly struct RewardImage
	{
		[JsonPropertyName("url_1x")]
		public string Url1x { get; init; }

		[JsonPropertyName("url_2x")]
		public string Url2x { get; init; }

		[JsonPropertyName("url_4x")]
		public string Url4x { get; init; }
	}
}
