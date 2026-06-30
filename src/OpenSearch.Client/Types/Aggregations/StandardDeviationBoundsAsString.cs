// Hand-maintained DTO: referenced by the hand-written Aggregate type, so the code generator
// no longer reaches it. Kept outside Generated/ so the generated tree stays reproducible.
#nullable enable

namespace OpenSearch.Client;

public sealed class StandardDeviationBoundsAsString
{
	public string? Upper { get; set; }
	public string? Lower { get; set; }
	public string? UpperPopulation { get; set; }
	public string? LowerPopulation { get; set; }
	public string? UpperSampling { get; set; }
	public string? LowerSampling { get; set; }
}
