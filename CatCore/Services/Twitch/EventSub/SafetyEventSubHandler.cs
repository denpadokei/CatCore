using System;
using System.Collections.Generic;
using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.EventSub.Safety;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch.EventSub
{
	internal sealed class SafetyEventSubHandler : IEventSubEventHandler, ITwitchEventSubSafetyEvents
	{
		private readonly ILogger _logger;

		public SafetyEventSubHandler(ILogger logger)
		{
			_logger = logger;
		}

		public IReadOnlyCollection<EventSubSubscriptionSpec> Subscriptions { get; } = new[]
		{
			new EventSubSubscriptionSpec("channel.suspicious_user.message", "1", EventSubConditionKind.BroadcasterAndModerator, new[] { "moderator:read:suspicious_users" }, true),
			new EventSubSubscriptionSpec("channel.suspicious_user.update", "1", EventSubConditionKind.BroadcasterAndModerator, new[] { "moderator:read:suspicious_users" }, true),
			new EventSubSubscriptionSpec("channel.unban_request.create", "1", EventSubConditionKind.BroadcasterAndModerator, new[] { "moderator:read:unban_requests|moderator:manage:unban_requests" }, true),
			new EventSubSubscriptionSpec("channel.unban_request.resolve", "1", EventSubConditionKind.BroadcasterAndModerator, new[] { "moderator:read:unban_requests|moderator:manage:unban_requests" }, true),
			new EventSubSubscriptionSpec("channel.warning.acknowledge", "1", EventSubConditionKind.BroadcasterAndModerator, new[] { "moderator:read:warnings|moderator:manage:warnings" }, true),
			new EventSubSubscriptionSpec("channel.warning.send", "1", EventSubConditionKind.BroadcasterAndModerator, new[] { "moderator:read:warnings|moderator:manage:warnings" }, true)
		};

		public event Action<string, SuspiciousUserMessageEvent>? OnSuspiciousUserMessage;
		public event Action<string, SuspiciousUserUpdateEvent>? OnSuspiciousUserUpdated;
		public event Action<string, UnbanRequestEvent>? OnUnbanRequestCreated;
		public event Action<string, UnbanRequestEvent>? OnUnbanRequestResolved;
		public event Action<string, WarningEvent>? OnWarningSent;
		public event Action<string, WarningEvent>? OnWarningAcknowledged;

		public void Handle(string subscriptionType, string subscriptionVersion, string eventJson)
		{
			try
			{
				switch (subscriptionType)
				{
					case "channel.suspicious_user.message":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubSafetySerializerContext.Default.SuspiciousUserMessageEvent);
						OnSuspiciousUserMessage?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.suspicious_user.update":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubSafetySerializerContext.Default.SuspiciousUserUpdateEvent);
						OnSuspiciousUserUpdated?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.unban_request.create":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubSafetySerializerContext.Default.UnbanRequestEvent);
						OnUnbanRequestCreated?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.unban_request.resolve":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubSafetySerializerContext.Default.UnbanRequestEvent);
						OnUnbanRequestResolved?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.warning.send":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubSafetySerializerContext.Default.WarningEvent);
						OnWarningSent?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.warning.acknowledge":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubSafetySerializerContext.Default.WarningEvent);
						OnWarningAcknowledged?.Invoke(data.BroadcasterUserId, data);
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
