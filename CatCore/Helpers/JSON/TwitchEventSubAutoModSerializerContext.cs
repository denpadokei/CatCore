using System.Text.Json.Serialization;
using CatCore.Models.Twitch.EventSub.AutoMod;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(AutoModMessageHoldEvent))]
	[JsonSerializable(typeof(AutoModMessageUpdateEvent))]
	[JsonSerializable(typeof(AutoModMessage))]
	[JsonSerializable(typeof(AutoModFragment))]
	[JsonSerializable(typeof(AutoModEmote))]
	[JsonSerializable(typeof(AutoModCheermote))]
	[JsonSerializable(typeof(AutoModDetails))]
	[JsonSerializable(typeof(AutoModBoundary))]
	[JsonSerializable(typeof(AutoModBlockedTerm))]
	[JsonSerializable(typeof(AutoModTermFound))]
	[JsonSerializable(typeof(AutoModSettingsEvent))]
	[JsonSerializable(typeof(AutoModTermsEvent))]
	[JsonSerializable(typeof(ChatUserMessageHoldEvent))]
	[JsonSerializable(typeof(ChatUserMessageUpdateEvent))]
	internal partial class TwitchEventSubAutoModSerializerContext : JsonSerializerContext
	{
	}
}
