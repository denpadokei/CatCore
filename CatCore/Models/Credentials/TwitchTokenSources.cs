namespace CatCore.Models.Credentials
{
	/// <summary>
	/// Describes how a token pair was obtained and therefore how it can be refreshed.
	/// </summary>
	internal static class TwitchTokenSources
	{
		/// <summary>
		/// Token pair obtained through the CatCore auth back-end (authorization code grant).
		/// Refreshing it requires the back-end because the client secret is only known there.
		/// </summary>
		public const string AuthBackend = "catcore-auth-backend";

		/// <summary>
		/// Token pair obtained through Twitch's device code grant flow with the public
		/// client id only. It can be refreshed locally, without a client secret.
		/// </summary>
		public const string LocalDeviceCode = "local-device-code";
	}
}
