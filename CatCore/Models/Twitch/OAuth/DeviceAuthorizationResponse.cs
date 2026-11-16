using System;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.OAuth
{
	/// <summary>
	/// Response of Twitch's device authorization endpoint (<c>POST /oauth2/device</c>).
	/// </summary>
	internal readonly struct DeviceAuthorizationResponse
	{
		[JsonPropertyName("device_code")]
		public string DeviceCode { get; }

		[JsonPropertyName("user_code")]
		public string UserCode { get; }

		[JsonPropertyName("verification_uri")]
		public string VerificationUri { get; }

		[JsonPropertyName("expires_in")]
		public int ExpiresInRaw { get; }

		[JsonPropertyName("interval")]
		public int IntervalRaw { get; }

		[JsonConstructor]
		public DeviceAuthorizationResponse(string deviceCode, string userCode, string verificationUri, int expiresInRaw, int intervalRaw)
		{
			DeviceCode = deviceCode;
			UserCode = userCode;
			VerificationUri = verificationUri;
			ExpiresInRaw = expiresInRaw;
			IntervalRaw = intervalRaw;
		}
	}

	/// <summary>
	/// Error body returned by Twitch's OAuth endpoints. The device code grant flow reports
	/// transient states through the <see cref="Message"/> property.
	/// </summary>
	internal readonly struct OAuthErrorResponse
	{
		[JsonPropertyName("status")]
		public int Status { get; }

		[JsonPropertyName("message")]
		public string? Message { get; }

		[JsonPropertyName("error")]
		public string? Error { get; }

		[JsonPropertyName("error_description")]
		public string? ErrorDescription { get; }

		[JsonConstructor]
		public OAuthErrorResponse(int status, string? message, string? error, string? errorDescription)
		{
			Status = status;
			Message = message;
			Error = error;
			ErrorDescription = errorDescription;
		}
	}

	/// <summary>
	/// User-facing information about an in-progress local device code authorization.
	/// </summary>
	internal readonly struct TwitchDeviceAuthorizationInfo
	{
		public string UserCode { get; }
		public string VerificationUri { get; }
		public DateTimeOffset ExpiresAt { get; }

		public TwitchDeviceAuthorizationInfo(string userCode, string verificationUri, DateTimeOffset expiresAt)
		{
			UserCode = userCode;
			VerificationUri = verificationUri;
			ExpiresAt = expiresAt;
		}
	}
}
