using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.EventSub.Safety
{
	/// <summary>
	/// Payload of a <c>channel.suspicious_user.message</c> EventSub notification.
	/// </summary>
	[PublicAPI]
	public readonly struct SuspiciousUserMessageEvent
	{
		/// <summary>
		/// Id of the broadcaster (channel) the suspicious message was sent in.
		/// </summary>
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		/// <summary>
		/// Login of the broadcaster (channel) the suspicious message was sent in.
		/// </summary>
		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		/// <summary>
		/// Display name of the broadcaster (channel) the suspicious message was sent in.
		/// </summary>
		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		/// <summary>
		/// Id of the moderator (the logged in user) that received this notification.
		/// </summary>
		[JsonPropertyName("moderator_user_id")]
		public string ModeratorUserId { get; init; }

		/// <summary>
		/// Login of the moderator (the logged in user) that received this notification.
		/// </summary>
		[JsonPropertyName("moderator_user_login")]
		public string ModeratorUserLogin { get; init; }

		/// <summary>
		/// Display name of the moderator (the logged in user) that received this notification.
		/// </summary>
		[JsonPropertyName("moderator_user_name")]
		public string ModeratorUserName { get; init; }

		/// <summary>
		/// Id of the suspicious user.
		/// </summary>
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		/// <summary>
		/// Login of the suspicious user.
		/// </summary>
		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		/// <summary>
		/// Display name of the suspicious user.
		/// </summary>
		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		/// <summary>
		/// Low trust status of the suspicious user: <c>none</c>, <c>active_monitoring</c> or <c>restricted</c>.
		/// </summary>
		[JsonPropertyName("low_trust_status")]
		public string LowTrustStatus { get; init; }

		/// <summary>
		/// Ids of the channels that share a ban with the suspicious user's channel.
		/// </summary>
		[JsonPropertyName("shared_ban_channel_ids")]
		public string[]? SharedBanChannelIds { get; init; }

		/// <summary>
		/// Types that apply to the suspicious user: <c>manually_added</c>, <c>ban_evader</c> or <c>banned_in_shared_channel</c>.
		/// </summary>
		[JsonPropertyName("types")]
		public string[]? Types { get; init; }

		/// <summary>
		/// Ban evasion evaluation of the suspicious user, if any.
		/// </summary>
		[JsonPropertyName("ban_evasion_evaluation")]
		public string? BanEvasionEvaluation { get; init; }

		/// <summary>
		/// The message that triggered this notification, if any.
		/// </summary>
		[JsonPropertyName("message")]
		public SuspiciousUserMessage? Message { get; init; }
	}

	/// <summary>
	/// The chat message of a suspicious user.
	/// </summary>
	[PublicAPI]
	public readonly struct SuspiciousUserMessage
	{
		/// <summary>
		/// Id of the message.
		/// </summary>
		[JsonPropertyName("message_id")]
		public string MessageId { get; init; }

		/// <summary>
		/// Plain text of the message.
		/// </summary>
		[JsonPropertyName("text")]
		public string Text { get; init; }

		/// <summary>
		/// The message split into fragments (text, cheermotes and emotes).
		/// </summary>
		[JsonPropertyName("fragments")]
		public SuspiciousUserFragment[]? Fragments { get; init; }
	}

	/// <summary>
	/// A fragment of a suspicious user's chat message.
	/// </summary>
	[PublicAPI]
	public readonly struct SuspiciousUserFragment
	{
		/// <summary>
		/// Type of the fragment: <c>text</c>, <c>cheermote</c> or <c>emote</c>.
		/// </summary>
		[JsonPropertyName("type")]
		public string Type { get; init; }

		/// <summary>
		/// Text of the fragment.
		/// </summary>
		[JsonPropertyName("text")]
		public string Text { get; init; }

		/// <summary>
		/// Cheermote data when <see cref="Type"/> is <c>cheermote</c>.
		/// </summary>
		[JsonPropertyName("cheermote")]
		public SuspiciousUserCheermote? Cheermote { get; init; }

		/// <summary>
		/// Emote data when <see cref="Type"/> is <c>emote</c>.
		/// </summary>
		[JsonPropertyName("emote")]
		public SuspiciousUserEmote? Emote { get; init; }
	}

	/// <summary>
	/// Cheermote data of a suspicious user's chat message fragment.
	/// </summary>
	[PublicAPI]
	public readonly struct SuspiciousUserCheermote
	{
		/// <summary>
		/// Prefix of the cheermote.
		/// </summary>
		[JsonPropertyName("prefix")]
		public string Prefix { get; init; }

		/// <summary>
		/// Number of bits cheered with this cheermote.
		/// </summary>
		[JsonPropertyName("bits")]
		public int Bits { get; init; }

		/// <summary>
		/// Tier of the cheermote.
		/// </summary>
		[JsonPropertyName("tier")]
		public int Tier { get; init; }
	}

	/// <summary>
	/// Emote data of a suspicious user's chat message fragment.
	/// </summary>
	[PublicAPI]
	public readonly struct SuspiciousUserEmote
	{
		/// <summary>
		/// Id of the emote.
		/// </summary>
		[JsonPropertyName("id")]
		public string Id { get; init; }

		/// <summary>
		/// Id of the emote set the emote belongs to.
		/// </summary>
		[JsonPropertyName("emote_set_id")]
		public string EmoteSetId { get; init; }
	}

	/// <summary>
	/// Payload of a <c>channel.suspicious_user.update</c> EventSub notification.
	/// </summary>
	[PublicAPI]
	public readonly struct SuspiciousUserUpdateEvent
	{
		/// <summary>
		/// Id of the broadcaster (channel) the update happened in.
		/// </summary>
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		/// <summary>
		/// Login of the broadcaster (channel) the update happened in.
		/// </summary>
		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		/// <summary>
		/// Display name of the broadcaster (channel) the update happened in.
		/// </summary>
		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		/// <summary>
		/// Id of the moderator (the logged in user) that received this notification.
		/// </summary>
		[JsonPropertyName("moderator_user_id")]
		public string ModeratorUserId { get; init; }

		/// <summary>
		/// Login of the moderator (the logged in user) that received this notification.
		/// </summary>
		[JsonPropertyName("moderator_user_login")]
		public string ModeratorUserLogin { get; init; }

		/// <summary>
		/// Display name of the moderator (the logged in user) that received this notification.
		/// </summary>
		[JsonPropertyName("moderator_user_name")]
		public string ModeratorUserName { get; init; }

		/// <summary>
		/// Id of the suspicious user.
		/// </summary>
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		/// <summary>
		/// Login of the suspicious user.
		/// </summary>
		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		/// <summary>
		/// Display name of the suspicious user.
		/// </summary>
		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		/// <summary>
		/// Low trust status of the suspicious user: <c>none</c>, <c>active_monitoring</c> or <c>restricted</c>.
		/// </summary>
		[JsonPropertyName("low_trust_status")]
		public string LowTrustStatus { get; init; }
	}

	/// <summary>
	/// Payload shared by <c>channel.unban_request.create</c> and <c>channel.unban_request.resolve</c>
	/// EventSub notifications. <c>text</c> and <c>created_at</c> are set on the create event,
	/// <c>resolution_text</c> and <c>status</c> on the resolve event.
	/// </summary>
	[PublicAPI]
	public readonly struct UnbanRequestEvent
	{
		/// <summary>
		/// Id of the unban request.
		/// </summary>
		[JsonPropertyName("id")]
		public string Id { get; init; }

		/// <summary>
		/// Id of the broadcaster (channel) the unban request was filed in.
		/// </summary>
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		/// <summary>
		/// Login of the broadcaster (channel) the unban request was filed in.
		/// </summary>
		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		/// <summary>
		/// Display name of the broadcaster (channel) the unban request was filed in.
		/// </summary>
		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		/// <summary>
		/// Id of the moderator (the logged in user) that resolved the request. Null on the create event.
		/// </summary>
		[JsonPropertyName("moderator_user_id")]
		public string? ModeratorUserId { get; init; }

		/// <summary>
		/// Login of the moderator (the logged in user) that resolved the request. Null on the create event.
		/// </summary>
		[JsonPropertyName("moderator_user_login")]
		public string? ModeratorUserLogin { get; init; }

		/// <summary>
		/// Display name of the moderator (the logged in user) that resolved the request. Null on the create event.
		/// </summary>
		[JsonPropertyName("moderator_user_name")]
		public string? ModeratorUserName { get; init; }

		/// <summary>
		/// Id of the user that filed the unban request.
		/// </summary>
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		/// <summary>
		/// Login of the user that filed the unban request.
		/// </summary>
		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		/// <summary>
		/// Display name of the user that filed the unban request.
		/// </summary>
		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		/// <summary>
		/// Text of the unban request. Set on the create event.
		/// </summary>
		[JsonPropertyName("text")]
		public string? Text { get; init; }

		/// <summary>
		/// Timestamp (RFC3339) of the unban request. Set on the create event.
		/// </summary>
		[JsonPropertyName("created_at")]
		public string? CreatedAt { get; init; }

		/// <summary>
		/// Message the moderator left when resolving the request. Set on the resolve event.
		/// </summary>
		[JsonPropertyName("resolution_text")]
		public string? ResolutionText { get; init; }

		/// <summary>
		/// Resolution status of the request. Set on the resolve event.
		/// </summary>
		[JsonPropertyName("status")]
		public string? Status { get; init; }
	}

	/// <summary>
	/// Payload shared by <c>channel.warning.send</c> and <c>channel.warning.acknowledge</c> EventSub notifications.
	/// The moderator fields, <c>reason</c> and <c>chat_rules_cited</c> are only set on the send event.
	/// </summary>
	[PublicAPI]
	public readonly struct WarningEvent
	{
		/// <summary>
		/// Id of the broadcaster (channel) the warning was issued in.
		/// </summary>
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		/// <summary>
		/// Login of the broadcaster (channel) the warning was issued in.
		/// </summary>
		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		/// <summary>
		/// Display name of the broadcaster (channel) the warning was issued in.
		/// </summary>
		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		/// <summary>
		/// Id of the moderator that issued the warning. Only set on the send event.
		/// </summary>
		[JsonPropertyName("moderator_user_id")]
		public string? ModeratorUserId { get; init; }

		/// <summary>
		/// Login of the moderator that issued the warning. Only set on the send event.
		/// </summary>
		[JsonPropertyName("moderator_user_login")]
		public string? ModeratorUserLogin { get; init; }

		/// <summary>
		/// Display name of the moderator that issued the warning. Only set on the send event.
		/// </summary>
		[JsonPropertyName("moderator_user_name")]
		public string? ModeratorUserName { get; init; }

		/// <summary>
		/// Id of the user that was warned.
		/// </summary>
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		/// <summary>
		/// Login of the user that was warned.
		/// </summary>
		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		/// <summary>
		/// Display name of the user that was warned.
		/// </summary>
		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		/// <summary>
		/// Reason of the warning. Only set on the send event.
		/// </summary>
		[JsonPropertyName("reason")]
		public string? Reason { get; init; }

		/// <summary>
		/// Chat rules cited in the warning. Only set on the send event.
		/// </summary>
		[JsonPropertyName("chat_rules_cited")]
		public string[]? ChatRulesCited { get; init; }
	}
}
