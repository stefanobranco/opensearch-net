using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client.Core;

/// <summary>Response from the msearch API.</summary>
public sealed class MsearchResponse
{
	/// <summary>How long the operation took, in milliseconds.</summary>
	public long Took { get; set; }

	/// <summary>The list of search responses, one per search in the request.</summary>
	public List<MsearchResponseItem>? Responses { get; set; }
}

/// <summary>A single search response within a multi-search response. On failure, <see cref="Error"/> is populated instead of <see cref="Hits"/>.</summary>
public sealed class MsearchResponseItem
{
	private static readonly JsonSerializerOptions s_options = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
	};

	public int? Status { get; set; }

	public JsonElement? Error { get; set; }

	public long? Took { get; set; }

	[JsonPropertyName("timed_out")]
	public bool? TimedOut { get; set; }

	[JsonPropertyName("_shards")]
	public JsonElement? Shards { get; set; }

	public MsearchHitsMetadata? Hits { get; set; }

	public JsonElement? Aggregations { get; set; }

	public JsonElement? Suggest { get; set; }

	/// <summary>
	/// Deserializes the raw hits into typed <see cref="Hit{T}"/> objects.
	/// </summary>
	public IReadOnlyList<Hit<T>> GetHits<T>(JsonSerializerOptions? options = null)
	{
		if (Hits?.Hits is null) return [];
		var opts = options ?? s_options;
		return Hits.Hits
			.Select(el => JsonSerializer.Deserialize<Hit<T>>(el.GetRawText(), opts))
			.Where(h => h is not null)
			.Select(h => h!)
			.ToList();
	}

	/// <summary>
	/// Parses the raw aggregations JsonElement into a typed <see cref="AggregateDictionary"/>.
	/// </summary>
	public AggregateDictionary GetAggregations()
	{
		if (Aggregations is null || Aggregations.Value.ValueKind == JsonValueKind.Undefined)
			return new AggregateDictionary(null);

		var raw = JsonSerializer.Deserialize<Dictionary<string, Common.Aggregate<JsonElement>>>(
			Aggregations.Value.GetRawText(), s_options);
		return new AggregateDictionary(raw);
	}
}

/// <summary>Hits metadata in a multi-search response item (non-generic since sub-searches can target different types).</summary>
public sealed class MsearchHitsMetadata
{
	public TotalHits? Total { get; set; }

	[JsonPropertyName("max_score")]
	public double? MaxScore { get; set; }

	public List<JsonElement>? Hits { get; set; }
}
