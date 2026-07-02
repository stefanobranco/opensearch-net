using System.Text;
using BenchmarkDotNet.Attributes;
using OpenSearch.Benchmarks;
using OpenSearch.Client;
using OpenSearch.Net;

namespace OpenSearch.Benchmarks.Legacy;

/// <summary>
/// The same scenarios as the v2 project, run against the official OpenSearch.Client 1.x through
/// <see cref="InMemoryConnection"/>: everything except the network is measured — request building,
/// body serialization, and response deserialization over byte-identical canned payloads.
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
	private ISearchRequest _richRequest = null!;
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

		_serializer = _searchRichClient.ConnectionSettings.RequestResponseSerializer;
		_richRequest = BuildRichSearch(new SearchDescriptor<Product>());
		_richResponseStream = new MemoryStream(
			Encoding.UTF8.GetBytes(BenchmarkData.SearchResponseJson(100, withAggregations: true)), writable: false);
		_hitsOnlyResponseStream = new MemoryStream(
			Encoding.UTF8.GetBytes(BenchmarkData.SearchResponseJson(100, withAggregations: false)), writable: false);
	}

	private static OpenSearchClient CreateClient(string cannedResponse)
	{
		var bytes = Encoding.UTF8.GetBytes(cannedResponse);
		var pool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
		var settings = new ConnectionSettings(pool, new InMemoryConnection(bytes))
			.DisablePing();
		return new OpenSearchClient(settings);
	}

	private static ISearchRequest BuildRichSearch(SearchDescriptor<Product> s) => s
		.Index("products")
		.Size(100)
		.Query(q => q.Bool(b => b
			.Must(m => m.Match(mm => mm.Field(f => f.Description).Query("premium widget")))
			.Filter(
				f => f.Term(t => t.Field(p => p.Tags).Value("alpha")),
				f => f.Range(r => r.Field(p => p.Price).GreaterThanOrEquals(10).LessThan(100)))))
		.Sort(so => so.Descending(p => p.Price))
		.Aggregations(a => a
			.Terms("tags", t => t.Field(p => p.Tags).Size(5)
				.Aggregations(sub => sub.Average("avg_price", avg => avg.Field(p => p.Price))))
			.DateHistogram("per_month", d => d.Field(p => p.Created).CalendarInterval(DateInterval.Month)));

	[Benchmark]
	public Task<ISearchResponse<Product>> SearchSimple() =>
		_searchSimpleClient.SearchAsync<Product>(s => s
			.Index("products")
			.Size(10)
			.Query(q => q.Match(m => m.Field(f => f.Name).Query("widget"))));

	[Benchmark]
	public Task<ISearchResponse<Product>> SearchRich() =>
		_searchRichClient.SearchAsync<Product>(s => (SearchDescriptor<Product>)BuildRichSearch(s));

	[Benchmark]
	public Task<BulkResponse> BulkIndex1000() =>
		_bulkClient.BulkAsync(b => b
			.Index("products")
			.IndexMany(_products, (op, product) => op.Id(product.Id)));

	[Benchmark]
	public Task<GetResponse<Product>> GetDocument() =>
		_getClient.GetAsync<Product>("product-1", g => g.Index("products"));

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
