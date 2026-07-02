using System.Text.Json;
using FluentAssertions;
using OpenSearch.Client;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Pins the union types added in the validator-debt burn-down: every spec union that previously
/// deserialized as raw JsonElement now has a modeled type with both wire forms covered.
/// </summary>
public class ValidatorDebtUnionTests
{
	private static readonly JsonSerializerOptions Options = OpenSearchJsonOptions.Default;

	[Fact]
	public void SingleOrMany_ReadsBothWireForms()
	{
		var single = JsonSerializer.Deserialize<BoolQuery>(
			"""{"must":{"term":{"status":{"value":"active"}}}}""", Options)!;
		single.Must.Should().ContainSingle();

		var many = JsonSerializer.Deserialize<BoolQuery>(
			"""{"must":[{"term":{"status":{"value":"active"}}},{"exists":{"field":"price"}}]}""", Options)!;
		many.Must.Should().HaveCount(2);
	}

	[Fact]
	public void SingleOrMany_WritesArrayForm()
	{
		var query = new BoolQuery { Must = [QueryContainer.Exists(new ExistsQuery { Field = "price" })] };
		var json = JsonSerializer.Serialize(query, Options);
		json.Should().Contain("\"must\":[");
	}

	[Fact]
	public void MatchedQueries_ReadsNamesAndScores()
	{
		var names = JsonSerializer.Deserialize<MatchedQueries>("""["q1","q2"]""", Options)!;
		names.Names.Should().BeEquivalentTo(["q1", "q2"]);
		names.Scores.Should().BeNull();

		var scores = JsonSerializer.Deserialize<MatchedQueries>("""{"q1":1.5,"q2":0.5}""", Options)!;
		scores.Names.Should().BeEquivalentTo(["q1", "q2"]);
		scores.Scores!["q1"].Should().Be(1.5);
	}

	[Fact]
	public void ByteSize_ReadsNumberAndString()
	{
		JsonSerializer.Deserialize<ByteSize>("1024", Options).Bytes.Should().Be(1024);
		JsonSerializer.Deserialize<ByteSize>("\"10gb\"", Options).Text.Should().Be("10gb");
	}

	[Fact]
	public void GeoHashPrecision_RoundTripsBothForms()
	{
		JsonSerializer.Serialize<GeoHashPrecision>(6, Options).Should().Be("6");
		JsonSerializer.Serialize<GeoHashPrecision>("10m", Options).Should().Be("\"10m\"");
		JsonSerializer.Deserialize<GeoHashPrecision>("6", Options).Length.Should().Be(6);
		JsonSerializer.Deserialize<GeoHashPrecision>("\"10m\"", Options).Distance.Should().Be("10m");
	}

	[Fact]
	public void CompletionContext_WritesBareAndObjectForms()
	{
		JsonSerializer.Serialize<CompletionContext>("en", Options).Should().Be("\"en\"");

		var boosted = new CompletionContext { Context = "en", Boost = 2.0, Prefix = true };
		var json = JsonDocument.Parse(JsonSerializer.Serialize(boosted, Options)).RootElement;
		json.GetProperty("context").GetString().Should().Be("en");
		json.GetProperty("boost").GetDouble().Should().Be(2.0);
		json.GetProperty("prefix").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void CompletionContext_ReadsBareAndObjectForms()
	{
		var bare = JsonSerializer.Deserialize<CompletionContext>("\"en\"", Options)!;
		bare.Context!.Category.Should().Be("en");

		var geo = JsonSerializer.Deserialize<CompletionContext>(
			"""{"context":{"lat":52.37,"lon":4.89},"precision":6,"neighbours":[5,"10m"]}""", Options)!;
		geo.Precision!.Value.Length.Should().Be(6);
		geo.Neighbours.Should().HaveCount(2);
	}

	[Fact]
	public void SourceFilter_ReadsShorthandForms()
	{
		JsonSerializer.Deserialize<SourceFilter>("\"title\"", Options)!.Includes.Should().BeEquivalentTo(["title"]);
		JsonSerializer.Deserialize<SourceFilter>("""["title","price"]""", Options)!.Includes.Should().HaveCount(2);

		var full = JsonSerializer.Deserialize<SourceFilter>(
			"""{"includes":"title","excludes":["secret"]}""", Options)!;
		full.Includes.Should().BeEquivalentTo(["title"]);
		full.Excludes.Should().BeEquivalentTo(["secret"]);
	}

	[Fact]
	public void BulkByScrollSliceStatus_SniffsStatusVsError()
	{
		var status = JsonSerializer.Deserialize<BulkByScrollTaskStatusOrException>(
			"""{"batches":1,"deleted":5,"total":5,"noops":0,"version_conflicts":0}""", Options)!;
		status.Status!.Deleted.Should().Be(5);
		status.Exception.Should().BeNull();

		var error = JsonSerializer.Deserialize<BulkByScrollTaskStatusOrException>(
			"""{"type":"exception","reason":"boom"}""", Options)!;
		error.Exception!.Reason.Should().Be("boom");
		error.Status.Should().BeNull();
	}

	[Fact]
	public void Geometry_SerializesShapesAndCollections()
	{
		var collection = Geometry.Collection(
			Geometry.Point(4.89, 52.37),
			new Geometry { Type = "LineString", Coordinates = JsonSerializer.SerializeToElement(new[] { new[] { 0.0, 0.0 }, new[] { 1.0, 1.0 } }) });

		var json = JsonDocument.Parse(JsonSerializer.Serialize(collection, Options)).RootElement;
		json.GetProperty("type").GetString().Should().Be("GeometryCollection");
		var members = json.GetProperty("geometries");
		members.GetArrayLength().Should().Be(2);
		members[0].GetProperty("type").GetString().Should().Be("Point");
		members[0].GetProperty("coordinates")[0].GetDouble().Should().Be(4.89);
	}

	[Fact]
	public void SemanticChunking_ReadsBothForms()
	{
		JsonSerializer.Deserialize<SemanticChunking>("true", Options)!.Enabled.Should().BeTrue();

		var configured = JsonSerializer.Deserialize<SemanticChunking>(
			"""[{"algorithm":"fixed_token_length"}]""", Options)!;
		configured.Strategies.Should().ContainSingle();
	}
}
