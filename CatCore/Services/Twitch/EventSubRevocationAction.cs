namespace CatCore.Services.Twitch
{
	/// <summary>
	/// What the EventSub transport does when Twitch revokes a subscription, derived from the
	/// <c>subscription.status</c> value of the revocation message.
	/// </summary>
	internal enum EventSubRevocationAction
	{
		/// <summary>The authorization behind the subscription is gone; refresh the token and restart explicitly.</summary>
		ForceReauth,

		/// <summary>The user can no longer receive this channel's events; detach the channel.</summary>
		DetachChannel,

		/// <summary>The subscription type/version is no longer supported by Twitch; only an update can fix it.</summary>
		LogVersionRemoved,

		/// <summary>Unknown/undocumented status; log and ignore it until the next reconnect cycle.</summary>
		Ignore
	}
}
