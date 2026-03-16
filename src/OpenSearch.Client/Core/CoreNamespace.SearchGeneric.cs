using OpenSearch.Client.Core;
using OpenSearch.Net;

namespace OpenSearch.Client;

public sealed partial class CoreNamespace
{
	/// <summary>
	/// Returns results matching a query, using the generic <see cref="SearchRequestDescriptor{TDocument}"/>
	/// which supports expression-based field selection.
	/// <code>
	/// var response = client.Core.Search&lt;MyDoc&gt;(s =&gt; s
	///     .Index(["my-index"])
	///     .Query(q =&gt; q.Term(f =&gt; f.Status!, t =&gt; t.Value("active")))
	///     .Size(10));
	/// </code>
	/// </summary>
	public SearchResponse<TDocument> Search<TDocument>(
		Action<SearchRequestDescriptor<TDocument>> configure,
		TransportOptions? options = null)
	{
		var descriptor = new SearchRequestDescriptor<TDocument>();
		configure(descriptor);
		return Search<TDocument>((SearchRequest)descriptor, options);
	}

	/// <summary>
	/// Returns results matching a query, using the generic <see cref="SearchRequestDescriptor{TDocument}"/>
	/// which supports expression-based field selection.
	/// </summary>
	public Task<SearchResponse<TDocument>> SearchAsync<TDocument>(
		Action<SearchRequestDescriptor<TDocument>> configure,
		TransportOptions? options = null,
		CancellationToken ct = default)
	{
		var descriptor = new SearchRequestDescriptor<TDocument>();
		configure(descriptor);
		return SearchAsync<TDocument>((SearchRequest)descriptor, options, ct);
	}
}
