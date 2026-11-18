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
	public class RefreshTokensFunction
	{
		[Function("RefreshTokensFunction")]
		public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "twitch/refresh")]
			HttpRequestData req,
			FunctionContext executionContext)
		{
			var logger = executionContext.GetLogger(nameof(RefreshTokensFunction));

			var queryParametersCollection = HttpUtility.ParseQueryString(req.Url.Query);
			var formParametersCollection = await req.ReadFormCollectionAsync().ConfigureAwait(false);
			var refreshToken = formParametersCollection["refresh_token"] ?? queryParametersCollection["refresh_token"];
			if (string.IsNullOrWhiteSpace(refreshToken))
			{
				logger.LogWarning("API Call is missing one of the required parameters");
				return req.CreateResponse(HttpStatusCode.BadRequest);
			}

			var twitchAuthService = executionContext.InstanceServices.GetService<TwitchAuthService>()!;
			var tokenResponse = await twitchAuthService.RefreshTokens(refreshToken).ConfigureAwait(false);

			HttpResponseData response;
			await using var authorizationResponseStream = tokenResponse.Body;
			if (authorizationResponseStream != null)
			{
				response = req.CreateResponse(HttpStatusCode.OK);
				await authorizationResponseStream.CopyToAsync(response.Body).ConfigureAwait(false);
			}
			else
			{
				logger.LogWarning("Twitch rejected the token refresh: status {TwitchStatus}, error {Error}", tokenResponse.TwitchStatus, tokenResponse.ErrorMessage);
				response = req.CreateResponse(tokenResponse.ToHttpStatusCode());
			}

			return response;
		}
	}
}