using System.Text.Json.Serialization;
using CatCore.Models.Twitch.EventSub.Subscription;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(SubscriptionEvent))]
	[JsonSerializable(typeof(SubscriptionEndEvent))]
	[JsonSerializable(typeof(SubscriptionGiftEvent))]
	[JsonSerializable(typeof(SubscriptionMessageEvent))]
	[JsonSerializable(typeof(SubscriptionMessage))]
	[JsonSerializable(typeof(SubscriptionEmote))]
	[JsonSerializable(typeof(VipEvent))]
	internal partial class TwitchEventSubSubscriptionSerializerContext : JsonSerializerContext
	{
	}
}
