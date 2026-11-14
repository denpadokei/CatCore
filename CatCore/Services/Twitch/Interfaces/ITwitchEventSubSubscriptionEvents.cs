using System;
using CatCore.Models.Twitch.EventSub.Subscription;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	/// <summary>
	/// EventSub events for channel subscriptions, gifted subscriptions and VIPs.
	/// Every callback receives the channel (broadcaster) user id as its first argument.
	/// </summary>
	[PublicAPI]
	public interface ITwitchEventSubSubscriptionEvents
	{
		/// <summary>
		/// Fired when a user subscribes to the channel for the first time (a new subscription, not a resubscription).
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, SubscriptionEvent>? OnSubscribed;

		/// <summary>
		/// Fired when a subscription to the channel ends.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, SubscriptionEndEvent>? OnSubscriptionEnded;

		/// <summary>
		/// Fired when one or more subscriptions are gifted on the channel.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, SubscriptionGiftEvent>? OnSubscriptionGifted;

		/// <summary>
		/// Fired when a user resubscribes to the channel and shares a message.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, SubscriptionMessageEvent>? OnSubscriptionMessage;

		/// <summary>
		/// Fired when a user is made a VIP on the channel.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, VipEvent>? OnVipAdded;

		/// <summary>
		/// Fired when a user is removed as a VIP on the channel.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, VipEvent>? OnVipRemoved;
	}
}
