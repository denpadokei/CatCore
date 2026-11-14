using System;
using CatCore.Models.Twitch.EventSub.ChannelPoints;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	/// <summary>
	/// Channel points related EventSub events.
	/// </summary>
	[PublicAPI]
	public interface ITwitchEventSubChannelPointsEvents
	{
		/// <summary>
		/// Fired when a viewer redeems an automatic channel points reward.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, AutomaticRewardRedemptionEvent>? OnAutomaticRewardRedeemed;

		/// <summary>
		/// Fired when the status of a custom reward redemption changes.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, CustomRewardRedemptionEvent>? OnRewardRedemptionUpdated;

		/// <summary>
		/// Fired when a custom reward is created.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, CustomRewardEvent>? OnRewardCreated;

		/// <summary>
		/// Fired when a custom reward is removed.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, CustomRewardEvent>? OnRewardRemoved;

		/// <summary>
		/// Fired when a custom reward is updated.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, CustomRewardEvent>? OnRewardUpdated;
	}
}
