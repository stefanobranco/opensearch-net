using OpenSearch.Client.Core;

namespace OpenSearch.Client;

/// <summary>
/// Convenience extension methods for <see cref="SearchResponse{TDocument}"/>.
/// Since SearchResponse is sealed and auto-generated, all enhancements live here.
/// </summary>
public static class SearchResponseExtensions
{
	/// <summary>
	/// Returns the source documents from all hits, excluding hits with no source.
	/// </summary>
	public static IReadOnlyList<TDocument> Documents<TDocument>(this SearchResponse<TDocument> response)
		=> response.Hits?.Hits?
			.Where(h => h.Source is not null)
			.Select(h => h.Source!)
			.ToList() ?? [];

	/// <summary>
	/// Returns the total number of matching documents, or 0 if unavailable.
	/// </summary>
	public static long Total<TDocument>(this SearchResponse<TDocument> response)
		=> response.Hits?.Total?.Value ?? 0;

	/// <summary>
	/// Returns a typed <see cref="AggregateDictionary"/> for accessing aggregation results.
	/// </summary>
	public static AggregateDictionary Aggs<TDocument>(this SearchResponse<TDocument> response)
		=> new AggregateDictionary(response.Aggregations);

	/// <summary>
	/// Returns a typed <see cref="SuggestDictionary{TDocument}"/> for accessing suggest results.
	/// </summary>
	public static SuggestDictionary<TDocument> Suggestions<TDocument>(this SearchResponse<TDocument> response)
		=> new(response.Suggest);
}
