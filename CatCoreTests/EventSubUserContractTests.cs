using System.Text.Json;
using CatCore.Helpers.JSON;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Contract tests for the user scoped EventSub payload models.
	/// They use the payload shapes Twitch documents for the user.update and user.whisper.message types.
	/// </summary>
	public class EventSubUserContractTests
	{
		[Fact]
		public void UserUpdateEventBindsOfficialPayload()
		{
			const string json = @"{
				""user_id"": ""1337"", ""user_login"": ""cool_user"", ""user_name"": ""Cool_User"",
				""email"": ""user@email.com"", ""email_verified"": true, ""description"": ""cool description""
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubUserSerializerContext.Default.UserUpdateEvent);

			data.UserId.Should().Be("1337");
			data.UserLogin.Should().Be("cool_user");
			data.UserName.Should().Be("Cool_User");
			data.Email.Should().Be("user@email.com");
			data.EmailVerified.Should().BeTrue();
			data.Description.Should().Be("cool description");
		}

		[Fact]
		public void WhisperEventBindsOfficialPayload()
		{
			const string json = @"{
				""from_user_id"": ""423374343"", ""from_user_login"": ""glowillig"", ""from_user_name"": ""glowillig"",
				""to_user_id"": ""424596340"", ""to_user_login"": ""quotrok"", ""to_user_name"": ""quotrok"",
				""whisper_id"": ""some-whisper-id"", ""whisper"": { ""text"": ""a secret"" }
			}";

			var data = JsonSerializer.Deserialize(json, TwitchEventSubUserSerializerContext.Default.WhisperEvent);

			data.FromUserId.Should().Be("423374343");
			data.FromUserLogin.Should().Be("glowillig");
			data.FromUserName.Should().Be("glowillig");
			data.ToUserId.Should().Be("424596340");
			data.ToUserLogin.Should().Be("quotrok");
			data.ToUserName.Should().Be("quotrok");
			data.WhisperId.Should().Be("some-whisper-id");
			data.Whisper.Text.Should().Be("a secret");
		}
	}
}
