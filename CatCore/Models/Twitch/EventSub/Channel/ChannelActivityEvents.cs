using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.EventSub.Channel
{
	/// <summary>
	/// Payload of a channel.bits.use EventSub notification.
	/// </summary>
	[PublicAPI]
	public readonly struct BitsUseEvent
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("bits")]
		public int Bits { get; init; }

		[JsonPropertyName("type")]
		public string Type { get; init; }

		[JsonPropertyName("message")]
		public BitsUseMessage? Message { get; init; }

		[JsonPropertyName("power_up")]
		public BitsPowerUp? PowerUp { get; init; }

		[JsonPropertyName("custom_power_up")]
		public BitsCustomPowerUp? CustomPowerUp { get; init; }
	}

	/// <summary>
	/// Chat message attached to a channel.bits.use notification.
	/// </summary>
	[PublicAPI]
	public readonly struct BitsUseMessage
	{
		[JsonPropertyName("text")]
		public string Text { get; init; }

		[JsonPropertyName("fragments")]
		public BitsUseFragment[]? Fragments { get; init; }
	}

	/// <summary>
	/// Single fragment of a channel.bits.use chat message.
	/// </summary>
	[PublicAPI]
	public readonly struct BitsUseFragment
	{
		[JsonPropertyName("type")]
		public string Type { get; init; }

		[JsonPropertyName("text")]
		public string Text { get; init; }

		[JsonPropertyName("cheermote")]
		public BitsCheermote? Cheermote { get; init; }

		[JsonPropertyName("emote")]
		public BitsEmote? Emote { get; init; }
	}

	/// <summary>
	/// Cheermote information of a channel.bits.use message fragment.
	/// </summary>
	[PublicAPI]
	public readonly struct BitsCheermote
	{
		[JsonPropertyName("prefix")]
		public string Prefix { get; init; }

		[JsonPropertyName("bits")]
		public int Bits { get; init; }

		[JsonPropertyName("tier")]
		public int Tier { get; init; }
	}

	/// <summary>
	/// Emote information of a channel.bits.use message fragment or power-up.
	/// </summary>
	[PublicAPI]
	public readonly struct BitsEmote
	{
		[JsonPropertyName("id")]
		public string Id { get; init; }

		[JsonPropertyName("emote_set_id")]
		public string EmoteSetId { get; init; }

		[JsonPropertyName("owner_id")]
		public string OwnerId { get; init; }

		[JsonPropertyName("format")]
		public string[]? Format { get; init; }
	}

	/// <summary>
	/// Power-up information of a channel.bits.use notification.
	/// </summary>
	[PublicAPI]
	public readonly struct BitsPowerUp
	{
		[JsonPropertyName("type")]
		public string Type { get; init; }

		[JsonPropertyName("emote")]
		public BitsEmote? Emote { get; init; }

		[JsonPropertyName("message_effect_id")]
		public string? MessageEffectId { get; init; }
	}

	/// <summary>
	/// Custom power-up information of a channel.bits.use notification.
	/// </summary>
	[PublicAPI]
	public readonly struct BitsCustomPowerUp
	{
		[JsonPropertyName("title")]
		public string Title { get; init; }

		[JsonPropertyName("reward_id")]
		public string RewardId { get; init; }
	}

	/// <summary>
	/// Payload of a channel.cheer EventSub notification.
	/// </summary>
	[PublicAPI]
	public readonly struct CheerEvent
	{
		[JsonPropertyName("is_anonymous")]
		public bool IsAnonymous { get; init; }

		[JsonPropertyName("user_id")]
		public string? UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string? UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string? UserName { get; init; }

		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("message")]
		public string Message { get; init; }

		[JsonPropertyName("bits")]
		public int Bits { get; init; }
	}

	/// <summary>
	/// Payload of a channel.custom_power_up_redemption.add EventSub notification.
	/// </summary>
	[PublicAPI]
	public readonly struct CustomPowerUpRedemptionEvent
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

		[JsonPropertyName("custom_power_up")]
		public CustomPowerUpInfo CustomPowerUp { get; init; }

		[JsonPropertyName("redeemed_at")]
		public string RedeemedAt { get; init; }
	}

	/// <summary>
	/// Custom power-up information of a channel.custom_power_up_redemption.add notification.
	/// </summary>
	[PublicAPI]
	public readonly struct CustomPowerUpInfo
	{
		[JsonPropertyName("id")]
		public string Id { get; init; }

		[JsonPropertyName("title")]
		public string Title { get; init; }

		[JsonPropertyName("bits")]
		public int Bits { get; init; }

		[JsonPropertyName("prompt")]
		public string Prompt { get; init; }
	}

	/// <summary>
	/// Payload of a channel.raid EventSub notification.
	/// </summary>
	[PublicAPI]
	public readonly struct RaidEvent
	{
		[JsonPropertyName("from_broadcaster_user_id")]
		public string FromBroadcasterUserId { get; init; }

		[JsonPropertyName("from_broadcaster_user_login")]
		public string FromBroadcasterUserLogin { get; init; }

		[JsonPropertyName("from_broadcaster_user_name")]
		public string FromBroadcasterUserName { get; init; }

		[JsonPropertyName("to_broadcaster_user_id")]
		public string ToBroadcasterUserId { get; init; }

		[JsonPropertyName("to_broadcaster_user_login")]
		public string ToBroadcasterUserLogin { get; init; }

		[JsonPropertyName("to_broadcaster_user_name")]
		public string ToBroadcasterUserName { get; init; }

		[JsonPropertyName("viewers")]
		public int Viewers { get; init; }
	}

	/// <summary>
	/// Payload of a channel.update (v2) EventSub notification.
	/// </summary>
	[PublicAPI]
	public readonly struct ChannelUpdateEvent
	{
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("title")]
		public string Title { get; init; }

		[JsonPropertyName("language")]
		public string Language { get; init; }

		[JsonPropertyName("category_id")]
		public string CategoryId { get; init; }

		[JsonPropertyName("category_name")]
		public string CategoryName { get; init; }

		[JsonPropertyName("content_classification_labels")]
		public string[]? ContentClassificationLabels { get; init; }
	}

	/// <summary>
	/// Payload of a channel.shoutout.create EventSub notification.
	/// </summary>
	[PublicAPI]
	public readonly struct ShoutoutCreatedEvent
	{
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("to_broadcaster_user_id")]
		public string ToBroadcasterUserId { get; init; }

		[JsonPropertyName("to_broadcaster_user_login")]
		public string ToBroadcasterUserLogin { get; init; }

		[JsonPropertyName("to_broadcaster_user_name")]
		public string ToBroadcasterUserName { get; init; }

		[JsonPropertyName("moderator_user_id")]
		public string ModeratorUserId { get; init; }

		[JsonPropertyName("moderator_user_login")]
		public string ModeratorUserLogin { get; init; }

		[JsonPropertyName("moderator_user_name")]
		public string ModeratorUserName { get; init; }

		[JsonPropertyName("viewer_count")]
		public int ViewerCount { get; init; }

		[JsonPropertyName("started_at")]
		public string StartedAt { get; init; }

		[JsonPropertyName("cooldown_ends_at")]
		public string CooldownEndsAt { get; init; }

		[JsonPropertyName("target_cooldown_ends_at")]
		public string TargetCooldownEndsAt { get; init; }
	}

	/// <summary>
	/// Payload of a channel.shoutout.receive EventSub notification.
	/// </summary>
	[PublicAPI]
	public readonly struct ShoutoutReceivedEvent
	{
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("from_broadcaster_user_id")]
		public string FromBroadcasterUserId { get; init; }

		[JsonPropertyName("from_broadcaster_user_login")]
		public string FromBroadcasterUserLogin { get; init; }

		[JsonPropertyName("from_broadcaster_user_name")]
		public string FromBroadcasterUserName { get; init; }

		[JsonPropertyName("viewer_count")]
		public int ViewerCount { get; init; }

		[JsonPropertyName("started_at")]
		public string StartedAt { get; init; }
	}
}
