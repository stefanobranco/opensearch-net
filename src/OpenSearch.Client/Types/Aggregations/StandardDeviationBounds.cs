// Hand-maintained DTO: referenced by the hand-written Aggregate type, so the code generator
// no longer reaches it. Kept outside Generated/ so the generated tree stays reproducible.
#nullable enable

namespace OpenSearch.Client;

public sealed class StandardDeviationBounds
{
	public double? Upper { get; set; }
	public double? Lower { get; set; }
	public double? UpperPopulation { get; set; }
	public double? LowerPopulation { get; set; }
	public double? UpperSampling { get; set; }
	public double? LowerSampling { get; set; }
}
