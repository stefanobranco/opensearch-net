using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class DescriptorTests
{
	private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

	private static JsonSerializerOptions CreateOptions()
	{
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			NumberHandling = JsonNumberHandling.AllowReadingFromString,
		};
		options.Converters.Add(new JsonEnumConverterFactory());
		return options;
	}

	// ── 1. Simple property chaining ──

	[Fact]
	public void SearchRequestDescriptor_SetsSimpleProperties()
	{
		var descriptor = new SearchRequestDescriptor();
		descriptor.Size(10).From(5).TrackScores(true);

		SearchRequest request = descriptor;

		request.Size.Should().Be(10);
		request.From.Should().Be(5);
		request.TrackScores.Should().BeTrue();
	}

	// ── 2. Implicit conversion ──

	[Fact]
	public void SearchRequestDescriptor_ImplicitConversion()
	{
		SearchRequest request = new SearchRequestDescriptor().Size(25);

		request.Size.Should().Be(25);
	}

	// ── 3. Single nested descriptor (object) ──

	[Fact]
	public void SearchRequestDescriptor_NestedObjectDescriptor_ConstantScore()
	{
		SearchRequest request = new SearchRequestDescriptor()
			.Query(q => q.ConstantScore(cs => cs.Boost(1.2f)));

		request.Query.Should().NotBeNull();
		request.Query!.Kind.Should().Be(QueryKind.ConstantScore);
		request.Query.Get<ConstantScoreQuery>().Boost.Should().Be(1.2f);
	}

	// ── 4. Single nested descriptor (union) ──

	[Fact]
	public void SearchRequestDescriptor_NestedUnionDescriptor_Bool()
	{
		SearchRequest request = new SearchRequestDescriptor()
			.Query(q => q.Bool(b => b.Boost(1.0f)));

		request.Query.Should().NotBeNull();
		request.Query!.Kind.Should().Be(QueryKind.Bool);
		request.Query.Get<BoolQuery>().Boost.Should().Be(1.0f);
	}

	// ── 5. List of nested union descriptors ──

	[Fact]
	public void BoolQueryDescriptor_MustWithMultipleQueries()
	{
		BoolQuery boolQuery = new BoolQueryDescriptor()
			.Must(
				q => q.Exists(e => e.Field("status")),
				q => q.MatchAll(m => m.Boost(1.0f))
			);

		boolQuery.Must.Should().NotBeNull();
		boolQuery.Must.Should().HaveCount(2);
		boolQuery.Must![0].Kind.Should().Be(QueryKind.Exists);
		boolQuery.Must[1].Kind.Should().Be(QueryKind.MatchAll);
	}

	// ── 6. Deep nesting (Query → Bool → Must → Exists) ──

	[Fact]
	public void SearchRequestDescriptor_DeepNesting()
	{
		SearchRequest request = new SearchRequestDescriptor()
			.Query(q => q.Bool(b => b
				.Must(
					m => m.Exists(e => e.Field("name")),
					m => m.MatchAll(ma => ma.Boost(2.0f))
				)
				.MinimumShouldMatch("1")
			));

		request.Query.Should().NotBeNull();
		var boolQuery = request.Query!.Get<BoolQuery>();
		boolQuery.Must.Should().HaveCount(2);
		boolQuery.Must![0].Kind.Should().Be(QueryKind.Exists);
		boolQuery.Must[0].Get<ExistsQuery>().Field.Should().Be("name");
		boolQuery.Must[1].Kind.Should().Be(QueryKind.MatchAll);
		boolQuery.MinimumShouldMatch.Should().Be("1");
	}

	// ── 7. Request descriptor with path params ──

	[Fact]
	public void DeleteRequestDescriptor_SetsPathParams()
	{
		DeleteRequest request = new DeleteRequestDescriptor()
			.Index("my-index")
			.Id("doc-1");

		request.Index.Should().Be("my-index");
		request.Id.Should().Be("doc-1");
	}

	// ── 8. Request descriptor with raw body ──

	[Fact]
	public void IndexRequestDescriptor_SetsBody()
	{
		var doc = new { title = "Test Doc", value = 42 };
		IndexRequest request = new IndexRequestDescriptor()
			.Index("my-index")
			.Id("1")
			.Body(doc);

		request.Index.Should().Be("my-index");
		request.Id.Should().Be("1");
		request.Body.Should().Be(doc);
	}

	// ── 9. Descriptor → POCO → JSON round-trip ──

	[Fact]
	public void Descriptor_SerializesToCorrectJson()
	{
		SearchRequest request = new SearchRequestDescriptor()
			.Size(5)
			.Query(q => q.MatchAll(m => m.Boost(1.5f)));

		// Size is a query param so it's on the POCO but not in the JSON body
		request.Size.Should().Be(5);

		var json = JsonSerializer.Serialize(request, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("query", out var queryEl).Should().BeTrue();
		queryEl.TryGetProperty("match_all", out var matchAllEl).Should().BeTrue();
		matchAllEl.TryGetProperty("boost", out var boostEl).Should().BeTrue();
		boostEl.GetSingle().Should().Be(1.5f);
	}

	// ── 10. ConstantScore with nested filter ──

	[Fact]
	public void ConstantScoreDescriptor_WithFilterLambda()
	{
		ConstantScoreQuery cs = new ConstantScoreQueryDescriptor()
			.Boost(3.0f)
			.Filter(f => f.Exists(e => e.Field("active")));

		cs.Boost.Should().Be(3.0f);
		cs.Filter.Should().NotBeNull();
		cs.Filter!.Kind.Should().Be(QueryKind.Exists);
		cs.Filter.Get<ExistsQuery>().Field.Should().Be("active");
	}

	// ── 11. Field-keyed convenience: QueryContainer.Term(string, TermQuery) ──

	[Fact]
	public void QueryContainer_Term_FieldKeyed_Convenience()
	{
		var query = QueryContainer.Term("status", new TermQuery { Value = JsonSerializer.SerializeToElement("active") });

		query.Kind.Should().Be(QueryKind.Term);
		var dict = query.Get<Dictionary<string, TermQuery>>();
		dict.Should().ContainKey("status");
		dict["status"].Value!.Value.GetString().Should().Be("active");
	}

	// ── 12. Field-keyed convenience: QueryContainer.Match(string, MatchQuery) ──

	[Fact]
	public void QueryContainer_Match_FieldKeyed_Convenience()
	{
		var query = QueryContainer.Match("title", new MatchQuery { Query = JsonSerializer.SerializeToElement("hello") });

		query.Kind.Should().Be(QueryKind.Match);
		var dict = query.Get<Dictionary<string, MatchQuery>>();
		dict.Should().ContainKey("title");
	}

	// ── 13. Field-keyed descriptor: d.Term("field", t => t.Value(...)) ──

	[Fact]
	public void QueryContainerDescriptor_Term_FieldKeyed_Lambda()
	{
		QueryContainer? query = new QueryContainerDescriptor()
			.Term("status", t => t.Value(JsonSerializer.SerializeToElement("active")));

		query.Should().NotBeNull();
		query!.Kind.Should().Be(QueryKind.Term);
		var dict = query.Get<Dictionary<string, TermQuery>>();
		dict.Should().ContainKey("status");
		dict["status"].Value!.Value.GetString().Should().Be("active");
	}

	// ── 14. Field-keyed descriptor: d.Match("field", m => m.Query(...)) ──

	[Fact]
	public void QueryContainerDescriptor_Match_FieldKeyed_Lambda()
	{
		QueryContainer? query = new QueryContainerDescriptor()
			.Match("title", m => m.Query(JsonSerializer.SerializeToElement("hello")));

		query.Should().NotBeNull();
		query!.Kind.Should().Be(QueryKind.Match);
		var dict = query.Get<Dictionary<string, MatchQuery>>();
		dict.Should().ContainKey("title");
	}

	// ── 15. Field-keyed round-trip: convenience → serialize → verify JSON shape ──

	[Fact]
	public void FieldKeyed_Term_RoundTrip_JsonShape()
	{
		var query = QueryContainer.Term("status", new TermQuery { Value = JsonSerializer.SerializeToElement("active") });

		var json = JsonSerializer.Serialize(query, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("term", out var termEl).Should().BeTrue();
		termEl.TryGetProperty("status", out var statusEl).Should().BeTrue();
		statusEl.TryGetProperty("value", out var valueEl).Should().BeTrue();
		valueEl.GetString().Should().Be("active");
	}

	// ── 16. Verify allOf inheritance: IntegerNumberProperty has inherited fields ──

	[Fact]
	public void IntegerNumberProperty_HasInheritedFields()
	{
		// IntegerNumberProperty should have fields inherited from the allOf chain:
		// PropertyBase → CorePropertyBase → DocValuesPropertyBase → NumberPropertyBase → IntegerNumberProperty
		var prop = new IntegerNumberProperty();
		var type = typeof(IntegerNumberProperty);

		// Check inherited fields exist as properties
		type.GetProperty("DocValues").Should().NotBeNull("inherited from DocValuesPropertyBase");
		type.GetProperty("Store").Should().NotBeNull("inherited from DocValuesPropertyBase");
		type.GetProperty("Index").Should().NotBeNull("inherited from DocValuesPropertyBase");
		type.GetProperty("Meta").Should().NotBeNull("inherited from PropertyBase");
		type.GetProperty("Coerce").Should().NotBeNull("inherited from NumberPropertyBase");
		type.GetProperty("IgnoreMalformed").Should().NotBeNull("inherited from NumberPropertyBase");
		type.GetProperty("NullValue").Should().NotBeNull("own field on IntegerNumberProperty");

		// Should have way more than the original 2 fields
		var propertyCount = type.GetProperties().Length;
		propertyCount.Should().BeGreaterThan(10, "allOf inheritance should flatten ~15+ fields");
	}
}
