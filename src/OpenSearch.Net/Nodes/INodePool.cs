namespace OpenSearch.Net;

/// <summary>
/// Abstraction for a pool of OpenSearch nodes with node selection and dead-node tracking.
/// Enables swapping pool strategies (static, sniffing, sticky) without breaking the API.
/// </summary>
public interface INodePool
{
	/// <summary>
	/// The list of all nodes in the pool (alive and dead).
	/// </summary>
	IReadOnlyList<Node> Nodes { get; }

	/// <summary>
	/// Selects the next node to send a request to.
	/// </summary>
	Node SelectNode();

	/// <summary>
	/// Marks a node as dead, applying exponential backoff before it will be retried.
	/// </summary>
	void MarkDead(Node node);

	/// <summary>
	/// Marks a node as alive, removing it from the dead-node tracking.
	/// </summary>
	void MarkAlive(Node node);

	/// <summary>
	/// Returns true if the node is not in the dead list, or if its backoff period has elapsed.
	/// </summary>
	bool IsAlive(Node node);
}
