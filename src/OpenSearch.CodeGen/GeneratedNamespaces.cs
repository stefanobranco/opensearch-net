namespace OpenSearch.CodeGen;

/// <summary>
/// The canonical set of API namespaces the generator emits into
/// <c>src/OpenSearch.Client/Generated</c>. This is the single source of truth:
/// <c>build/regenerate.sh</c> must pass exactly this set (enforced by a test), and the
/// coverage report treats every other spec namespace as not-yet-wired.
/// </summary>
public static class GeneratedNamespaces
{
	public static readonly IReadOnlyList<string> All =
	[
		"_core",
		"cat",
		"cluster",
		"dangling_indices",
		"geospatial",
		"indices",
		"ingest",
		"ingestion",
		"ism",
		"knn",
		"ltr",
		"ml",
		"nodes",
		"security",
	];
}
