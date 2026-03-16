using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class QueryContainerSerializationTests
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

	// ── MatchAll query ──

	[Fact]
	public void Serialize_MatchAllQuery_ProducesExternalTag()
	{
		var query = QueryContainer.MatchAll(new MatchAllQuery { Boost = 1.0f });

		var json = JsonSerializer.Serialize(query, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("match_all", out var matchAll).Should().BeTrue();
		matchAll.TryGetProperty("boost", out var boost).Should().BeTrue();
		boost.GetSingle().Should().Be(1.0f);
	}

	[Fact]
	public void Deserialize_MatchAllQuery_ResolvesCorrectKind()
	{
		var json = """{ "match_all": { "boost": 2.0 } }""";

		var query = JsonSerializer.Deserialize<QueryContainer>(json, JsonOptions);

		query.Should().NotBeNull();
		query!.Kind.Should().Be(QueryKind.MatchAll);
		var matchAll = query.Get<MatchAllQuery>();
		matchAll.Boost.Should().Be(2.0f);
	}

	[Fact]
	public void RoundTrip_MatchAllQuery()
	{
		var original = QueryContainer.MatchAll(new MatchAllQuery { Boost = 1.5f, Name = "my_match_all" });

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<QueryContainer>(json, JsonOptions);

		deserialized.Should().NotBeNull();
		deserialized!.Kind.Should().Be(QueryKind.MatchAll);
		var matchAll = deserialized.Get<MatchAllQuery>();
		matchAll.Boost.Should().Be(1.5f);
		matchAll.Name.Should().Be("my_match_all");
	}

	// ── MatchNone query ──

	[Fact]
	public void Serialize_MatchNoneQuery_ProducesExternalTag()
	{
		var query = QueryContainer.MatchNone(new MatchNoneQuery());

		var json = JsonSerializer.Serialize(query, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("match_none", out _).Should().BeTrue();
	}

	[Fact]
	public void Deserialize_MatchNoneQuery_ResolvesCorrectKind()
	{
		var json = """{ "match_none": {} }""";

		var query = JsonSerializer.Deserialize<QueryContainer>(json, JsonOptions);

		query.Should().NotBeNull();
		query!.Kind.Should().Be(QueryKind.MatchNone);
	}

	// ── Exists query ──

	[Fact]
	public void Serialize_ExistsQuery_IncludesField()
	{
		var query = QueryContainer.Exists(new ExistsQuery { Field = "status" });

		var json = JsonSerializer.Serialize(query, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("exists", out var exists).Should().BeTrue();
		exists.TryGetProperty("field", out var field).Should().BeTrue();
		field.GetString().Should().Be("status");
	}

	[Fact]
	public void Deserialize_ExistsQuery_ParsesField()
	{
		var json = """{ "exists": { "field": "email" } }""";

		var query = JsonSerializer.Deserialize<QueryContainer>(json, JsonOptions);

		query.Should().NotBeNull();
		query!.Kind.Should().Be(QueryKind.Exists);
		var exists = query.Get<ExistsQuery>();
		exists.Field.Should().Be("email");
	}

	[Fact]
	public void RoundTrip_ExistsQuery()
	{
		var original = QueryContainer.Exists(new ExistsQuery
		{
			Field = "user.name",
			Boost = 1.2f,
		});

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<QueryContainer>(json, JsonOptions);

		deserialized.Should().NotBeNull();
		deserialized!.Kind.Should().Be(QueryKind.Exists);
		var exists = deserialized.Get<ExistsQuery>();
		exists.Field.Should().Be("user.name");
		exists.Boost.Should().Be(1.2f);
	}

	// ── Ids query ──

	[Fact]
	public void Serialize_IdsQuery_IncludesValues()
	{
		var query = QueryContainer.Ids(new IdsQuery { Values = ["1", "2", "3"] });

		var json = JsonSerializer.Serialize(query, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("ids", out var ids).Should().BeTrue();
		ids.TryGetProperty("values", out var values).Should().BeTrue();
		values.GetArrayLength().Should().Be(3);
	}

	[Fact]
	public void Deserialize_IdsQuery_ParsesValues()
	{
		var json = """{ "ids": { "values": ["doc-a", "doc-b"] } }""";

		var query = JsonSerializer.Deserialize<QueryContainer>(json, JsonOptions);

		query.Should().NotBeNull();
		query!.Kind.Should().Be(QueryKind.Ids);
		var ids = query.Get<IdsQuery>();
		ids.Values.Should().NotBeNull();
		ids.Values.Should().BeEquivalentTo(new[] { "doc-a", "doc-b" });
	}

	// ── Bool query ──

	[Fact]
	public void Serialize_BoolQuery_ProducesCorrectStructure()
	{
		var query = QueryContainer.Bool(new BoolQuery
		{
			Boost = 1.0f,
			AdjustPureNegative = true,
		});

		var json = JsonSerializer.Serialize(query, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("bool", out var boolEl).Should().BeTrue();
		boolEl.TryGetProperty("boost", out var boost).Should().BeTrue();
		boost.GetSingle().Should().Be(1.0f);
		boolEl.TryGetProperty("adjust_pure_negative", out var apn).Should().BeTrue();
		apn.GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void Deserialize_BoolQuery_ResolvesKind()
	{
		var json = """
		{
			"bool": {
				"boost": 1.0,
				"adjust_pure_negative": false
			}
		}
		""";

		var query = JsonSerializer.Deserialize<QueryContainer>(json, JsonOptions);

		query.Should().NotBeNull();
		query!.Kind.Should().Be(QueryKind.Bool);
		var boolQuery = query.Get<BoolQuery>();
		boolQuery.Boost.Should().Be(1.0f);
		boolQuery.AdjustPureNegative.Should().BeFalse();
	}

	[Fact]
	public void RoundTrip_BoolQuery()
	{
		var original = QueryContainer.Bool(new BoolQuery
		{
			Boost = 2.0f,
			Name = "my_bool",
			AdjustPureNegative = true,
		});

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<QueryContainer>(json, JsonOptions);

		deserialized.Should().NotBeNull();
		deserialized!.Kind.Should().Be(QueryKind.Bool);
		var boolQuery = deserialized.Get<BoolQuery>();
		boolQuery.Boost.Should().Be(2.0f);
		boolQuery.Name.Should().Be("my_bool");
		boolQuery.AdjustPureNegative.Should().BeTrue();
	}

	// ── ConstantScore query ──

	[Fact]
	public void Serialize_ConstantScoreQuery_WithNestedFilter()
	{
		var query = QueryContainer.ConstantScore(new ConstantScoreQuery
		{
			Boost = 1.2f,
			Filter = QueryContainer.Exists(new ExistsQuery { Field = "status" }),
		});

		var json = JsonSerializer.Serialize(query, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("constant_score", out var cs).Should().BeTrue();
		cs.TryGetProperty("boost", out var boost).Should().BeTrue();
		boost.GetSingle().Should().Be(1.2f);
		cs.TryGetProperty("filter", out var filter).Should().BeTrue();
		filter.TryGetProperty("exists", out var exists).Should().BeTrue();
		exists.TryGetProperty("field", out var field).Should().BeTrue();
		field.GetString().Should().Be("status");
	}

	[Fact]
	public void Deserialize_ConstantScoreQuery_WithNestedFilter()
	{
		var json = """
		{
			"constant_score": {
				"boost": 1.5,
				"filter": {
					"exists": { "field": "email" }
				}
			}
		}
		""";

		var query = JsonSerializer.Deserialize<QueryContainer>(json, JsonOptions);

		query.Should().NotBeNull();
		query!.Kind.Should().Be(QueryKind.ConstantScore);
		var cs = query.Get<ConstantScoreQuery>();
		cs.Boost.Should().Be(1.5f);
		cs.Filter.Should().NotBeNull();
		cs.Filter!.Kind.Should().Be(QueryKind.Exists);
		cs.Filter.Get<ExistsQuery>().Field.Should().Be("email");
	}

	[Fact]
	public void RoundTrip_ConstantScoreQuery_WithNestedFilter()
	{
		var original = QueryContainer.ConstantScore(new ConstantScoreQuery
		{
			Boost = 3.0f,
			Filter = QueryContainer.MatchAll(new MatchAllQuery()),
		});

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<QueryContainer>(json, JsonOptions);

		deserialized.Should().NotBeNull();
		deserialized!.Kind.Should().Be(QueryKind.ConstantScore);
		var cs = deserialized.Get<ConstantScoreQuery>();
		cs.Boost.Should().Be(3.0f);
		cs.Filter!.Kind.Should().Be(QueryKind.MatchAll);
	}

	// ── Boosting query ──

	[Fact]
	public void RoundTrip_BoostingQuery_WithNestedQueries()
	{
		var original = QueryContainer.Boosting(new BoostingQuery
		{
			NegativeBoost = 0.5f,
			Positive = QueryContainer.MatchAll(new MatchAllQuery()),
			Negative = QueryContainer.Exists(new ExistsQuery { Field = "deprecated" }),
		});

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<QueryContainer>(json, JsonOptions);

		deserialized.Should().NotBeNull();
		deserialized!.Kind.Should().Be(QueryKind.Boosting);
		var boosting = deserialized.Get<BoostingQuery>();
		boosting.NegativeBoost.Should().Be(0.5f);
		boosting.Positive!.Kind.Should().Be(QueryKind.MatchAll);
		boosting.Negative!.Kind.Should().Be(QueryKind.Exists);
		boosting.Negative.Get<ExistsQuery>().Field.Should().Be("deprecated");
	}

	// ── Terms query ──

	[Fact]
	public void RoundTrip_TermsQuery()
	{
		var original = QueryContainer.Terms(new TermsQuery
		{
			Boost = 1.0f,
			Name = "status_filter",
		});

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<QueryContainer>(json, JsonOptions);

		deserialized.Should().NotBeNull();
		deserialized!.Kind.Should().Be(QueryKind.Terms);
		var terms = deserialized.Get<TermsQuery>();
		terms.Boost.Should().Be(1.0f);
		terms.Name.Should().Be("status_filter");
	}

	// ── Edge cases ──

	[Fact]
	public void Deserialize_QueryContainer_UnknownVariant_ThrowsJsonException()
	{
		var json = """{ "unknown_query_type": { "field": "value" } }""";

		var act = () => JsonSerializer.Deserialize<QueryContainer>(json, JsonOptions);

		act.Should().Throw<JsonException>()
			.Which.Message.Should().Contain("Unknown QueryContainer variant");
	}

	[Fact]
	public void Serialize_QueryContainer_OmitsNullFields()
	{
		var query = QueryContainer.MatchAll(new MatchAllQuery());

		var json = JsonSerializer.Serialize(query, JsonOptions);

		// With WhenWritingNull, the empty MatchAllQuery should not have boost or _name
		json.Should().NotContain("\"boost\"");
		json.Should().NotContain("\"_name\"");
	}

	[Fact]
	public void Deserialize_QueryContainer_Null_ReturnsNull()
	{
		var json = "null";

		var query = JsonSerializer.Deserialize<QueryContainer>(json, JsonOptions);

		query.Should().BeNull();
	}

	[Fact]
	public void Serialize_QueryContainer_WritesExactWireName()
	{
		// Verify that the tagged union converter uses the exact wire names
		// (e.g., "match_all" not "MatchAll")
		var query = QueryContainer.MatchAll(new MatchAllQuery());
		var json = JsonSerializer.Serialize(query, JsonOptions);

		json.Should().Contain("\"match_all\"");
		json.Should().NotContain("\"MatchAll\"");
	}
}
