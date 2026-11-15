using System.Text.Json.Serialization;
using CatCore.Models.Twitch.EventSub.Channel;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(BitsUseEvent))]
	[JsonSerializable(typeof(BitsUseMessage))]
	[JsonSerializable(typeof(BitsUseFragment))]
	[JsonSerializable(typeof(BitsCheermote))]
	[JsonSerializable(typeof(BitsEmote))]
	[JsonSerializable(typeof(BitsPowerUp))]
	[JsonSerializable(typeof(BitsCustomPowerUp))]
	[JsonSerializable(typeof(CheerEvent))]
	[JsonSerializable(typeof(CustomPowerUpRedemptionEvent))]
	[JsonSerializable(typeof(CustomPowerUpInfo))]
	[JsonSerializable(typeof(RaidEvent))]
	[JsonSerializable(typeof(ChannelUpdateEvent))]
	[JsonSerializable(typeof(ShoutoutCreatedEvent))]
	[JsonSerializable(typeof(ShoutoutReceivedEvent))]
	internal partial class TwitchEventSubChannelActivitySerializerContext : JsonSerializerContext
	{
	}
}
