using System;
using System.Collections.Generic;
using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.EventSub.User;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch.EventSub
{
	/// <summary>
	/// Handles the user scoped EventSub subscription types.
	/// These subscriptions are created once per EventSub session, so the raised events carry no channel id.
	/// </summary>
	internal sealed class UserEventSubHandler : IEventSubEventHandler, ITwitchEventSubUserEvents
	{
		private readonly ILogger _logger;

		public UserEventSubHandler(ILogger logger)
		{
			_logger = logger;
		}

		public IReadOnlyCollection<EventSubSubscriptionSpec> Subscriptions { get; } = new[]
		{
			new EventSubSubscriptionSpec("user.update", "1", EventSubConditionKind.User, Array.Empty<string>(), false),
			new EventSubSubscriptionSpec("user.whisper.message", "1", EventSubConditionKind.User, new[] { "user:read:whispers" }, true)
		};

		public event Action<UserUpdateEvent>? OnUserUpdated;
		public event Action<WhisperEvent>? OnWhisperReceived;

		public void Handle(string subscriptionType, string subscriptionVersion, string eventJson)
		{
			try
			{
				switch (subscriptionType)
				{
					case "user.update":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubUserSerializerContext.Default.UserUpdateEvent);
						OnUserUpdated?.Invoke(data);
						break;
					}
					case "user.whisper.message":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubUserSerializerContext.Default.WhisperEvent);
						OnWhisperReceived?.Invoke(data);
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
