using System.Text.Json.Serialization;
using CatCore.Models.Twitch.EventSub.ChannelPoints;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(AutomaticRewardRedemptionEvent))]
	[JsonSerializable(typeof(AutomaticReward))]
	[JsonSerializable(typeof(AutomaticRewardEmote))]
	[JsonSerializable(typeof(AutomaticRewardMessage))]
	[JsonSerializable(typeof(AutomaticRewardFragment))]
	[JsonSerializable(typeof(CustomRewardRedemptionEvent))]
	[JsonSerializable(typeof(CustomRedemptionReward))]
	[JsonSerializable(typeof(CustomRewardEvent))]
	[JsonSerializable(typeof(RewardLimit))]
	[JsonSerializable(typeof(RewardCooldown))]
	[JsonSerializable(typeof(RewardImage))]
	internal partial class TwitchEventSubChannelPointsSerializerContext : JsonSerializerContext
	{
	}
}
