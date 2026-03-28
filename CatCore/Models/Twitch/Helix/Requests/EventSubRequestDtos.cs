using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.Helix.Requests
{
	[PublicAPI]
	internal record struct SendChatMessageRequestDto
	{
		[JsonPropertyName("broadcaster_id")]
		public string BroadcasterId { get; init; }

		[JsonPropertyName("sender_id")]
		public string SenderId { get; init; }

		[JsonPropertyName("message")]
		public string Message { get; init; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		[JsonPropertyName("reply_parent_message_id")]
		public string? ReplyParentMessageId { get; init; }
	}

	[PublicAPI]
	internal record struct EventSubSubscriptionRequestDto
	{
		[JsonPropertyName("type")]
		public string Type { get; init; }

		[JsonPropertyName("version")]
		public string Version { get; init; }

		[JsonPropertyName("condition")]
		public Dictionary<string, string> Condition { get; init; }

		[JsonPropertyName("transport")]
		public EventSubTransportDto Transport { get; init; }
	}

	[PublicAPI]
	internal record struct EventSubTransportDto
	{
		[JsonPropertyName("method")]
		public string Method { get; init; }

		[JsonPropertyName("session_id")]
		public string SessionId { get; init; }
	}

	[PublicAPI]
	internal record struct EventSubSubscriptionResponseDto
	{
		[JsonPropertyName("data")]
		public List<EventSubSubscriptionInfoDto> Data { get; init; }

		[JsonPropertyName("total")]
		public int Total { get; init; }

		[JsonPropertyName("total_cost")]
		public int TotalCost { get; init; }

		[JsonPropertyName("max_total_cost")]
		public int MaxTotalCost { get; init; }

		[JsonPropertyName("pagination")]
		public Dictionary<string, string>? Pagination { get; init; }
	}

	[PublicAPI]
	internal record struct EventSubSubscriptionInfoDto
	{
		[JsonPropertyName("id")]
		public string Id { get; init; }

		[JsonPropertyName("type")]
		public string Type { get; init; }

		[JsonPropertyName("version")]
		public string Version { get; init; }

		[JsonPropertyName("status")]
		public string Status { get; init; }

		[JsonPropertyName("condition")]
		public Dictionary<string, string> Condition { get; init; }

		[JsonPropertyName("transport")]
		public Dictionary<string, string> Transport { get; init; }

		[JsonPropertyName("created_at")]
		public DateTime CreatedAt { get; init; }

		[JsonPropertyName("cost")]
		public int Cost { get; init; }
	}
}
