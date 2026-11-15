using System;
using System.Collections.Generic;
using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.EventSub.Moderation;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch.EventSub
{
	internal sealed class ModerationEventSubHandler : IEventSubEventHandler, ITwitchEventSubModerationEvents
	{
		private readonly ILogger _logger;

		public ModerationEventSubHandler(ILogger logger)
		{
			_logger = logger;
		}

		public IReadOnlyCollection<EventSubSubscriptionSpec> Subscriptions { get; } = new[]
		{
			new EventSubSubscriptionSpec("channel.ban", "1", EventSubConditionKind.Broadcaster, new[] { "channel:moderate" }, true),
			new EventSubSubscriptionSpec("channel.unban", "1", EventSubConditionKind.Broadcaster, new[] { "channel:moderate" }, true),
			new EventSubSubscriptionSpec(
				"channel.moderate",
				"2",
				EventSubConditionKind.BroadcasterAndModerator,
				new[]
				{
					"moderator:read:blocked_terms|moderator:manage:blocked_terms",
					"moderator:read:chat_settings|moderator:manage:chat_settings",
					"moderator:read:unban_requests|moderator:manage:unban_requests",
					"moderator:read:banned_users|moderator:manage:banned_users",
					"moderator:read:chat_messages|moderator:manage:chat_messages",
					"moderator:read:warnings|moderator:manage:warnings",
					"moderator:read:moderators",
					"moderator:read:vips"
				},
				true),
			new EventSubSubscriptionSpec("channel.moderator.add", "1", EventSubConditionKind.Broadcaster, new[] { "moderation:read" }, true),
			new EventSubSubscriptionSpec("channel.moderator.remove", "1", EventSubConditionKind.Broadcaster, new[] { "moderation:read" }, true),
			new EventSubSubscriptionSpec("channel.shield_mode.begin", "1", EventSubConditionKind.BroadcasterAndModerator, new[] { "moderator:read:shield_mode|moderator:manage:shield_mode" }, true),
			new EventSubSubscriptionSpec("channel.shield_mode.end", "1", EventSubConditionKind.BroadcasterAndModerator, new[] { "moderator:read:shield_mode|moderator:manage:shield_mode" }, true)
		};

		public event Action<string, BanEvent>? OnBanned;
		public event Action<string, UnbanEvent>? OnUnbanned;
		public event Action<string, ModerationActionEvent>? OnModerated;
		public event Action<string, ModeratorEvent>? OnModeratorAdded;
		public event Action<string, ModeratorEvent>? OnModeratorRemoved;
		public event Action<string, ShieldModeEvent>? OnShieldModeStarted;
		public event Action<string, ShieldModeEvent>? OnShieldModeEnded;

		public void Handle(string subscriptionType, string subscriptionVersion, string eventJson)
		{
			try
			{
				switch (subscriptionType)
				{
					case "channel.ban":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubModerationSerializerContext.Default.BanEvent);
						OnBanned?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.unban":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubModerationSerializerContext.Default.UnbanEvent);
						OnUnbanned?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.moderate":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubModerationSerializerContext.Default.ModerationActionEvent);
						OnModerated?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.moderator.add":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubModerationSerializerContext.Default.ModeratorEvent);
						OnModeratorAdded?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.moderator.remove":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubModerationSerializerContext.Default.ModeratorEvent);
						OnModeratorRemoved?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.shield_mode.begin":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubModerationSerializerContext.Default.ShieldModeEvent);
						OnShieldModeStarted?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.shield_mode.end":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubModerationSerializerContext.Default.ShieldModeEvent);
						OnShieldModeEnded?.Invoke(data.BroadcasterUserId, data);
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
