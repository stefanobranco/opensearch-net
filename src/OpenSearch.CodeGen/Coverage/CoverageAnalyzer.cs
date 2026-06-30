using OpenSearch.CodeGen.OpenApi;
using OpenSearch.CodeGen.Transformer;

namespace OpenSearch.CodeGen.Coverage;

/// <summary>
/// Computes, for every operation group in the spec, whether this client ships it — and if not,
/// why (namespace not wired, NDJSON not yet hand-written, or intentionally ignorable). The result
/// is rendered to a committed report and enforced by a test, so the gap map stays current.
/// </summary>
public static class CoverageAnalyzer
{
	/// <param name="spec">The loaded multi-file OpenAPI spec.</param>
	/// <param name="wiredNamespaces">Namespaces the generator emits (see <see cref="GeneratedNamespaces"/>).</param>
	/// <param name="javaNamespaces">Namespaces the opensearch-java client ships, for cross-reference.</param>
	/// <param name="handWrittenRequestNames">Names of hand-written <c>*Request</c> types, to credit NDJSON endpoints.</param>
	public static CoverageReport Analyze(
		OpenApiSpecification spec,
		IReadOnlyCollection<string> wiredNamespaces,
		IReadOnlyCollection<string> javaNamespaces,
		IReadOnlyCollection<string> handWrittenRequestNames)
	{
		var wired = new HashSet<string>(wiredNamespaces, StringComparer.Ordinal);
		var java = new HashSet<string>(javaNamespaces, StringComparer.Ordinal);
		var handWritten = new HashSet<string>(handWrittenRequestNames, StringComparer.Ordinal);

		// Group every operation by its x-operation-group, then bucket groups by namespace.
		var groupsByNamespace = spec.Operations
			.Where(op => !string.IsNullOrEmpty(op.OperationGroup))
			.GroupBy(op => op.OperationGroup)
			.GroupBy(g => NamespaceOf(g.Key))
			.ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

		// For each wired namespace, the authoritative set of groups the generator emits.
		var generatedByNamespace = wired.ToDictionary(
			ns => ns,
			ns => OperationGrouper.Group(spec.Operations, ns)
				.Select(g => g.OperationGroupName)
				.ToHashSet(StringComparer.Ordinal),
			StringComparer.Ordinal);

		var namespaces = new List<NamespaceCoverage>();

		foreach (var nsEntry in groupsByNamespace.OrderBy(e => e.Key, StringComparer.Ordinal))
		{
			var ns = nsEntry.Key;
			var isWired = wired.Contains(ns);
			var generated = isWired ? generatedByNamespace[ns] : null;

			var operations = nsEntry.Value
				.OrderBy(g => g.Key, StringComparer.Ordinal)
				.Select(g => ClassifyGroup(g.Key, g.ToList(), isWired, generated, handWritten))
				.ToList();

			namespaces.Add(new NamespaceCoverage(
				Namespace: ns,
				Wired: isWired,
				InJavaClient: java.Contains(ns),
				Operations: operations));
		}

		return new CoverageReport(namespaces);
	}

	private static OperationCoverage ClassifyGroup(
		string operationGroup,
		IReadOnlyList<OpenApiOperation> variants,
		bool isWired,
		HashSet<string>? generated,
		HashSet<string> handWritten)
	{
		var httpMethod = RepresentativeMethod(variants);
		var deprecated = variants.All(v => v.Deprecated);

		CoverageStatus status;
		if (!isWired)
		{
			status = CoverageStatus.NamespaceNotWired;
		}
		else if (generated!.Contains(operationGroup))
		{
			status = CoverageStatus.Generated;
		}
		else if (variants.All(v => v.Ignorable))
		{
			// OperationGrouper drops a group when every variant is x-ignorable.
			status = CoverageStatus.Ignorable;
		}
		else
		{
			// Remaining wired-but-not-generated groups are NDJSON endpoints (bulk, msearch, ...),
			// covered iff a hand-written request type carries the name the generator would have used.
			var requestName = NamingConventions.OperationGroupToNames(operationGroup).RequestName;
			status = handWritten.Contains(requestName)
				? CoverageStatus.HandWritten
				: CoverageStatus.NdjsonMissing;
		}

		return new OperationCoverage(operationGroup, status, httpMethod, deprecated);
	}

	/// <summary>The namespace owning an operation group: the prefix before the first dot, or _core for bare names.</summary>
	private static string NamespaceOf(string operationGroup) =>
		operationGroup.IndexOf('.') is var i and >= 0 ? operationGroup[..i] : "_core";

	private static readonly string[] s_methodPriority = ["put", "post", "get", "delete", "head", "patch"];

	/// <summary>Picks one representative HTTP method for display, mirroring the generator's preference order.</summary>
	private static string RepresentativeMethod(IReadOnlyList<OpenApiOperation> variants)
	{
		var methods = variants.Select(v => v.HttpMethod.ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
		foreach (var m in s_methodPriority)
			if (methods.Contains(m))
				return m.ToUpperInvariant();
		return methods.OrderBy(m => m, StringComparer.Ordinal).First().ToUpperInvariant();
	}
}
