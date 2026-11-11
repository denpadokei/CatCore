using System;

namespace CatCore.Services.Twitch
{
	/// <summary>
	/// Classifies EventSub revocation statuses and coalesces bursts of authorization revocations so a
	/// single forced token refresh/restart cycle handles the whole wave instead of one per subscription.
	/// </summary>
	internal sealed class EventSubRevocationPolicy
	{
		internal static readonly TimeSpan AuthorizationRefreshCooldown = TimeSpan.FromSeconds(30);

		private readonly object _lock = new();
		private readonly Func<DateTimeOffset> _utcNow;
		private DateTimeOffset? _lastAuthorizationRefreshAt;

		public EventSubRevocationPolicy(Func<DateTimeOffset>? utcNow = null)
		{
			_utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
		}

		/// <summary>
		/// Maps the documented revocation statuses (authorization_revoked, user_removed,
		/// version_removed) to an action. Unknown statuses are logged and ignored until the next
		/// reconnect cycle instead of blindly force re-subscribing.
		/// </summary>
		public static EventSubRevocationAction Classify(string? status)
		{
			return status switch
			{
				"authorization_revoked" => EventSubRevocationAction.ForceReauth,
				"user_removed" => EventSubRevocationAction.DetachChannel,
				"version_removed" => EventSubRevocationAction.LogVersionRemoved,
				_ => EventSubRevocationAction.Ignore
			};
		}

		/// <summary>
		/// Returns true for the first authorization revocation of a burst; later revocations within the
		/// cooldown window are coalesced into that first refresh/restart cycle.
		/// </summary>
		public bool TryBeginAuthorizationRefresh()
		{
			lock (_lock)
			{
				var now = _utcNow();
				if (_lastAuthorizationRefreshAt is { } lastAuthorizationRefreshAt
				    && now - lastAuthorizationRefreshAt < AuthorizationRefreshCooldown)
				{
					return false;
				}

				_lastAuthorizationRefreshAt = now;
				return true;
			}
		}
	}
}
