using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.EventSub
{
	internal struct EventSubChatNotificationPayload
	{
		[JsonPropertyName("subscription")]
		public EventSubSubscription Subscription { get; init; }

		[JsonPropertyName("event")]
		public EventSubChatNotificationEvent Event { get; init; }
	}

	[PublicAPI]
	public struct EventSubChatNotificationEvent
	{
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("chatter_user_id")]
		public string ChatterUserId { get; init; }

		[JsonPropertyName("chatter_user_login")]
		public string ChatterUserLogin { get; init; }

		[JsonPropertyName("chatter_user_name")]
		public string ChatterUserName { get; init; }

		[JsonPropertyName("chatter_is_anonymous")]
		public bool ChatterIsAnonymous { get; init; }

		[JsonPropertyName("color")]
		public string Color { get; init; }

		[JsonPropertyName("badges")]
		public List<EventSubBadge>? Badges { get; init; }

		[JsonPropertyName("message_id")]
		public string MessageId { get; init; }

		[JsonPropertyName("notice_type")]
		public string NoticeType { get; init; }

		[JsonPropertyName("message")]
		public EventSubChatMessageContent Message { get; init; }

		[JsonPropertyName("system_message")]
		public string? SystemMessage { get; init; }

		[JsonPropertyName("sub")]
		public EventSubNotificationSub? Sub { get; init; }

		[JsonPropertyName("resub")]
		public EventSubNotificationResub? Resub { get; init; }

		[JsonPropertyName("sub_gift")]
		public EventSubNotificationGiftSub? SubGift { get; init; }

		[JsonPropertyName("raid")]
		public EventSubNotificationRaid? Raid { get; init; }

		[JsonPropertyName("unraid")]
		public EventSubNotificationUnraid? Unraid { get; init; }

		[JsonPropertyName("pay_it_forward")]
		public EventSubNotificationPayItForward? PayItForward { get; init; }

		[JsonPropertyName("announcement")]
		public EventSubNotificationAnnouncement? Announcement { get; init; }

		[JsonPropertyName("bits_badge_tier")]
		public EventSubNotificationBitsBadgeTier? BitsBadgeTier { get; init; }

		[JsonPropertyName("community_sub_gift")]
		public EventSubNotificationCommunitySubGift? CommunitySubGift { get; init; }

		[JsonPropertyName("gift_paid_upgrade")]
		public EventSubNotificationGiftPaidUpgrade? GiftPaidUpgrade { get; init; }

		[JsonPropertyName("prime_paid_upgrade")]
		public EventSubNotificationPrimePaidUpgrade? PrimePaidUpgrade { get; init; }

		[JsonPropertyName("charity_donation")]
		public EventSubNotificationCharityDonation? CharityDonation { get; init; }

		[JsonPropertyName("watch_streak")]
		public EventSubNotificationWatchStreak? WatchStreak { get; init; }

		[JsonPropertyName("modiversary")]
		public EventSubNotificationModiversary? Modiversary { get; init; }

		[JsonPropertyName("gifted_drops_summary")]
		public EventSubNotificationGiftedDropsSummary? GiftedDropsSummary { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationSub
	{
		[JsonPropertyName("is_prime")]
		public bool IsPrime { get; init; }

		[JsonPropertyName("sub_tier")]
		public string SubTier { get; init; }

		[JsonPropertyName("duration_months")]
		public int DurationMonths { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationResub
	{
		[JsonPropertyName("duration_months")]
		public int DurationMonths { get; init; }

		[JsonPropertyName("cumulative_months")]
		public int CumulativeMonths { get; init; }

		[JsonPropertyName("streak_months")]
		public int? StreakMonths { get; init; }

		[JsonPropertyName("sub_tier")]
		public string SubTier { get; init; }

		[JsonPropertyName("is_prime")]
		public bool? IsPrime { get; init; }

		[JsonPropertyName("is_gift")]
		public bool IsGift { get; init; }

		[JsonPropertyName("gifter_is_anonymous")]
		public bool? GifterIsAnonymous { get; init; }

		[JsonPropertyName("gifter_user_id")]
		public string? GifterUserId { get; init; }

		[JsonPropertyName("gifter_user_name")]
		public string? GifterUserName { get; init; }

		[JsonPropertyName("gifter_user_login")]
		public string? GifterUserLogin { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationGiftSub
	{
		[JsonPropertyName("community_gift_id")]
		public string? CommunityGiftId { get; init; }

		[JsonPropertyName("duration_months")]
		public int DurationMonths { get; init; }

		[JsonPropertyName("cumulative_total")]
		public int? CumulativeTotal { get; init; }

		[JsonPropertyName("recipient_user_id")]
		public string RecipientUserId { get; init; }

		[JsonPropertyName("recipient_user_login")]
		public string RecipientUserLogin { get; init; }

		[JsonPropertyName("recipient_user_name")]
		public string RecipientUserName { get; init; }

		[JsonPropertyName("sub_tier")]
		public string SubTier { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationRaid
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		[JsonPropertyName("viewer_count")]
		public int ViewerCount { get; init; }

		[JsonPropertyName("profile_image_url")]
		public string? ProfileImageUrl { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationUnraid
	{
	}

	[PublicAPI]
	public struct EventSubNotificationPayItForward
	{
		[JsonPropertyName("gifter_is_anonymous")]
		public bool GifterIsAnonymous { get; init; }

		[JsonPropertyName("gifter_user_id")]
		public string? GifterUserId { get; init; }

		[JsonPropertyName("gifter_user_name")]
		public string? GifterUserName { get; init; }

		[JsonPropertyName("gifter_user_login")]
		public string? GifterUserLogin { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationAnnouncement
	{
		[JsonPropertyName("color")]
		public string Color { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationBitsBadgeTier
	{
		[JsonPropertyName("tier")]
		public int Tier { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationCommunitySubGift
	{
		[JsonPropertyName("id")]
		public string Id { get; init; }

		[JsonPropertyName("total")]
		public int Total { get; init; }

		[JsonPropertyName("sub_tier")]
		public string SubTier { get; init; }

		[JsonPropertyName("cumulative_total")]
		public int? CumulativeTotal { get; init; }

		[JsonPropertyName("gifter_is_anonymous")]
		public bool GifterIsAnonymous { get; init; }

		[JsonPropertyName("gifter_user_id")]
		public string? GifterUserId { get; init; }

		[JsonPropertyName("gifter_user_name")]
		public string? GifterUserName { get; init; }

		[JsonPropertyName("gifter_user_login")]
		public string? GifterUserLogin { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationGiftPaidUpgrade
	{
		[JsonPropertyName("gifter_is_anonymous")]
		public bool GifterIsAnonymous { get; init; }

		[JsonPropertyName("gifter_user_id")]
		public string? GifterUserId { get; init; }

		[JsonPropertyName("gifter_user_name")]
		public string? GifterUserName { get; init; }

		[JsonPropertyName("gifter_user_login")]
		public string? GifterUserLogin { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationPrimePaidUpgrade
	{
		[JsonPropertyName("sub_tier")]
		public string SubTier { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationCharityDonation
	{
		[JsonPropertyName("charity_name")]
		public string CharityName { get; init; }

		[JsonPropertyName("amount")]
		public EventSubNotificationCharityAmount Amount { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationCharityAmount
	{
		[JsonPropertyName("value")]
		public string Value { get; init; }

		[JsonPropertyName("decimal_places")]
		public int DecimalPlaces { get; init; }

		[JsonPropertyName("currency")]
		public string Currency { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationWatchStreak
	{
		[JsonPropertyName("streak_count")]
		public int StreakCount { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationModiversary
	{
		[JsonPropertyName("months")]
		public int Months { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationGiftedDropsSummary
	{
		[JsonPropertyName("recipient_count")]
		public int RecipientCount { get; init; }
	}
}
