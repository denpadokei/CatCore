using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.EventSub.Subscription
{
	/// <summary>
	/// Event payload of a <c>channel.subscribe</c> notification.
	/// </summary>
	[PublicAPI]
	public readonly struct SubscriptionEvent
	{
		/// <summary>
		/// The user id of the subscriber.
		/// </summary>
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		/// <summary>
		/// The login name of the subscriber.
		/// </summary>
		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		/// <summary>
		/// The display name of the subscriber.
		/// </summary>
		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		/// <summary>
		/// The broadcaster (channel) user id.
		/// </summary>
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		/// <summary>
		/// The broadcaster (channel) login name.
		/// </summary>
		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		/// <summary>
		/// The broadcaster (channel) display name.
		/// </summary>
		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		/// <summary>
		/// The subscription tier, for example <c>1000</c>.
		/// </summary>
		[JsonPropertyName("tier")]
		public string Tier { get; init; }

		/// <summary>
		/// True when the subscription was gifted to the user.
		/// </summary>
		[JsonPropertyName("is_gift")]
		public bool IsGift { get; init; }
	}

	/// <summary>
	/// Event payload of a <c>channel.subscription.end</c> notification.
	/// </summary>
	[PublicAPI]
	public readonly struct SubscriptionEndEvent
	{
		/// <summary>
		/// The user id of the subscriber.
		/// </summary>
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		/// <summary>
		/// The login name of the subscriber.
		/// </summary>
		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		/// <summary>
		/// The display name of the subscriber.
		/// </summary>
		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		/// <summary>
		/// The broadcaster (channel) user id.
		/// </summary>
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		/// <summary>
		/// The broadcaster (channel) login name.
		/// </summary>
		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		/// <summary>
		/// The broadcaster (channel) display name.
		/// </summary>
		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		/// <summary>
		/// The subscription tier, for example <c>1000</c>.
		/// </summary>
		[JsonPropertyName("tier")]
		public string Tier { get; init; }

		/// <summary>
		/// True when the subscription was gifted to the user.
		/// </summary>
		[JsonPropertyName("is_gift")]
		public bool IsGift { get; init; }
	}

	/// <summary>
	/// Event payload of a <c>channel.subscription.gift</c> notification.
	/// </summary>
	[PublicAPI]
	public readonly struct SubscriptionGiftEvent
	{
		/// <summary>
		/// The user id of the gifter, or null when the gift is anonymous.
		/// </summary>
		[JsonPropertyName("user_id")]
		public string? UserId { get; init; }

		/// <summary>
		/// The login name of the gifter, or null when the gift is anonymous.
		/// </summary>
		[JsonPropertyName("user_login")]
		public string? UserLogin { get; init; }

		/// <summary>
		/// The display name of the gifter, or null when the gift is anonymous.
		/// </summary>
		[JsonPropertyName("user_name")]
		public string? UserName { get; init; }

		/// <summary>
		/// The broadcaster (channel) user id.
		/// </summary>
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		/// <summary>
		/// The broadcaster (channel) login name.
		/// </summary>
		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		/// <summary>
		/// The broadcaster (channel) display name.
		/// </summary>
		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		/// <summary>
		/// The number of subscriptions gifted in this single event.
		/// </summary>
		[JsonPropertyName("total")]
		public int Total { get; init; }

		/// <summary>
		/// The subscription tier, for example <c>1000</c>.
		/// </summary>
		[JsonPropertyName("tier")]
		public string Tier { get; init; }

		/// <summary>
		/// The cumulative number of subscriptions the gifter has gifted in this channel,
		/// or null when the gift is anonymous or the gifter opted out.
		/// </summary>
		[JsonPropertyName("cumulative_total")]
		public int? CumulativeTotal { get; init; }

		/// <summary>
		/// True when the gift is anonymous.
		/// </summary>
		[JsonPropertyName("is_anonymous")]
		public bool IsAnonymous { get; init; }
	}

	/// <summary>
	/// Event payload of a <c>channel.subscription.message</c> notification.
	/// </summary>
	[PublicAPI]
	public readonly struct SubscriptionMessageEvent
	{
		/// <summary>
		/// The user id of the resubscribing user.
		/// </summary>
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		/// <summary>
		/// The login name of the resubscribing user.
		/// </summary>
		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		/// <summary>
		/// The display name of the resubscribing user.
		/// </summary>
		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		/// <summary>
		/// The broadcaster (channel) user id.
		/// </summary>
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		/// <summary>
		/// The broadcaster (channel) login name.
		/// </summary>
		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		/// <summary>
		/// The broadcaster (channel) display name.
		/// </summary>
		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		/// <summary>
		/// The subscription tier, for example <c>1000</c>.
		/// </summary>
		[JsonPropertyName("tier")]
		public string Tier { get; init; }

		/// <summary>
		/// The resubscription message and its emotes.
		/// </summary>
		[JsonPropertyName("message")]
		public SubscriptionMessage Message { get; init; }

		/// <summary>
		/// The total number of months the user has been subscribed to the channel.
		/// </summary>
		[JsonPropertyName("cumulative_months")]
		public int CumulativeMonths { get; init; }

		/// <summary>
		/// The number of consecutive months the user has been subscribed,
		/// or null when the user is not on a streak or opted out.
		/// </summary>
		[JsonPropertyName("streak_months")]
		public int? StreakMonths { get; init; }

		/// <summary>
		/// The number of months the resubscription covers.
		/// </summary>
		[JsonPropertyName("duration_months")]
		public int DurationMonths { get; init; }
	}

	/// <summary>
	/// The message attached to a resubscription.
	/// </summary>
	[PublicAPI]
	public readonly struct SubscriptionMessage
	{
		/// <summary>
		/// The raw message text.
		/// </summary>
		[JsonPropertyName("text")]
		public string Text { get; init; }

		/// <summary>
		/// The emotes contained in the message, or null when there are none.
		/// </summary>
		[JsonPropertyName("emotes")]
		public SubscriptionEmote[]? Emotes { get; init; }
	}

	/// <summary>
	/// An emote occurrence inside a resubscription message.
	/// </summary>
	[PublicAPI]
	public readonly struct SubscriptionEmote
	{
		/// <summary>
		/// The index of the first character of the emote in the message text.
		/// </summary>
		[JsonPropertyName("begin")]
		public int Begin { get; init; }

		/// <summary>
		/// The index of the last character of the emote in the message text.
		/// </summary>
		[JsonPropertyName("end")]
		public int End { get; init; }

		/// <summary>
		/// The emote id.
		/// </summary>
		[JsonPropertyName("id")]
		public string Id { get; init; }
	}

	/// <summary>
	/// Event payload of a <c>channel.vip.add</c> or <c>channel.vip.remove</c> notification.
	/// </summary>
	[PublicAPI]
	public readonly struct VipEvent
	{
		/// <summary>
		/// The user id of the VIP.
		/// </summary>
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		/// <summary>
		/// The login name of the VIP.
		/// </summary>
		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		/// <summary>
		/// The display name of the VIP.
		/// </summary>
		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		/// <summary>
		/// The broadcaster (channel) user id.
		/// </summary>
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		/// <summary>
		/// The broadcaster (channel) login name.
		/// </summary>
		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		/// <summary>
		/// The broadcaster (channel) display name.
		/// </summary>
		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }
	}
}
