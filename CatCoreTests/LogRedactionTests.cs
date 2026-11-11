using CatCore.Helpers;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	public class LogRedactionTests
	{
		[Fact]
		public void UriHostRedactionDropsSessionMaterial()
		{
			const string reconnectUrl = "wss://eventsub.wss.twitch.tv/ws?session_id=AgoQexAWVYKSTIu4ec_2VAxyuhAB";

			LogRedaction.GetUriHost(reconnectUrl).Should().Be("eventsub.wss.twitch.tv");
		}

		[Fact]
		public void UriHostRedactionDropsPathAndQueryForBaseEndpoint()
		{
			LogRedaction.GetUriHost("https://catcore.example/authorize?code=secret&state=secret").Should().Be("catcore.example");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("not a uri")]
		public void UriHostRedactionReturnsPlaceholderForInvalidInput(string? value)
		{
			LogRedaction.GetUriHost(value).Should().Be("<invalid>");
		}
	}
}
