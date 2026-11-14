using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.EventSub.User
{
	/// <summary>
	/// The <c>event</c> object of a <c>user.update</c> notification.
	/// This event is user scoped and carries no channel id.
	/// </summary>
	[PublicAPI]
	public readonly struct UserUpdateEvent
	{
		/// <summary>Identifier of the user.</summary>
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		/// <summary>Login name of the user.</summary>
		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		/// <summary>Display name of the user.</summary>
		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		/// <summary>
		/// Email address of the user, or an empty string when the application was not granted the
		/// <c>user:read:email</c> scope.
		/// </summary>
		[JsonPropertyName("email")]
		public string Email { get; init; }

		/// <summary>
		/// Whether the email address is verified. Meaningless when <see cref="Email"/> is empty.
		/// </summary>
		[JsonPropertyName("email_verified")]
		public bool EmailVerified { get; init; }

		/// <summary>Description of the user.</summary>
		[JsonPropertyName("description")]
		public string Description { get; init; }
	}

	/// <summary>
	/// The <c>event</c> object of a <c>user.whisper.message</c> notification.
	/// This event is user scoped and carries no channel id.
	/// </summary>
	[PublicAPI]
	public readonly struct WhisperEvent
	{
		/// <summary>Identifier of the user that sent the whisper.</summary>
		[JsonPropertyName("from_user_id")]
		public string FromUserId { get; init; }

		/// <summary>Login name of the user that sent the whisper.</summary>
		[JsonPropertyName("from_user_login")]
		public string FromUserLogin { get; init; }

		/// <summary>Display name of the user that sent the whisper.</summary>
		[JsonPropertyName("from_user_name")]
		public string FromUserName { get; init; }

		/// <summary>Identifier of the user that received the whisper.</summary>
		[JsonPropertyName("to_user_id")]
		public string ToUserId { get; init; }

		/// <summary>Login name of the user that received the whisper.</summary>
		[JsonPropertyName("to_user_login")]
		public string ToUserLogin { get; init; }

		/// <summary>Display name of the user that received the whisper.</summary>
		[JsonPropertyName("to_user_name")]
		public string ToUserName { get; init; }

		/// <summary>Identifier of the whisper.</summary>
		[JsonPropertyName("whisper_id")]
		public string WhisperId { get; init; }

		/// <summary>The whispered message.</summary>
		[JsonPropertyName("whisper")]
		public WhisperMessage Whisper { get; init; }
	}

	/// <summary>
	/// The message body of a whisper.
	/// </summary>
	[PublicAPI]
	public readonly struct WhisperMessage
	{
		/// <summary>Text of the whisper.</summary>
		[JsonPropertyName("text")]
		public string Text { get; init; }
	}
}
