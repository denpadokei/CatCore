using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace CatCore.Shared.Models.Twitch.OAuth
{
	public class AppTokenAuthorizationResponse
	{
		[JsonPropertyName("access_token")]
		public string? AccessToken { get; set; }

		[JsonIgnore]
		private int? _expiresInRaw;
		[JsonPropertyName("expires_in")]
		public int? ExpiresInRaw
		{
			get
			{
				return _expiresInRaw;
			}

			set
			{
				_expiresInRaw = value;
				ExpiresIn = value.HasValue ? DateTimeOffset.Now.AddSeconds(value.Value) : null;
			}
		}

		[JsonPropertyName("token_type")]
		public string? TokenType { get; set; }
		[JsonIgnore]
		public DateTimeOffset? ExpiresIn { get; set; }
	}
}
