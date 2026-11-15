using System;
using System.Collections.Generic;
using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.EventSub.ChannelPoints;
using CatCore.Services.Twitch.Interfaces;
using CatCore.Services.Twitch.EventSub;
using Serilog;

namespace CatCore.Services.Twitch.EventSub
{
	internal sealed class ChannelPointsEventSubHandler : IEventSubEventHandler, ITwitchEventSubChannelPointsEvents
	{
		private const string RedemptionScopes = "channel:read:redemptions|channel:manage:redemptions";

		private readonly ILogger _logger;

		public ChannelPointsEventSubHandler(ILogger logger)
		{
			_logger = logger;
		}

		public IReadOnlyCollection<EventSubSubscriptionSpec> Subscriptions { get; } = new[]
		{
			new EventSubSubscriptionSpec("channel.channel_points_automatic_reward_redemption.add", "2", EventSubConditionKind.Broadcaster, new[] { RedemptionScopes }, true),
			new EventSubSubscriptionSpec("channel.channel_points_custom_reward_redemption.update", "1", EventSubConditionKind.Broadcaster, new[] { RedemptionScopes }, true),
			new EventSubSubscriptionSpec("channel.channel_points_custom_reward.add", "1", EventSubConditionKind.Broadcaster, new[] { RedemptionScopes }, true),
			new EventSubSubscriptionSpec("channel.channel_points_custom_reward.remove", "1", EventSubConditionKind.Broadcaster, new[] { RedemptionScopes }, true),
			new EventSubSubscriptionSpec("channel.channel_points_custom_reward.update", "1", EventSubConditionKind.Broadcaster, new[] { RedemptionScopes }, true)
		};

		public event Action<string, AutomaticRewardRedemptionEvent>? OnAutomaticRewardRedeemed;
		public event Action<string, CustomRewardRedemptionEvent>? OnRewardRedemptionUpdated;
		public event Action<string, CustomRewardEvent>? OnRewardCreated;
		public event Action<string, CustomRewardEvent>? OnRewardRemoved;
		public event Action<string, CustomRewardEvent>? OnRewardUpdated;

		public void Handle(string subscriptionType, string subscriptionVersion, string eventJson)
		{
			try
			{
				switch (subscriptionType)
				{
					case "channel.channel_points_automatic_reward_redemption.add":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubChannelPointsSerializerContext.Default.AutomaticRewardRedemptionEvent);
						OnAutomaticRewardRedeemed?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.channel_points_custom_reward_redemption.update":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubChannelPointsSerializerContext.Default.CustomRewardRedemptionEvent);
						OnRewardRedemptionUpdated?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.channel_points_custom_reward.add":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubChannelPointsSerializerContext.Default.CustomRewardEvent);
						OnRewardCreated?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.channel_points_custom_reward.remove":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubChannelPointsSerializerContext.Default.CustomRewardEvent);
						OnRewardRemoved?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.channel_points_custom_reward.update":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubChannelPointsSerializerContext.Default.CustomRewardEvent);
						OnRewardUpdated?.Invoke(data.BroadcasterUserId, data);
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
