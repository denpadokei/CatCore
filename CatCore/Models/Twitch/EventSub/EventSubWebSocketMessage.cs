using System;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.EventSub
{
	internal record struct EventSubMetadata
	{
		[JsonPropertyName("message_id")]
		public string MessageId { get; init; }

		[JsonPropertyName("message_type")]
		public string MessageType { get; init; }

		[JsonPropertyName("message_timestamp")]
		public DateTime MessageTimestamp { get; init; }

		[JsonPropertyName("subscription_type")]
		public string SubscriptionType { get; init; }

		[JsonPropertyName("subscription_version")]
		public string SubscriptionVersion { get; init; }
	}

	internal record struct EventSubWebSocketMessage<TPayload> where TPayload : struct
	{
		[JsonPropertyName("metadata")]
		public EventSubMetadata Metadata { get; init; }

		[JsonPropertyName("payload")]
		public TPayload Payload { get; init; }
	}
}
