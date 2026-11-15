using System.Text.Json.Serialization;
using CatCore.Models.Twitch.EventSub.User;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(UserUpdateEvent))]
	[JsonSerializable(typeof(WhisperEvent))]
	[JsonSerializable(typeof(WhisperMessage))]
	internal partial class TwitchEventSubUserSerializerContext : JsonSerializerContext
	{
	}
}
