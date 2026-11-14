using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.EventSub.Charity
{
	/// <summary>
	/// The <c>event</c> object of a <c>channel.charity_campaign.donate</c> notification.
	/// </summary>
	[PublicAPI]
	public readonly struct CharityDonationEvent
	{
		/// <summary>Identifier of the donation.</summary>
		[JsonPropertyName("id")]
		public string Id { get; init; }

		/// <summary>Identifier of the charity campaign the donation belongs to.</summary>
		[JsonPropertyName("campaign_id")]
		public string CampaignId { get; init; }

		/// <summary>Identifier of the broadcaster that received the donation.</summary>
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		/// <summary>Login name of the broadcaster that received the donation.</summary>
		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		/// <summary>Display name of the broadcaster that received the donation.</summary>
		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		/// <summary>Identifier of the user that donated.</summary>
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		/// <summary>Login name of the user that donated.</summary>
		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		/// <summary>Display name of the user that donated.</summary>
		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		/// <summary>Name of the charity the donation was made to.</summary>
		[JsonPropertyName("charity_name")]
		public string CharityName { get; init; }

		/// <summary>Description of the charity the donation was made to.</summary>
		[JsonPropertyName("charity_description")]
		public string CharityDescription { get; init; }

		/// <summary>Logo URL of the charity the donation was made to.</summary>
		[JsonPropertyName("charity_logo")]
		public string CharityLogo { get; init; }

		/// <summary>Website URL of the charity the donation was made to.</summary>
		[JsonPropertyName("charity_website")]
		public string CharityWebsite { get; init; }

		/// <summary>Amount that was donated.</summary>
		[JsonPropertyName("amount")]
		public CharityAmount Amount { get; init; }
	}

	/// <summary>
	/// The <c>event</c> object of a <c>channel.charity_campaign.progress</c>, <c>.start</c> or <c>.stop</c> notification.
	/// </summary>
	[PublicAPI]
	public readonly struct CharityCampaignEvent
	{
		/// <summary>Identifier of the charity campaign.</summary>
		[JsonPropertyName("id")]
		public string Id { get; init; }

		/// <summary>Identifier of the broadcaster the campaign runs on.</summary>
		[JsonPropertyName("broadcaster_id")]
		public string BroadcasterId { get; init; }

		/// <summary>Login name of the broadcaster the campaign runs on.</summary>
		[JsonPropertyName("broadcaster_login")]
		public string BroadcasterLogin { get; init; }

		/// <summary>Display name of the broadcaster the campaign runs on.</summary>
		[JsonPropertyName("broadcaster_name")]
		public string BroadcasterName { get; init; }

		/// <summary>Name of the charity the campaign supports.</summary>
		[JsonPropertyName("charity_name")]
		public string CharityName { get; init; }

		/// <summary>Description of the charity the campaign supports.</summary>
		[JsonPropertyName("charity_description")]
		public string CharityDescription { get; init; }

		/// <summary>Logo URL of the charity the campaign supports.</summary>
		[JsonPropertyName("charity_logo")]
		public string CharityLogo { get; init; }

		/// <summary>Website URL of the charity the campaign supports.</summary>
		[JsonPropertyName("charity_website")]
		public string CharityWebsite { get; init; }

		/// <summary>Amount that has been raised so far.</summary>
		[JsonPropertyName("current_amount")]
		public CharityAmount CurrentAmount { get; init; }

		/// <summary>Amount the campaign tries to raise.</summary>
		[JsonPropertyName("target_amount")]
		public CharityAmount TargetAmount { get; init; }

		/// <summary>Timestamp of when the campaign started, only set on a <c>.start</c> notification.</summary>
		[JsonPropertyName("started_at")]
		public string? StartedAt { get; init; }

		/// <summary>Timestamp of when the campaign stopped, only set on a <c>.stop</c> notification.</summary>
		[JsonPropertyName("stopped_at")]
		public string? StoppedAt { get; init; }
	}

	/// <summary>
	/// A monetary amount of a charity event.
	/// </summary>
	[PublicAPI]
	public readonly struct CharityAmount
	{
		/// <summary>Amount value in the smallest unit implied by <see cref="DecimalPlaces"/>.</summary>
		[JsonPropertyName("value")]
		public int Value { get; init; }

		/// <summary>Number of decimal places to apply to <see cref="Value"/>.</summary>
		[JsonPropertyName("decimal_places")]
		public int DecimalPlaces { get; init; }

		/// <summary>ISO 4217 currency code of the amount.</summary>
		[JsonPropertyName("currency")]
		public string Currency { get; init; }
	}
}
