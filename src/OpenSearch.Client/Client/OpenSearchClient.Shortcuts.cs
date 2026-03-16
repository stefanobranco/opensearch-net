using OpenSearch.Client.Core;
using OpenSearch.Net;

namespace OpenSearch.Client;

/// <summary>
/// Root-level shortcuts for the most common Core namespace operations.
/// Delegates to <see cref="OpenSearchClient.Core"/> so consumers can write
/// <c>client.Search&lt;T&gt;(...)</c> instead of <c>client.Core.Search&lt;T&gt;(...)</c>.
/// </summary>
public sealed partial class OpenSearchClient
{
	// ── Search ──

	/// <summary>Returns results matching a query.</summary>
	public SearchResponse<TDocument> Search<TDocument>(SearchRequest request, TransportOptions? options = null)
		=> Core.Search<TDocument>(request, options);

	/// <summary>Returns results matching a query.</summary>
	public Task<SearchResponse<TDocument>> SearchAsync<TDocument>(SearchRequest request, TransportOptions? options = null, CancellationToken ct = default)
		=> Core.SearchAsync<TDocument>(request, options, ct);

	/// <summary>Returns results matching a query using a generic fluent descriptor.</summary>
	public SearchResponse<TDocument> Search<TDocument>(Action<SearchRequestDescriptor<TDocument>> configure, TransportOptions? options = null)
		=> Core.Search(configure, options);

	/// <summary>Returns results matching a query using a generic fluent descriptor.</summary>
	public Task<SearchResponse<TDocument>> SearchAsync<TDocument>(Action<SearchRequestDescriptor<TDocument>> configure, TransportOptions? options = null, CancellationToken ct = default)
		=> Core.SearchAsync(configure, options, ct);

	// ── Index ──

	/// <summary>Creates or updates a document in an index.</summary>
	public IndexResponse Index(IndexRequest request, TransportOptions? options = null)
		=> Core.Index(request, options);

	/// <summary>Creates or updates a document in an index.</summary>
	public Task<IndexResponse> IndexAsync(IndexRequest request, TransportOptions? options = null, CancellationToken ct = default)
		=> Core.IndexAsync(request, options, ct);

	// ── Get ──

	/// <summary>Returns a document.</summary>
	public GetResponse<TDocument> Get<TDocument>(GetRequest request, TransportOptions? options = null)
		=> Core.Get<TDocument>(request, options);

	/// <summary>Returns a document.</summary>
	public Task<GetResponse<TDocument>> GetAsync<TDocument>(GetRequest request, TransportOptions? options = null, CancellationToken ct = default)
		=> Core.GetAsync<TDocument>(request, options, ct);

	// ── Delete ──

	/// <summary>Removes a document from the index.</summary>
	public DeleteResponse Delete(DeleteRequest request, TransportOptions? options = null)
		=> Core.Delete(request, options);

	/// <summary>Removes a document from the index.</summary>
	public Task<DeleteResponse> DeleteAsync(DeleteRequest request, TransportOptions? options = null, CancellationToken ct = default)
		=> Core.DeleteAsync(request, options, ct);

	// ── Bulk ──

	/// <summary>Allows to perform multiple index/update/delete operations in a single request.</summary>
	public BulkResponse Bulk(BulkRequest request, TransportOptions? options = null)
		=> Core.Bulk(request, options);

	/// <summary>Allows to perform multiple index/update/delete operations in a single request.</summary>
	public Task<BulkResponse> BulkAsync(BulkRequest request, TransportOptions? options = null, CancellationToken ct = default)
		=> Core.BulkAsync(request, options, ct);

	// ── Multi-Get ──

	/// <summary>Allows to get multiple documents in one request.</summary>
	public MgetResponse Mget(MgetRequest request, TransportOptions? options = null)
		=> Core.Mget(request, options);

	/// <summary>Allows to get multiple documents in one request.</summary>
	public Task<MgetResponse> MgetAsync(MgetRequest request, TransportOptions? options = null, CancellationToken ct = default)
		=> Core.MgetAsync(request, options, ct);

	// ── Multi-Search ──

	/// <summary>Allows to execute several search operations in one request.</summary>
	public MsearchResponse Msearch(MsearchRequest request, TransportOptions? options = null)
		=> Core.Msearch(request, options);

	/// <summary>Allows to execute several search operations in one request.</summary>
	public Task<MsearchResponse> MsearchAsync(MsearchRequest request, TransportOptions? options = null, CancellationToken ct = default)
		=> Core.MsearchAsync(request, options, ct);

	// ── Scroll ──

	/// <summary>Allows to retrieve a large numbers of results from a single search request.</summary>
	public ScrollResponse<TDocument> Scroll<TDocument>(ScrollRequest request, TransportOptions? options = null)
		=> Core.Scroll<TDocument>(request, options);

	/// <summary>Allows to retrieve a large numbers of results from a single search request.</summary>
	public Task<ScrollResponse<TDocument>> ScrollAsync<TDocument>(ScrollRequest request, TransportOptions? options = null, CancellationToken ct = default)
		=> Core.ScrollAsync<TDocument>(request, options, ct);
}
