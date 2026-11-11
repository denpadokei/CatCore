using System.Threading;

namespace CatCore.Services.Twitch
{
	internal enum ReconnectAttemptState
	{
		Idle,
		Pending
	}

	internal readonly struct ReconnectAttempt
	{
		public ReconnectAttempt(int generation, CancellationToken cancellationToken)
		{
			Generation = generation;
			CancellationToken = cancellationToken;
		}

		public int Generation { get; }

		public CancellationToken CancellationToken { get; }
	}

	/// <summary>
	/// Owns the lifecycle of a single EventSub reconnect attempt. The fallback timer and the Welcome
	/// handler race against each other; this tracker guarantees that only one of them can claim the
	/// attempt, so a committed connection can never be aborted by a stale fallback timer.
	/// </summary>
	internal sealed class EventSubReconnectAttemptTracker<TProvider> where TProvider : class
	{
		private readonly object _lock = new();
		private ReconnectAttemptState _state;
		private int _generation;
		private CancellationTokenSource? _cancellationTokenSource;
		private TProvider? _pendingProvider;

		public bool TryBegin(out ReconnectAttempt attempt)
		{
			lock (_lock)
			{
				if (_state != ReconnectAttemptState.Idle)
				{
					attempt = default;
					return false;
				}

				_generation++;
				_state = ReconnectAttemptState.Pending;
				_cancellationTokenSource = new CancellationTokenSource();
				attempt = new ReconnectAttempt(_generation, _cancellationTokenSource.Token);
				return true;
			}
		}

		public bool TryAttach(TProvider provider, int generation)
		{
			lock (_lock)
			{
				if (_state != ReconnectAttemptState.Pending || _generation != generation)
				{
					return false;
				}

				_pendingProvider = provider;
				return true;
			}
		}

		public bool IsPending(int generation)
		{
			lock (_lock)
			{
				return _state == ReconnectAttemptState.Pending && _generation == generation;
			}
		}

		public bool TryCommit(out TProvider? provider)
		{
			lock (_lock)
			{
				provider = null;
				if (_state != ReconnectAttemptState.Pending || _pendingProvider == null)
				{
					return false;
				}

				_state = ReconnectAttemptState.Idle;
				provider = _pendingProvider;
				_pendingProvider = null;
				CancelTimerLocked();
				return true;
			}
		}

		public bool TryAbort(int generation, out TProvider? provider)
		{
			lock (_lock)
			{
				provider = null;
				if (_state != ReconnectAttemptState.Pending || _generation != generation)
				{
					return false;
				}

				_state = ReconnectAttemptState.Idle;
				provider = _pendingProvider;
				_pendingProvider = null;
				CancelTimerLocked();
				return true;
			}
		}

		public bool TryAbortAny(out TProvider? provider)
		{
			lock (_lock)
			{
				provider = _pendingProvider;
				_pendingProvider = null;
				_state = ReconnectAttemptState.Idle;
				_generation++;
				CancelTimerLocked();
				return provider != null;
			}
		}

		private void CancelTimerLocked()
		{
			var cancellationTokenSource = _cancellationTokenSource;
			_cancellationTokenSource = null;
			cancellationTokenSource?.Cancel();
			cancellationTokenSource?.Dispose();
		}
	}
}
