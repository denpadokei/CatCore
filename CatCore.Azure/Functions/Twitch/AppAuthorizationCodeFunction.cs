using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using CatCore.Azure.Services.Twitch;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CatCore.Azure.Functions.Twitch
{
    public class AppAuthorizationCodeFunction
    {
        private readonly ILogger _logger;

        public AppAuthorizationCodeFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AppAuthorizationCodeFunction>();
        }

        [Function("AppAuthorizationCodeFunction")]
        public async Task<HttpResponseData?> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "twitch/appauthorize")]
			HttpRequestData req,
			FunctionContext executionContext)
        {
			var logger = executionContext.GetLogger(nameof(AuthorizationCodeToTokensFunction));
			var twitchAuthService = executionContext.InstanceServices.GetService<TwitchAuthService>()!;
			await using var authorizationResponseStream = await twitchAuthService.GetAppTokens().ConfigureAwait(false);

			HttpResponseData response;
			if (authorizationResponseStream != null)
			{
				response = req.CreateResponse(HttpStatusCode.OK);
				await authorizationResponseStream.CopyToAsync(response.Body).ConfigureAwait(false);
			}
			else
			{
				logger.LogInformation("Couldn't trade authorization code for credentials from Twitch Auth server");
				response = req.CreateResponse(HttpStatusCode.Unauthorized);
			}

			return response;
		}
    }
}
