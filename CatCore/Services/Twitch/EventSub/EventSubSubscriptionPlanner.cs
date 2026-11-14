using System;
using System.Collections.Generic;
using System.Linq;

namespace CatCore.Services.Twitch.EventSub
{
	/// <summary>
	/// A single EventSub subscription request with its resolved condition.
	/// </summary>
	internal readonly struct EventSubSubscriptionPlan
	{
		public string Type { get; }

		public string Version { get; }

		public Dictionary<string, string> Condition { get; }

		/// <summary>
		/// True when the subscription type requires the user of the condition to have authorized
		/// the application. Those subscriptions are free of the Twitch subscription cost limit.
		/// </summary>
		public bool RequiresUserAuthorization { get; }

		public EventSubSubscriptionPlan(string type, string version, Dictionary<string, string> condition, bool requiresUserAuthorization)
		{
			Type = type;
			Version = version;
			Condition = condition;
			RequiresUserAuthorization = requiresUserAuthorization;
		}
	}

	/// <summary>
	/// Turns the subscription specs of all registered EventSub event handlers into concrete
	/// subscription requests, taking the condition kind and the granted scopes into account.
	/// </summary>
	internal sealed class EventSubSubscriptionPlanner
	{
		private readonly EventSubSubscriptionSpec[] _specs;

		public EventSubSubscriptionPlanner(IEnumerable<EventSubSubscriptionSpec> specs)
		{
			_specs = specs?.ToArray() ?? Array.Empty<EventSubSubscriptionSpec>();
		}

		/// <summary>
		/// Builds the subscriptions that have to be created for a single watched channel.
		/// </summary>
		public IReadOnlyList<EventSubSubscriptionPlan> CreateChannelSubscriptions(string channelId, string loggedInUserId, IReadOnlyCollection<string>? grantedScopes)
		{
			var plans = new List<EventSubSubscriptionPlan>();

			foreach (var spec in _specs)
			{
				if (spec.ConditionKind == EventSubConditionKind.User)
				{
					// User scoped subscriptions are created once per EventSub session, not per channel.
					continue;
				}

				if (!spec.AreScopesSatisfied(grantedScopes))
				{
					continue;
				}

				var condition = CreateCondition(spec.ConditionKind, channelId, loggedInUserId);
				plans.Add(new EventSubSubscriptionPlan(spec.Type, spec.Version, condition, spec.RequiresUserAuthorization));
			}

			return plans;
		}

		/// <summary>
		/// Builds the subscriptions that are created once per EventSub session.
		/// </summary>
		public IReadOnlyList<EventSubSubscriptionPlan> CreateSessionSubscriptions(string loggedInUserId, IReadOnlyCollection<string>? grantedScopes)
		{
			var plans = new List<EventSubSubscriptionPlan>();

			foreach (var spec in _specs)
			{
				if (spec.ConditionKind != EventSubConditionKind.User)
				{
					continue;
				}

				if (!spec.AreScopesSatisfied(grantedScopes))
				{
					continue;
				}

				var condition = new Dictionary<string, string> { { "user_id", loggedInUserId } };
				plans.Add(new EventSubSubscriptionPlan(spec.Type, spec.Version, condition, spec.RequiresUserAuthorization));
			}

			return plans;
		}

		private static Dictionary<string, string> CreateCondition(EventSubConditionKind conditionKind, string channelId, string loggedInUserId)
		{
			switch (conditionKind)
			{
				case EventSubConditionKind.Broadcaster:
					return new Dictionary<string, string> { { "broadcaster_user_id", channelId } };

				case EventSubConditionKind.BroadcasterAndModerator:
					return new Dictionary<string, string>
					{
						{ "broadcaster_user_id", channelId },
						{ "moderator_user_id", loggedInUserId }
					};

				case EventSubConditionKind.BroadcasterAndUser:
					return new Dictionary<string, string>
					{
						{ "broadcaster_user_id", channelId },
						{ "user_id", loggedInUserId }
					};

				case EventSubConditionKind.ToBroadcaster:
					// Twitch rejects the request when both the from_ and the to_ id are supplied.
					return new Dictionary<string, string> { { "to_broadcaster_user_id", channelId } };

				default:
					throw new ArgumentOutOfRangeException(nameof(conditionKind), conditionKind, "Unsupported EventSub condition kind");
			}
		}
	}
}
