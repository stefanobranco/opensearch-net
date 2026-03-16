namespace OpenSearch.Client;

/// <summary>Result from a stats aggregation.</summary>
public class StatsResult
{
	public long Count { get; set; }
	public double Min { get; set; }
	public double Max { get; set; }
	public double Avg { get; set; }
	public double Sum { get; set; }
}

/// <summary>Result from an extended_stats aggregation.</summary>
public sealed class ExtendedStatsResult : StatsResult
{
	public double SumOfSquares { get; set; }
	public double Variance { get; set; }
	public double VariancePopulation { get; set; }
	public double VarianceSampling { get; set; }
	public double StdDeviation { get; set; }
	public double StdDeviationPopulation { get; set; }
	public double StdDeviationSampling { get; set; }
}
