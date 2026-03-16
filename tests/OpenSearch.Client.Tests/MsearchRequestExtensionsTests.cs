using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class MsearchRequestExtensionsTests
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
		Converters = { new JsonEnumConverterFactory() },
	};

	private sealed class MyDoc
	{
		public string? Title { get; set; }
		public int? Price { get; set; }
	}

	[Fact]
	public void AddSearch_WithSearchRequest_MapsBodyFields()
	{
		var request = new MsearchRequest { Index = "default" };

		request.AddSearch("books", new SearchRequest
		{
			Query = QueryContainer.MatchAll(new MatchAllQuery()),
			Size = 10,
			From = 5,
		});

		request.Searches.Should().HaveCount(1);
		request.Searches[0].Header.Index.Should().Be("books");

		var body = request.Searches[0].Body;
		body.Size.Should().Be(10);
		body.From.Should().Be(5);
		body.Query.Should().NotBeNull();
		body.Query!.Value.TryGetProperty("match_all", out _).Should().BeTrue();
	}

	[Fact]
	public void AddSearch_WithDescriptor_BuildsFromFluent()
	{
		var request = new MsearchRequest { Index = "default" };

		request.AddSearch<MyDoc>("products", s => s
			.Query(q => q.MatchAll(_ => { }))
			.Size(20)
			.From(0));

		request.Searches.Should().HaveCount(1);
		request.Searches[0].Header.Index.Should().Be("products");

		var body = request.Searches[0].Body;
		body.Size.Should().Be(20);
		body.From.Should().Be(0);
		body.Query.Should().NotBeNull();
	}

	[Fact]
	public void AddSearch_WithAggregations_MapsToBody()
	{
		var request = new MsearchRequest();

		request.AddSearch("logs", new SearchRequest
		{
			Size = 0,
			Aggregations = new Dictionary<string, AggregationContainer>
			{
				["avg_price"] = AggregationContainer.Avg(new AverageAggregation { Field = "price" }),
			},
		});

		var body = request.Searches[0].Body;
		body.Size.Should().Be(0);
		body.Aggregations.Should().NotBeNull();
		body.Aggregations!.Value.TryGetProperty("avg_price", out var avgEl).Should().BeTrue();
		avgEl.TryGetProperty("avg", out _).Should().BeTrue();
	}

	[Fact]
	public void AddSearch_WithSort_MapsToBody()
	{
		var request = new MsearchRequest();

		request.AddSearch("products", new SearchRequest
		{
			Sort = [SortOptions.Descending("price"), SortOptions.Ascending("name")],
		});

		var body = request.Searches[0].Body;
		body.Sort.Should().NotBeNull();
		body.Sort!.Value.ValueKind.Should().Be(JsonValueKind.Array);
		body.Sort!.Value.GetArrayLength().Should().Be(2);
	}

	[Fact]
	public void AddSearch_WithSource_MapsToBody()
	{
		var request = new MsearchRequest();

		request.AddSearch("idx", new SearchRequest
		{
			Source = SourceConfig.Enabled(false),
		});

		var body = request.Searches[0].Body;
		body.Source.Should().NotBeNull();
	}

	[Fact]
	public void AddSearch_InheritsIndexFromSearchRequest()
	{
		var request = new MsearchRequest();

		request.AddSearch(new SearchRequest
		{
			Index = ["my-index"],
			Size = 5,
		});

		request.Searches[0].Header.Index.Should().Be("my-index");
	}

	[Fact]
	public void AddSearch_MultipleSearches_ChainsCorrectly()
	{
		var request = new MsearchRequest { Index = "default" };

		request
			.AddSearch("books", new SearchRequest { Size = 10 })
			.AddSearch("logs", new SearchRequest { Size = 0 })
			.AddSearch<MyDoc>("products", s => s.Size(5));

		request.Searches.Should().HaveCount(3);
		request.Searches[0].Header.Index.Should().Be("books");
		request.Searches[0].Body.Size.Should().Be(10);
		request.Searches[1].Header.Index.Should().Be("logs");
		request.Searches[1].Body.Size.Should().Be(0);
		request.Searches[2].Header.Index.Should().Be("products");
		request.Searches[2].Body.Size.Should().Be(5);
	}

	[Fact]
	public void AddSearch_NullFields_AreOmitted()
	{
		var request = new MsearchRequest();

		request.AddSearch("idx", new SearchRequest { Size = 10 });

		var body = request.Searches[0].Body;
		body.Query.Should().BeNull();
		body.Aggregations.Should().BeNull();
		body.Sort.Should().BeNull();
		body.Source.Should().BeNull();
		body.Highlight.Should().BeNull();
		body.Suggest.Should().BeNull();
	}

	[Fact]
	public void AddSearch_FullRoundTrip_SerializesAsNdjson()
	{
		var request = new MsearchRequest { Index = "default" };

		request
			.AddSearch("books", new SearchRequest
			{
				Query = QueryContainer.MatchAll(new MatchAllQuery()),
				Size = 5,
			})
			.AddSearch("logs", new SearchRequest
			{
				Size = 0,
			});

		// Verify the MsearchBody serializes correctly
		var firstBody = JsonSerializer.Serialize(request.Searches[0].Body, JsonOptions);
		var doc1 = JsonDocument.Parse(firstBody);
		doc1.RootElement.GetProperty("size").GetInt32().Should().Be(5);
		doc1.RootElement.TryGetProperty("query", out var q).Should().BeTrue();
		q.TryGetProperty("match_all", out _).Should().BeTrue();

		var secondBody = JsonSerializer.Serialize(request.Searches[1].Body, JsonOptions);
		var doc2 = JsonDocument.Parse(secondBody);
		doc2.RootElement.GetProperty("size").GetInt32().Should().Be(0);
		doc2.RootElement.TryGetProperty("query", out _).Should().BeFalse();
	}
}
