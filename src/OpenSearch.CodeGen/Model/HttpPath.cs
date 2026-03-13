namespace OpenSearch.CodeGen.Model;

/// <summary>
/// Represents an HTTP URL template for an operation (e.g., "/{index}/_settings/{name}").
/// </summary>
public sealed class HttpPath
{
	/// <summary>The raw path template from the spec.</summary>
	public required string Template { get; init; }

	/// <summary>
	/// Path parameter names in this template, in order.
	/// For "/{index}/_settings/{name}" this is ["index", "name"].
	/// </summary>
	public required IReadOnlyList<string> ParameterNames { get; init; }

	/// <summary>
	/// Parses a path template string into an HttpPath.
	/// </summary>
	public static HttpPath Parse(string template)
	{
		var paramNames = new List<string>();
		var span = template.AsSpan();
		var idx = 0;
		while (idx < span.Length)
		{
			var start = span[idx..].IndexOf('{');
			if (start < 0)
				break;
			start += idx;
			var end = span[start..].IndexOf('}');
			if (end < 0)
				break;
			end += start;
			paramNames.Add(span[(start + 1)..end].ToString());
			idx = end + 1;
		}

		return new HttpPath
		{
			Template = template,
			ParameterNames = paramNames
		};
	}

	/// <summary>
	/// Returns the number of path parameters.
	/// </summary>
	public int Specificity => ParameterNames.Count;

	public override string ToString() => Template;
}
