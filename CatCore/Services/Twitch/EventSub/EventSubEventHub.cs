using System;
using System.Collections.Generic;
using System.Linq;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch.EventSub
{
	/// <summary>
	/// Owns the EventSub event handlers, routes the incoming notifications to them and exposes
	/// the public per family event interfaces to the library consumers.
	/// </summary>
	internal sealed class EventSubEventHub : ITwitchEventSubService
	{
		private readonly Dictionary<string, IEventSubEventHandler> _handlersBySubscription;
		private readonly Dictionary<string, EventSubSubscriptionSpec> _specsBySubscription;

		private readonly HypeTrainEventSubHandler _hypeTrainHandler;
		private readonly GoalEventSubHandler _goalHandler;
		private readonly CharityEventSubHandler _charityHandler;
		private readonly UserEventSubHandler _userHandler;
		private readonly SubscriptionEventSubHandler _subscriptionHandler;
		private readonly ChannelActivityEventSubHandler _channelActivityHandler;
		private readonly ModerationEventSubHandler _moderationHandler;
		private readonly ChannelPointsEventSubHandler _channelPointsHandler;
		private readonly AutoModEventSubHandler _autoModHandler;
		private readonly SafetyEventSubHandler _safetyHandler;

		public EventSubEventHub(ILogger logger)
		{
			_hypeTrainHandler = new HypeTrainEventSubHandler(logger);
			_goalHandler = new GoalEventSubHandler(logger);
			_charityHandler = new CharityEventSubHandler(logger);
			_userHandler = new UserEventSubHandler(logger);
			_subscriptionHandler = new SubscriptionEventSubHandler(logger);
			_channelActivityHandler = new ChannelActivityEventSubHandler(logger);
			_moderationHandler = new ModerationEventSubHandler(logger);
			_channelPointsHandler = new ChannelPointsEventSubHandler(logger);
			_autoModHandler = new AutoModEventSubHandler(logger);
			_safetyHandler = new SafetyEventSubHandler(logger);

			var handlers = new IEventSubEventHandler[]
			{
				_hypeTrainHandler,
				_goalHandler,
				_charityHandler,
				_userHandler,
				_subscriptionHandler,
				_channelActivityHandler,
				_moderationHandler,
				_channelPointsHandler,
				_autoModHandler,
				_safetyHandler
			};

			_handlersBySubscription = new Dictionary<string, IEventSubEventHandler>(StringComparer.Ordinal);
			_specsBySubscription = new Dictionary<string, EventSubSubscriptionSpec>(StringComparer.Ordinal);
			foreach (var handler in handlers)
			{
				foreach (var spec in handler.Subscriptions)
				{
					var key = BuildKey(spec.Type, spec.Version);
					_handlersBySubscription[key] = handler;
					_specsBySubscription[key] = spec;
				}
			}
		}

		/// <summary>
		/// All subscription types the handlers want to receive.
		/// </summary>
		public IEnumerable<EventSubSubscriptionSpec> SubscriptionSpecs => _specsBySubscription.Values;

		/// <inheritdoc />
		public ITwitchEventSubHypeTrainEvents HypeTrain => _hypeTrainHandler;

		/// <inheritdoc />
		public ITwitchEventSubGoalEvents Goal => _goalHandler;

		/// <inheritdoc />
		public ITwitchEventSubCharityEvents Charity => _charityHandler;

		/// <inheritdoc />
		public ITwitchEventSubUserEvents User => _userHandler;

		/// <inheritdoc />
		public ITwitchEventSubSubscriptionEvents Subscriptions => _subscriptionHandler;

		/// <inheritdoc />
		public ITwitchEventSubChannelActivityEvents ChannelActivity => _channelActivityHandler;

		/// <inheritdoc />
		public ITwitchEventSubModerationEvents Moderation => _moderationHandler;

		/// <inheritdoc />
		public ITwitchEventSubChannelPointsEvents ChannelPoints => _channelPointsHandler;

		/// <inheritdoc />
		public ITwitchEventSubAutoModEvents AutoMod => _autoModHandler;

		/// <inheritdoc />
		public ITwitchEventSubSafetyEvents Safety => _safetyHandler;

		/// <summary>
		/// Checks whether a notification of the given subscription type is handled.
		/// </summary>
		public bool CanHandle(string subscriptionType, string subscriptionVersion)
		{
			return _handlersBySubscription.ContainsKey(BuildKey(subscriptionType, subscriptionVersion));
		}

		/// <summary>
		/// Routes a notification to the handler that owns the subscription type.
		/// </summary>
		/// <param name="subscriptionType">The EventSub subscription type, e.g. channel.hype_train.begin.</param>
		/// <param name="subscriptionVersion">The EventSub subscription version, e.g. 2.</param>
		/// <param name="eventJson">The raw JSON of the EventSub "event" object, not the whole message.</param>
		public void Handle(string subscriptionType, string subscriptionVersion, string eventJson)
		{
			if (!_handlersBySubscription.TryGetValue(BuildKey(subscriptionType, subscriptionVersion), out var handler))
			{
				throw new InvalidOperationException($"No EventSub handler is registered for {subscriptionType} (version {subscriptionVersion})");
			}

			handler.Handle(subscriptionType, subscriptionVersion, eventJson);
		}

		private static string BuildKey(string subscriptionType, string subscriptionVersion)
		{
			return subscriptionType + "/" + subscriptionVersion;
		}
	}
}
