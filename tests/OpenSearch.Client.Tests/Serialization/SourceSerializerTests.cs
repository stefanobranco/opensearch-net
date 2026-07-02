using System.Text;
using System.Text.Json;
using FluentAssertions;
using OpenSearch.Net;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Proves the dual-serializer contract: a custom <c>SourceSerializer</c> is honored for user documents
/// on BOTH sides — reads (<c>Hit&lt;T&gt;.Source</c>, <c>GetResponse&lt;T&gt;.Source</c>) and writes (the
/// index/create document body, bulk NDJSON operations) — while the surrounding envelope stays on the
/// request/response serializer. Exists because the setting was once accepted but silently ignored.
/// </summary>
public class SourceSerializerTests
{
	private sealed class CamelDoc
	{
		public string? FirstName { get; set; }
		public int? PageCount { get; set; }
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

	private static T Deserialize<T>(OpenSearchClientSettings settings, string json)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
		return settings.RequestResponseSerializer.Deserialize<T>(stream)!;
	}

	private static string WriteBody(OpenSearchClientSettings settings, RequestBody body)
	{
		using var stream = new MemoryStream();
		body.WriteTo(stream, settings.RequestResponseSerializer);
		return Encoding.UTF8.GetString(stream.ToArray());
	}

	private const string SearchResponseWithCamelSource = """
		{"took":1,"timed_out":false,"hits":{"total":{"value":1,"relation":"eq"},"hits":[
			{"_index":"books","_id":"1","_score":1.0,"_source":{"firstName":"Ada","pageCount":42}}
		]}}
		""";

	[Fact]
	public void Custom_source_serializer_deserializes_hit_source()
	{
		var response = Deserialize<SearchResponse<CamelDoc>>(CamelSettings, SearchResponseWithCamelSource);

		var doc = response.Hits!.Hits![0].Source!;
		doc.FirstName.Should().Be("Ada");
		doc.PageCount.Should().Be(42);
	}

	[Fact]
	public void Default_settings_still_deserialize_snake_case_source()
	{
		var json = SearchResponseWithCamelSource
			.Replace("firstName", "first_name").Replace("pageCount", "page_count");
		var response = Deserialize<SearchResponse<CamelDoc>>(DefaultSettings, json);

		var doc = response.Hits!.Hits![0].Source!;
		doc.FirstName.Should().Be("Ada");
		doc.PageCount.Should().Be(42);
	}

	[Fact]
	public void Custom_source_serializer_deserializes_get_response_source()
	{
		var json = """{"_index":"books","_id":"1","found":true,"_source":{"firstName":"Ada"}}""";

		var response = Deserialize<GetResponse<CamelDoc>>(CamelSettings, json);

		response.Source!.FirstName.Should().Be("Ada");
	}

	[Fact]
	public void Index_document_body_serializes_through_source_serializer()
	{
		var request = new IndexRequest { Index = "books", Body = new CamelDoc { FirstName = "Ada", PageCount = 42 } };

		var json = WriteBody(CamelSettings, IndexEndpoint.Instance.GetBody(request)!);

		json.Should().Be("""{"firstName":"Ada","pageCount":42}""");
	}

	[Fact]
	public void Index_document_body_defaults_to_snake_case()
	{
		var request = new IndexRequest { Index = "books", Body = new CamelDoc { FirstName = "Ada", PageCount = 42 } };

		var json = WriteBody(DefaultSettings, IndexEndpoint.Instance.GetBody(request)!);

		json.Should().Be("""{"first_name":"Ada","page_count":42}""");
	}

	[Fact]
	public void Bulk_ndjson_documents_serialize_through_source_serializer_with_snake_envelope()
	{
		var request = new BulkRequest
		{
			Index = "books",
			Operations =
			[
				new BulkIndexOperation<CamelDoc> { Document = new CamelDoc { FirstName = "Ada" }, Id = "1" },
				new BulkUpdateOperation<CamelDoc> { Doc = new CamelDoc { PageCount = 7 }, Id = "2" },
			],
		};

		var ndjson = WriteBody(CamelSettings, BulkEndpoint.Instance.GetBody(request)!);
		var lines = ndjson.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		lines[0].Should().Contain("\"_id\":\"1\"", "the action envelope stays on the request serializer");
		lines[1].Should().Be("""{"firstName":"Ada"}""");
		lines[2].Should().Contain("\"_id\":\"2\"");
		lines[3].Should().Be("""{"doc":{"pageCount":7}}""");
	}
}
