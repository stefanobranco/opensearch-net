namespace OpenSearch.Client;

/// <summary>
/// Implemented by bucket types that can contain sub-aggregation results.
/// Used by <see cref="AggregateDictionary"/> to set sub-agg data without
/// a fragile type switch.
/// </summary>
public interface IBucketWithSubAggregations
{
	/// <summary>Sub-aggregations within this bucket.</summary>
	AggregateDictionary? Aggregations { get; set; }
}
