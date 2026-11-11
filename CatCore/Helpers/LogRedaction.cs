using System;

namespace CatCore.Helpers
{
	/// <summary>
	/// Helpers for values that must not be written to logs in full, such as session URLs or tokens.
	/// </summary>
	internal static class LogRedaction
	{
		/// <summary>
		/// Returns only the host part of an absolute URI. Query strings and path segments of EventSub
		/// session/reconnect URLs contain authorization material and must never be logged.
		/// </summary>
		public static string GetUriHost(string? url)
		{
			return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : "<invalid>";
		}
	}
}
