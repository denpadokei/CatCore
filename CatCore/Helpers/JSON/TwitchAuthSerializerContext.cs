using System.Text.Json.Serialization;
using CatCore.Models.Twitch.OAuth;
using CatCore.Shared.Models.Twitch.OAuth;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(ValidationResponse))]
	[JsonSerializable(typeof(AuthorizationResponse))]
	[JsonSerializable(typeof(AppTokenAuthorizationResponse))]
	internal partial class TwitchAuthSerializerContext : JsonSerializerContext
	{
	}
}