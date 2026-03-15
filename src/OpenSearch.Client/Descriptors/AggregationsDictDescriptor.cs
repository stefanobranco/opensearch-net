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

	// ── Helper for bucket aggs (with sub-aggregation support) ──

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

	// ── Bucket aggregations ──

	public AggregationsDictDescriptor Terms(string name,
		Action<TermsAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new TermsAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.Terms((TermsAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor Nested(string name,
		Action<NestedAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new NestedAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.Nested((NestedAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor Filter(string name,
		QueryContainer filter,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var element = JsonSerializer.SerializeToElement(filter);
		return AddBucket(name, AggregationContainer.Filter(element), subAggs);
	}

	public AggregationsDictDescriptor Filters(string name,
		Action<FiltersAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new FiltersAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.Filters((FiltersAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor Range(string name,
		Action<RangeAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new RangeAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.Range((RangeAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor DateRange(string name,
		Action<DateRangeAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new DateRangeAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.DateRange((DateRangeAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor Composite(string name,
		Action<CompositeAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new CompositeAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.Composite((CompositeAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor Missing(string name,
		Action<MissingAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new MissingAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.Missing((MissingAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor ReverseNested(string name,
		Action<ReverseNestedAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new ReverseNestedAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.ReverseNested((ReverseNestedAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor Sampler(string name,
		Action<SamplerAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new SamplerAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.Sampler((SamplerAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor Children(string name,
		Action<ChildrenAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new ChildrenAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.Children((ChildrenAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor SignificantTerms(string name,
		Action<SignificantTermsAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new SignificantTermsAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.SignificantTerms((SignificantTermsAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor MultiTerms(string name,
		Action<MultiTermsAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new MultiTermsAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.MultiTerms((MultiTermsAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor AdjacencyMatrix(string name,
		Action<AdjacencyMatrixAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new AdjacencyMatrixAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.AdjacencyMatrix((AdjacencyMatrixAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor DiversifiedSampler(string name,
		Action<DiversifiedSamplerAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new DiversifiedSamplerAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.DiversifiedSampler((DiversifiedSamplerAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor AutoDateHistogram(string name,
		Action<AutoDateHistogramAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new AutoDateHistogramAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.AutoDateHistogram((AutoDateHistogramAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor GeoDistance(string name,
		Action<GeoDistanceAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new GeoDistanceAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.GeoDistance((GeoDistanceAggregationFields)desc), subAggs);
	}

	public AggregationsDictDescriptor IpRange(string name,
		Action<IpRangeAggregationFieldsDescriptor> configure,
		Action<AggregationsDictDescriptor>? subAggs = null)
	{
		var desc = new IpRangeAggregationFieldsDescriptor();
		configure(desc);
		return AddBucket(name, AggregationContainer.IpRange((IpRangeAggregationFields)desc), subAggs);
	}

	// ── Metric aggregations ──

	public AggregationsDictDescriptor Avg(string name, Action<AverageAggregationDescriptor> configure)
	{
		var desc = new AverageAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.Avg((AverageAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor Max(string name, Action<MaxAggregationDescriptor> configure)
	{
		var desc = new MaxAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.Max((MaxAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor Min(string name, Action<MinAggregationDescriptor> configure)
	{
		var desc = new MinAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.Min((MinAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor Sum(string name, Action<SumAggregationDescriptor> configure)
	{
		var desc = new SumAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.Sum((SumAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor Cardinality(string name, Action<CardinalityAggregationDescriptor> configure)
	{
		var desc = new CardinalityAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.Cardinality((CardinalityAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor ValueCount(string name, Action<ValueCountAggregationDescriptor> configure)
	{
		var desc = new ValueCountAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.ValueCount((ValueCountAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor Stats(string name, Action<StatsAggregationDescriptor> configure)
	{
		var desc = new StatsAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.Stats((StatsAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor ExtendedStats(string name, Action<ExtendedStatsAggregationDescriptor> configure)
	{
		var desc = new ExtendedStatsAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.ExtendedStats((ExtendedStatsAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor Percentiles(string name, Action<PercentilesAggregationDescriptor> configure)
	{
		var desc = new PercentilesAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.Percentiles((PercentilesAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor PercentileRanks(string name, Action<PercentileRanksAggregationDescriptor> configure)
	{
		var desc = new PercentileRanksAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.PercentileRanks((PercentileRanksAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor TopHits(string name, Action<TopHitsAggregationDescriptor> configure)
	{
		var desc = new TopHitsAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.TopHits((TopHitsAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor GeoBounds(string name, Action<GeoBoundsAggregationDescriptor> configure)
	{
		var desc = new GeoBoundsAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.GeoBounds((GeoBoundsAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor GeoCentroid(string name, Action<GeoCentroidAggregationDescriptor> configure)
	{
		var desc = new GeoCentroidAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.GeoCentroid((GeoCentroidAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor MedianAbsoluteDeviation(string name, Action<MedianAbsoluteDeviationAggregationDescriptor> configure)
	{
		var desc = new MedianAbsoluteDeviationAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.MedianAbsoluteDeviation((MedianAbsoluteDeviationAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor ScriptedMetric(string name, Action<ScriptedMetricAggregationDescriptor> configure)
	{
		var desc = new ScriptedMetricAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.ScriptedMetric((ScriptedMetricAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor WeightedAvg(string name, Action<WeightedAverageAggregationDescriptor> configure)
	{
		var desc = new WeightedAverageAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.WeightedAvg((WeightedAverageAggregation)desc);
		return this;
	}

	// ── Pipeline aggregations ──

	public AggregationsDictDescriptor Derivative(string name, Action<DerivativeAggregationDescriptor> configure)
	{
		var desc = new DerivativeAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.Derivative((DerivativeAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor CumulativeSum(string name, Action<CumulativeSumAggregationDescriptor> configure)
	{
		var desc = new CumulativeSumAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.CumulativeSum((CumulativeSumAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor BucketScript(string name, Action<BucketScriptAggregationDescriptor> configure)
	{
		var desc = new BucketScriptAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.BucketScript((BucketScriptAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor BucketSelector(string name, Action<BucketSelectorAggregationDescriptor> configure)
	{
		var desc = new BucketSelectorAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.BucketSelector((BucketSelectorAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor BucketSort(string name, Action<BucketSortAggregationDescriptor> configure)
	{
		var desc = new BucketSortAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.BucketSort((BucketSortAggregation)desc);
		return this;
	}

	public AggregationsDictDescriptor SerialDiff(string name, Action<SerialDifferencingAggregationDescriptor> configure)
	{
		var desc = new SerialDifferencingAggregationDescriptor();
		configure(desc);
		_dict[name] = AggregationContainer.SerialDiff((SerialDifferencingAggregation)desc);
		return this;
	}

	public static implicit operator Dictionary<string, AggregationContainer>(AggregationsDictDescriptor d) => d._dict;
}
