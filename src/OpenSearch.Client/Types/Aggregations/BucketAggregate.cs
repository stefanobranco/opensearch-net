using System.Collections;

namespace OpenSearch.Client;

/// <summary>
/// Wraps a list of buckets from a bucket aggregation, providing NEST-compatible
/// <c>.Buckets</c> property access pattern. Also implements <see cref="IReadOnlyList{T}"/>
/// for direct indexing, enumeration, and LINQ.
/// </summary>
public sealed class BucketAggregate<TBucket> : IReadOnlyList<TBucket>
{
	/// <summary>The buckets in this aggregation.</summary>
	public IReadOnlyList<TBucket> Buckets { get; }

	public BucketAggregate(IReadOnlyList<TBucket>? buckets) =>
		Buckets = buckets ?? [];

	public TBucket this[int index] => Buckets[index];
	public int Count => Buckets.Count;
	public IEnumerator<TBucket> GetEnumerator() => Buckets.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
