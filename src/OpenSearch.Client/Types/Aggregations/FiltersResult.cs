namespace OpenSearch.Client;

/// <summary>
/// The result of a <c>filters</c> aggregation. Named filters return a keyed map of buckets
/// (<see cref="Named"/>); anonymous filters return an ordered list (<see cref="Anonymous"/>).
/// Exactly one of the two is populated, mirroring the request form.
/// </summary>
public sealed class FiltersResult
{
	internal FiltersResult(IReadOnlyDictionary<string, FilterBucket>? named, IReadOnlyList<FilterBucket>? anonymous)
	{
		Named = named;
		Anonymous = anonymous;
	}

	/// <summary>Buckets keyed by filter name, or <c>null</c> when the request used anonymous filters.</summary>
	public IReadOnlyDictionary<string, FilterBucket>? Named { get; }

	/// <summary>Buckets in request order, or <c>null</c> when the request used named filters.</summary>
	public IReadOnlyList<FilterBucket>? Anonymous { get; }

	/// <summary>The bucket for the given filter name, or <c>null</c>.</summary>
	public FilterBucket? Bucket(string name) =>
		Named is not null && Named.TryGetValue(name, out var b) ? b : null;
}
