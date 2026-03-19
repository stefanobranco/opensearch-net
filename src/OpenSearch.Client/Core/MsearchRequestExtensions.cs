using System.Text.Json;
using OpenSearch.Client.Core;

namespace OpenSearch.Client;

/// <summary>
/// Convenience methods for building <see cref="MsearchRequest"/> from typed
/// <see cref="SearchRequest"/> objects, eliminating manual JsonElement construction.
/// </summary>
public static class MsearchRequestExtensions
{
	private static JsonSerializerOptions SerializationOptions => OpenSearchJsonOptions.RequestSerialization;

	/// <summary>
	/// Adds a search to the multi-search request using a fluent descriptor.
	/// <code>
	/// request.AddSearch&lt;MyDoc&gt;("books", s => s
	///     .Query(q => q.MatchAll())
	///     .Size(10));
	/// </code>
	/// </summary>
	public static MsearchRequest AddSearch<TDocument>(
		this MsearchRequest request,
		string? index,
		Action<SearchRequestDescriptor<TDocument>> configure)
	{
		var descriptor = new SearchRequestDescriptor<TDocument>();
		configure(descriptor);
		SearchRequest search = descriptor;
		return AddSearch(request, index, search);
	}

	/// <summary>
	/// Adds a search to the multi-search request using a fluent descriptor,
	/// inheriting the index from the <see cref="MsearchRequest"/>.
	/// </summary>
	public static MsearchRequest AddSearch<TDocument>(
		this MsearchRequest request,
		Action<SearchRequestDescriptor<TDocument>> configure)
		=> AddSearch(request, null, configure);

	/// <summary>
	/// Adds a pre-built <see cref="SearchRequest"/> to the multi-search request.
	/// The search body fields are serialized to JSON and mapped into <see cref="MsearchBody"/>.
	/// </summary>
	public static MsearchRequest AddSearch(
		this MsearchRequest request,
		string? index,
		SearchRequest search)
	{
		var body = ToMsearchBody(search);
		var header = new MsearchHeader
		{
			Index = index ?? search.Index?.FirstOrDefault(),
		};

		request.Searches.Add(new MsearchItem { Header = header, Body = body });
		return request;
	}

	/// <summary>
	/// Adds a pre-built <see cref="SearchRequest"/> to the multi-search request,
	/// inheriting the index from the <see cref="MsearchRequest"/>.
	/// </summary>
	public static MsearchRequest AddSearch(
		this MsearchRequest request,
		SearchRequest search)
		=> AddSearch(request, null, search);

	/// <summary>
	/// Serializes a <see cref="SearchRequest"/> and extracts body fields into an <see cref="MsearchBody"/>.
	/// Primitive fields are copied directly; complex fields (query, aggregations, etc.)
	/// are extracted as cloned JsonElements from the serialized JSON.
	/// </summary>
	private static MsearchBody ToMsearchBody(SearchRequest search)
	{
		var json = JsonSerializer.SerializeToUtf8Bytes(search, SerializationOptions);
		using var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		var body = new MsearchBody
		{
			// Primitive fields — copy directly for efficiency
			From = search.From,
			Size = search.Size,
			MinScore = search.MinScore.HasValue ? (double)search.MinScore.Value : null,
			Explain = search.Explain,
			Profile = search.Profile,
			TerminateAfter = search.TerminateAfter,
			Timeout = search.Timeout,
			TrackScores = search.TrackScores,
			Version = search.Version,
			SeqNoPrimaryTerm = search.SeqNoPrimaryTerm,
			Stats = search.Stats,
		};

		// Complex fields — extract as cloned JsonElements from the serialized form
		CloneProperty(root, "query", v => body.Query = v);
		CloneProperty(root, "aggregations", v => body.Aggregations = v);
		CloneProperty(root, "sort", v => body.Sort = v);
		CloneProperty(root, "_source", v => body.Source = v);
		CloneProperty(root, "highlight", v => body.Highlight = v);
		CloneProperty(root, "collapse", v => body.Collapse = v);
		CloneProperty(root, "post_filter", v => body.PostFilter = v);
		CloneProperty(root, "suggest", v => body.Suggest = v);
		CloneProperty(root, "rescore", v => body.Rescore = v);
		CloneProperty(root, "script_fields", v => body.ScriptFields = v);
		CloneProperty(root, "knn", v => body.Knn = v);
		CloneProperty(root, "fields", v => body.Fields = v);
		CloneProperty(root, "docvalue_fields", v => body.DocvalueFields = v);
		CloneProperty(root, "stored_fields", v => body.StoredFields = v);
		CloneProperty(root, "search_after", v => body.SearchAfter = v);
		CloneProperty(root, "track_total_hits", v => body.TrackTotalHits = v);
		CloneProperty(root, "indices_boost", v => body.IndicesBoost = v);
		CloneProperty(root, "pit", v => body.Pit = v);
		CloneProperty(root, "ext", v => body.Ext = v);

		return body;
	}

	private static void CloneProperty(JsonElement root, string name, Action<JsonElement> setter)
	{
		if (root.TryGetProperty(name, out var value))
			setter(value.Clone());
	}
}
