using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// The <c>buckets_path</c> of a pipeline aggregation. OpenSearch accepts three forms, and which are
/// valid depends on the aggregation:
/// <list type="bullet">
///   <item><c>"sales"</c> — a single path string</item>
///   <item><c>["sales", "costs"]</c> — an array of path strings</item>
///   <item><c>{"my_var1": "sales", "my_var2": "costs"}</c> — named paths (required by
///     <c>bucket_script</c> / <c>bucket_selector</c>, whose scripts reference the names)</item>
/// </list>
/// Construct implicitly from a <see cref="string"/>, a string array/list, or a
/// <see cref="Dictionary{TKey,TValue}"/> — e.g. <c>BucketsPath = "sales"</c> or
/// <c>BucketsPath = new Dictionary&lt;string, string&gt; { ["t"] = "total>sales" }</c>.
/// </summary>
[JsonConverter(typeof(BucketsPathConverter))]
public sealed class BucketsPath
{
	/// <summary>The single-path form, or <c>null</c> when another form is used.</summary>
	public string? Single { get; }

	/// <summary>The array-of-paths form, or <c>null</c> when another form is used.</summary>
	public IReadOnlyList<string>? Multiple { get; }

	/// <summary>The named-paths (map) form, or <c>null</c> when another form is used.</summary>
	public IReadOnlyDictionary<string, string>? Named { get; }

	private BucketsPath(string single) => Single = single;
	private BucketsPath(IReadOnlyList<string> multiple) => Multiple = multiple;
	private BucketsPath(IReadOnlyDictionary<string, string> named) => Named = named;

	public static BucketsPath Path(string path) => new(path);
	public static BucketsPath Paths(params string[] paths) => new((IReadOnlyList<string>)paths);
	public static BucketsPath NamedPaths(IReadOnlyDictionary<string, string> named) => new(named);

	public static implicit operator BucketsPath(string path) => new(path);
	public static implicit operator BucketsPath(string[] paths) => new((IReadOnlyList<string>)paths);
	public static implicit operator BucketsPath(List<string> paths) => new(paths);
	public static implicit operator BucketsPath(Dictionary<string, string> named) => new(named);
}
