using System.Collections.Concurrent;

namespace OpenSearch.Net;

/// <summary>
/// Manages a pool of OpenSearch nodes with round-robin selection and dead-node tracking.
/// Thread-safe for concurrent access.
/// </summary>
public sealed class NodePool
{
	private readonly IReadOnlyList<Node> _nodes;
	private readonly ConcurrentDictionary<Uri, DeadNodeState> _deadNodes = new();
	private int _roundRobinIndex = -1;

	/// <summary>
	/// Creates a node pool from the given collection of nodes.
	/// </summary>
	/// <exception cref="ArgumentException">Thrown when no nodes are provided.</exception>
	public NodePool(IEnumerable<Node> nodes)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		_nodes = nodes.ToList().AsReadOnly();
		if (_nodes.Count == 0)
			throw new ArgumentException("At least one node must be provided.", nameof(nodes));
	}

	/// <summary>
	/// Creates a node pool from the given collection of URIs.
	/// </summary>
	public NodePool(IEnumerable<Uri> uris) : this(uris.Select(u => new Node(u))) { }

	/// <summary>
	/// The list of all nodes in the pool (alive and dead).
	/// </summary>
	public IReadOnlyList<Node> Nodes => _nodes;

	/// <summary>
	/// Selects the next node using round-robin ordering, preferring alive nodes.
	/// If all nodes are dead, returns the one closest to its revival time.
	/// </summary>
	public Node SelectNode()
	{
		var count = _nodes.Count;
		if (count == 1)
		{
			// Fast path: single node, always return it (even if dead).
			return _nodes[0];
		}

		// Try a full round-robin sweep looking for an alive node.
		var startIndex = Interlocked.Increment(ref _roundRobinIndex);

		for (var i = 0; i < count; i++)
		{
			// Use unsigned modulo to handle int overflow gracefully.
			var index = (int)((uint)(startIndex + i) % (uint)count);
			var node = _nodes[index];
			if (IsAlive(node))
				return node;
		}

		// All nodes are dead. Pick the one closest to revival (lowest DeadUntilTicks).
		Node? bestNode = null;
		var bestTicks = long.MaxValue;

		foreach (var node in _nodes)
		{
			if (_deadNodes.TryGetValue(node.Host, out var state) && state.DeadUntilTicks < bestTicks)
			{
				bestTicks = state.DeadUntilTicks;
				bestNode = node;
			}
		}

		return bestNode ?? _nodes[0];
	}

	/// <summary>
	/// Marks a node as dead, applying exponential backoff before it will be retried.
	/// </summary>
	public void MarkDead(Node node) =>
		_deadNodes.AddOrUpdate(
			node.Host,
			static _ => DeadNodeState.Initial(),
			static (_, existing) => existing.IncrementFailure());

	/// <summary>
	/// Marks a node as alive, removing it from the dead-node tracking.
	/// </summary>
	public void MarkAlive(Node node) =>
		_deadNodes.TryRemove(node.Host, out _);

	/// <summary>
	/// Returns true if the node is not in the dead list, or if its backoff period has elapsed.
	/// </summary>
	public bool IsAlive(Node node) =>
		!_deadNodes.TryGetValue(node.Host, out var state) || state.IsAlive;
}
