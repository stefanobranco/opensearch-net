namespace OpenSearch.Net;

/// <summary>
/// Tracks the state of a dead node, including how many times it has failed
/// and when it should next be retried using exponential backoff.
/// </summary>
public readonly record struct DeadNodeState
{
	private static readonly TimeSpan MaxDeadTime = TimeSpan.FromMinutes(5);
	private static readonly TimeSpan MinDeadTime = TimeSpan.FromSeconds(1);

	/// <summary>
	/// The number of consecutive failed attempts for this node.
	/// </summary>
	public int FailedAttempts { get; init; }

	/// <summary>
	/// The <see cref="Environment.TickCount64"/> value at which the node may be retried.
	/// </summary>
	public long DeadUntilTicks { get; init; }

	/// <summary>
	/// Creates the initial dead state representing the first failure.
	/// </summary>
	public static DeadNodeState Initial() => CreateForAttempt(1);

	/// <summary>
	/// Returns a new state with an incremented failure count and extended backoff.
	/// </summary>
	public DeadNodeState IncrementFailure() => CreateForAttempt(FailedAttempts + 1);

	/// <summary>
	/// Returns true if the backoff period has elapsed and the node may be retried.
	/// </summary>
	public bool IsAlive => Environment.TickCount64 >= DeadUntilTicks;

	private static DeadNodeState CreateForAttempt(int attempt)
	{
		// Exponential backoff: 1s, 2s, 4s, 8s, ... capped at 5 minutes.
		// Clamp the exponent to avoid overflow on extreme attempt counts.
		var backoffSeconds = Math.Min(
			MaxDeadTime.TotalSeconds,
			MinDeadTime.TotalSeconds * Math.Pow(2, Math.Min(attempt - 1, 31)));

		return new DeadNodeState
		{
			FailedAttempts = attempt,
			DeadUntilTicks = Environment.TickCount64 + (long)(backoffSeconds * 1000)
		};
	}
}
