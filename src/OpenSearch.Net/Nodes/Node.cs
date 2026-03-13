namespace OpenSearch.Net;

/// <summary>
/// Represents a single OpenSearch node identified by its host URI.
/// </summary>
public sealed class Node
{
	/// <summary>
	/// The base URI of the node (scheme + host + port).
	/// </summary>
	public Uri Host { get; }

	/// <summary>
	/// Optional human-readable name for the node.
	/// </summary>
	public string? Name { get; init; }

	/// <summary>
	/// Optional OpenSearch version reported by the node.
	/// </summary>
	public string? Version { get; init; }

	/// <summary>
	/// Creates a new node with the given host URI. The URI must use the http or https scheme.
	/// </summary>
	public Node(Uri host)
	{
		ArgumentNullException.ThrowIfNull(host);
		Host = host.Scheme is "http" or "https"
			? host
			: throw new ArgumentException("Node host must use http or https scheme.", nameof(host));
	}

	/// <inheritdoc />
	public override string ToString() => Host.ToString();

	/// <inheritdoc />
	public override int GetHashCode() => Host.GetHashCode();

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Node other && Host == other.Host;
}
