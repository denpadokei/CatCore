using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.EventSub.Goal
{
	/// <summary>
	/// Event payload shared by the <c>channel.goal.begin</c>, <c>channel.goal.progress</c>
	/// and <c>channel.goal.end</c> EventSub notifications.
	/// Fields that are only present for some of those notifications are nullable.
	/// </summary>
	[PublicAPI]
	public readonly struct GoalEvent
	{
		/// <summary>
		/// The ID of the goal.
		/// </summary>
		[JsonPropertyName("id")]
		public string Id { get; init; }

		/// <summary>
		/// The ID of the broadcaster that created the goal.
		/// </summary>
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		/// <summary>
		/// The login of the broadcaster that created the goal.
		/// </summary>
		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		/// <summary>
		/// The display name of the broadcaster that created the goal.
		/// </summary>
		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		/// <summary>
		/// The type of goal: <c>follow</c>, <c>subscription</c>, <c>subscription_count</c>,
		/// <c>new_subscription</c>, <c>new_subscription_count</c>, <c>new_bit</c> or <c>new_cheerer</c>.
		/// </summary>
		[JsonPropertyName("type")]
		public string Type { get; init; }

		/// <summary>
		/// The description of the goal.
		/// </summary>
		[JsonPropertyName("description")]
		public string Description { get; init; }

		/// <summary>
		/// Whether the goal was achieved. Only present on the end notification.
		/// </summary>
		[JsonPropertyName("is_achieved")]
		public bool? IsAchieved { get; init; }

		/// <summary>
		/// The current amount of the goal.
		/// </summary>
		[JsonPropertyName("current_amount")]
		public int CurrentAmount { get; init; }

		/// <summary>
		/// The target amount of the goal.
		/// </summary>
		[JsonPropertyName("target_amount")]
		public int TargetAmount { get; init; }

		/// <summary>
		/// RFC3339 timestamp of when the goal started.
		/// </summary>
		[JsonPropertyName("started_at")]
		public string StartedAt { get; init; }

		/// <summary>
		/// RFC3339 timestamp of when the goal ended. Only present on the end notification.
		/// </summary>
		[JsonPropertyName("ended_at")]
		public string? EndedAt { get; init; }
	}
}
