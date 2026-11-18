using System;
using System.IO;
using System.Net;

namespace CatCore.Azure.Services.Twitch
{
	/// <summary>
	/// Result of a Twitch token request. Keeping the original status and error code allows the
	/// HTTP function to distinguish a permanent rejection from a temporary Twitch outage instead
	/// of collapsing everything into 401.
	/// </summary>
	internal readonly struct TwitchTokenResponse
	{
		public Stream? Body { get; }
		public HttpStatusCode? TwitchStatus { get; }
		public string? ErrorMessage { get; }

		private TwitchTokenResponse(Stream? body, HttpStatusCode? twitchStatus, string? errorMessage)
		{
			Body = body;
			TwitchStatus = twitchStatus;
			ErrorMessage = errorMessage;
		}

		public static TwitchTokenResponse Success(Stream body) => new(body, null, null);

		public static TwitchTokenResponse Failure(HttpStatusCode statusCode, string? errorMessage) => new(null, statusCode, errorMessage);

		public HttpStatusCode ToHttpStatusCode()
		{
			if (Body != null)
			{
				return HttpStatusCode.OK;
			}

			if (TwitchStatus == HttpStatusCode.TooManyRequests)
			{
				return HttpStatusCode.TooManyRequests;
			}

			if (TwitchStatus.HasValue && (int)TwitchStatus.Value >= 500)
			{
				return HttpStatusCode.BadGateway;
			}

			if (ErrorMessage != null &&
			    (ErrorMessage.IndexOf("invalid grant", StringComparison.OrdinalIgnoreCase) >= 0
			     || ErrorMessage.IndexOf("invalid client", StringComparison.OrdinalIgnoreCase) >= 0))
			{
				return HttpStatusCode.BadRequest;
			}

			return HttpStatusCode.Unauthorized;
		}
	}
}
