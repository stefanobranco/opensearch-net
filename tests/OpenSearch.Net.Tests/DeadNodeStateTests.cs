using FluentAssertions;
using OpenSearch.Net;
using Xunit;

namespace OpenSearch.Net.Tests;

public class DeadNodeStateTests
{
	[Fact]
	public void Initial_HasFailedAttempts1()
	{
		var state = DeadNodeState.Initial();
		state.FailedAttempts.Should().Be(1);
	}

	[Fact]
	public void IncrementFailure_IncreasesFailedAttempts()
	{
		var state = DeadNodeState.Initial();
		state = state.IncrementFailure();
		state.FailedAttempts.Should().Be(2);

		state = state.IncrementFailure();
		state.FailedAttempts.Should().Be(3);
	}

	[Fact]
	public void Backoff_IsExponential()
	{
		// Capture DeadUntilTicks across successive failures to verify the backoff pattern.
		// Each state is created independently, so we check relative offsets.
		var state1 = DeadNodeState.Initial(); // attempt=1 -> 1s
		var state2 = state1.IncrementFailure(); // attempt=2 -> 2s
		var state3 = state2.IncrementFailure(); // attempt=3 -> 4s
		var state4 = state3.IncrementFailure(); // attempt=4 -> 8s

		// The differences between DeadUntilTicks and current ticks should roughly
		// follow the 1s, 2s, 4s, 8s pattern. We verify each subsequent state has
		// a higher DeadUntilTicks (i.e., longer backoff).
		state2.DeadUntilTicks.Should().BeGreaterThan(state1.DeadUntilTicks);
		state3.DeadUntilTicks.Should().BeGreaterThan(state2.DeadUntilTicks);
		state4.DeadUntilTicks.Should().BeGreaterThan(state3.DeadUntilTicks);
	}

	[Fact]
	public void Backoff_CapsAtFiveMinutes()
	{
		// Simulate many failures and verify the backoff doesn't exceed 5 minutes.
		var state = DeadNodeState.Initial();
		for (var i = 0; i < 50; i++)
			state = state.IncrementFailure();

		var now = Environment.TickCount64;
		var fiveMinutesMs = (long)TimeSpan.FromMinutes(5).TotalMilliseconds;

		// DeadUntilTicks should be at most now + 5 minutes (with small tolerance for execution time).
		var maxDeadUntil = now + fiveMinutesMs + 1000; // 1s tolerance
		state.DeadUntilTicks.Should().BeLessThanOrEqualTo(maxDeadUntil);

		// Also verify it's at least now (not in the past).
		state.DeadUntilTicks.Should().BeGreaterThanOrEqualTo(now);
	}

	[Fact]
	public void IsAlive_ReturnsFalseWhenRecent()
	{
		// A newly created dead state should have a DeadUntilTicks in the future.
		var state = DeadNodeState.Initial();

		// The 1s backoff means DeadUntilTicks is ~1000ms ahead of now.
		state.DeadUntilTicks.Should().BeGreaterThan(Environment.TickCount64);
		state.IsAlive.Should().BeFalse();
	}

	[Fact]
	public void IsAlive_ReturnsTrueWhenDeadlineInPast()
	{
		// Construct a state with DeadUntilTicks well in the past.
		var state = new DeadNodeState
		{
			FailedAttempts = 1,
			DeadUntilTicks = Environment.TickCount64 - 10_000 // 10 seconds ago
		};

		state.IsAlive.Should().BeTrue();
	}

	[Theory]
	[InlineData(1, 1)]    // 2^0 = 1s
	[InlineData(2, 2)]    // 2^1 = 2s
	[InlineData(3, 4)]    // 2^2 = 4s
	[InlineData(4, 8)]    // 2^3 = 8s
	[InlineData(5, 16)]   // 2^4 = 16s
	[InlineData(10, 300)] // 2^9 = 512s -> capped at 300s
	[InlineData(20, 300)] // Capped at 300s
	public void BackoffSeconds_MatchesExpectedPattern(int attempt, int expectedSeconds)
	{
		// Build a state for the given attempt number by starting at Initial and incrementing.
		var state = DeadNodeState.Initial();
		for (var i = 1; i < attempt; i++)
			state = state.IncrementFailure();

		state.FailedAttempts.Should().Be(attempt);

		// The backoff in ticks should be approximately expectedSeconds * 1000 from the time of creation.
		// We can't test the exact moment of creation, but we can verify the relative pattern
		// by checking that DeadUntilTicks is in a reasonable range.
		var now = Environment.TickCount64;
		var expectedMs = expectedSeconds * 1000L;
		var tolerance = 500L; // 500ms tolerance for test execution time

		// DeadUntilTicks should be approximately now + expectedMs.
		state.DeadUntilTicks.Should().BeGreaterThanOrEqualTo(now + expectedMs - tolerance);
		state.DeadUntilTicks.Should().BeLessThanOrEqualTo(now + expectedMs + tolerance);
	}
}
