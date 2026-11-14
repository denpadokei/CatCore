using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.EventSub.AutoMod
{
	/// <summary>
	/// The event payload of an automod.message.hold (v2) notification.
	/// </summary>
	[PublicAPI]
	public readonly struct AutoModMessageHoldEvent
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

		[JsonPropertyName("message_id")]
		public string MessageId { get; init; }

		[JsonPropertyName("message")]
		public AutoModMessage Message { get; init; }

		[JsonPropertyName("held_at")]
		public string HeldAt { get; init; }

		[JsonPropertyName("reason")]
		public string Reason { get; init; }

		[JsonPropertyName("automod")]
		public AutoModDetails? Automod { get; init; }

		[JsonPropertyName("blocked_term")]
		public AutoModBlockedTerm? BlockedTerm { get; init; }
	}

	/// <summary>
	/// The event payload of an automod.message.update (v2) notification.
	/// </summary>
	[PublicAPI]
	public readonly struct AutoModMessageUpdateEvent
	{
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("moderator_user_id")]
		public string ModeratorUserId { get; init; }

		[JsonPropertyName("moderator_user_login")]
		public string ModeratorUserLogin { get; init; }

		[JsonPropertyName("moderator_user_name")]
		public string ModeratorUserName { get; init; }

		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		[JsonPropertyName("message_id")]
		public string MessageId { get; init; }

		[JsonPropertyName("message")]
		public AutoModMessage Message { get; init; }

		[JsonPropertyName("held_at")]
		public string HeldAt { get; init; }

		[JsonPropertyName("reason")]
		public string Reason { get; init; }

		[JsonPropertyName("automod")]
		public AutoModDetails? Automod { get; init; }

		[JsonPropertyName("blocked_term")]
		public AutoModBlockedTerm? BlockedTerm { get; init; }

		[JsonPropertyName("status")]
		public string Status { get; init; }
	}

	/// <summary>
	/// A chat message, split into text, emote and cheermote fragments.
	/// </summary>
	[PublicAPI]
	public readonly struct AutoModMessage
	{
		[JsonPropertyName("text")]
		public string Text { get; init; }

		[JsonPropertyName("fragments")]
		public AutoModFragment[]? Fragments { get; init; }
	}

	/// <summary>
	/// A single fragment of an <see cref="AutoModMessage"/>.
	/// </summary>
	[PublicAPI]
	public readonly struct AutoModFragment
	{
		[JsonPropertyName("type")]
		public string? Type { get; init; }

		[JsonPropertyName("text")]
		public string Text { get; init; }

		[JsonPropertyName("emote")]
		public AutoModEmote? Emote { get; init; }

		[JsonPropertyName("cheermote")]
		public AutoModCheermote? Cheermote { get; init; }
	}

	/// <summary>
	/// The emote of an <see cref="AutoModFragment"/>.
	/// </summary>
	[PublicAPI]
	public readonly struct AutoModEmote
	{
		[JsonPropertyName("id")]
		public string Id { get; init; }

		[JsonPropertyName("emote_set_id")]
		public string EmoteSetId { get; init; }
	}

	/// <summary>
	/// The cheermote of an <see cref="AutoModFragment"/>.
	/// </summary>
	[PublicAPI]
	public readonly struct AutoModCheermote
	{
		[JsonPropertyName("prefix")]
		public string Prefix { get; init; }

		[JsonPropertyName("bits")]
		public int Bits { get; init; }

		[JsonPropertyName("tier")]
		public int Tier { get; init; }
	}

	/// <summary>
	/// The AutoMod category, level and boundaries that matched a held message.
	/// </summary>
	[PublicAPI]
	public readonly struct AutoModDetails
	{
		[JsonPropertyName("category")]
		public string? Category { get; init; }

		[JsonPropertyName("level")]
		public int? Level { get; init; }

		[JsonPropertyName("boundaries")]
		public AutoModBoundary[]? Boundaries { get; init; }
	}

	/// <summary>
	/// A start/end character position inside the message text.
	/// </summary>
	[PublicAPI]
	public readonly struct AutoModBoundary
	{
		[JsonPropertyName("start_pos")]
		public int StartPos { get; init; }

		[JsonPropertyName("end_pos")]
		public int EndPos { get; init; }
	}

	/// <summary>
	/// The blocked terms that matched a held message.
	/// </summary>
	[PublicAPI]
	public readonly struct AutoModBlockedTerm
	{
		[JsonPropertyName("terms_found")]
		public AutoModTermFound[]? TermsFound { get; init; }
	}

	/// <summary>
	/// A single blocked term match, including its owner and position in the message.
	/// </summary>
	[PublicAPI]
	public readonly struct AutoModTermFound
	{
		[JsonPropertyName("term_id")]
		public string TermId { get; init; }

		[JsonPropertyName("boundary")]
		public AutoModBoundary Boundary { get; init; }

		[JsonPropertyName("owner_broadcaster_user_id")]
		public string OwnerBroadcasterUserId { get; init; }

		[JsonPropertyName("owner_broadcaster_user_login")]
		public string OwnerBroadcasterUserLogin { get; init; }

		[JsonPropertyName("owner_broadcaster_user_name")]
		public string OwnerBroadcasterUserName { get; init; }
	}

	/// <summary>
	/// The event payload of an automod.settings.update notification.
	/// </summary>
	[PublicAPI]
	public readonly struct AutoModSettingsEvent
	{
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("moderator_user_id")]
		public string ModeratorUserId { get; init; }

		[JsonPropertyName("moderator_user_login")]
		public string ModeratorUserLogin { get; init; }

		[JsonPropertyName("moderator_user_name")]
		public string ModeratorUserName { get; init; }

		[JsonPropertyName("bullying")]
		public int Bullying { get; init; }

		[JsonPropertyName("overall_level")]
		public int? OverallLevel { get; init; }

		[JsonPropertyName("disability")]
		public int Disability { get; init; }

		[JsonPropertyName("race_ethnicity_or_religion")]
		public int RaceEthnicityOrReligion { get; init; }

		[JsonPropertyName("misogyny")]
		public int Misogyny { get; init; }

		[JsonPropertyName("sexuality_sex_or_gender")]
		public int SexualitySexOrGender { get; init; }

		[JsonPropertyName("aggression")]
		public int Aggression { get; init; }

		[JsonPropertyName("sex_based_terms")]
		public int SexBasedTerms { get; init; }

		[JsonPropertyName("swearing")]
		public int Swearing { get; init; }
	}

	/// <summary>
	/// The event payload of an automod.terms.update notification.
	/// </summary>
	[PublicAPI]
	public readonly struct AutoModTermsEvent
	{
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("moderator_user_id")]
		public string ModeratorUserId { get; init; }

		[JsonPropertyName("moderator_user_login")]
		public string ModeratorUserLogin { get; init; }

		[JsonPropertyName("moderator_user_name")]
		public string ModeratorUserName { get; init; }

		[JsonPropertyName("action")]
		public string Action { get; init; }

		[JsonPropertyName("from_automod")]
		public bool FromAutomod { get; init; }

		[JsonPropertyName("terms")]
		public string[] Terms { get; init; }
	}

	/// <summary>
	/// The event payload of a channel.chat.user_message_hold notification.
	/// </summary>
	[PublicAPI]
	public readonly struct ChatUserMessageHoldEvent
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

		[JsonPropertyName("message_id")]
		public string MessageId { get; init; }

		[JsonPropertyName("message")]
		public AutoModMessage Message { get; init; }
	}

	/// <summary>
	/// The event payload of a channel.chat.user_message_update notification.
	/// </summary>
	[PublicAPI]
	public readonly struct ChatUserMessageUpdateEvent
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

		[JsonPropertyName("message_id")]
		public string MessageId { get; init; }

		[JsonPropertyName("message")]
		public AutoModMessage Message { get; init; }

		[JsonPropertyName("status")]
		public string Status { get; init; }
	}
}
