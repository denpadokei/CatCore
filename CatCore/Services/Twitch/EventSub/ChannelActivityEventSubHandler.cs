using System;
using System.Collections.Generic;
using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.EventSub.Channel;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch.EventSub
{
	/// <summary>
	/// Handles the channel activity EventSub subscription types:
	/// channel.bits.use, channel.cheer, channel.custom_power_up_redemption.add, channel.raid,
	/// channel.update, channel.shoutout.create and channel.shoutout.receive.
	/// </summary>
	internal sealed class ChannelActivityEventSubHandler : IEventSubEventHandler, ITwitchEventSubChannelActivityEvents
	{
		private readonly ILogger _logger;

		public ChannelActivityEventSubHandler(ILogger logger)
		{
			_logger = logger;
		}

		public IReadOnlyCollection<EventSubSubscriptionSpec> Subscriptions { get; } = new[]
		{
			new EventSubSubscriptionSpec("channel.bits.use", "1", EventSubConditionKind.Broadcaster, new[] { "bits:read" }, true),
			new EventSubSubscriptionSpec("channel.cheer", "1", EventSubConditionKind.Broadcaster, new[] { "bits:read" }, true),
			new EventSubSubscriptionSpec("channel.custom_power_up_redemption.add", "1", EventSubConditionKind.Broadcaster, new[] { "bits:read" }, true),
			new EventSubSubscriptionSpec("channel.raid", "1", EventSubConditionKind.ToBroadcaster, Array.Empty<string>(), false),
			new EventSubSubscriptionSpec("channel.update", "2", EventSubConditionKind.Broadcaster, Array.Empty<string>(), false),
			new EventSubSubscriptionSpec("channel.shoutout.create", "1", EventSubConditionKind.BroadcasterAndModerator, new[] { "moderator:read:shoutouts|moderator:manage:shoutouts" }, true),
			new EventSubSubscriptionSpec("channel.shoutout.receive", "1", EventSubConditionKind.BroadcasterAndModerator, new[] { "moderator:read:shoutouts|moderator:manage:shoutouts" }, true)
		};

		public event Action<string, BitsUseEvent>? OnBitsUsed;

		public event Action<string, CheerEvent>? OnCheer;

		public event Action<string, CustomPowerUpRedemptionEvent>? OnCustomPowerUpRedeemed;

		public event Action<string, RaidEvent>? OnRaid;

		public event Action<string, ChannelUpdateEvent>? OnChannelUpdated;

		public event Action<string, ShoutoutCreatedEvent>? OnShoutoutCreated;

		public event Action<string, ShoutoutReceivedEvent>? OnShoutoutReceived;

		public void Handle(string subscriptionType, string subscriptionVersion, string eventJson)
		{
			try
			{
				switch (subscriptionType)
				{
					case "channel.bits.use":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubChannelActivitySerializerContext.Default.BitsUseEvent);
						OnBitsUsed?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.cheer":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubChannelActivitySerializerContext.Default.CheerEvent);
						OnCheer?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.custom_power_up_redemption.add":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubChannelActivitySerializerContext.Default.CustomPowerUpRedemptionEvent);
						OnCustomPowerUpRedeemed?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.raid":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubChannelActivitySerializerContext.Default.RaidEvent);
						OnRaid?.Invoke(data.ToBroadcasterUserId, data);
						break;
					}
					case "channel.update":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubChannelActivitySerializerContext.Default.ChannelUpdateEvent);
						OnChannelUpdated?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.shoutout.create":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubChannelActivitySerializerContext.Default.ShoutoutCreatedEvent);
						OnShoutoutCreated?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.shoutout.receive":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubChannelActivitySerializerContext.Default.ShoutoutReceivedEvent);
						OnShoutoutReceived?.Invoke(data.BroadcasterUserId, data);
						break;
					}
				}
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to handle {SubscriptionType}", subscriptionType);
			}
		}
	}
}
