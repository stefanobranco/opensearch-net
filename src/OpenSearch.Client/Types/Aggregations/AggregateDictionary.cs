using System.Text.Json;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;

namespace OpenSearch.Client;

/// <summary>
/// Wraps a raw aggregation response dictionary and provides typed accessors
/// for metric and bucket aggregation results. Strips typed_keys prefixes
/// (e.g., "sterms#by_status" → "by_status") on construction, preserving the
/// type discriminator for use in typed accessor guards.
/// </summary>
public sealed class AggregateDictionary
{
	private static JsonSerializerOptions BucketOptions => OpenSearchJsonOptions.Default;

	private readonly Dictionary<string, Aggregate> _raw;
	private readonly Dictionary<string, string> _kinds;

	public AggregateDictionary(Dictionary<string, Aggregate>? raw)
	{
		(_raw, _kinds) = StripTypedKeys(raw);
	}

	// ── Metric accessors ──

	// All single-value metric aggs (avg, sum, min, max, cardinality, value_count)
	// return { "value": N } — always read from the Value property.
	public double? Average(string name) => TryGet(name, out var a) ? a.Value : null;
	public double? Sum(string name) => TryGet(name, out var a) ? a.Value : null;
	public double? Min(string name) => TryGet(name, out var a) ? a.Value : null;
	public double? Max(string name) => TryGet(name, out var a) ? a.Value : null;
	public long? Cardinality(string name) => TryGet(name, out var a) ? (long?)a.Value : null;
	public long? ValueCount(string name) => TryGet(name, out var a) ? (long?)a.Value : null;

	public StatsResult? Stats(string name)
	{
		if (!TryGet(name, out var a)) return null;
		return new StatsResult
		{
			Count = a.Count,
			Min = a.Min,
			Max = a.Max,
			Avg = a.Avg,
			Sum = a.Sum,
		};
	}

	public ExtendedStatsResult? ExtendedStats(string name)
	{
		if (!TryGet(name, out var a)) return null;
		return new ExtendedStatsResult
		{
			Count = a.Count,
			Min = a.Min,
			Max = a.Max,
			Avg = a.Avg,
			Sum = a.Sum,
			SumOfSquares = a.SumOfSquares,
			Variance = a.Variance,
			VariancePopulation = a.VariancePopulation,
			VarianceSampling = a.VarianceSampling,
			StdDeviation = a.StdDeviation,
			StdDeviationPopulation = a.StdDeviationPopulation,
			StdDeviationSampling = a.StdDeviationSampling,
		};
	}

	// ── Bucket accessors ──

	public BucketAggregate<TermsBucket>? Terms(string name) =>
		WrapBuckets(ParseBuckets<TermsBucket>(name));

	public BucketAggregate<DateHistogramBucket>? DateHistogram(string name) =>
		WrapBuckets(ParseBuckets<DateHistogramBucket>(name));

	public BucketAggregate<HistogramBucket>? Histogram(string name) =>
		WrapBuckets(ParseBuckets<HistogramBucket>(name));

	public BucketAggregate<RangeBucket>? Range(string name) =>
		WrapBuckets(ParseBuckets<RangeBucket>(name));

	public BucketAggregate<CompositeBucket>? Composite(string name) =>
		WrapBuckets(ParseBuckets<CompositeBucket>(name));

	public BucketAggregate<SignificantTermsBucket>? SignificantTerms(string name) =>
		WrapBuckets(ParseBuckets<SignificantTermsBucket>(name));

	private static BucketAggregate<T>? WrapBuckets<T>(IReadOnlyList<T>? buckets) =>
		buckets is not null ? new BucketAggregate<T>(buckets) : null;

	public FilterBucket? Filter(string name)
	{
		if (!TryGet(name, out var a)) return null;
		if (!IsSingleBucketKind(name)) return null;
		return new FilterBucket
		{
			DocCount = a.DocCount,
			Aggregations = BuildSubAggs(a),
		};
	}

	public NestedBucket? Nested(string name)
	{
		if (!TryGet(name, out var a)) return null;
		if (!IsSingleBucketKind(name)) return null;
		return new NestedBucket
		{
			DocCount = a.DocCount,
			Aggregations = BuildSubAggs(a),
		};
	}

	public ReverseNestedBucket? ReverseNested(string name)
	{
		if (!TryGet(name, out var a)) return null;
		if (!IsSingleBucketKind(name)) return null;
		return new ReverseNestedBucket
		{
			DocCount = a.DocCount,
			Aggregations = BuildSubAggs(a),
		};
	}

	public GlobalBucket? Global(string name)
	{
		if (!TryGet(name, out var a)) return null;
		if (!IsSingleBucketKind(name)) return null;
		return new GlobalBucket
		{
			DocCount = a.DocCount,
			Aggregations = BuildSubAggs(a),
		};
	}

	// ── TopHits accessor ──

	/// <summary>
	/// Returns the top_hits aggregation result, deserializing hit sources as <typeparamref name="TDocument"/>.
	/// </summary>
	public HitsMetadataJsonValue<TDocument>? TopHits<TDocument>(string name)
	{
		if (!TryGet(name, out var a)) return null;
		if (a.Hits is null || a.Hits.Value.ValueKind == JsonValueKind.Undefined) return null;

		return JsonSerializer.Deserialize<HitsMetadataJsonValue<TDocument>>(
			a.Hits.Value, BucketOptions);
	}

	// ── Raw access ──

	/// <summary>Returns all aggregation names in this dictionary.</summary>
	public IReadOnlyCollection<string> Keys => _raw.Keys;

	/// <summary>Returns the number of aggregations.</summary>
	public int Count => _raw.Count;

	/// <summary>Returns whether an aggregation with the given name exists.</summary>
	public bool ContainsKey(string name) => _raw.ContainsKey(name);

	public Aggregate? GetRaw(string name) => TryGet(name, out var a) ? a : null;

	// ── Internals ──

	private bool TryGet(string name, out Aggregate agg)
	{
		agg = default!;
		return _raw.TryGetValue(name, out agg!);
	}


	/// <summary>
	/// Checks whether the aggregation is a single-bucket kind (filter, nested, reverse_nested, global)
	/// as opposed to a multi-bucket kind (terms, histogram, etc.).
	/// When typed_keys is available, uses the type discriminator. Otherwise, falls back to
	/// a structural heuristic: single-bucket aggregates have no buckets array.
	/// </summary>
	private bool IsSingleBucketKind(string name)
	{
		if (_kinds.TryGetValue(name, out var kind))
		{
			// typed_keys discriminator available — use it
			return kind is "filter" or "nested" or "reverse_nested" or "global" or "children" or "sampler";
		}

		// No typed_keys — fall back to structural check:
		// single-bucket aggregates have doc_count + sub-aggs but no buckets array
		if (!TryGet(name, out var a)) return false;
		return a.Buckets is null || a.Buckets.Value.ValueKind != JsonValueKind.Array;
	}

	/// <summary>
	/// Strips typed_keys prefixes and preserves the type discriminator.
	/// When typed_keys=true, OpenSearch returns keys like "sterms#by_status".
	/// We strip the prefix, keep just the name, and store the type separately.
	/// </summary>
	private static (Dictionary<string, Aggregate> Raw, Dictionary<string, string> Kinds) StripTypedKeys(
		Dictionary<string, Aggregate>? raw)
	{
		if (raw is null)
			return (new Dictionary<string, Aggregate>(StringComparer.Ordinal),
				new Dictionary<string, string>(StringComparer.Ordinal));

		var result = new Dictionary<string, Aggregate>(raw.Count, StringComparer.Ordinal);
		var kinds = new Dictionary<string, string>(raw.Count, StringComparer.Ordinal);
		foreach (var (key, value) in raw)
		{
			var (type, name) = ExternallyTaggedUnion.ParseKey(key);
			var resolvedName = name ?? key;
			result[resolvedName] = value;
			if (name is not null) // has typed_keys prefix
				kinds[resolvedName] = type;
		}
		return (result, kinds);
	}

	/// <summary>
	/// Parses the Buckets JsonElement from an aggregate into a typed bucket list.
	/// Each bucket object may contain sub-aggregation fields alongside the bucket's
	/// own fields (key, doc_count, etc.). Unknown properties are collected into a
	/// sub-aggregation dictionary on each bucket.
	/// </summary>
	private IReadOnlyList<TBucket>? ParseBuckets<TBucket>(string name)
		where TBucket : class
	{
		if (!TryGet(name, out var agg)) return null;
		if (agg.Buckets is null || agg.Buckets.Value.ValueKind == JsonValueKind.Undefined)
			return null;

		var bucketsEl = agg.Buckets.Value;
		if (bucketsEl.ValueKind != JsonValueKind.Array)
			return null;

		var buckets = new List<TBucket>();
		foreach (var bucketEl in bucketsEl.EnumerateArray())
		{
			var bucket = JsonSerializer.Deserialize<TBucket>(bucketEl.GetRawText(), BucketOptions);
			if (bucket is null) continue;

			// Parse sub-aggregations from unknown properties
			var subAggs = ParseSubAggregations(bucketEl);
			SetSubAggs(bucket, subAggs);

			buckets.Add(bucket);
		}
		return buckets;
	}

	/// <summary>
	/// Collects sub-aggregation properties from a bucket JSON element.
	/// Known bucket properties (key, doc_count, etc.) are skipped; remaining object
	/// properties are treated as sub-aggregation results.
	/// </summary>
	private static readonly HashSet<string> s_knownBucketProps = new(StringComparer.Ordinal)
	{
		"key", "key_as_string", "doc_count", "doc_count_error_upper_bound",
		"from", "to", "from_as_string", "to_as_string",
		"bg_count", "score"
	};

	private static AggregateDictionary? ParseSubAggregations(JsonElement bucketEl)
	{
		Dictionary<string, Aggregate>? subAggs = null;
		foreach (var prop in bucketEl.EnumerateObject())
		{
			if (s_knownBucketProps.Contains(prop.Name)) continue;
			if (prop.Value.ValueKind != JsonValueKind.Object) continue;

			subAggs ??= new Dictionary<string, Aggregate>(StringComparer.Ordinal);
			var subAgg = JsonSerializer.Deserialize<Aggregate>(
				prop.Value.GetRawText(), BucketOptions);
			if (subAgg is not null)
				subAggs[prop.Name] = subAgg;
		}

		return subAggs is not null ? new AggregateDictionary(subAggs) : null;
	}

	/// <summary>Sets the Aggregations property on a bucket via the <see cref="IBucketWithSubAggregations"/> interface.</summary>
	private static void SetSubAggs<TBucket>(TBucket bucket, AggregateDictionary? subAggs)
	{
		if (subAggs is null) return;

		if (bucket is IBucketWithSubAggregations b)
			b.Aggregations = subAggs;
	}

	/// <summary>
	/// Builds sub-aggregation dictionary from a single-bucket aggregate (filter, nested).
	/// These aggregates don't have a Buckets array — sub-aggs are additional top-level
	/// properties captured via <see cref="Aggregate.ExtensionData"/>.
	/// </summary>
	private static AggregateDictionary? BuildSubAggs(Aggregate agg)
	{
		if (agg.ExtensionData is not { Count: > 0 })
			return null;

		var subAggs = new Dictionary<string, Aggregate>(StringComparer.Ordinal);
		foreach (var (key, element) in agg.ExtensionData)
		{
			if (element.ValueKind != JsonValueKind.Object) continue;

			var subAgg = JsonSerializer.Deserialize<Aggregate>(
				element.GetRawText(), BucketOptions);
			if (subAgg is not null)
				subAggs[key] = subAgg;
		}

		return subAggs.Count > 0 ? new AggregateDictionary(subAggs) : null;
	}
}
