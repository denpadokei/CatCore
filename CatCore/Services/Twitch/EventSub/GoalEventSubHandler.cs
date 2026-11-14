using System;
using System.Collections.Generic;
using System.Text.Json;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.EventSub.Goal;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch.EventSub
{
	/// <summary>
	/// Handles the channel goal EventSub subscription types (<c>channel.goal.begin</c>,
	/// <c>channel.goal.progress</c> and <c>channel.goal.end</c>, version 1).
	/// Goal progress/end notifications may arrive before the begin notification; they are not reordered or suppressed.
	/// </summary>
	internal sealed class GoalEventSubHandler : IEventSubEventHandler, ITwitchEventSubGoalEvents
	{
		private readonly ILogger _logger;

		public GoalEventSubHandler(ILogger logger)
		{
			_logger = logger;
		}

		/// <inheritdoc />
		public IReadOnlyCollection<EventSubSubscriptionSpec> Subscriptions { get; } = new[]
		{
			new EventSubSubscriptionSpec("channel.goal.begin", "1", EventSubConditionKind.Broadcaster, new[] { "channel:read:goals" }, true),
			new EventSubSubscriptionSpec("channel.goal.progress", "1", EventSubConditionKind.Broadcaster, new[] { "channel:read:goals" }, true),
			new EventSubSubscriptionSpec("channel.goal.end", "1", EventSubConditionKind.Broadcaster, new[] { "channel:read:goals" }, true)
		};

		/// <inheritdoc />
		public event Action<string, GoalEvent>? OnBegin;

		/// <inheritdoc />
		public event Action<string, GoalEvent>? OnProgress;

		/// <inheritdoc />
		public event Action<string, GoalEvent>? OnEnd;

		/// <inheritdoc />
		public void Handle(string subscriptionType, string subscriptionVersion, string eventJson)
		{
			try
			{
				switch (subscriptionType)
				{
					case "channel.goal.begin":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubGoalSerializerContext.Default.GoalEvent);
						OnBegin?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.goal.progress":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubGoalSerializerContext.Default.GoalEvent);
						OnProgress?.Invoke(data.BroadcasterUserId, data);
						break;
					}
					case "channel.goal.end":
					{
						var data = JsonSerializer.Deserialize(eventJson, TwitchEventSubGoalSerializerContext.Default.GoalEvent);
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
