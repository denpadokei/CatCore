using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.EventSub.HypeTrain
{
	/// <summary>
	/// Event payload shared by the <c>channel.hype_train.begin</c>, <c>channel.hype_train.progress</c>
	/// and <c>channel.hype_train.end</c> EventSub notifications.
	/// Fields that are only present for some of those notifications are nullable.
	/// </summary>
	[PublicAPI]
	public readonly struct HypeTrainEvent
	{
		/// <summary>
		/// The ID of the hype train.
		/// </summary>
		[JsonPropertyName("id")]
		public string Id { get; init; }

		/// <summary>
		/// The ID of the broadcaster running the hype train.
		/// </summary>
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		/// <summary>
		/// The login of the broadcaster running the hype train.
		/// </summary>
		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		/// <summary>
		/// The display name of the broadcaster running the hype train.
		/// </summary>
		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		/// <summary>
		/// Total amount contributed to the hype train.
		/// </summary>
		[JsonPropertyName("total")]
		public int Total { get; init; }

		/// <summary>
		/// Progress towards the next hype train level. Absent on the end notification.
		/// </summary>
		[JsonPropertyName("progress")]
		public int? Progress { get; init; }

		/// <summary>
		/// The goal of the hype train. Absent on the end notification.
		/// </summary>
		[JsonPropertyName("goal")]
		public int? Goal { get; init; }

		/// <summary>
		/// The current level of the hype train.
		/// </summary>
		[JsonPropertyName("level")]
		public int Level { get; init; }

		/// <summary>
		/// The type of the hype train: <c>treasure</c>, <c>golden_kappa</c> or <c>regular</c>.
		/// </summary>
		[JsonPropertyName("type")]
		public string Type { get; init; }

		/// <summary>
		/// Whether the hype train is shared between channels.
		/// </summary>
		[JsonPropertyName("is_shared_train")]
		public bool IsSharedTrain { get; init; }

		/// <summary>
		/// The highest level the channel ever reached. Only present on the begin notification.
		/// </summary>
		[JsonPropertyName("all_time_high_level")]
		public int? AllTimeHighLevel { get; init; }

		/// <summary>
		/// The highest total the channel ever reached. Only present on the begin notification.
		/// </summary>
		[JsonPropertyName("all_time_high_total")]
		public int? AllTimeHighTotal { get; init; }

		/// <summary>
		/// RFC3339 timestamp of when the hype train started.
		/// </summary>
		[JsonPropertyName("started_at")]
		public string StartedAt { get; init; }

		/// <summary>
		/// RFC3339 timestamp of when the hype train expires. Present on begin and progress.
		/// </summary>
		[JsonPropertyName("expires_at")]
		public string? ExpiresAt { get; init; }

		/// <summary>
		/// RFC3339 timestamp of when the hype train ended. Only present on the end notification.
		/// </summary>
		[JsonPropertyName("ended_at")]
		public string? EndedAt { get; init; }

		/// <summary>
		/// RFC3339 timestamp of when the cooldown ends. Only present on the end notification.
		/// </summary>
		[JsonPropertyName("cooldown_ends_at")]
		public string? CooldownEndsAt { get; init; }

		/// <summary>
		/// The top contributions to this hype train.
		/// </summary>
		[JsonPropertyName("top_contributions")]
		public HypeTrainContribution[]? TopContributions { get; init; }

		/// <summary>
		/// The channels participating in a shared hype train.
		/// </summary>
		[JsonPropertyName("shared_train_participants")]
		public HypeTrainParticipant[]? SharedTrainParticipants { get; init; }
	}

	/// <summary>
	/// A single top contribution to a hype train.
	/// </summary>
	[PublicAPI]
	public readonly struct HypeTrainContribution
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		/// <summary>
		/// The source of the contribution: <c>bits</c>, <c>subscription</c> or <c>other</c>.
		/// </summary>
		[JsonPropertyName("type")]
		public string Type { get; init; }

		[JsonPropertyName("total")]
		public int Total { get; init; }
	}

	/// <summary>
	/// A channel participating in a shared hype train.
	/// </summary>
	[PublicAPI]
	public readonly struct HypeTrainParticipant
	{
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }
	}
}
