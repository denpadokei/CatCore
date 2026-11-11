using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.EventSub
{
	internal struct EventSubSessionPayload
	{
		[JsonPropertyName("session")]
		public EventSubSessionInfo Session { get; init; }
	}

	[PublicAPI]
	public struct EventSubSessionInfo
	{
		[JsonPropertyName("id")]
		public string Id { get; init; }

		[JsonPropertyName("status")]
		public string Status { get; init; }

		[JsonPropertyName("keepalive_timeout_seconds")]
		public int? KeepaliveTimeoutSeconds { get; init; }

		[JsonPropertyName("reconnect_url")]
		public string? ReconnectUrl { get; init; }

		[JsonPropertyName("connected_at")]
		public string ConnectedAt { get; init; }
	}
}
