using System.Text.Json;
using OpenSearch.Client.Core;

namespace OpenSearch.Client.Common;

/// <summary>
/// Builds a <c>Dictionary&lt;string, AggregationContainer&gt;</c> with named aggregations.
/// Bucket aggregations support sub-aggregation nesting.
/// </summary>
public sealed class AggregationsDictDescriptor
{
	internal Dictionary<string, AggregationContainer> _dict = new();

	// ── Shared helpers ──

	private AggregationsDictDescriptor AddBucket(string name, AggregationContainer container,
		Action<AggregationsDictDescriptor>? subAggs)
	{
		if (subAggs is not null)
		{
			var sub = new AggregationsDictDescriptor();
			subAggs(sub);
			container.Aggregations = sub._dict;
		}
		_dict[name] = container;
		return this;
	}

	private AggregationsDictDescriptor AddBucket<TDesc>(string name,
		Action<TDesc> configure, Func<TDesc, AggregationContainer> factory,
		Action<AggregationsDictDescriptor>? subAggs) where TDesc : new()
	{
		var desc = new TDesc();
		configure(desc);
		return AddBucket(name, factory(desc), subAggs);
	}

	private AggregationsDictDescriptor AddMetric<TDesc>(string name,
		Action<TDesc> configure, Func<TDesc, AggregationContainer> factory) where TDesc : new()
	{
		var desc = new TDesc();
		configure(desc);
		_dict[name] = factory(desc);
		return this;
	}

	// ── Bucket aggregations ──

	public AggregationsDictDescriptor Terms(string name,
		Action<TermsAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Terms(d), subAggs);

	public AggregationsDictDescriptor Nested(string name,
		Action<NestedAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Nested(d), subAggs);

	public AggregationsDictDescriptor Filter(string name,
		QueryContainer filter,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var element = JsonSerializer.SerializeToElement(filter);
		return AddBucket(name, AggregationContainer.Filter(element), subAggs);
	}

	public AggregationsDictDescriptor Filter(string name,
		Action<QueryContainerDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var descriptor = new QueryContainerDescriptor();
		configure(descriptor);
		QueryContainer? filter = descriptor;
		var element = JsonSerializer.SerializeToElement(filter);
		return AddBucket(name, AggregationContainer.Filter(element), subAggs);
	}

	public AggregationsDictDescriptor Filters(string name,
		Action<FiltersAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Filters(d), subAggs);

	public AggregationsDictDescriptor Range(string name,
		Action<RangeAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Range(d), subAggs);

	public AggregationsDictDescriptor DateRange(string name,
		Action<DateRangeAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.DateRange(d), subAggs);

	public AggregationsDictDescriptor Composite(string name,
		Action<CompositeAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Composite(d), subAggs);

	public AggregationsDictDescriptor Missing(string name,
		Action<MissingAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Missing(d), subAggs);

	public AggregationsDictDescriptor ReverseNested(string name,
		Action<ReverseNestedAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.ReverseNested(d), subAggs);

	public AggregationsDictDescriptor Sampler(string name,
		Action<SamplerAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Sampler(d), subAggs);

	public AggregationsDictDescriptor Children(string name,
		Action<ChildrenAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.Children(d), subAggs);

	public AggregationsDictDescriptor SignificantTerms(string name,
		Action<SignificantTermsAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.SignificantTerms(d), subAggs);

	public AggregationsDictDescriptor MultiTerms(string name,
		Action<MultiTermsAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.MultiTerms(d), subAggs);

	public AggregationsDictDescriptor AdjacencyMatrix(string name,
		Action<AdjacencyMatrixAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.AdjacencyMatrix(d), subAggs);

	public AggregationsDictDescriptor DiversifiedSampler(string name,
		Action<DiversifiedSamplerAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.DiversifiedSampler(d), subAggs);

	public AggregationsDictDescriptor AutoDateHistogram(string name,
		Action<AutoDateHistogramAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.AutoDateHistogram(d), subAggs);

	public AggregationsDictDescriptor GeoDistance(string name,
		Action<GeoDistanceAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.GeoDistance(d), subAggs);

	public AggregationsDictDescriptor IpRange(string name,
		Action<IpRangeAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null) =>
		AddBucket(name, configure, d => AggregationContainer.IpRange(d), subAggs);

	public AggregationsDictDescriptor DateHistogram<T>(string name,
		Action<DateHistogramAggregationFieldsDescriptor<T>> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new DateHistogramAggregationFieldsDescriptor<T>();
		configure(desc);
		var element = JsonSerializer.SerializeToElement((DateHistogramAggregationFields<T>)desc);
		return AddBucket(name, AggregationContainer.DateHistogram(element), subAggs);
	}

	public AggregationsDictDescriptor Histogram<T>(string name,
		Action<HistogramAggregationFieldsDescriptor<T>> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new HistogramAggregationFieldsDescriptor<T>();
		configure(desc);
		var element = JsonSerializer.SerializeToElement((HistogramAggregationFields<T>)desc);
		return AddBucket(name, AggregationContainer.Histogram(element), subAggs);
	}

	// ── Metric aggregations ──

	public AggregationsDictDescriptor Avg(string name, Action<AverageAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Avg(d));

	public AggregationsDictDescriptor Max(string name, Action<MaxAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Max(d));

	public AggregationsDictDescriptor Min(string name, Action<MinAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Min(d));

	public AggregationsDictDescriptor Sum(string name, Action<SumAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Sum(d));

	public AggregationsDictDescriptor Cardinality(string name, Action<CardinalityAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Cardinality(d));

	public AggregationsDictDescriptor ValueCount(string name, Action<ValueCountAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.ValueCount(d));

	public AggregationsDictDescriptor Stats(string name, Action<StatsAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Stats(d));

	public AggregationsDictDescriptor ExtendedStats(string name, Action<ExtendedStatsAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.ExtendedStats(d));

	public AggregationsDictDescriptor Percentiles(string name, Action<PercentilesAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Percentiles(d));

	public AggregationsDictDescriptor PercentileRanks(string name, Action<PercentileRanksAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.PercentileRanks(d));

	public AggregationsDictDescriptor TopHits(string name, Action<TopHitsAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.TopHits(d));

	public AggregationsDictDescriptor GeoBounds(string name, Action<GeoBoundsAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.GeoBounds(d));

	public AggregationsDictDescriptor GeoCentroid(string name, Action<GeoCentroidAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.GeoCentroid(d));

	public AggregationsDictDescriptor MedianAbsoluteDeviation(string name, Action<MedianAbsoluteDeviationAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.MedianAbsoluteDeviation(d));

	public AggregationsDictDescriptor ScriptedMetric(string name, Action<ScriptedMetricAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.ScriptedMetric(d));

	public AggregationsDictDescriptor WeightedAvg(string name, Action<WeightedAverageAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.WeightedAvg(d));

	// ── Pipeline aggregations ──

	public AggregationsDictDescriptor Derivative(string name, Action<DerivativeAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.Derivative(d));

	public AggregationsDictDescriptor CumulativeSum(string name, Action<CumulativeSumAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.CumulativeSum(d));

	public AggregationsDictDescriptor BucketScript(string name, Action<BucketScriptAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.BucketScript(d));

	public AggregationsDictDescriptor BucketSelector(string name, Action<BucketSelectorAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.BucketSelector(d));

	public AggregationsDictDescriptor BucketSort(string name, Action<BucketSortAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.BucketSort(d));

	public AggregationsDictDescriptor SerialDiff(string name, Action<SerialDifferencingAggregationDescriptor> configure) =>
		AddMetric(name, configure, d => AggregationContainer.SerialDiff(d));

	public static implicit operator Dictionary<string, AggregationContainer>(AggregationsDictDescriptor d) => d._dict;
}
