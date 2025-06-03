using System;
using System.Text.Json.Serialization;
using CatCore.Shared.Models.Twitch.OAuth;

namespace CatCore.Models.Credentials
{
	internal sealed class TwitchCredentials : ICredentials, IEquatable<TwitchCredentials>
	{
		public string? AccessToken { get; }
		public string? AppAccessToken { get; }
		public string? RefreshToken { get; }
		public DateTimeOffset? ValidUntil { get; }
		public DateTimeOffset? ValidUntilAppToken { get; }

		public TwitchCredentials()
		{
		}

		[JsonConstructor]
		public TwitchCredentials(string? accessToken, string? refreshToken, string? appAccessToken, DateTimeOffset? validUntil, DateTimeOffset? validUntilAppToken)
		{
			AccessToken = accessToken;
			AppAccessToken = appAccessToken;
			RefreshToken = refreshToken;
			ValidUntil = validUntil;
			ValidUntilAppToken = validUntilAppToken;
		}

		public TwitchCredentials(AuthorizationResponse authorizationResponse, AppTokenAuthorizationResponse? appTokenAuthorizationResponse)
		{
			AccessToken = authorizationResponse.AccessToken;
			AppAccessToken = appTokenAuthorizationResponse?.AccessToken;
			RefreshToken = authorizationResponse.RefreshToken;
			ValidUntil = authorizationResponse.ExpiresIn;
			ValidUntilAppToken = appTokenAuthorizationResponse?.ExpiresIn;
		}

		public static TwitchCredentials Empty() => new();

		public bool Equals(TwitchCredentials? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return AccessToken == other.AccessToken && RefreshToken == other.RefreshToken && AppAccessToken == other.AppAccessToken;
		}

		public override bool Equals(object? obj)
		{
			return ReferenceEquals(this, obj) || obj is TwitchCredentials other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (AccessToken != null ? AccessToken.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (RefreshToken != null ? RefreshToken.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (AppAccessToken != null ? AppAccessToken.GetHashCode() : 0);
				return hashCode;
			}
		}
	}
}