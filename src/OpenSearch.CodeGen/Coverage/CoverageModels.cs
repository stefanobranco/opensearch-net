namespace OpenSearch.CodeGen.Coverage;

/// <summary>
/// Coverage status of a single API operation group relative to what this client ships.
/// </summary>
public enum CoverageStatus
{
	/// <summary>The generator emits a typed request/response for this operation group.</summary>
	Generated,

	/// <summary>NDJSON endpoint excluded from codegen, but covered by a hand-written request type.</summary>
	HandWritten,

	/// <summary>NDJSON endpoint excluded from codegen and not yet hand-written.</summary>
	NdjsonMissing,

	/// <summary>Every variant is marked <c>x-ignorable</c> in the spec; intentionally not generated.</summary>
	Ignorable,

	/// <summary>The operation group's namespace is not wired into the generator yet.</summary>
	NamespaceNotWired,
}

/// <summary>Coverage of one operation group (e.g. <c>indices.create</c>).</summary>
public sealed record OperationCoverage(
	string OperationGroup,
	CoverageStatus Status);

/// <summary>Coverage of one API namespace and all its operation groups.</summary>
public sealed record NamespaceCoverage(
	string Namespace,
	bool Wired,
	bool InJavaClient,
	IReadOnlyList<OperationCoverage> Operations)
{
	public int Total => Operations.Count;
	public int Covered => Operations.Count(o => o.Status is CoverageStatus.Generated or CoverageStatus.HandWritten);

	/// <summary>Operation groups that are genuine gaps (not generated, not hand-written, not intentionally excluded).</summary>
	public int Gaps => Operations.Count(o => o.Status is CoverageStatus.NamespaceNotWired or CoverageStatus.NdjsonMissing);
}

/// <summary>The full coverage report across every namespace in the spec.</summary>
public sealed record CoverageReport(IReadOnlyList<NamespaceCoverage> Namespaces)
{
	public int TotalOperations => Namespaces.Sum(n => n.Total);
	public int CoveredOperations => Namespaces.Sum(n => n.Covered);
	public int WiredNamespaces => Namespaces.Count(n => n.Wired);
	public int JavaNamespaces => Namespaces.Count(n => n.InJavaClient);
}
