using System;
using CatCore.Models.Twitch.EventSub.HypeTrain;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	/// <summary>
	/// Hype train related EventSub events.
	/// The first argument of every callback is the id of the channel (broadcaster) the event belongs to.
	/// </summary>
	[PublicAPI]
	public interface ITwitchEventSubHypeTrainEvents
	{
		/// <summary>
		/// Fired when a hype train begins on a channel.
		/// </summary>
		event Action<string, HypeTrainEvent>? OnBegin;

		/// <summary>
		/// Fired when a hype train progresses on a channel.
		/// </summary>
		event Action<string, HypeTrainEvent>? OnProgress;

		/// <summary>
		/// Fired when a hype train ends on a channel.
		/// </summary>
		event Action<string, HypeTrainEvent>? OnEnd;
	}
}
