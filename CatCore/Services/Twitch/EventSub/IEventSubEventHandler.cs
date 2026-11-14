using System.Collections.Generic;

namespace CatCore.Services.Twitch.EventSub
{
	/// <summary>
	/// Handles the notifications of one or more EventSub subscription types.
	/// </summary>
	internal interface IEventSubEventHandler
	{
		/// <summary>
		/// The subscription types this handler wants to receive.
		/// </summary>
		IReadOnlyCollection<EventSubSubscriptionSpec> Subscriptions { get; }

		/// <summary>
		/// Handles a single notification.
		/// </summary>
		/// <param name="subscriptionType">The EventSub subscription type, e.g. channel.hype_train.begin.</param>
		/// <param name="subscriptionVersion">The EventSub subscription version, e.g. 2.</param>
		/// <param name="eventJson">The raw JSON of the EventSub "event" object, not the whole message.</param>
		void Handle(string subscriptionType, string subscriptionVersion, string eventJson);
	}
}
