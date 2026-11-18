using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Services;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Tests for the guard that protects the state-changing portal endpoints (Twitch login/logout)
	/// against cross-site request forgery. It follows the same defense strategy as the OAuth state
	/// validation: a CSPRNG token, constant-time comparison and rejecting invalid values outright.
	/// </summary>
	public class PortalRequestGuardTests
	{
		private const string PortalAuthority = "http://localhost:8338";
		private const string Token = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

		[Fact]
		public void CreateCsrfTokenReturnsARandomHexStringPerCall()
		{
			var token1 = PortalRequestGuard.CreateCsrfToken();
			var token2 = PortalRequestGuard.CreateCsrfToken();

			token1.Should().MatchRegex("^[0-9a-f]{64}$");
			token2.Should().MatchRegex("^[0-9a-f]{64}$");
			token1.Should().NotBe(token2);
		}

		[Fact]
		public void ValidateCsrfTokenAcceptsOnlyTheExactToken()
		{
			PortalRequestGuard.ValidateCsrfToken(Token, Token).Should().BeTrue();
		}

		[Fact]
		public void ValidateCsrfTokenRejectsMissingOrMalformedTokens()
		{
			PortalRequestGuard.ValidateCsrfToken(Token, null).Should().BeFalse();
			PortalRequestGuard.ValidateCsrfToken(Token, "").Should().BeFalse();
			PortalRequestGuard.ValidateCsrfToken(Token, " ").Should().BeFalse();
			PortalRequestGuard.ValidateCsrfToken(null, Token).Should().BeFalse();
			PortalRequestGuard.ValidateCsrfToken("", Token).Should().BeFalse();
			PortalRequestGuard.ValidateCsrfToken(null, null).Should().BeFalse();
			// Comparison is ordinal: a case-varied token is not the same token.
			PortalRequestGuard.ValidateCsrfToken(Token, Token.ToUpperInvariant()).Should().BeFalse();
			PortalRequestGuard.ValidateCsrfToken(Token, Token + "0").Should().BeFalse();
			PortalRequestGuard.ValidateCsrfToken(Token, Token.Substring(1)).Should().BeFalse();
		}

		[Fact]
		public void ValidateCsrfTokenRejectsASingleCharacterDifferenceAtAnyPosition()
		{
			// The comparison walks the whole token without early-exiting on the first difference,
			// so a mismatch is caught wherever it sits. (The comparison time itself cannot be
			// asserted in a unit test; FixedTimeEquals is constant-time by construction.)
			for (var i = 0; i < Token.Length; i++)
			{
				var mutated = MutateChar(Token, i);
				PortalRequestGuard.ValidateCsrfToken(Token, mutated)
					.Should().BeFalse("the mutated token must be rejected at position {0}", i);
			}
		}

		[Fact]
		public void IsPostMethodAcceptsOnlyPost()
		{
			PortalRequestGuard.IsPostMethod("POST").Should().BeTrue();
			PortalRequestGuard.IsPostMethod("post").Should().BeTrue();
			PortalRequestGuard.IsPostMethod("GET").Should().BeFalse();
			PortalRequestGuard.IsPostMethod("HEAD").Should().BeFalse();
			PortalRequestGuard.IsPostMethod("OPTIONS").Should().BeFalse();
			PortalRequestGuard.IsPostMethod(null).Should().BeFalse();
			PortalRequestGuard.IsPostMethod("").Should().BeFalse();
		}

		[Fact]
		public void LoginIsRejectedAsMethodNotAllowedForGet()
		{
			// Even a fully valid token cannot turn a GET into an accepted state-changing request.
			PortalRequestGuard.EvaluateStateChangingRequest("login", "GET", Token, Token, PortalAuthority, PortalAuthority + "/", PortalAuthority, PortalAuthority)
				.Should().Be(405);
			PortalRequestGuard.EvaluateStateChangingRequest("login", "GET", Token, null, null, null, PortalAuthority, PortalAuthority)
				.Should().Be(405);
		}

		[Fact]
		public void LogoutIsRejectedAsMethodNotAllowedForGet()
		{
			PortalRequestGuard.EvaluateStateChangingRequest("logout", "GET", Token, Token, null, null, PortalAuthority, PortalAuthority)
				.Should().Be(405);
		}

		[Fact]
		public void NonStateChangingEndpointsAreNotAffectedByTheGuard()
		{
			PortalRequestGuard.EvaluateStateChangingRequest("state", "GET", Token, null, null, null, PortalAuthority, PortalAuthority).Should().BeNull();
			PortalRequestGuard.EvaluateStateChangingRequest("state", "POST", Token, null, null, null, PortalAuthority, PortalAuthority).Should().BeNull();
			PortalRequestGuard.EvaluateStateChangingRequest("channels", "GET", Token, null, null, null, PortalAuthority, PortalAuthority).Should().BeNull();
			PortalRequestGuard.EvaluateStateChangingRequest(null, "GET", Token, null, null, null, PortalAuthority, PortalAuthority).Should().BeNull();
		}

		[Fact]
		public void AValidPostFromTheLocalPortalIsAccepted()
		{
			// Browsers always send an Origin header for POST fetches; non-browser clients may omit
			// both headers, in which case the CSRF token alone decides.
			PortalRequestGuard.EvaluateStateChangingRequest("login", "POST", Token, Token, PortalAuthority, null, PortalAuthority, PortalAuthority).Should().BeNull();
			PortalRequestGuard.EvaluateStateChangingRequest("logout", "POST", Token, Token, PortalAuthority, null, PortalAuthority, PortalAuthority).Should().BeNull();
			PortalRequestGuard.EvaluateStateChangingRequest("login", "POST", Token, Token, null, null, PortalAuthority, PortalAuthority).Should().BeNull();
		}

		[Fact]
		public void APostWithoutACsrfTokenIsRejected()
		{
			PortalRequestGuard.EvaluateStateChangingRequest("login", "POST", Token, null, null, null, PortalAuthority, PortalAuthority).Should().Be(403);
			PortalRequestGuard.EvaluateStateChangingRequest("logout", "POST", Token, "", null, null, PortalAuthority, PortalAuthority).Should().Be(403);
		}

		[Fact]
		public void APostWithAForgedCsrfTokenIsRejected()
		{
			PortalRequestGuard.EvaluateStateChangingRequest("login", "POST", Token, MutateChar(Token, 0), null, null, PortalAuthority, PortalAuthority).Should().Be(403);
			PortalRequestGuard.EvaluateStateChangingRequest("logout", "POST", Token, Token.ToUpperInvariant(), null, null, PortalAuthority, PortalAuthority).Should().Be(403);
			PortalRequestGuard.EvaluateStateChangingRequest("login", "POST", null, Token, null, null, PortalAuthority, PortalAuthority).Should().Be(403);
		}

		[Fact]
		public void APostWithAForeignOriginIsRejectedEvenWithAValidCsrfToken()
		{
			PortalRequestGuard.EvaluateStateChangingRequest("login", "POST", Token, Token, "http://evil.example", null, PortalAuthority, PortalAuthority).Should().Be(403);
		}

		[Fact]
		public void APostFromAnAlternateLoopbackOriginIsAccepted()
		{
			PortalRequestGuard.EvaluateStateChangingRequest("login", "POST", Token, Token, "http://127.0.0.1:8338", null, "http://127.0.0.1:8338", PortalAuthority)
				.Should().BeNull();
		}

		[Fact]
		public void OriginHeaderMatchingTheLocalPortalIsTrusted()
		{
			PortalRequestGuard.IsTrustedOriginOrReferer(PortalAuthority, null, PortalAuthority, PortalAuthority).Should().BeTrue();
			PortalRequestGuard.IsTrustedOriginOrReferer("http://LOCALHOST:8338", null, PortalAuthority, PortalAuthority).Should().BeTrue();
		}

		[Fact]
		public void ForeignOriginsAreRejected()
		{
			PortalRequestGuard.IsTrustedOriginOrReferer("http://evil.example", null, PortalAuthority, PortalAuthority).Should().BeFalse();
			// "null" is what sandboxed contexts and privacy modes send; it is not the local portal.
			PortalRequestGuard.IsTrustedOriginOrReferer("null", null, PortalAuthority, PortalAuthority).Should().BeFalse();
			PortalRequestGuard.IsTrustedOriginOrReferer("http://localhost:8338.evil.example", null, PortalAuthority, PortalAuthority).Should().BeFalse();
			// A trusted Referer must not mask a foreign Origin.
			PortalRequestGuard.IsTrustedOriginOrReferer("http://evil.example", PortalAuthority + "/", PortalAuthority, PortalAuthority).Should().BeFalse();
		}

		[Fact]
		public void ARefererPointingAtTheLocalPortalIsTrusted()
		{
			PortalRequestGuard.IsTrustedOriginOrReferer(null, PortalAuthority, PortalAuthority, PortalAuthority).Should().BeTrue();
			PortalRequestGuard.IsTrustedOriginOrReferer(null, PortalAuthority + "/", PortalAuthority, PortalAuthority).Should().BeTrue();
			PortalRequestGuard.IsTrustedOriginOrReferer(null, PortalAuthority + "/api/twitch/state", PortalAuthority, PortalAuthority).Should().BeTrue();
		}

		[Fact]
		public void ForeignReferersAreRejected()
		{
			PortalRequestGuard.IsTrustedOriginOrReferer(null, "http://evil.example/", PortalAuthority, PortalAuthority).Should().BeFalse();
			// The boundary slash must be enforced so lookalike hosts cannot pass the prefix check.
			PortalRequestGuard.IsTrustedOriginOrReferer(null, "http://localhost:8338.evil.example/", PortalAuthority, PortalAuthority).Should().BeFalse();
			PortalRequestGuard.IsTrustedOriginOrReferer(null, "https://localhost:8338/", PortalAuthority, PortalAuthority).Should().BeFalse();
		}

		[Fact]
		public void MissingOriginAndRefererHeadersAreTrustedBecauseTheCsrfTokenIsThePrimaryDefense()
		{
			PortalRequestGuard.IsTrustedOriginOrReferer(null, null, PortalAuthority, PortalAuthority).Should().BeTrue();
			PortalRequestGuard.IsTrustedOriginOrReferer("", "", PortalAuthority, PortalAuthority).Should().BeTrue();
		}

		[Fact]
		public void TheRequestsOwnAuthorityIsOnlyTrustedWhenItIsLoopback()
		{
			// Alternate loopback spellings keep working for selfhosted setups...
			PortalRequestGuard.IsTrustedOriginOrReferer("http://127.0.0.1:8338", null, "http://127.0.0.1:8338", PortalAuthority).Should().BeTrue();
			PortalRequestGuard.IsTrustedOriginOrReferer("http://[::1]:8338", null, "http://[::1]:8338", PortalAuthority).Should().BeTrue();
			// ...but a non-loopback Host header must never become a trusted origin, even if the
			// attacker made it match the Origin of the request.
			PortalRequestGuard.IsTrustedOriginOrReferer("http://evil.example:8338", null, "http://evil.example:8338", PortalAuthority).Should().BeFalse();
			PortalRequestGuard.IsTrustedOriginOrReferer(null, "http://evil.example:8338/", "http://evil.example:8338", PortalAuthority).Should().BeFalse();
		}

		private static string MutateChar(string token, int position)
		{
			var characters = token.ToCharArray();
			characters[position] = characters[position] == '0' ? '1' : '0';
			return new string(characters);
		}
	}

	/// <summary>
	/// Tests for the portal page that <see cref="KittenApiService"/> builds: the per-startup CSRF
	/// token and the library version must be injected without leaving placeholders behind.
	/// </summary>
	public class KittenApiServicePageTests
	{
		[Fact]
		public async Task BuildWebSitePageInjectsTheCsrfTokenAndTheLibraryVersion()
		{
			const string csrfToken = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

			var page = await KittenApiService.BuildWebSitePage(new Version(2, 0, 0), csrfToken).ConfigureAwait(false);

			page.Should().Contain("catcore-csrf-token");
			page.Should().Contain(csrfToken);
			page.Should().NotContain(PortalRequestGuard.CsrfTokenPlaceholder);
			page.Should().NotContain("{libVersion}");
			page.Should().Contain("2.0.0");
			Regex.Match(page, "content=\"" + csrfToken + "\"").Success.Should().BeTrue("the token should be embedded as the meta content attribute");
		}
	}
}
