using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class AggregationsDictDescriptorTests
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

	[Fact]
	public void Single_Terms_Aggregation()
	{
		SearchRequest request = new SearchRequestDescriptor()
			.Aggregations(a => a
				.Terms("by_status", t => t.Field("status"))
			);

		request.Aggregations.Should().NotBeNull();
		request.Aggregations.Should().ContainKey("by_status");
		request.Aggregations!["by_status"].Kind.Should().Be(AggregationKind.Terms);
	}

	[Fact]
	public void Multiple_Aggregations()
	{
		SearchRequest request = new SearchRequestDescriptor()
			.Aggregations(a => a
				.Terms("by_status", t => t.Field("status"))
				.Avg("avg_price", avg => avg.Field("price"))
				.Max("max_price", max => max.Field("price"))
			);

		request.Aggregations.Should().HaveCount(3);
		request.Aggregations!["by_status"].Kind.Should().Be(AggregationKind.Terms);
		request.Aggregations["avg_price"].Kind.Should().Be(AggregationKind.Avg);
		request.Aggregations["max_price"].Kind.Should().Be(AggregationKind.Max);
	}

	[Fact]
	public void Sub_Aggregation_Nesting()
	{
		SearchRequest request = new SearchRequestDescriptor()
			.Aggregations(a => a
				.Terms("by_status", t => t.Field("status"),
					sub => sub
						.Avg("avg_price", avg => avg.Field("price"))
						.Min("min_price", min => min.Field("price"))
				)
			);

		var outerAgg = request.Aggregations!["by_status"];
		outerAgg.Kind.Should().Be(AggregationKind.Terms);
		outerAgg.Aggregations.Should().NotBeNull();
		outerAgg.Aggregations.Should().HaveCount(2);
		outerAgg.Aggregations!["avg_price"].Kind.Should().Be(AggregationKind.Avg);
		outerAgg.Aggregations["min_price"].Kind.Should().Be(AggregationKind.Min);
	}

	[Fact]
	public void Serialization_Produces_Correct_Json()
	{
		SearchRequest request = new SearchRequestDescriptor()
			.Size(0)
			.Aggregations(a => a
				.Terms("by_status", t => t.Field("status").Size(10))
			);

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var root = doc.RootElement;
		root.TryGetProperty("size", out var size).Should().BeTrue();
		size.GetInt32().Should().Be(0);

		root.TryGetProperty("aggregations", out var aggs).Should().BeTrue();
		aggs.TryGetProperty("by_status", out var byStatus).Should().BeTrue();
		byStatus.TryGetProperty("terms", out var terms).Should().BeTrue();
		terms.TryGetProperty("field", out var field).Should().BeTrue();
		field.GetString().Should().Be("status");
	}

	[Fact]
	public void Generic_Descriptor_With_Aggregations()
	{
		SearchRequest request = new SearchRequestDescriptor<object>()
			.Aggregations(a => a
				.Terms("by_status", t => t.Field("status"))
				.Avg("avg_price", avg => avg.Field("price"))
			);

		request.Aggregations.Should().HaveCount(2);
	}

	[Fact]
	public void Nested_Sub_Aggregation_Serialization()
	{
		SearchRequest request = new SearchRequestDescriptor()
			.Aggregations(a => a
				.Terms("by_status", t => t.Field("status"),
					sub => sub.Avg("avg_price", avg => avg.Field("price"))
				)
			);

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var byStatus = doc.RootElement
			.GetProperty("aggregations")
			.GetProperty("by_status");

		byStatus.TryGetProperty("terms", out _).Should().BeTrue();
		byStatus.TryGetProperty("aggregations", out var subAggs).Should().BeTrue();
		subAggs.TryGetProperty("avg_price", out var avgPrice).Should().BeTrue();
		avgPrice.TryGetProperty("avg", out var avg).Should().BeTrue();
		avg.GetProperty("field").GetString().Should().Be("price");
	}
}
