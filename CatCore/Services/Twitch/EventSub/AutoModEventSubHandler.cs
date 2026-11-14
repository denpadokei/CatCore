using System;
using System.Collections.Generic;
using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.EventSub.AutoMod;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch.EventSub
{
	/// <summary>
	/// Handles the AutoMod and chat user message hold/update EventSub subscription types.
	/// </summary>
	internal sealed class AutoModEventSubHandler : IEventSubEventHandler, ITwitchEventSubAutoModEvents
	{
		private readonly ILogger _logger;

		public AutoModEventSubHandler(ILogger logger)
		{
			_logger = logger;
		}

		public IReadOnlyCollection<EventSubSubscriptionSpec> Subscriptions { get; } = new[]
		{
			new EventSubSubscriptionSpec("automod.message.hold", "2", EventSubConditionKind.BroadcasterAndModerator, new[] { "moderator:manage:automod" }, true),
			new EventSubSubscriptionSpec("automod.message.update", "2", EventSubConditionKind.BroadcasterAndModerator, new[] { "moderator:manage:automod" }, true),
			new EventSubSubscriptionSpec("automod.settings.update", "1", EventSubConditionKind.BroadcasterAndModerator, new[] { "moderator:manage:automod_settings" }, true),
			new EventSubSubscriptionSpec("automod.terms.update", "1", EventSubConditionKind.BroadcasterAndModerator, new[] { "moderator:manage:automod" }, true),
			new EventSubSubscriptionSpec("channel.chat.user_message_hold", "1", EventSubConditionKind.BroadcasterAndUser, new[] { "user:read:chat" }, true),
			new EventSubSubscriptionSpec("channel.chat.user_message_update", "1", EventSubConditionKind.BroadcasterAndUser, new[] { "user:read:chat" }, true)
		};

		public event Action<string, AutoModMessageHoldEvent>? OnMessageHeld;
		public event Action<string, AutoModMessageUpdateEvent>? OnMessageUpdated;
		public event Action<string, AutoModSettingsEvent>? OnSettingsUpdated;
		public event Action<string, AutoModTermsEvent>? OnTermsUpdated;
		public event Action<string, ChatUserMessageHoldEvent>? OnUserMessageHeld;
		public event Action<string, ChatUserMessageUpdateEvent>? OnUserMessageUpdated;

		public void Handle(string subscriptionType, string subscriptionVersion, string eventJson)
		{
			try
			{
				switch (subscriptionType)
				{
					case "automod.message.hold":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubAutoModSerializerContext.Default.AutoModMessageHoldEvent);
						OnMessageHeld?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "automod.message.update":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubAutoModSerializerContext.Default.AutoModMessageUpdateEvent);
						OnMessageUpdated?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "automod.settings.update":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubAutoModSerializerContext.Default.AutoModSettingsEvent);
						OnSettingsUpdated?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "automod.terms.update":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubAutoModSerializerContext.Default.AutoModTermsEvent);
						OnTermsUpdated?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.chat.user_message_hold":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubAutoModSerializerContext.Default.ChatUserMessageHoldEvent);
						OnUserMessageHeld?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.chat.user_message_update":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubAutoModSerializerContext.Default.ChatUserMessageUpdateEvent);
						OnUserMessageUpdated?.Invoke(data.BroadcasterUserId, data);
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
