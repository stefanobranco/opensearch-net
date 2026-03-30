using System.Text.Json;
using System.Text.Json.Serialization;
using OpenSearch.Net;

namespace OpenSearch.Client.Core;

/// <summary>Response from the msearch API.</summary>
public sealed class MsearchResponse : OpenSearchResponse
{
	/// <summary>How long the operation took, in milliseconds.</summary>
	public long Took { get; set; }

	/// <summary>The list of search responses, one per search in the request.</summary>
	public List<MsearchResponseItem>? Responses { get; set; }

	/// <summary>
	/// Returns typed search responses for all items in the multi-search response.
	/// Each item's hits are deserialized into <typeparamref name="T"/>.
	/// </summary>
	public IReadOnlyList<MsearchTypedResponse<T>> GetResponses<T>(JsonSerializerOptions? options = null)
	{
		if (Responses is null) return [];
		var result = new List<MsearchTypedResponse<T>>(Responses.Count);
		foreach (var item in Responses)
		{
			result.Add(new MsearchTypedResponse<T>
			{
				Status = item.Status,
				Took = item.Took,
				TimedOut = item.TimedOut,
				Hits = item.GetHits<T>(options),
				Aggregations = item.GetAggregations(),
				Suggest = item.GetSuggest<T>(),
				Error = item.Error,
			});
			// Release raw JsonElement references so the backing JsonDocument can be GC'd.
			item.ReleaseRawData();
		}
		return result;
	}
}

/// <summary>A typed multi-search response item with deserialized hits.</summary>
public sealed class MsearchTypedResponse<T>
{
	public int? Status { get; init; }
	public long? Took { get; init; }
	public bool? TimedOut { get; init; }
	public IReadOnlyList<Hit<T>> Hits { get; init; } = [];
	public AggregateDictionary? Aggregations { get; init; }
	public JsonElement? Error { get; init; }

	/// <summary>Raw suggest data from this search response item.</summary>
	public SuggestDictionary<T>? Suggest { get; init; }

	/// <summary>Whether this individual search response is successful (2xx and no error).</summary>
	public bool IsValid => Status is null or (>= 200 and < 300) && Error is null;

	/// <summary>Returns the source documents from all hits.</summary>
	public IReadOnlyList<T> Documents => Hits
		.Where(h => h.Source is not null)
		.Select(h => h.Source!)
		.ToList();
}

/// <summary>A single search response within a multi-search response. On failure, <see cref="Error"/> is populated instead of <see cref="Hits"/>.</summary>
public sealed class MsearchResponseItem
{
	private static JsonSerializerOptions DefaultOptions => OpenSearchJsonOptions.Default;

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
		var opts = options ?? DefaultOptions;
		var result = new List<Hit<T>>(Hits.Hits.Count);
		foreach (var el in Hits.Hits)
		{
			var hit = JsonSerializer.Deserialize<Hit<T>>(el, opts);
			if (hit is not null) result.Add(hit);
		}
		return result;
	}

	/// <summary>
	/// Parses the raw suggest JsonElement into a typed <see cref="SuggestDictionary{T}"/>.
	/// </summary>
	public SuggestDictionary<T>? GetSuggest<T>()
	{
		if (Suggest is null || Suggest.Value.ValueKind == JsonValueKind.Undefined)
			return null;

		var raw = JsonSerializer.Deserialize<Dictionary<string, List<JsonElement>>>(
			Suggest.Value, DefaultOptions);
		return new SuggestDictionary<T>(raw);
	}

	/// <summary>
	/// Parses the raw aggregations JsonElement into a typed <see cref="AggregateDictionary"/>.
	/// </summary>
	public AggregateDictionary GetAggregations()
	{
		if (Aggregations is null || Aggregations.Value.ValueKind == JsonValueKind.Undefined)
			return new AggregateDictionary(null);

		var raw = JsonSerializer.Deserialize<Dictionary<string, Common.Aggregate>>(
			Aggregations.Value, DefaultOptions);
		return new AggregateDictionary(raw);
	}

	/// <summary>
	/// Releases raw <see cref="JsonElement"/> fields so the backing <see cref="JsonDocument"/> can be GC'd.
	/// Called after typed extraction to prevent the full response JSON from being retained in memory.
	/// </summary>
	internal void ReleaseRawData()
	{
		Hits = null;
		Aggregations = null;
		Suggest = null;
		Shards = null;
		Error = null;
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
