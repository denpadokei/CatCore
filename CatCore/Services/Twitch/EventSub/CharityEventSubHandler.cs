using System;
using System.Collections.Generic;
using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.EventSub.Charity;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch.EventSub
{
	/// <summary>
	/// Handles the charity campaign EventSub subscription types.
	/// </summary>
	internal sealed class CharityEventSubHandler : IEventSubEventHandler, ITwitchEventSubCharityEvents
	{
		private readonly ILogger _logger;

		public CharityEventSubHandler(ILogger logger)
		{
			_logger = logger;
		}

		public IReadOnlyCollection<EventSubSubscriptionSpec> Subscriptions { get; } = new[]
		{
			new EventSubSubscriptionSpec("channel.charity_campaign.donate", "1", EventSubConditionKind.Broadcaster, new[] { "channel:read:charity" }, true),
			new EventSubSubscriptionSpec("channel.charity_campaign.progress", "1", EventSubConditionKind.Broadcaster, new[] { "channel:read:charity" }, true),
			new EventSubSubscriptionSpec("channel.charity_campaign.start", "1", EventSubConditionKind.Broadcaster, new[] { "channel:read:charity" }, true),
			new EventSubSubscriptionSpec("channel.charity_campaign.stop", "1", EventSubConditionKind.Broadcaster, new[] { "channel:read:charity" }, true)
		};

		public event Action<string, CharityDonationEvent>? OnDonation;
		public event Action<string, CharityCampaignEvent>? OnProgress;
		public event Action<string, CharityCampaignEvent>? OnStarted;
		public event Action<string, CharityCampaignEvent>? OnStopped;

		public void Handle(string subscriptionType, string subscriptionVersion, string eventJson)
		{
			try
			{
				switch (subscriptionType)
				{
					case "channel.charity_campaign.donate":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubCharitySerializerContext.Default.CharityDonationEvent);
						OnDonation?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.charity_campaign.progress":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubCharitySerializerContext.Default.CharityCampaignEvent);
						OnProgress?.Invoke(data.BroadcasterId, data);
						break;
					}
					case "channel.charity_campaign.start":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubCharitySerializerContext.Default.CharityCampaignEvent);
						OnStarted?.Invoke(data.BroadcasterId, data);
						break;
					}
					case "channel.charity_campaign.stop":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubCharitySerializerContext.Default.CharityCampaignEvent);
						OnStopped?.Invoke(data.BroadcasterId, data);
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
