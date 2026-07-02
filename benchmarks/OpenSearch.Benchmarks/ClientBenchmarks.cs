using System.Text;
using BenchmarkDotNet.Attributes;
using OpenSearch.Client;
using OpenSearch.Net;

namespace OpenSearch.Benchmarks;

/// <summary>
/// End-to-end scenarios run through the real <see cref="HttpClientTransport"/> with a canned-response
/// handler at the bottom, mirroring the legacy project's <c>InMemoryConnection</c>: everything except
/// the network is measured — request building, body serialization, HTTP message construction, and
/// response deserialization. The serializer-only scenarios isolate the System.Text.Json core.
/// </summary>
[MemoryDiagnoser]
public class ClientBenchmarks
{
	private OpenSearchClient _searchSimpleClient = null!;
	private OpenSearchClient _searchRichClient = null!;
	private OpenSearchClient _bulkClient = null!;
	private OpenSearchClient _getClient = null!;
	private IOpenSearchSerializer _serializer = null!;
	private List<Product> _products = null!;
	private SearchRequest _richRequest = null!;
	private MemoryStream _richResponseStream = null!;
	private MemoryStream _hitsOnlyResponseStream = null!;

	[GlobalSetup]
	public void Setup()
	{
		_products = BenchmarkData.Products(1000);
		_searchSimpleClient = CreateClient(BenchmarkData.SearchResponseJson(10, withAggregations: false));
		_searchRichClient = CreateClient(BenchmarkData.SearchResponseJson(100, withAggregations: true));
		_bulkClient = CreateClient(BenchmarkData.BulkResponseJson(1000));
		_getClient = CreateClient(BenchmarkData.GetResponseJson());

		_serializer = _searchRichClient.Settings.RequestResponseSerializer;
		_richRequest = BuildRichSearch(new SearchRequestDescriptor<Product>());
		_richResponseStream = new MemoryStream(
			Encoding.UTF8.GetBytes(BenchmarkData.SearchResponseJson(100, withAggregations: true)), writable: false);
		_hitsOnlyResponseStream = new MemoryStream(
			Encoding.UTF8.GetBytes(BenchmarkData.SearchResponseJson(100, withAggregations: false)), writable: false);
	}

	private static OpenSearchClient CreateClient(string cannedResponse)
	{
		var bytes = Encoding.UTF8.GetBytes(cannedResponse);
		var settings = OpenSearchClientSettings
			.Create(new Uri("http://localhost:9200"))
			.HttpMessageHandlerFactory(_ => new CannedResponseHandler(bytes))
			.Build();
		return new OpenSearchClient(settings);
	}

	private static SearchRequestDescriptor<Product> BuildRichSearch(SearchRequestDescriptor<Product> s) => s
		.Index("products")
		.Size(100)
		.Query(q => q.Bool(b => b
			.Must(m => m.Match(f => f.Description, mm => mm.Query("premium widget")))
			.Filter(
				f => f.Term(p => p.Tags, t => t.Value("alpha")),
				f => f.Range(p => p.Price, r => r.Gte(10).Lt(100)))))
		.Sort(SortOptions.Descending("price"))
		.Aggregations(a => a
			.Terms("tags", t => t.Field(x => x.Tags).Size(5),
				sub => sub.Avg("avg_price", avg => avg.Field(x => x.Price)))
			.DateHistogram("per_month", d => d.Field(x => x.Created).CalendarInterval("month")));

	[Benchmark]
	public Task<SearchResponse<Product>> SearchSimple() =>
		_searchSimpleClient.SearchAsync<Product>(s => s
			.Index("products")
			.Size(10)
			.Query(q => q.Match(f => f.Name, m => m.Query("widget"))));

	[Benchmark]
	public Task<SearchResponse<Product>> SearchRich() =>
		_searchRichClient.SearchAsync<Product>(s => BuildRichSearch(s));

	[Benchmark]
	public Task<BulkResponse> BulkIndex1000()
	{
		var request = new BulkRequest { Index = "products" };
		var operations = new List<BulkOperation>(_products.Count);
		foreach (var product in _products)
			operations.Add(new BulkIndexOperation<Product> { Document = product, Id = product.Id });
		request.Operations = operations;
		return _bulkClient.BulkAsync(request);
	}

	[Benchmark]
	public Task<GetResponse<Product>> GetDocument() =>
		_getClient.GetAsync<Product>(new GetRequest { Index = "products", Id = "product-1" });

	[Benchmark]
	public void SerializeSearchRequest() => _serializer.Serialize(_richRequest, Stream.Null);

	[Benchmark]
	public SearchResponse<Product>? DeserializeSearchResponse()
	{
		_richResponseStream.Position = 0;
		return _serializer.Deserialize<SearchResponse<Product>>(_richResponseStream);
	}

	[Benchmark]
	public SearchResponse<Product>? DeserializeSearchResponseNoAggs()
	{
		_hitsOnlyResponseStream.Position = 0;
		return _serializer.Deserialize<SearchResponse<Product>>(_hitsOnlyResponseStream);
	}
}
