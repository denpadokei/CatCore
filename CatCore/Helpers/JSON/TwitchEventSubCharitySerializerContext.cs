using System.Text.Json.Serialization;
using CatCore.Models.Twitch.EventSub.Charity;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(CharityDonationEvent))]
	[JsonSerializable(typeof(CharityCampaignEvent))]
	[JsonSerializable(typeof(CharityAmount))]
	internal partial class TwitchEventSubCharitySerializerContext : JsonSerializerContext
	{
	}
}
