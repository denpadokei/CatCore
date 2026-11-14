using System;
using CatCore.Models.Twitch.EventSub.User;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	/// <summary>
	/// Public events of the user scoped EventSub subscription types.
	/// These subscriptions are created once per EventSub session, so the callbacks carry no channel id.
	/// </summary>
	[PublicAPI]
	public interface ITwitchEventSubUserEvents
	{
		/// <summary>
		/// Fired when the authorized user updates their account.
		/// </summary>
		event Action<UserUpdateEvent>? OnUserUpdated;

		/// <summary>
		/// Fired when the authorized user receives a whisper.
		/// </summary>
		event Action<WhisperEvent>? OnWhisperReceived;
	}
}
