using System.Net;
using System.Threading.Tasks;
using System.Web;
using CatCore.Azure.Http;
using CatCore.Azure.Services.Twitch;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CatCore.Azure.Functions.Twitch
{
	public static class AuthorizationCodeToTokensFunction
	{
		[Function("AuthorizationCodeToTokensFunction")]
		public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "twitch/authorize")]
			HttpRequestData req,
			FunctionContext executionContext)
		{
			var logger = executionContext.GetLogger(nameof(AuthorizationCodeToTokensFunction));

			var queryParametersCollection = HttpUtility.ParseQueryString(req.Url.Query);
			var formParametersCollection = await req.ReadFormCollectionAsync().ConfigureAwait(false);
			var authorizationCode = formParametersCollection["code"] ?? queryParametersCollection["code"];
			var redirectUrl = formParametersCollection["redirect_uri"] ?? queryParametersCollection["redirect_uri"];
			if (string.IsNullOrWhiteSpace(authorizationCode) || string.IsNullOrWhiteSpace(redirectUrl))
			{
				logger.LogInformation("API Call is missing one of the required parameters");
				return req.CreateResponse(HttpStatusCode.BadRequest);
			}

			var twitchAuthService = executionContext.InstanceServices.GetService<TwitchAuthService>()!;
			var tokenResponse = await twitchAuthService.GetTokensByAuthorizationCode(authorizationCode, redirectUrl).ConfigureAwait(false);

			HttpResponseData response;
			await using var authorizationResponseStream = tokenResponse.Body;
			if (authorizationResponseStream != null)
			{
				response = req.CreateResponse(HttpStatusCode.OK);
				await authorizationResponseStream.CopyToAsync(response.Body).ConfigureAwait(false);
			}
			else
			{
				logger.LogWarning("Twitch rejected the authorization code exchange: status {TwitchStatus}, error {Error}", tokenResponse.TwitchStatus, tokenResponse.ErrorMessage);
				response = req.CreateResponse(tokenResponse.ToHttpStatusCode());
			}

			return response;
		}
	}
}