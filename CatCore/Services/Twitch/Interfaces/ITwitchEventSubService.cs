using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	/// <summary>
	/// Provides access to the events of the EventSub subscription types that CatCore supports.
	/// The individual event families are exposed as separate interfaces so that a consumer only
	/// has to reference the families it actually uses.
	/// </summary>
	[PublicAPI]
	public interface ITwitchEventSubService
	{
		/// <summary>
		/// Events of the hype train subscription types.
		/// </summary>
		[PublicAPI]
		ITwitchEventSubHypeTrainEvents HypeTrain { get; }

		/// <summary>
		/// Events of the creator goal subscription types.
		/// </summary>
		[PublicAPI]
		ITwitchEventSubGoalEvents Goal { get; }

		/// <summary>
		/// Events of the charity campaign subscription types.
		/// </summary>
		[PublicAPI]
		ITwitchEventSubCharityEvents Charity { get; }

		/// <summary>
		/// Events of the user scoped subscription types.
		/// </summary>
		[PublicAPI]
		ITwitchEventSubUserEvents User { get; }

		/// <summary>
		/// Events of the channel subscription and VIP subscription types.
		/// </summary>
		[PublicAPI]
		ITwitchEventSubSubscriptionEvents Subscriptions { get; }

		/// <summary>
		/// Events of the channel activity subscription types (bits, cheer, raids, channel updates and shoutouts).
		/// </summary>
		[PublicAPI]
		ITwitchEventSubChannelActivityEvents ChannelActivity { get; }

		/// <summary>
		/// Events of the channel moderation subscription types.
		/// </summary>
		[PublicAPI]
		ITwitchEventSubModerationEvents Moderation { get; }

		/// <summary>
		/// Events of the channel points subscription types.
		/// </summary>
		[PublicAPI]
		ITwitchEventSubChannelPointsEvents ChannelPoints { get; }

		/// <summary>
		/// Events of the AutoMod subscription types.
		/// </summary>
		[PublicAPI]
		ITwitchEventSubAutoModEvents AutoMod { get; }

		/// <summary>
		/// Events of the suspicious user, unban request and warning subscription types.
		/// </summary>
		[PublicAPI]
		ITwitchEventSubSafetyEvents Safety { get; }
	}
}
