using OpenSearch.Client.Common;
using OpenSearch.Client.Core;
using OpenSearch.Net;

namespace OpenSearch.Client;

/// <summary>
/// Convenience overloads for the most common OpenSearch operations.
/// These provide NEST-compatible call patterns (document-first Index, string-based Get/Delete, etc.).
/// </summary>
public sealed partial class OpenSearchClient
{
	// ── Index<T>(document, index) ──

	/// <summary>Indexes a document into the specified index.</summary>
	public IndexResponse Index<TDocument>(TDocument document, string index, TransportOptions? options = null)
		where TDocument : class
		=> Index(new IndexRequest { Index = index, Body = document }, options);

	/// <summary>Indexes a document into the specified index.</summary>
	public Task<IndexResponse> IndexAsync<TDocument>(TDocument document, string index, CancellationToken ct = default)
		where TDocument : class
		=> IndexAsync(new IndexRequest { Index = index, Body = document }, ct: ct);

	/// <summary>Indexes a document into the specified index with a specific ID.</summary>
	public IndexResponse Index<TDocument>(TDocument document, string index, string id, TransportOptions? options = null)
		where TDocument : class
		=> Index(new IndexRequest { Index = index, Id = id, Body = document }, options);

	/// <summary>Indexes a document into the specified index with a specific ID.</summary>
	public Task<IndexResponse> IndexAsync<TDocument>(TDocument document, string index, string id, CancellationToken ct = default)
		where TDocument : class
		=> IndexAsync(new IndexRequest { Index = index, Id = id, Body = document }, ct: ct);

	/// <summary>Indexes a document using a fluent configurator.</summary>
	public IndexResponse Index<TDocument>(TDocument document, Action<IndexRequest> configure, TransportOptions? options = null)
		where TDocument : class
	{
		var request = new IndexRequest { Body = document };
		configure(request);
		return Index(request, options);
	}

	/// <summary>Indexes a document using a fluent configurator.</summary>
	public Task<IndexResponse> IndexAsync<TDocument>(TDocument document, Action<IndexRequest> configure, CancellationToken ct = default)
		where TDocument : class
	{
		var request = new IndexRequest { Body = document };
		configure(request);
		return IndexAsync(request, ct: ct);
	}

	// ── IndexMany ──

	/// <summary>Indexes multiple documents in a single bulk request.</summary>
	public BulkResponse IndexMany<TDocument>(IEnumerable<TDocument> documents, string index, TransportOptions? options = null)
		where TDocument : class
	{
		var request = new BulkRequest { Index = index };
		request.Operations ??= [];
		foreach (var doc in documents)
			request.Operations.Add(new BulkIndexOperation<TDocument> { Document = doc });
		return Bulk(request, options);
	}

	/// <summary>Indexes multiple documents in a single bulk request.</summary>
	public Task<BulkResponse> IndexManyAsync<TDocument>(IEnumerable<TDocument> documents, string index, CancellationToken ct = default)
		where TDocument : class
	{
		var request = new BulkRequest { Index = index };
		request.Operations ??= [];
		foreach (var doc in documents)
			request.Operations.Add(new BulkIndexOperation<TDocument> { Document = doc });
		return BulkAsync(request, ct: ct);
	}

	// ── Get by ID ──

	/// <summary>Returns a document by index and ID.</summary>
	public GetResponse<TDocument> Get<TDocument>(string index, string id, TransportOptions? options = null)
		=> Get<TDocument>(new GetRequest { Index = index, Id = id }, options);

	/// <summary>Returns a document by index and ID.</summary>
	public Task<GetResponse<TDocument>> GetAsync<TDocument>(string index, string id, CancellationToken ct = default)
		=> GetAsync<TDocument>(new GetRequest { Index = index, Id = id }, ct: ct);

	/// <summary>Returns a document by index and Guid ID.</summary>
	public GetResponse<TDocument> Get<TDocument>(string index, Guid id, TransportOptions? options = null)
		=> Get<TDocument>(index, id.ToString(), options);

	/// <summary>Returns a document by index and Guid ID.</summary>
	public Task<GetResponse<TDocument>> GetAsync<TDocument>(string index, Guid id, CancellationToken ct = default)
		=> GetAsync<TDocument>(index, id.ToString(), ct);

	// ── Delete by ID ──

	/// <summary>Deletes a document by index and ID.</summary>
	public DeleteResponse Delete(string index, string id, TransportOptions? options = null)
		=> Delete(new DeleteRequest { Index = index, Id = id }, options);

	/// <summary>Deletes a document by index and ID.</summary>
	public Task<DeleteResponse> DeleteAsync(string index, string id, CancellationToken ct = default)
		=> DeleteAsync(new DeleteRequest { Index = index, Id = id }, ct: ct);

	/// <summary>Deletes a document by index and Guid ID.</summary>
	public DeleteResponse Delete(string index, Guid id, TransportOptions? options = null)
		=> Delete(index, id.ToString(), options);

	/// <summary>Deletes a document by index and Guid ID.</summary>
	public Task<DeleteResponse> DeleteAsync(string index, Guid id, CancellationToken ct = default)
		=> DeleteAsync(index, id.ToString(), ct);

	// ── DeleteByQuery ──

	/// <summary>Deletes documents matching a query.</summary>
	public DeleteByQueryResponse DeleteByQuery(DeleteByQueryRequest request, TransportOptions? options = null)
		=> Core.DeleteByQuery(request, options);

	/// <summary>Deletes documents matching a query.</summary>
	public Task<DeleteByQueryResponse> DeleteByQueryAsync(DeleteByQueryRequest request, TransportOptions? options = null, CancellationToken ct = default)
		=> Core.DeleteByQueryAsync(request, options, ct);

	/// <summary>Deletes documents matching a query using a fluent configurator.</summary>
	public DeleteByQueryResponse DeleteByQuery<TDocument>(string index, Action<QueryContainerDescriptor<TDocument>> query, TransportOptions? options = null)
	{
		var qd = new QueryContainerDescriptor<TDocument>();
		query(qd);
		return DeleteByQuery(new DeleteByQueryRequest { Index = new List<string> { index }, Query = qd }, options);
	}

	/// <summary>Deletes documents matching a query using a fluent configurator.</summary>
	public Task<DeleteByQueryResponse> DeleteByQueryAsync<TDocument>(string index, Action<QueryContainerDescriptor<TDocument>> query, CancellationToken ct = default)
	{
		var qd = new QueryContainerDescriptor<TDocument>();
		query(qd);
		return DeleteByQueryAsync(new DeleteByQueryRequest { Index = new List<string> { index }, Query = qd }, ct: ct);
	}

	// ── Scroll by scroll ID ──

	/// <summary>Continues a scroll search with the given scroll ID and timeout.</summary>
	public ScrollResponse<TDocument> Scroll<TDocument>(string scrollTimeout, string scrollId, TransportOptions? options = null)
		=> Scroll<TDocument>(new ScrollRequest { Scroll = scrollTimeout, ScrollId = scrollId }, options);

	/// <summary>Continues a scroll search with the given scroll ID and timeout.</summary>
	public Task<ScrollResponse<TDocument>> ScrollAsync<TDocument>(string scrollTimeout, string scrollId, CancellationToken ct = default)
		=> ScrollAsync<TDocument>(new ScrollRequest { Scroll = scrollTimeout, ScrollId = scrollId }, ct: ct);

	// ── MultiSearch ──

	/// <summary>Executes several search operations in one request.</summary>
	public MsearchResponse MultiSearch(MsearchRequest request, TransportOptions? options = null)
		=> Msearch(request, options);

	/// <summary>Executes several search operations in one request.</summary>
	public Task<MsearchResponse> MultiSearchAsync(MsearchRequest request, TransportOptions? options = null, CancellationToken ct = default)
		=> MsearchAsync(request, options, ct);

	// ── MultiGet ──

	/// <summary>Allows to get multiple documents in one request.</summary>
	public MgetResponse MultiGet(MgetRequest request, TransportOptions? options = null)
		=> Mget(request, options);

	/// <summary>Allows to get multiple documents in one request.</summary>
	public Task<MgetResponse> MultiGetAsync(MgetRequest request, TransportOptions? options = null, CancellationToken ct = default)
		=> MgetAsync(request, options, ct);

	// ── CancellationToken-friendly overloads ──
	// These let consumers pass CancellationToken without explicitly naming the parameter.

	/// <summary>Indexes a document.</summary>
	public Task<IndexResponse> IndexAsync(IndexRequest request, CancellationToken ct)
		=> IndexAsync(request, options: null, ct: ct);

	/// <summary>Returns results matching a query.</summary>
	public Task<SearchResponse<TDocument>> SearchAsync<TDocument>(SearchRequest request, CancellationToken ct)
		=> SearchAsync<TDocument>(request, options: null, ct: ct);

	/// <summary>Returns results matching a query using a fluent descriptor.</summary>
	public Task<SearchResponse<TDocument>> SearchAsync<TDocument>(Action<SearchRequestDescriptor<TDocument>> configure, CancellationToken ct)
		=> SearchAsync(configure, options: null, ct: ct);

	/// <summary>Returns a document.</summary>
	public Task<GetResponse<TDocument>> GetAsync<TDocument>(GetRequest request, CancellationToken ct)
		=> GetAsync<TDocument>(request, options: null, ct: ct);

	/// <summary>Removes a document from the index.</summary>
	public Task<DeleteResponse> DeleteAsync(DeleteRequest request, CancellationToken ct)
		=> DeleteAsync(request, options: null, ct: ct);

	/// <summary>Bulk operations.</summary>
	public Task<BulkResponse> BulkAsync(BulkRequest request, CancellationToken ct)
		=> BulkAsync(request, options: null, ct: ct);

	/// <summary>Multi-search.</summary>
	public Task<MsearchResponse> MsearchAsync(MsearchRequest request, CancellationToken ct)
		=> MsearchAsync(request, options: null, ct: ct);

	/// <summary>Scroll.</summary>
	public Task<ScrollResponse<TDocument>> ScrollAsync<TDocument>(ScrollRequest request, CancellationToken ct)
		=> ScrollAsync<TDocument>(request, options: null, ct: ct);

	/// <summary>Delete by query.</summary>
	public Task<DeleteByQueryResponse> DeleteByQueryAsync(DeleteByQueryRequest request, CancellationToken ct)
		=> DeleteByQueryAsync(request, options: null, ct: ct);

	/// <summary>Multi-get.</summary>
	public Task<MgetResponse> MgetAsync(MgetRequest request, CancellationToken ct)
		=> MgetAsync(request, options: null, ct: ct);
}
