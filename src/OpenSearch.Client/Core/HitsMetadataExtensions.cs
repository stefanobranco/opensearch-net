using OpenSearch.Client.Core;

namespace OpenSearch.Client;

/// <summary>
/// LINQ extension methods for <see cref="HitsMetadata{TDocument}"/> that operate
/// on the <see cref="HitsMetadata{TDocument}.Hits"/> list, enabling direct
/// <c>response.Hits.Select(...)</c> patterns without accessing <c>.Hits.Hits</c>.
/// </summary>
public static class HitsMetadataExtensions
{
	/// <summary>Projects each hit using a transform function.</summary>
	public static IEnumerable<TResult> Select<TDocument, TResult>(
		this HitsMetadata<TDocument> metadata,
		Func<Hit<TDocument>, TResult> selector)
		=> metadata.Hits?.Select(selector) ?? Enumerable.Empty<TResult>();

	/// <summary>Filters hits using a predicate.</summary>
	public static IEnumerable<Hit<TDocument>> Where<TDocument>(
		this HitsMetadata<TDocument> metadata,
		Func<Hit<TDocument>, bool> predicate)
		=> metadata.Hits?.Where(predicate) ?? Enumerable.Empty<Hit<TDocument>>();

	/// <summary>Returns the first hit, or default if none.</summary>
	public static Hit<TDocument>? FirstOrDefault<TDocument>(
		this HitsMetadata<TDocument> metadata)
		=> metadata.Hits?.FirstOrDefault();

	/// <summary>Returns the first hit matching a predicate, or default if none.</summary>
	public static Hit<TDocument>? FirstOrDefault<TDocument>(
		this HitsMetadata<TDocument> metadata,
		Func<Hit<TDocument>, bool> predicate)
		=> metadata.Hits?.FirstOrDefault(predicate);

	/// <summary>Returns a <see cref="IEnumerator{T}"/> over the hits.</summary>
	public static IEnumerator<Hit<TDocument>> GetEnumerator<TDocument>(
		this HitsMetadata<TDocument> metadata)
		=> (metadata.Hits ?? []).GetEnumerator();

	/// <summary>Returns the number of hits.</summary>
	public static int Count<TDocument>(this HitsMetadata<TDocument> metadata)
		=> metadata.Hits?.Count ?? 0;

	/// <summary>Converts hits to a list.</summary>
	public static List<Hit<TDocument>> ToList<TDocument>(this HitsMetadata<TDocument> metadata)
		=> metadata.Hits?.ToList() ?? [];
}
