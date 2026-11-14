using System;
using CatCore.Models.Twitch.EventSub.Channel;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	/// <summary>
	/// EventSub events about channel activity: bits, cheers, custom power-ups, raids, channel updates and shoutouts.
	/// </summary>
	[PublicAPI]
	public interface ITwitchEventSubChannelActivityEvents
	{
		/// <summary>
		/// Fired when a user uses bits in the channel (cheer, power-up or custom power-up).
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, BitsUseEvent>? OnBitsUsed;

		/// <summary>
		/// Fired when a user cheers in the channel.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, CheerEvent>? OnCheer;

		/// <summary>
		/// Fired when a user redeems a custom power-up in the channel.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, CustomPowerUpRedemptionEvent>? OnCustomPowerUpRedeemed;

		/// <summary>
		/// Fired when another channel raids into the watched channel.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, RaidEvent>? OnRaid;

		/// <summary>
		/// Fired when the channel's title, language or category changes.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, ChannelUpdateEvent>? OnChannelUpdated;

		/// <summary>
		/// Fired when a shoutout is created on the channel.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, ShoutoutCreatedEvent>? OnShoutoutCreated;

		/// <summary>
		/// Fired when the channel receives a shoutout from another channel.
		/// First argument: the broadcaster (channel) user id.
		/// </summary>
		event Action<string, ShoutoutReceivedEvent>? OnShoutoutReceived;
	}
}
