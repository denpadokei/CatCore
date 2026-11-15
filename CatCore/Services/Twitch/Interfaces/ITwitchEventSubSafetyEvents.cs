using System;
using CatCore.Models.Twitch.EventSub.Safety;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	/// <summary>
	/// EventSub events for moderator scoped safety features: suspicious users, unban requests and warnings.
	/// All of these are only delivered for channels the logged in user moderates.
	/// </summary>
	[PublicAPI]
	public interface ITwitchEventSubSafetyEvents
	{
		/// <summary>
		/// Fired when a suspicious user sends a message in a channel.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, SuspiciousUserMessageEvent>? OnSuspiciousUserMessage;

		/// <summary>
		/// Fired when the low trust status of a suspicious user changes.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, SuspiciousUserUpdateEvent>? OnSuspiciousUserUpdated;

		/// <summary>
		/// Fired when a user files an unban request in a channel.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, UnbanRequestEvent>? OnUnbanRequestCreated;

		/// <summary>
		/// Fired when an unban request gets resolved.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, UnbanRequestEvent>? OnUnbanRequestResolved;

		/// <summary>
		/// Fired when a warning is sent to a user in a channel.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, WarningEvent>? OnWarningSent;

		/// <summary>
		/// Fired when a warned user acknowledges the warning.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, WarningEvent>? OnWarningAcknowledged;
	}
}
