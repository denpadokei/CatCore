using System.Text.Json.Serialization;
using CatCore.Models.Twitch.EventSub.Moderation;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(BanEvent))]
	[JsonSerializable(typeof(UnbanEvent))]
	[JsonSerializable(typeof(ModerationActionEvent))]
	[JsonSerializable(typeof(ModerationFollowers))]
	[JsonSerializable(typeof(ModerationSlow))]
	[JsonSerializable(typeof(ModerationVip))]
	[JsonSerializable(typeof(ModerationUser))]
	[JsonSerializable(typeof(ModerationBan))]
	[JsonSerializable(typeof(ModerationWarn))]
	[JsonSerializable(typeof(ModerationTimeout))]
	[JsonSerializable(typeof(ModerationRaid))]
	[JsonSerializable(typeof(ModerationDelete))]
	[JsonSerializable(typeof(ModerationAutomodTerms))]
	[JsonSerializable(typeof(ModerationUnbanRequest))]
	[JsonSerializable(typeof(ModeratorEvent))]
	[JsonSerializable(typeof(ShieldModeEvent))]
	internal partial class TwitchEventSubModerationSerializerContext : JsonSerializerContext
	{
	}
}
