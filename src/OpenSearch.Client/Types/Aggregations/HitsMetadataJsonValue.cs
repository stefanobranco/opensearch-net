// Hand-maintained DTO: referenced by the hand-written Aggregate/top-hits accessors, so the code
// generator no longer reaches it. Kept outside Generated/ so the generated tree stays reproducible.
#nullable enable

namespace OpenSearch.Client;

public sealed class HitsMetadataJsonValue<TDocument>
{
	/// <summary>The total number of hits, present only if `track_total_hits` is not set to `false` in the search request.</summary>
	public TotalHits? Total { get; set; }
	public List<Hit<TDocument>>? Hits { get; set; }
	public float? MaxScore { get; set; }
}
