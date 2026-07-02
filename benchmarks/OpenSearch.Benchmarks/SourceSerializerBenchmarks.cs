using System.Text;
using BenchmarkDotNet.Attributes;
using OpenSearch.Client;
using OpenSearch.Net;

namespace OpenSearch.Benchmarks;

/// <summary>
/// Quantifies the cost of a custom source serializer on the document-heavy paths. Three
/// configurations per scenario: the default (documents on the built-in STJ serializer, no
/// boundary), a second <see cref="SystemTextJsonSerializer"/> as source serializer (isolates the
/// buffering boundary that any custom serializer pays), and <see cref="NewtonsoftSourceSerializer"/>
/// (boundary + the Newtonsoft engine). API-type parsing is identical across all three — only
/// document (de)serialization differs.
/// </summary>
[MemoryDiagnoser]
public class SourceSerializerBenchmarks
{
	private OpenSearchClient _searchDefault = null!;
	private OpenSearchClient _searchStjCustom = null!;
	private OpenSearchClient _searchNewtonsoft = null!;
	private OpenSearchClient _bulkDefault = null!;
	private OpenSearchClient _bulkStjCustom = null!;
	private OpenSearchClient _bulkNewtonsoft = null!;
	private List<Product> _products = null!;

	[GlobalSetup]
	public void Setup()
	{
		_products = BenchmarkData.Products(1000);
		var searchJson = BenchmarkData.SearchResponseJson(100, withAggregations: false);
		var bulkJson = BenchmarkData.BulkResponseJson(1000);

		_searchDefault = CreateClient(searchJson, sourceSerializer: null);
		_searchStjCustom = CreateClient(searchJson, CreateCustomStjSerializer);
		_searchNewtonsoft = CreateClient(searchJson, () => new NewtonsoftSourceSerializer());
		_bulkDefault = CreateClient(bulkJson, sourceSerializer: null);
		_bulkStjCustom = CreateClient(bulkJson, CreateCustomStjSerializer);
		_bulkNewtonsoft = CreateClient(bulkJson, () => new NewtonsoftSourceSerializer());
	}

	private static IOpenSearchSerializer CreateCustomStjSerializer() =>
		new SystemTextJsonSerializer(new System.Text.Json.JsonSerializerOptions
		{
			PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
		});

	private static OpenSearchClient CreateClient(string cannedResponse, Func<IOpenSearchSerializer>? sourceSerializer)
	{
		var bytes = Encoding.UTF8.GetBytes(cannedResponse);
		var builder = OpenSearchClientSettings
			.Create(new Uri("http://localhost:9200"))
			.HttpMessageHandlerFactory(_ => new CannedResponseHandler(bytes));
		if (sourceSerializer is not null)
			builder.SourceSerializer(sourceSerializer);
		return new OpenSearchClient(builder.Build());
	}

	private static Task<SearchResponse<Product>> Search(OpenSearchClient client) =>
		client.SearchAsync<Product>(s => s
			.Index("products")
			.Size(100)
			.Query(q => q.Match(f => f.Name, m => m.Query("widget"))));

	private Task<BulkResponse> Bulk(OpenSearchClient client)
	{
		var request = new BulkRequest { Index = "products" };
		var operations = new List<BulkOperation>(_products.Count);
		foreach (var product in _products)
			operations.Add(new BulkIndexOperation<Product> { Document = product, Id = product.Id });
		request.Operations = operations;
		return client.BulkAsync(request);
	}

	[Benchmark(Baseline = true)]
	public Task<SearchResponse<Product>> Search100_DefaultStj() => Search(_searchDefault);

	[Benchmark]
	public Task<SearchResponse<Product>> Search100_CustomStjSource() => Search(_searchStjCustom);

	[Benchmark]
	public Task<SearchResponse<Product>> Search100_NewtonsoftSource() => Search(_searchNewtonsoft);

	[Benchmark]
	public Task<BulkResponse> Bulk1000_DefaultStj() => Bulk(_bulkDefault);

	[Benchmark]
	public Task<BulkResponse> Bulk1000_CustomStjSource() => Bulk(_bulkStjCustom);

	[Benchmark]
	public Task<BulkResponse> Bulk1000_NewtonsoftSource() => Bulk(_bulkNewtonsoft);
}
