using System;
using System.Collections.Generic;
using System.Linq;

namespace CatCore.Services.Twitch.EventSub
{
	/// <summary>
	/// Describes the condition keys Twitch requires for a subscription type.
	/// </summary>
	internal enum EventSubConditionKind
	{
		/// <summary>
		/// Condition: { broadcaster_user_id }. Created once per watched channel.
		/// </summary>
		Broadcaster,

		/// <summary>
		/// Condition: { broadcaster_user_id, moderator_user_id }. Created once per watched channel.
		/// The moderator user id is always the logged in user.
		/// </summary>
		BroadcasterAndModerator,

		/// <summary>
		/// Condition: { broadcaster_user_id, user_id }. Created once per watched channel.
		/// The user id is always the logged in user.
		/// </summary>
		BroadcasterAndUser,

		/// <summary>
		/// Condition: { to_broadcaster_user_id }. Created once per watched channel.
		/// Twitch rejects the request when both from_ and to_ ids are supplied.
		/// </summary>
		ToBroadcaster,

		/// <summary>
		/// Condition: { user_id }. Created once per EventSub session, not per channel.
		/// </summary>
		User
	}

	/// <summary>
	/// Describes a single EventSub subscription type a handler wants to receive.
	/// </summary>
	internal sealed class EventSubSubscriptionSpec
	{
		public string Type { get; }

		public string Version { get; }

		public EventSubConditionKind ConditionKind { get; }

		/// <summary>
		/// Scope groups that have to be satisfied before the subscription is attempted.
		/// Each entry is one group; a group is satisfied when any of its pipe separated alternatives
		/// is present in the granted scopes. All groups must be satisfied.
		/// </summary>
		public IReadOnlyCollection<string> RequiredScopeGroups { get; }

		/// <summary>
		/// True when the subscription type requires the user in the condition to have authorized the
		/// application. Those subscriptions are free of the Twitch subscription cost limit.
		/// </summary>
		public bool RequiresUserAuthorization { get; }

		public EventSubSubscriptionSpec(string type, string version, EventSubConditionKind conditionKind, IReadOnlyCollection<string> requiredScopeGroups, bool requiresUserAuthorization)
		{
			Type = type;
			Version = version;
			ConditionKind = conditionKind;
			RequiredScopeGroups = requiredScopeGroups;
			RequiresUserAuthorization = requiresUserAuthorization;
		}

		/// <summary>
		/// Checks whether all required scope groups are satisfied by the granted scopes.
		/// </summary>
		public bool AreScopesSatisfied(IReadOnlyCollection<string>? grantedScopes)
		{
			if (grantedScopes == null)
			{
				return false;
			}

			foreach (var group in RequiredScopeGroups)
			{
				var satisfied = false;
				foreach (var alternative in group.Split('|'))
				{
					var scope = alternative.Trim();
					if (scope.Length == 0)
					{
						continue;
					}

					if (grantedScopes.Contains(scope, StringComparer.Ordinal))
					{
						satisfied = true;
						break;
					}
				}

				if (!satisfied)
				{
					return false;
				}
			}

			return true;
		}
	}
}
