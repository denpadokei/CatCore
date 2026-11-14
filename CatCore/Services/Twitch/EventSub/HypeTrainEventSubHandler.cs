using System;
using System.Collections.Generic;
using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.EventSub.HypeTrain;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch.EventSub
{
	/// <summary>
	/// Handles the hype train EventSub subscription types (<c>channel.hype_train.begin</c>,
	/// <c>channel.hype_train.progress</c> and <c>channel.hype_train.end</c>, version 2).
	/// </summary>
	internal sealed class HypeTrainEventSubHandler : IEventSubEventHandler, ITwitchEventSubHypeTrainEvents
	{
		private readonly ILogger _logger;

		public HypeTrainEventSubHandler(ILogger logger)
		{
			_logger = logger;
		}

		/// <inheritdoc />
		public IReadOnlyCollection<EventSubSubscriptionSpec> Subscriptions { get; } = new[]
		{
			new EventSubSubscriptionSpec("channel.hype_train.begin", "2", EventSubConditionKind.Broadcaster, new[] { "channel:read:hype_train" }, true),
			new EventSubSubscriptionSpec("channel.hype_train.progress", "2", EventSubConditionKind.Broadcaster, new[] { "channel:read:hype_train" }, true),
			new EventSubSubscriptionSpec("channel.hype_train.end", "2", EventSubConditionKind.Broadcaster, new[] { "channel:read:hype_train" }, true)
		};

		/// <inheritdoc />
		public event Action<string, HypeTrainEvent>? OnBegin;

		/// <inheritdoc />
		public event Action<string, HypeTrainEvent>? OnProgress;

		/// <inheritdoc />
		public event Action<string, HypeTrainEvent>? OnEnd;

		/// <inheritdoc />
		public void Handle(string subscriptionType, string subscriptionVersion, string eventJson)
		{
			try
			{
				switch (subscriptionType)
				{
					case "channel.hype_train.begin":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubHypeTrainSerializerContext.Default.HypeTrainEvent);
						OnBegin?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.hype_train.progress":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubHypeTrainSerializerContext.Default.HypeTrainEvent);
						OnProgress?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.hype_train.end":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubHypeTrainSerializerContext.Default.HypeTrainEvent);
						OnEnd?.Invoke(data.BroadcasterUserId, data);
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
