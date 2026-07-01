using System.Text.Json;
using FluentAssertions;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Shared scaffolding for aggregation wire-format fixtures. Serializes through the production
/// serializer (see <see cref="SerializationTestBase"/>) and asserts the canonical
/// <c>{ "&lt;kind&gt;": { ... } }</c> envelope the OpenSearch <c>_search</c> API expects.
/// </summary>
public abstract class AggregationSerializationTestBase : SerializationTestBase
{
	/// <summary>
	/// Round-trips a single <see cref="AggregationContainer"/> (serialize → deserialize → serialize,
	/// asserting byte-identical JSON) and returns the inner body under the given wire <paramref name="kind"/>.
	/// </summary>
	protected static JsonElement AggBody(AggregationContainer agg, string kind)
	{
		var root = AssertRoundTrips(agg);
		root.TryGetProperty(kind, out var body).Should().BeTrue($"expected a '{kind}' aggregation wrapper");
		return body;
	}

	/// <summary>As <see cref="AggBody"/> but without the round-trip assertion — for aggregations whose
	/// value type carries <c>JsonElement</c> members that do not deserialize back to an identical tree.</summary>
	protected static JsonElement AggBodyWriteOnly(AggregationContainer agg, string kind)
	{
		var root = Parse(Serialize(agg));
		root.TryGetProperty(kind, out var body).Should().BeTrue($"expected a '{kind}' aggregation wrapper");
		return body;
	}
}
