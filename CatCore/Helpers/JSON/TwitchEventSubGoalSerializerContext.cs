using System.Text.Json.Serialization;
using CatCore.Models.Twitch.EventSub.Goal;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(GoalEvent))]
	internal partial class TwitchEventSubGoalSerializerContext : JsonSerializerContext
	{
	}
}
