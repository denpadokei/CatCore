namespace CatCore.Models.Api.Responses
{
	/// <summary>
	/// Response of POST /api/twitch/login: the URL the browser has to visit to authorize CatCore.
	/// The login request is answered with a JSON body instead of a redirect because the portal
	/// sends the CSRF token in a custom header through fetch, and a cross-origin redirect to
	/// Twitch could not be followed from JavaScript.
	/// </summary>
	internal readonly struct TwitchLoginResponseDto
	{
		public string LoginUrl { get; }

		public TwitchLoginResponseDto(string loginUrl)
		{
			LoginUrl = loginUrl;
		}
	}
}
