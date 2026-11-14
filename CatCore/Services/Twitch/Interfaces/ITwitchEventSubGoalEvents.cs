using System;
using CatCore.Models.Twitch.EventSub.Goal;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	/// <summary>
	/// Channel goal related EventSub events.
	/// The first argument of every callback is the id of the channel (broadcaster) the event belongs to.
	/// </summary>
	[PublicAPI]
	public interface ITwitchEventSubGoalEvents
	{
		/// <summary>
		/// Fired when a goal begins on a channel.
		/// </summary>
		event Action<string, GoalEvent>? OnBegin;

		/// <summary>
		/// Fired when a goal progresses on a channel.
		/// </summary>
		event Action<string, GoalEvent>? OnProgress;

		/// <summary>
		/// Fired when a goal ends on a channel.
		/// </summary>
		event Action<string, GoalEvent>? OnEnd;
	}
}
