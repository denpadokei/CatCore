using System;
using System.Threading.Tasks;
using CatCore.Models.Credentials;
using CatCore.Models.Twitch.OAuth;

namespace CatCore.Services.Twitch.Interfaces
{
	internal interface ITwitchAuthService
	{
		string? AccessToken { get; }
		bool HasTokens { get; }
		bool TokenIsValid { get; }

		AuthenticationStatus Status { get; }
		event Action? OnCredentialsChanged;
		event Action<AuthenticationStatus>? OnAuthenticationStatusChanged;

		/// <summary>
		/// Information about an in-progress local device code authorization, or <c>null</c>
		/// when no such authorization is pending.
		/// </summary>
		TwitchDeviceAuthorizationInfo? PendingDeviceAuthorization { get; }

		void Initialize();
		ValidationResponse? FetchLoggedInUserInfo();
		Task<ValidationResponse?> FetchLoggedInUserInfoWithRefresh();

		string AuthorizationUrl(string redirectUrl);
		Task<string> GetLoginUrl(string redirectUrl);
		Task<string?> GetTokensByAuthorizationCode(string authorizationCode, string redirectUrl, string? state);
		Task<ValidationResponse?> ValidateAccessToken(TwitchCredentials credentials, bool resetDataOnFailure = true);
		Task<bool> RefreshTokens(bool force = false);
		Task<bool> RevokeTokens();
	}
}
