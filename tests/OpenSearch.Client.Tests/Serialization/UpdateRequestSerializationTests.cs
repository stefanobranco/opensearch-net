using System.Text;
using System.Text.Json;
using FluentAssertions;
using OpenSearch.Net;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Pins the typed <see cref="UpdateRequest{TDocument, TPartialDocument}"/> body: doc/upsert are
/// real C# values (anonymous objects, document instances) rather than pre-serialized JsonElements,
/// and both route through the configured source serializer like every other user document.
/// </summary>
public class UpdateRequestSerializationTests
{
	private sealed class Product
	{
		public string? ProductName { get; set; }
		public int? StockCount { get; set; }
	}

	private static readonly OpenSearchClientSettings DefaultSettings =
		OpenSearchClientSettings.Create(new Uri("http://localhost:9200")).Build();

	private static readonly OpenSearchClientSettings CamelSettings =
		OpenSearchClientSettings.Create(new Uri("http://localhost:9200"))
			.SourceSerializer(() => new SystemTextJsonSerializer(new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
			}))
			.Build();

	private static JsonElement WriteBody<TDocument, TPartialDocument>(
		OpenSearchClientSettings settings, UpdateRequest<TDocument, TPartialDocument> request)
	{
		using var stream = new MemoryStream();
		UpdateEndpoint<TDocument, TPartialDocument>.Instance.GetBody(request)!
			.WriteTo(stream, settings.RequestResponseSerializer);
		return JsonDocument.Parse(Encoding.UTF8.GetString(stream.ToArray())).RootElement;
	}

	[Fact]
	public void AnonymousPartialDoc_SerializesAsBody()
	{
		var request = new UpdateRequest<Product, object>
		{
			Index = "products",
			Id = "1",
			Doc = new { stock_count = 7 },
			DetectNoop = true,
		};

		var body = WriteBody(DefaultSettings, request);
		body.GetProperty("doc").GetProperty("stock_count").GetInt32().Should().Be(7);
		body.GetProperty("detect_noop").GetBoolean().Should().BeTrue();
		body.TryGetProperty("upsert", out _).Should().BeFalse();
	}

	[Fact]
	public void TypedUpsert_SerializesThroughDefaultSourceSerializer()
	{
		var request = new UpdateRequest<Product, object>
		{
			Doc = new { stock_count = 7 },
			Upsert = new Product { ProductName = "Widget", StockCount = 7 },
		};

		var body = WriteBody(DefaultSettings, request);
		body.GetProperty("upsert").GetProperty("product_name").GetString().Should().Be("Widget");
		body.GetProperty("upsert").GetProperty("stock_count").GetInt32().Should().Be(7);
	}

	[Fact]
	public void DocAndUpsert_RouteThroughCustomSourceSerializer()
	{
		var request = new UpdateRequest<Product, Product>
		{
			Doc = new Product { StockCount = 7 },
			Upsert = new Product { ProductName = "Widget" },
			DetectNoop = true,
		};

		var body = WriteBody(CamelSettings, request);
		// User documents follow the camelCase source serializer...
		body.GetProperty("doc").GetProperty("stockCount").GetInt32().Should().Be(7);
		body.GetProperty("upsert").GetProperty("productName").GetString().Should().Be("Widget");
		// ...while the envelope stays on the request serializer's snake_case.
		body.GetProperty("detect_noop").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void ScriptOnlyUpdate_OmitsDocAndUpsert()
	{
		var request = new UpdateRequest<Product, Product>
		{
			Script = Script.Inline("ctx._source.stock_count += 1", "painless"),
		};

		var body = WriteBody(DefaultSettings, request);
		body.TryGetProperty("doc", out _).Should().BeFalse();
		body.TryGetProperty("upsert", out _).Should().BeFalse();
		body.GetProperty("script").GetProperty("source").GetString()
			.Should().Be("ctx._source.stock_count += 1");
	}
}
