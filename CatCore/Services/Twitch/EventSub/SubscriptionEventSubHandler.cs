using System;
using System.Collections.Generic;
using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.EventSub.Subscription;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch.EventSub
{
	/// <summary>
	/// Handles the EventSub subscription types for channel subscriptions, gifted subscriptions and VIPs.
	/// </summary>
	internal sealed class SubscriptionEventSubHandler : IEventSubEventHandler, ITwitchEventSubSubscriptionEvents
	{
		private readonly ILogger _logger;

		public SubscriptionEventSubHandler(ILogger logger)
		{
			_logger = logger;
		}

		public IReadOnlyCollection<EventSubSubscriptionSpec> Subscriptions { get; } = new[]
		{
			new EventSubSubscriptionSpec("channel.subscribe", "1", EventSubConditionKind.Broadcaster, new[] { "channel:read:subscriptions" }, true),
			new EventSubSubscriptionSpec("channel.subscription.end", "1", EventSubConditionKind.Broadcaster, new[] { "channel:read:subscriptions" }, true),
			new EventSubSubscriptionSpec("channel.subscription.gift", "1", EventSubConditionKind.Broadcaster, new[] { "channel:read:subscriptions" }, true),
			new EventSubSubscriptionSpec("channel.subscription.message", "1", EventSubConditionKind.Broadcaster, new[] { "channel:read:subscriptions" }, true),
			new EventSubSubscriptionSpec("channel.vip.add", "1", EventSubConditionKind.Broadcaster, new[] { "channel:read:vips|channel:manage:vips" }, true),
			new EventSubSubscriptionSpec("channel.vip.remove", "1", EventSubConditionKind.Broadcaster, new[] { "channel:read:vips|channel:manage:vips" }, true)
		};

		public event Action<string, SubscriptionEvent>? OnSubscribed;
		public event Action<string, SubscriptionEndEvent>? OnSubscriptionEnded;
		public event Action<string, SubscriptionGiftEvent>? OnSubscriptionGifted;
		public event Action<string, SubscriptionMessageEvent>? OnSubscriptionMessage;
		public event Action<string, VipEvent>? OnVipAdded;
		public event Action<string, VipEvent>? OnVipRemoved;

		public void Handle(string subscriptionType, string subscriptionVersion, string eventJson)
		{
			try
			{
				switch (subscriptionType)
				{
					case "channel.subscribe":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubSubscriptionSerializerContext.Default.SubscriptionEvent);
						OnSubscribed?.Invoke(data.BroadcasterUserId, data);
						break;
					}

					case "channel.subscription.end":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubSubscriptionSerializerContext.Default.SubscriptionEndEvent);
						OnSubscriptionEnded?.Invoke(data.BroadcasterUserId, data);
						break;
					}

					case "channel.subscription.gift":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubSubscriptionSerializerContext.Default.SubscriptionGiftEvent);
						OnSubscriptionGifted?.Invoke(data.BroadcasterUserId, data);
						break;
					}

					case "channel.subscription.message":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubSubscriptionSerializerContext.Default.SubscriptionMessageEvent);
						OnSubscriptionMessage?.Invoke(data.BroadcasterUserId, data);
						break;
					}

					case "channel.vip.add":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubSubscriptionSerializerContext.Default.VipEvent);
						OnVipAdded?.Invoke(data.BroadcasterUserId, data);
						break;
					}

					case "channel.vip.remove":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubSubscriptionSerializerContext.Default.VipEvent);
						OnVipRemoved?.Invoke(data.BroadcasterUserId, data);
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
