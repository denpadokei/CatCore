using System.Text.Json.Serialization;
using CatCore.Models.Twitch.EventSub.HypeTrain;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(HypeTrainEvent))]
	[JsonSerializable(typeof(HypeTrainContribution))]
	[JsonSerializable(typeof(HypeTrainParticipant))]
	internal partial class TwitchEventSubHypeTrainSerializerContext : JsonSerializerContext
	{
	}
}
