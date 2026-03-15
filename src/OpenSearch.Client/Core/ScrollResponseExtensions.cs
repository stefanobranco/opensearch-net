using OpenSearch.Client.Core;

namespace OpenSearch.Client;

/// <summary>
/// Convenience extension methods for <see cref="ScrollResponse{TDocument}"/>.
/// </summary>
public static class ScrollResponseExtensions
{
	/// <summary>Returns the source documents from all hits.</summary>
	public static IReadOnlyList<TDocument> Documents<TDocument>(this ScrollResponse<TDocument> response)
		=> response.Hits?.Hits?
			.Where(h => h.Source is not null)
			.Select(h => h.Source!)
			.ToList() ?? [];

	/// <summary>Returns the total number of matching documents, or 0 if unavailable.</summary>
	public static long Total<TDocument>(this ScrollResponse<TDocument> response)
		=> response.Hits?.Total?.Value ?? 0;
}
