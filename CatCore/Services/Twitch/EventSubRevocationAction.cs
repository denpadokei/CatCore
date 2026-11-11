namespace CatCore.Services.Twitch
{
	/// <summary>
	/// What the EventSub transport does when Twitch revokes a subscription, derived from the
	/// <c>subscription.status</c> value of the revocation message.
	/// </summary>
	internal enum EventSubRevocationAction
	{
		/// <summary>The authorization behind the subscription is gone; refresh the token and let the restart re-subscribe.</summary>
		ForceReauth,

		/// <summary>The user can no longer receive this channel's events (removed/banned/moderator removed); detach the channel.</summary>
		DetachChannel,

		/// <summary>The subscription type/version is no longer supported by Twitch; only an update can fix it.</summary>
		LogVersionRemoved,

		/// <summary>Transient/unknown reason; reconcile by re-subscribing the channel.</summary>
		ResubscribeChannel
	}
}
