using System;
using CatCore.Models.Twitch.EventSub.AutoMod;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	/// <summary>
	/// Events of the AutoMod and chat user message hold/update EventSub subscription types.
	/// </summary>
	[PublicAPI]
	public interface ITwitchEventSubAutoModEvents
	{
		/// <summary>
		/// Fired when AutoMod holds a message (automod.message.hold v2).
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, AutoModMessageHoldEvent>? OnMessageHeld;

		/// <summary>
		/// Fired when a held message is approved or denied (automod.message.update v2).
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, AutoModMessageUpdateEvent>? OnMessageUpdated;

		/// <summary>
		/// Fired when the AutoMod settings of a channel change (automod.settings.update).
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, AutoModSettingsEvent>? OnSettingsUpdated;

		/// <summary>
		/// Fired when AutoMod terms of a channel change (automod.terms.update).
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, AutoModTermsEvent>? OnTermsUpdated;

		/// <summary>
		/// Fired when a message of the logged in user is held (channel.chat.user_message_hold).
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, ChatUserMessageHoldEvent>? OnUserMessageHeld;

		/// <summary>
		/// Fired when a held message of the logged in user is approved or denied (channel.chat.user_message_update).
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, ChatUserMessageUpdateEvent>? OnUserMessageUpdated;
	}
}
