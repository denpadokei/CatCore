using System.Collections.Generic;
using System.Linq;
using CatCore.Services.Twitch.EventSub;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Tests for the subscription planner that turns the subscription specs of the EventSub event
	/// handlers into concrete subscription requests.
	/// </summary>
	public class EventSubSubscriptionPlannerTests
	{
		private static EventSubSubscriptionPlanner CreatePlanner(params EventSubSubscriptionSpec[] specs)
		{
			return new EventSubSubscriptionPlanner(specs);
		}

		[Fact]
		public void BroadcasterScopedTypesUseTheChannelAsCondition()
		{
			var planner = CreatePlanner(new EventSubSubscriptionSpec("channel.hype_train.begin", "2", EventSubConditionKind.Broadcaster,
				new[] { "channel:read:hype_train" }, true));

			var plans = planner.CreateChannelSubscriptions("1234", "9999", new[] { "channel:read:hype_train" });

			plans.Should().HaveCount(1);
			plans[0].Type.Should().Be("channel.hype_train.begin");
			plans[0].Version.Should().Be("2");
			plans[0].Condition.Should().ContainKey("broadcaster_user_id").WhoseValue.Should().Be("1234");
			plans[0].Condition.Should().NotContainKey("moderator_user_id");
			plans[0].Condition.Should().NotContainKey("user_id");
		}

		[Fact]
		public void ModeratorScopedTypesUseTheLoggedInUserAsModerator()
		{
			var planner = CreatePlanner(new EventSubSubscriptionSpec("channel.moderate", "2", EventSubConditionKind.BroadcasterAndModerator,
				new[] { "moderator:read:moderators" }, true));

			var plans = planner.CreateChannelSubscriptions("1234", "9999", new[] { "moderator:read:moderators" });

			plans.Should().HaveCount(1);
			plans[0].Condition["broadcaster_user_id"].Should().Be("1234");
			plans[0].Condition["moderator_user_id"].Should().Be("9999");
		}

		[Fact]
		public void UserScopedTypesUseTheLoggedInUserAsUser()
		{
			var planner = CreatePlanner(new EventSubSubscriptionSpec("channel.chat.user_message_hold", "1", EventSubConditionKind.BroadcasterAndUser,
				new[] { "user:read:chat" }, true));

			var plans = planner.CreateChannelSubscriptions("1234", "9999", new[] { "user:read:chat" });

			plans.Should().HaveCount(1);
			plans[0].Condition["broadcaster_user_id"].Should().Be("1234");
			plans[0].Condition["user_id"].Should().Be("9999");
		}

		[Fact]
		public void RaidTypesOnlyUseTheReceivingBroadcaster()
		{
			var planner = CreatePlanner(new EventSubSubscriptionSpec("channel.raid", "1", EventSubConditionKind.ToBroadcaster,
				new string[0], false));

			var plans = planner.CreateChannelSubscriptions("1234", "9999", new string[0]);

			plans.Should().HaveCount(1);
			plans[0].Condition.Should().ContainKey("to_broadcaster_user_id").WhoseValue.Should().Be("1234");
			plans[0].Condition.Should().NotContainKey("from_broadcaster_user_id");
		}

		[Fact]
		public void UserScopedTypesAreNotCreatedPerChannel()
		{
			var planner = CreatePlanner(new EventSubSubscriptionSpec("user.update", "1", EventSubConditionKind.User, new string[0], false));

			planner.CreateChannelSubscriptions("1234", "9999", new string[0]).Should().BeEmpty();
		}

		[Fact]
		public void UserScopedTypesAreCreatedOncePerSession()
		{
			var planner = CreatePlanner(new EventSubSubscriptionSpec("user.update", "1", EventSubConditionKind.User, new string[0], false));

			var plans = planner.CreateSessionSubscriptions("9999", new string[0]);

			plans.Should().HaveCount(1);
			plans[0].Type.Should().Be("user.update");
			plans[0].Condition.Should().ContainKey("user_id").WhoseValue.Should().Be("9999");
		}

		[Fact]
		public void TypesWithMissingScopesAreNotPlanned()
		{
			var planner = CreatePlanner(new EventSubSubscriptionSpec("channel.hype_train.begin", "2", EventSubConditionKind.Broadcaster,
				new[] { "channel:read:hype_train" }, true));

			planner.CreateChannelSubscriptions("1234", "9999", new string[0]).Should().BeEmpty();
		}

		[Fact]
		public void EveryScopeGroupNeedsAnAlternativeScope()
		{
			var planner = CreatePlanner(new EventSubSubscriptionSpec("channel.moderate", "2", EventSubConditionKind.BroadcasterAndModerator,
				new[] { "moderator:read:moderators", "moderator:read:vips" }, true));

			planner.CreateChannelSubscriptions("1234", "9999", new[] { "moderator:read:moderators" }).Should().BeEmpty();
			planner.CreateChannelSubscriptions("1234", "9999", new[] { "moderator:read:moderators", "moderator:read:vips" }).Should().HaveCount(1);
		}

		[Fact]
		public void ScopeGroupsAcceptAnyOfTheirAlternativeScopes()
		{
			var planner = CreatePlanner(new EventSubSubscriptionSpec("channel.vip.add", "1", EventSubConditionKind.Broadcaster,
				new[] { "channel:read:vips|channel:manage:vips" }, true));

			planner.CreateChannelSubscriptions("1234", "9999", new[] { "channel:manage:vips" }).Should().HaveCount(1);
		}

		[Fact]
		public void PlannerKeepsEveryRequestedSubscriptionType()
		{
			var planner = CreatePlanner(
				new EventSubSubscriptionSpec("channel.hype_train.begin", "2", EventSubConditionKind.Broadcaster, new[] { "channel:read:hype_train" }, true),
				new EventSubSubscriptionSpec("channel.goal.begin", "1", EventSubConditionKind.Broadcaster, new[] { "channel:read:goals" }, true),
				new EventSubSubscriptionSpec("user.update", "1", EventSubConditionKind.User, new string[0], false));

			var channelPlans = planner.CreateChannelSubscriptions("1234", "9999", new[] { "channel:read:hype_train", "channel:read:goals" });
			var sessionPlans = planner.CreateSessionSubscriptions("9999", new[] { "channel:read:hype_train", "channel:read:goals" });

			channelPlans.Select(plan => plan.Type).Should().BeEquivalentTo(new[] { "channel.hype_train.begin", "channel.goal.begin" });
			sessionPlans.Select(plan => plan.Type).Should().BeEquivalentTo(new[] { "user.update" });
		}

		[Fact]
		public void EverySupportedSubscriptionTypeIsCoveredByASpec()
		{
			var hub = new EventSubEventHub(new Serilog.LoggerConfiguration().CreateLogger());

			var specs = hub.SubscriptionSpecs.ToList();

			specs.Should().NotBeEmpty();
			specs.Select(spec => spec.Type).Should().OnlyHaveUniqueItems();
			specs.Should().OnlyContain(spec => !string.IsNullOrEmpty(spec.Type) && !string.IsNullOrEmpty(spec.Version));
		}
	}
}
