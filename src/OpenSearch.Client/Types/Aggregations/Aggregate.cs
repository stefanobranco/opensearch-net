using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client.Common;

/// <summary>
/// Non-generic aggregate response type. Covers all aggregation variants (metric, bucket,
/// pipeline, matrix) as a flat bag of nullable fields. The <c>Hits</c> field (for top_hits)
/// is stored as raw <see cref="JsonElement"/> and lazily deserialized via
/// <see cref="AggregateDictionary.TopHits{TDocument}"/>.
/// </summary>
public sealed class Aggregate
{
	public Dictionary<string, object>? Meta { get; set; }
	public JsonElement? Buckets { get; set; }
	public string? Interval { get; set; }

	/// <summary>The metric value. A missing value generally means that there was no data to aggregate, unless specified otherwise.</summary>
	public double? Value { get; set; }
	public string? ValueAsString { get; set; }
	public double Min { get; set; }
	public double Max { get; set; }
	public double Q1 { get; set; }
	public double Q2 { get; set; }
	public double Q3 { get; set; }
	public double Lower { get; set; }
	public double Upper { get; set; }
	public string? MinAsString { get; set; }
	public string? MaxAsString { get; set; }
	public string? Q1AsString { get; set; }
	public string? Q2AsString { get; set; }
	public string? Q3AsString { get; set; }
	public string? LowerAsString { get; set; }
	public string? UpperAsString { get; set; }
	public List<string>? Keys { get; set; }
	public long DocCount { get; set; }
	public Dictionary<string, JsonElement>? AfterKey { get; set; }
	public double? NormalizedValue { get; set; }
	public string? NormalizedValueAsString { get; set; }
	public long? DocCountErrorUpperBound { get; set; }
	public long? SumOtherDocCount { get; set; }
	public long Count { get; set; }
	public double? Avg { get; set; }
	public double Sum { get; set; }
	public string? AvgAsString { get; set; }
	public string? SumAsString { get; set; }
	public double? SumOfSquares { get; set; }
	public double? Variance { get; set; }
	public double? VariancePopulation { get; set; }
	public double? VarianceSampling { get; set; }
	public double? StdDeviation { get; set; }
	public double? StdDeviationPopulation { get; set; }
	public double? StdDeviationSampling { get; set; }
	public StandardDeviationBounds? StdDeviationBounds { get; set; }
	public string? SumOfSquaresAsString { get; set; }
	public string? VarianceAsString { get; set; }
	public string? VariancePopulationAsString { get; set; }
	public string? VarianceSamplingAsString { get; set; }
	public string? StdDeviationAsString { get; set; }
	public StandardDeviationBoundsAsString? StdDeviationBoundsAsString { get; set; }
	public JsonElement? Bounds { get; set; }
	public JsonElement? Location { get; set; }
	public JsonElement? Values { get; set; }
	public List<MatrixStatsFields>? Fields { get; set; }
	public long? BgCount { get; set; }

	/// <summary>Top hits metadata, stored as raw JSON. Deserialize via <see cref="AggregateDictionary.TopHits{TDocument}"/>.</summary>
	public JsonElement? Hits { get; set; }

	[JsonExtensionData]
	public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
