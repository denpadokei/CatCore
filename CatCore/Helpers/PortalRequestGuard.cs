using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace CatCore.Helpers
{
	/// <summary>
	/// Guards the state-changing portal endpoints (Twitch login/logout) against cross-site request
	/// forgery. The portal page is embedded with a per-startup random CSRF token that clients must
	/// echo back in the <see cref="CsrfTokenHeaderName"/> header, mirroring the defense strategy of
	/// the OAuth <c>state</c> validation: a CSPRNG token, a constant-time comparison and rejecting
	/// invalid values outright. The Origin/Referer check is defense in depth only, because browsers
	/// and local clients may omit those headers.
	/// </summary>
	internal static class PortalRequestGuard
	{
		/// <summary>Name of the header the portal uses to send the CSRF token back.</summary>
		internal const string CsrfTokenHeaderName = "X-CSRF-Token";

		/// <summary>Placeholder in the embedded portal page that is replaced by the CSRF token.</summary>
		internal const string CsrfTokenPlaceholder = "{csrfToken}";

		/// <summary>
		/// Creates a new random CSRF token: 32 bytes of CSPRNG output, hex encoded. Same
		/// construction as the OAuth authorization state, doubled in size.
		/// </summary>
		public static string CreateCsrfToken()
		{
			var tokenBytes = new byte[32];
			using (var randomNumberGenerator = RandomNumberGenerator.Create())
			{
				randomNumberGenerator.GetBytes(tokenBytes);
			}

			return BitConverter.ToString(tokenBytes).Replace("-", string.Empty).ToLowerInvariant();
		}

		/// <summary>Returns whether the given endpoint segment changes the authorization state.</summary>
		public static bool IsStateChangingEndpoint(string? endpointSegment)
		{
			return string.Equals(endpointSegment, "login", StringComparison.Ordinal)
			       || string.Equals(endpointSegment, "logout", StringComparison.Ordinal);
		}

		/// <summary>Returns whether the request method is POST, the only accepted method for state-changing endpoints.</summary>
		public static bool IsPostMethod(string? httpMethod)
		{
			return string.Equals(httpMethod, "POST", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Validates the CSRF token a request presented, using a constant-time comparison.
		/// A missing, malformed or non-matching token is rejected.
		/// </summary>
		public static bool ValidateCsrfToken(string? expectedToken, string? providedToken)
		{
			if (string.IsNullOrWhiteSpace(expectedToken) || string.IsNullOrWhiteSpace(providedToken))
			{
				return false;
			}

			return FixedTimeEquals(Encoding.UTF8.GetBytes(expectedToken), Encoding.UTF8.GetBytes(providedToken));
		}

		/// <summary>
		/// Validates the Origin/Referer headers against the local portal. These headers are defense
		/// in depth only: browsers may omit them, so the CSRF token remains the primary defense and
		/// requests without both headers are trusted to continue to the token validation.
		/// </summary>
		/// <param name="origin">Value of the <c>Origin</c> header, or <c>null</c> when absent.</param>
		/// <param name="referer">Value of the <c>Referer</c> header, or <c>null</c> when absent.</param>
		/// <param name="requestAuthority">Authority of the incoming request, e.g. <c>http://localhost:8338</c>.</param>
		/// <param name="configuredAuthority">Authority the portal is configured on, e.g. <c>http://localhost:8338</c>.</param>
		public static bool IsTrustedOriginOrReferer(string? origin, string? referer, string? requestAuthority, string? configuredAuthority)
		{
			if (origin != null && origin.Length > 0)
			{
				return IsAllowedAuthority(origin, requestAuthority, configuredAuthority);
			}

			if (referer != null && referer.Length > 0)
			{
				return IsAllowedReferer(referer, requestAuthority, configuredAuthority);
			}

			// No headers to check: older engines, privacy configurations and non-browser local
			// clients may omit them. The CSRF token is the primary defense.
			return true;
		}

		/// <summary>
		/// Validates a request against a state-changing portal endpoint (Twitch login/logout) in one
		/// pass. Returns the HTTP status code the request must be rejected with, or <c>null</c> when
		/// the request may proceed.
		/// </summary>
		public static int? EvaluateStateChangingRequest(string? endpointSegment, string? httpMethod, string? expectedCsrfToken, string? providedCsrfToken,
			string? origin, string? referer, string? requestAuthority, string? configuredAuthority)
		{
			if (!IsStateChangingEndpoint(endpointSegment))
			{
				return null;
			}

			if (!IsPostMethod(httpMethod))
			{
				// Login and logout change the authorization state, so only POST is accepted.
				return 405;
			}

			if (!ValidateCsrfToken(expectedCsrfToken, providedCsrfToken))
			{
				return 403;
			}

			if (!IsTrustedOriginOrReferer(origin, referer, requestAuthority, configuredAuthority))
			{
				return 403;
			}

			return null;
		}

		private static bool IsAllowedAuthority(string authority, string? requestAuthority, string? configuredAuthority)
		{
			if (string.Equals(authority, configuredAuthority, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			// Also accept the request's own authority when it is a loopback address. This keeps
			// alternate spellings such as http://127.0.0.1:8338 working on selfhosted setups where
			// the listener serves any Host header, while non-loopback authorities stay rejected.
			return IsLoopbackAuthority(requestAuthority)
			       && string.Equals(authority, requestAuthority, StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsAllowedReferer(string referer, string? requestAuthority, string? configuredAuthority)
		{
			return MatchesAuthority(referer, configuredAuthority)
			       || (IsLoopbackAuthority(requestAuthority) && MatchesAuthority(referer, requestAuthority));
		}

		private static bool MatchesAuthority(string referer, string? authority)
		{
			if (string.IsNullOrWhiteSpace(authority))
			{
				return false;
			}

			// The boundary slash prevents a match on lookalike hosts such as
			// http://localhost:8338.evil.example/
			return string.Equals(referer, authority, StringComparison.OrdinalIgnoreCase)
			       || referer.StartsWith(authority + "/", StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsLoopbackAuthority(string? authority)
		{
			if (string.IsNullOrWhiteSpace(authority) || !Uri.TryCreate(authority, UriKind.Absolute, out var uri))
			{
				return false;
			}

			if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
			    && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			var host = uri.Host;
			if (host.StartsWith("[", StringComparison.Ordinal) && host.EndsWith("]", StringComparison.Ordinal))
			{
				host = host.Substring(1, host.Length - 2);
			}

			return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
			       || (IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address));
		}

		/// <summary>
		/// Compares two byte sequences without early-exiting on the first difference, so the
		/// comparison time does not depend on the position of the first mismatching byte.
		/// </summary>
		private static bool FixedTimeEquals(byte[] left, byte[] right)
		{
			if (left.Length != right.Length)
			{
				// The token length is public: it is the length of the token embedded in the portal
				// page. Returning early here matches CryptographicOperations.FixedTimeEquals.
				return false;
			}

			var difference = 0;
			for (var i = 0; i < left.Length; i++)
			{
				difference |= left[i] ^ right[i];
			}

			return difference == 0;
		}
	}
}
