// Hand-maintained DTO: referenced by the hand-written Aggregate type, so the code generator
// no longer reaches it. Kept outside Generated/ so the generated tree stays reproducible.
#nullable enable

namespace OpenSearch.Client;

public sealed class MatrixStatsFields
{
	public string? Name { get; set; }
	public long Count { get; set; }
	public double Mean { get; set; }
	public double Variance { get; set; }
	public double Skewness { get; set; }
	public double Kurtosis { get; set; }
	public Dictionary<string, double>? Covariance { get; set; }
	public Dictionary<string, double>? Correlation { get; set; }
}
