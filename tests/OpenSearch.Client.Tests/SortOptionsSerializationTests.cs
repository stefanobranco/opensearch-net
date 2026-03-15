using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class SortOptionsSerializationTests
{
	private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

	private static JsonSerializerOptions CreateOptions()
	{
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		};
		options.Converters.Add(new JsonEnumConverterFactory());
		return options;
	}

	[Fact]
	public void SortOptions_FieldSort_SerializesAsSingleKeyDict()
	{
		var sort = SortOptions.Ascending("price");
		var json = JsonSerializer.Serialize(sort, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("price", out var priceEl).Should().BeTrue();
		priceEl.TryGetProperty("order", out var orderEl).Should().BeTrue();
		orderEl.GetString().Should().Be("asc");
	}

	[Fact]
	public void SortOptions_ScoreSort_SerializesWithKey()
	{
		var sort = SortOptions.Score(new ScoreSort { Order = SortOrder.Desc });
		var json = JsonSerializer.Serialize(sort, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("_score", out var scoreEl).Should().BeTrue();
		scoreEl.TryGetProperty("order", out var orderEl).Should().BeTrue();
		orderEl.GetString().Should().Be("desc");
	}

	[Fact]
	public void SortOptions_DocSort_SerializesWithDocKey()
	{
		var sort = SortOptions.Doc();
		var json = JsonSerializer.Serialize(sort, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("_doc", out _).Should().BeTrue();
	}

	[Fact]
	public void SortOptions_Deserialize_StringToken_ReturnsFieldSort()
	{
		var json = "\"price\"";
		var sort = JsonSerializer.Deserialize<SortOptions>(json, JsonOptions);

		sort.Should().NotBeNull();
		sort!.VariantKind.Should().Be(SortOptions.SortKind.Field);
		var fieldSort = (FieldSort)sort.Variant;
		fieldSort.FieldName.Should().Be("price");
	}

	[Fact]
	public void SortOptions_Deserialize_ScoreObject_ReturnsScoreSort()
	{
		var json = """{"_score": {"order": "desc"}}""";
		var sort = JsonSerializer.Deserialize<SortOptions>(json, JsonOptions);

		sort.Should().NotBeNull();
		sort!.VariantKind.Should().Be(SortOptions.SortKind.Score);
		var scoreSort = (ScoreSort)sort.Variant;
		scoreSort.Order.Should().Be(SortOrder.Desc);
	}

	[Fact]
	public void SortOptions_Deserialize_FieldObject_ReturnsFieldSort()
	{
		var json = """{"price": {"order": "asc"}}""";
		var sort = JsonSerializer.Deserialize<SortOptions>(json, JsonOptions);

		sort.Should().NotBeNull();
		sort!.VariantKind.Should().Be(SortOptions.SortKind.Field);
		var fieldSort = (FieldSort)sort.Variant;
		fieldSort.FieldName.Should().Be("price");
		fieldSort.Order.Should().Be(SortOrder.Asc);
	}

	[Fact]
	public void SortOptions_Deserialize_FieldShorthand_ReturnsFieldSort()
	{
		var json = """{"price": "asc"}""";
		var sort = JsonSerializer.Deserialize<SortOptions>(json, JsonOptions);

		sort.Should().NotBeNull();
		sort!.VariantKind.Should().Be(SortOptions.SortKind.Field);
		var fieldSort = (FieldSort)sort.Variant;
		fieldSort.FieldName.Should().Be("price");
		fieldSort.Order.Should().Be(SortOrder.Asc);
	}

	[Fact]
	public void SortOptions_RoundTrip_List()
	{
		var original = new List<SortOptions>
		{
			SortOptions.Ascending("price"),
			SortOptions.Score(new ScoreSort { Order = SortOrder.Desc }),
		};

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<List<SortOptions>>(json, JsonOptions);

		deserialized.Should().NotBeNull();
		deserialized.Should().HaveCount(2);
		deserialized![0].VariantKind.Should().Be(SortOptions.SortKind.Field);
		deserialized[1].VariantKind.Should().Be(SortOptions.SortKind.Score);
	}

	[Fact]
	public void SortOptions_ImplicitConversion_FromString()
	{
		SortOptions sort = "price";
		sort.VariantKind.Should().Be(SortOptions.SortKind.Field);
		var fieldSort = (FieldSort)sort.Variant;
		fieldSort.FieldName.Should().Be("price");
	}
}
