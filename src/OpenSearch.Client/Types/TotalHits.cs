using System.Text.Json.Serialization;
using OpenSearch.Client.Converters;

namespace OpenSearch.Client;

/// <summary>
/// Total number of hits matching a search query.
/// Handles both the object form <c>{ "value": N, "relation": "eq" }</c>
/// and the bare integer form <c>N</c> returned by OpenSearch.
/// </summary>
[JsonConverter(typeof(TotalHitsConverter))]
public sealed class TotalHits
{
	public long Value { get; set; }

	public string Relation { get; set; } = "eq";

	/// <summary>Implicit conversion to <see cref="long"/> for convenience comparisons.</summary>
	public static implicit operator long(TotalHits t) => t.Value;
}
