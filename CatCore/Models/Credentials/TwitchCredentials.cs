using System;
using System.Text.Json.Serialization;
using CatCore.Shared.Models.Twitch.OAuth;

namespace CatCore.Models.Credentials
{
	internal sealed class TwitchCredentials : ICredentials, IEquatable<TwitchCredentials>
	{
		public string? AccessToken { get; }
		public string? RefreshToken { get; }
		public DateTimeOffset? ValidUntil { get; }

		/// <summary>
		/// Identifies how this token pair was obtained. Tokens obtained through the local device
		/// code flow can be refreshed without a client secret, while tokens obtained through the
		/// CatCore auth back-end normally need it. A missing value means the tokens predate this
		/// property and were obtained through the auth back-end.
		/// </summary>
		public string? TokenSource { get; }

		public TwitchCredentials()
		{
		}

		[JsonConstructor]
		public TwitchCredentials(string? accessToken, string? refreshToken, DateTimeOffset? validUntil, string? tokenSource = null)
		{
			AccessToken = accessToken;
			RefreshToken = refreshToken;
			ValidUntil = validUntil;
			TokenSource = tokenSource;
		}

		public TwitchCredentials(AuthorizationResponse authorizationResponse, string tokenSource)
		{
			AccessToken = authorizationResponse.AccessToken;
			RefreshToken = authorizationResponse.RefreshToken;
			ValidUntil = authorizationResponse.ExpiresIn;
			TokenSource = tokenSource;
		}

		[JsonIgnore]
		public bool IsLocalDeviceCodeToken => string.Equals(TokenSource, TwitchTokenSources.LocalDeviceCode, StringComparison.Ordinal);

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

			return AccessToken == other.AccessToken && RefreshToken == other.RefreshToken && TokenSource == other.TokenSource;
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
				hashCode = (hashCode * 397) ^ (TokenSource != null ? TokenSource.GetHashCode() : 0);
				return hashCode;
			}
		}
	}
}
