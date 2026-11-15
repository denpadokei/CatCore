using System;
using CatCore.Models.Twitch.EventSub.Charity;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	/// <summary>
	/// Public events of the charity campaign EventSub subscription types.
	/// Every callback receives the broadcaster (channel) user id as its first argument.
	/// </summary>
	[PublicAPI]
	public interface ITwitchEventSubCharityEvents
	{
		/// <summary>
		/// Fired when a user donates to the charity campaign of a channel.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, CharityDonationEvent>? OnDonation;

		/// <summary>
		/// Fired when the charity campaign of a channel progresses.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, CharityCampaignEvent>? OnProgress;

		/// <summary>
		/// Fired when the charity campaign of a channel starts.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, CharityCampaignEvent>? OnStarted;

		/// <summary>
		/// Fired when the charity campaign of a channel stops.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, CharityCampaignEvent>? OnStopped;
	}
}
