using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.EventSub.Moderation
{
	/// <summary>
	/// Payload of a channel.ban event.
	/// </summary>
	[PublicAPI]
	public readonly struct BanEvent
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

		[JsonPropertyName("moderator_user_id")]
		public string ModeratorUserId { get; init; }

		[JsonPropertyName("moderator_user_login")]
		public string ModeratorUserLogin { get; init; }

		[JsonPropertyName("moderator_user_name")]
		public string ModeratorUserName { get; init; }

		[JsonPropertyName("reason")]
		public string Reason { get; init; }

		[JsonPropertyName("banned_at")]
		public string BannedAt { get; init; }

		/// <summary>
		/// Null when the ban is permanent.
		/// </summary>
		[JsonPropertyName("ends_at")]
		public string? EndsAt { get; init; }

		[JsonPropertyName("is_permanent")]
		public bool IsPermanent { get; init; }
	}

	/// <summary>
	/// Payload of a channel.unban event.
	/// </summary>
	[PublicAPI]
	public readonly struct UnbanEvent
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

		[JsonPropertyName("moderator_user_id")]
		public string ModeratorUserId { get; init; }

		[JsonPropertyName("moderator_user_login")]
		public string ModeratorUserLogin { get; init; }

		[JsonPropertyName("moderator_user_name")]
		public string ModeratorUserName { get; init; }
	}

	/// <summary>
	/// Payload of a channel.moderate (version 2) event. Only the property that matches
	/// <see cref="Action"/> is populated; all others are null.
	/// </summary>
	[PublicAPI]
	public readonly struct ModerationActionEvent
	{
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("source_broadcaster_user_id")]
		public string? SourceBroadcasterUserId { get; init; }

		[JsonPropertyName("source_broadcaster_user_login")]
		public string? SourceBroadcasterUserLogin { get; init; }

		[JsonPropertyName("source_broadcaster_user_name")]
		public string? SourceBroadcasterUserName { get; init; }

		[JsonPropertyName("moderator_user_id")]
		public string ModeratorUserId { get; init; }

		[JsonPropertyName("moderator_user_login")]
		public string ModeratorUserLogin { get; init; }

		[JsonPropertyName("moderator_user_name")]
		public string ModeratorUserName { get; init; }

		[JsonPropertyName("action")]
		public string Action { get; init; }

		[JsonPropertyName("followers")]
		public ModerationFollowers? Followers { get; init; }

		[JsonPropertyName("slow")]
		public ModerationSlow? Slow { get; init; }

		[JsonPropertyName("vip")]
		public ModerationVip? Vip { get; init; }

		[JsonPropertyName("unvip")]
		public ModerationVip? Unvip { get; init; }

		[JsonPropertyName("warn")]
		public ModerationWarn? Warn { get; init; }

		[JsonPropertyName("unmod")]
		public ModerationUser? Unmod { get; init; }

		[JsonPropertyName("ban")]
		public ModerationBan? Ban { get; init; }

		[JsonPropertyName("unban")]
		public ModerationUser? Unban { get; init; }

		[JsonPropertyName("timeout")]
		public ModerationTimeout? Timeout { get; init; }

		[JsonPropertyName("untimeout")]
		public ModerationUser? Untimeout { get; init; }

		[JsonPropertyName("raid")]
		public ModerationRaid? Raid { get; init; }

		[JsonPropertyName("unraid")]
		public ModerationUser? Unraid { get; init; }

		[JsonPropertyName("delete")]
		public ModerationDelete? Delete { get; init; }

		[JsonPropertyName("automod_terms")]
		public ModerationAutomodTerms? AutomodTerms { get; init; }

		[JsonPropertyName("unban_request")]
		public ModerationUnbanRequest? UnbanRequest { get; init; }
	}

	/// <summary>
	/// Followers mode change, part of a channel.moderate event.
	/// </summary>
	[PublicAPI]
	public readonly struct ModerationFollowers
	{
		[JsonPropertyName("follow_duration_minutes")]
		public int FollowDurationMinutes { get; init; }
	}

	/// <summary>
	/// Slow mode change, part of a channel.moderate event.
	/// </summary>
	[PublicAPI]
	public readonly struct ModerationSlow
	{
		[JsonPropertyName("wait_time_seconds")]
		public int WaitTimeSeconds { get; init; }
	}

	/// <summary>
	/// VIP added or removed, part of a channel.moderate event.
	/// </summary>
	[PublicAPI]
	public readonly struct ModerationVip
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }
	}

	/// <summary>
	/// Target user of a moderation action, part of a channel.moderate event.
	/// </summary>
	[PublicAPI]
	public readonly struct ModerationUser
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }
	}

	/// <summary>
	/// Ban action, part of a channel.moderate event.
	/// </summary>
	[PublicAPI]
	public readonly struct ModerationBan
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		[JsonPropertyName("reason")]
		public string? Reason { get; init; }
	}

	/// <summary>
	/// Warning action, part of a channel.moderate event.
	/// </summary>
	[PublicAPI]
	public readonly struct ModerationWarn
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		[JsonPropertyName("reason")]
		public string? Reason { get; init; }

		[JsonPropertyName("chat_rules_cited")]
		public string[]? ChatRulesCited { get; init; }
	}

	/// <summary>
	/// Timeout action, part of a channel.moderate event.
	/// </summary>
	[PublicAPI]
	public readonly struct ModerationTimeout
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		[JsonPropertyName("reason")]
		public string? Reason { get; init; }

		[JsonPropertyName("expires_at")]
		public string ExpiresAt { get; init; }
	}

	/// <summary>
	/// Raid action, part of a channel.moderate event.
	/// </summary>
	[PublicAPI]
	public readonly struct ModerationRaid
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		[JsonPropertyName("viewer_count")]
		public int ViewerCount { get; init; }
	}

	/// <summary>
	/// Message deleted action, part of a channel.moderate event.
	/// </summary>
	[PublicAPI]
	public readonly struct ModerationDelete
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		[JsonPropertyName("message_id")]
		public string MessageId { get; init; }

		[JsonPropertyName("message_body")]
		public string MessageBody { get; init; }
	}

	/// <summary>
	/// AutoMod term change, part of a channel.moderate event.
	/// </summary>
	[PublicAPI]
	public readonly struct ModerationAutomodTerms
	{
		[JsonPropertyName("action")]
		public string Action { get; init; }

		[JsonPropertyName("list")]
		public string List { get; init; }

		[JsonPropertyName("terms")]
		public string[] Terms { get; init; }

		[JsonPropertyName("from_automod")]
		public bool FromAutomod { get; init; }
	}

	/// <summary>
	/// Unban request resolution, part of a channel.moderate event.
	/// </summary>
	[PublicAPI]
	public readonly struct ModerationUnbanRequest
	{
		[JsonPropertyName("is_approved")]
		public bool IsApproved { get; init; }

		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		[JsonPropertyName("moderator_message")]
		public string ModeratorMessage { get; init; }
	}

	/// <summary>
	/// Payload of a channel.moderator.add or channel.moderator.remove event.
	/// </summary>
	[PublicAPI]
	public readonly struct ModeratorEvent
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
	}

	/// <summary>
	/// Payload of a channel.shield_mode.begin or channel.shield_mode.end event.
	/// </summary>
	[PublicAPI]
	public readonly struct ShieldModeEvent
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

		[JsonPropertyName("started_at")]
		public string? StartedAt { get; init; }

		[JsonPropertyName("ended_at")]
		public string? EndedAt { get; init; }
	}
}
