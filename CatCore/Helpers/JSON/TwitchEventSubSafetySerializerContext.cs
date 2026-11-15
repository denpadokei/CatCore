using System.Text.Json.Serialization;
using CatCore.Models.Twitch.EventSub.Safety;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(SuspiciousUserMessageEvent))]
	[JsonSerializable(typeof(SuspiciousUserMessage))]
	[JsonSerializable(typeof(SuspiciousUserFragment))]
	[JsonSerializable(typeof(SuspiciousUserCheermote))]
	[JsonSerializable(typeof(SuspiciousUserEmote))]
	[JsonSerializable(typeof(SuspiciousUserUpdateEvent))]
	[JsonSerializable(typeof(UnbanRequestEvent))]
	[JsonSerializable(typeof(WarningEvent))]
	internal partial class TwitchEventSubSafetySerializerContext : JsonSerializerContext
	{
	}
}
