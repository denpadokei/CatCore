using System.Threading;
using System.Threading.Tasks;
using CatCore.Services.Twitch;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	public class EventSubReconnectAttemptTrackerTests
	{
		private sealed class FakeProvider
		{
		}

		[Fact]
		public void SecondAttemptCannotBeginWhilePending()
		{
			var tracker = new EventSubReconnectAttemptTracker<FakeProvider>();

			tracker.TryBegin(out _).Should().BeTrue();
			tracker.TryBegin(out _).Should().BeFalse();
		}

		[Fact]
		public void AbortAfterCommitReturnsFalseAndDoesNotReturnCommittedProvider()
		{
			// Regression for the fallback timer racing a successful commit: once the Welcome handler
			// committed the connection, a stale fallback timer must not be able to abort it.
			var tracker = new EventSubReconnectAttemptTracker<FakeProvider>();
			tracker.TryBegin(out var attempt).Should().BeTrue();
			var provider = new FakeProvider();
			tracker.TryAttach(provider, attempt.Generation).Should().BeTrue();

			tracker.TryCommit(out var committedProvider).Should().BeTrue();
			committedProvider.Should().BeSameAs(provider);

			tracker.TryAbort(attempt.Generation, out var abortedProvider).Should().BeFalse();
			abortedProvider.Should().BeNull();
		}

		[Fact]
		public void CommitAfterAbortReturnsFalse()
		{
			var tracker = new EventSubReconnectAttemptTracker<FakeProvider>();
			tracker.TryBegin(out var attempt).Should().BeTrue();
			tracker.TryAttach(new FakeProvider(), attempt.Generation).Should().BeTrue();

			tracker.TryAbort(attempt.Generation, out _).Should().BeTrue();

			tracker.TryCommit(out var provider).Should().BeFalse();
			provider.Should().BeNull();
		}

		[Fact]
		public void CommitCancelsTheFallbackWaitToken()
		{
			var tracker = new EventSubReconnectAttemptTracker<FakeProvider>();
			tracker.TryBegin(out var attempt).Should().BeTrue();
			tracker.TryAttach(new FakeProvider(), attempt.Generation).Should().BeTrue();

			tracker.TryCommit(out _).Should().BeTrue();

			attempt.CancellationToken.IsCancellationRequested.Should().BeTrue();
		}

		[Fact]
		public void StaleGenerationCannotAbortANewerAttempt()
		{
			var tracker = new EventSubReconnectAttemptTracker<FakeProvider>();
			tracker.TryBegin(out var firstAttempt).Should().BeTrue();
			tracker.TryAbort(firstAttempt.Generation, out _).Should().BeTrue();

			tracker.TryBegin(out var secondAttempt).Should().BeTrue();
			secondAttempt.Generation.Should().NotBe(firstAttempt.Generation);

			tracker.TryAbort(firstAttempt.Generation, out var abortedProvider).Should().BeFalse();
			abortedProvider.Should().BeNull();
			tracker.IsPending(secondAttempt.Generation).Should().BeTrue();
		}

		[Fact]
		public void AbortAnyOnlyReturnsThePendingProviderOnce()
		{
			var tracker = new EventSubReconnectAttemptTracker<FakeProvider>();
			tracker.TryBegin(out var attempt).Should().BeTrue();
			var provider = new FakeProvider();
			tracker.TryAttach(provider, attempt.Generation).Should().BeTrue();

			tracker.TryAbortAny(out var abortedProvider).Should().BeTrue();
			abortedProvider.Should().BeSameAs(provider);

			tracker.TryAbortAny(out var secondAbortedProvider).Should().BeFalse();
			secondAbortedProvider.Should().BeNull();
		}

		[Fact]
		public void ConcurrentCommitAndAbortHaveExactlyOneWinner()
		{
			for (var iteration = 0; iteration < 200; iteration++)
			{
				var tracker = new EventSubReconnectAttemptTracker<FakeProvider>();
				tracker.TryBegin(out var attempt).Should().BeTrue();
				var provider = new FakeProvider();
				tracker.TryAttach(provider, attempt.Generation).Should().BeTrue();

				var winners = 0;
				var commitTask = Task.Run(() =>
				{
					if (tracker.TryCommit(out _))
					{
						Interlocked.Increment(ref winners);
					}
				});
				var abortTask = Task.Run(() =>
				{
					if (tracker.TryAbort(attempt.Generation, out _))
					{
						Interlocked.Increment(ref winners);
					}
				});
				Task.WaitAll(commitTask, abortTask);

				winners.Should().Be(1);
			}
		}
	}
}
