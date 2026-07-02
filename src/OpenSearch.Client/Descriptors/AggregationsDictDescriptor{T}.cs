namespace OpenSearch.Client;

/// <summary>
/// Generic companion to <see cref="AggregationsDictDescriptor"/> that provides expression-based
/// field selection (<c>t =&gt; t.Field(x =&gt; x.Category)</c>) through the generic aggregation
/// descriptors. Aggregations whose parameters never select a document field (filters, composite,
/// pipeline aggregations, ...) use the same descriptors as the non-generic builder.
/// </summary>
public sealed class AggregationsDictDescriptor<TDocument>
{
	internal Dictionary<string, AggregationContainer> _dict = new();

	// ── Shared helpers ──

	private AggregationsDictDescriptor<TDocument> AddBucket(string name, AggregationContainer container,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs)
	{
		if (subAggs is not null)
		{
			var sub = new AggregationsDictDescriptor<TDocument>();
			subAggs(sub);
			container.Aggregations = sub._dict;
		}
		_dict[name] = container;
		return this;
	}

	private AggregationsDictDescriptor<TDocument> AddBucket<TDesc>(string name,
		Action<TDesc> configure, Func<TDesc, AggregationContainer> factory,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs) where TDesc : new()
	{
		var desc = new TDesc();
		configure(desc);
		return AddBucket(name, factory(desc), subAggs);
	}

	private AggregationsDictDescriptor<TDocument> AddMetric<TDesc>(string name,
		Action<TDesc> configure, Func<TDesc, AggregationContainer> factory) where TDesc : new()
	{
		var desc = new TDesc();
		configure(desc);
		_dict[name] = factory(desc);
		return this;
	}

	// ── Bucket aggregations ──

	public AggregationsDictDescriptor<TDocument> Terms(string name,
		Action<TermsAggregationFieldsDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Terms(d), subAggs);

	public AggregationsDictDescriptor<TDocument> Nested(string name,
		Action<NestedAggregationFieldsDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Nested(d), subAggs);

	public AggregationsDictDescriptor<TDocument> Filter(string name,
		QueryContainer filter,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, AggregationContainer.Filter(filter), subAggs);

	public AggregationsDictDescriptor<TDocument> Filter(string name,
		Action<QueryContainerDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null)
	{
		var descriptor = new QueryContainerDescriptor<TDocument>();
		configure(descriptor);
		return AddBucket(name, AggregationContainer.Filter((QueryContainer)descriptor!), subAggs);
	}

	public AggregationsDictDescriptor<TDocument> Filters(string name,
		Action<FiltersAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Filters(d), subAggs);

	public AggregationsDictDescriptor<TDocument> Range(string name,
		Action<RangeAggregationFieldsDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Range(d), subAggs);

	public AggregationsDictDescriptor<TDocument> DateRange(string name,
		Action<DateRangeAggregationFieldsDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.DateRange(d), subAggs);

	public AggregationsDictDescriptor<TDocument> Composite(string name,
		Action<CompositeAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Composite(d), subAggs);

	public AggregationsDictDescriptor<TDocument> Missing(string name,
		Action<MissingAggregationFieldsDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Missing(d), subAggs);

	public AggregationsDictDescriptor<TDocument> ReverseNested(string name,
		Action<ReverseNestedAggregationFieldsDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.ReverseNested(d), subAggs);

	public AggregationsDictDescriptor<TDocument> Sampler(string name,
		Action<SamplerAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Sampler(d), subAggs);

	public AggregationsDictDescriptor<TDocument> Children(string name,
		Action<ChildrenAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Children(d), subAggs);

	public AggregationsDictDescriptor<TDocument> SignificantTerms(string name,
		Action<SignificantTermsAggregationFieldsDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.SignificantTerms(d), subAggs);

	public AggregationsDictDescriptor<TDocument> MultiTerms(string name,
		Action<MultiTermsAggregationFieldsDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.MultiTerms(d), subAggs);

	public AggregationsDictDescriptor<TDocument> AdjacencyMatrix(string name,
		Action<AdjacencyMatrixAggregationFieldsDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.AdjacencyMatrix(d), subAggs);

	public AggregationsDictDescriptor<TDocument> DiversifiedSampler(string name,
		Action<DiversifiedSamplerAggregationFieldsDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.DiversifiedSampler(d), subAggs);

	public AggregationsDictDescriptor<TDocument> AutoDateHistogram(string name,
		Action<AutoDateHistogramAggregationFieldsDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.AutoDateHistogram(d), subAggs);

	public AggregationsDictDescriptor<TDocument> GeoDistance(string name,
		Action<GeoDistanceAggregationFieldsDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.GeoDistance(d), subAggs);

	public AggregationsDictDescriptor<TDocument> IpRange(string name,
		Action<IpRangeAggregationFieldsDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.IpRange(d), subAggs);

	public AggregationsDictDescriptor<TDocument> DateHistogram(string name,
		Action<DateHistogramAggregationFieldsDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.DateHistogram(d), subAggs);

	public AggregationsDictDescriptor<TDocument> Histogram(string name,
		Action<HistogramAggregationFieldsDescriptor<TDocument>> configure,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Histogram(d), subAggs);

	/// <summary>A global aggregation has no parameters of its own; it exists to hold sub-aggregations
	/// evaluated over all documents, ignoring the query.</summary>
	public AggregationsDictDescriptor<TDocument> Global(string name,
		Action<AggregationsDictDescriptor<TDocument>>? subAggs = null) =>
		AddBucket(name, AggregationContainer.Global(s_emptyObject), subAggs);

	private static readonly System.Text.Json.JsonElement s_emptyObject =
		System.Text.Json.JsonSerializer.SerializeToElement(new { });

	// ── Metric aggregations ──

	public AggregationsDictDescriptor<TDocument> Avg(string name, Action<AverageAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Avg(d));

	public AggregationsDictDescriptor<TDocument> Max(string name, Action<MaxAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Max(d));

	public AggregationsDictDescriptor<TDocument> Min(string name, Action<MinAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Min(d));

	public AggregationsDictDescriptor<TDocument> Sum(string name, Action<SumAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Sum(d));

	public AggregationsDictDescriptor<TDocument> Cardinality(string name, Action<CardinalityAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Cardinality(d));

	public AggregationsDictDescriptor<TDocument> ValueCount(string name, Action<ValueCountAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.ValueCount(d));

	public AggregationsDictDescriptor<TDocument> Stats(string name, Action<StatsAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Stats(d));

	public AggregationsDictDescriptor<TDocument> ExtendedStats(string name, Action<ExtendedStatsAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.ExtendedStats(d));

	public AggregationsDictDescriptor<TDocument> Percentiles(string name, Action<PercentilesAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Percentiles(d));

	public AggregationsDictDescriptor<TDocument> PercentileRanks(string name, Action<PercentileRanksAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.PercentileRanks(d));

	public AggregationsDictDescriptor<TDocument> TopHits(string name, Action<TopHitsAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.TopHits(d));

	public AggregationsDictDescriptor<TDocument> GeoBounds(string name, Action<GeoBoundsAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.GeoBounds(d));

	public AggregationsDictDescriptor<TDocument> GeoCentroid(string name, Action<GeoCentroidAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.GeoCentroid(d));

	public AggregationsDictDescriptor<TDocument> MedianAbsoluteDeviation(string name, Action<MedianAbsoluteDeviationAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.MedianAbsoluteDeviation(d));

	public AggregationsDictDescriptor<TDocument> ScriptedMetric(string name, Action<ScriptedMetricAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.ScriptedMetric(d));

	public AggregationsDictDescriptor<TDocument> WeightedAvg(string name, Action<WeightedAverageAggregationDescriptor<TDocument>> configure) =>
		AddMetric(name, configure, d => AggregationContainer.WeightedAvg(d));

	// ── Pipeline aggregations ──

	public AggregationsDictDescriptor<TDocument> Derivative(string name, Action<DerivativeAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Derivative(d));

	public AggregationsDictDescriptor<TDocument> CumulativeSum(string name, Action<CumulativeSumAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.CumulativeSum(d));

	public AggregationsDictDescriptor<TDocument> BucketScript(string name, Action<BucketScriptAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.BucketScript(d));

	public AggregationsDictDescriptor<TDocument> BucketSelector(string name, Action<BucketSelectorAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.BucketSelector(d));

	public AggregationsDictDescriptor<TDocument> BucketSort(string name, Action<BucketSortAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.BucketSort(d));

	public AggregationsDictDescriptor<TDocument> SerialDiff(string name, Action<SerialDifferencingAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.SerialDiff(d));

	public static implicit operator Dictionary<string, AggregationContainer>(AggregationsDictDescriptor<TDocument> d) => d._dict;
}
