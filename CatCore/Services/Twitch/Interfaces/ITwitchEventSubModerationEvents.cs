using System;
using CatCore.Models.Twitch.EventSub.Moderation;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	/// <summary>
	/// Events emitted for the channel moderation EventSub subscription types.
	/// All callbacks receive the broadcaster (channel) user id as first argument.
	/// </summary>
	[PublicAPI]
	public interface ITwitchEventSubModerationEvents
	{
		/// <summary>
		/// Fired when a user is banned from the channel (channel.ban).
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, BanEvent>? OnBanned;

		/// <summary>
		/// Fired when a user is unbanned from the channel (channel.unban).
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, UnbanEvent>? OnUnbanned;

		/// <summary>
		/// Fired when a moderation action is taken in the channel (channel.moderate, version 2).
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, ModerationActionEvent>? OnModerated;

		/// <summary>
		/// Fired when a user is added as a moderator of the channel (channel.moderator.add).
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, ModeratorEvent>? OnModeratorAdded;

		/// <summary>
		/// Fired when a user is removed as a moderator of the channel (channel.moderator.remove).
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, ModeratorEvent>? OnModeratorRemoved;

		/// <summary>
		/// Fired when shield mode is activated on the channel (channel.shield_mode.begin).
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, ShieldModeEvent>? OnShieldModeStarted;

		/// <summary>
		/// Fired when shield mode is deactivated on the channel (channel.shield_mode.end).
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, ShieldModeEvent>? OnShieldModeEnded;
	}
}
