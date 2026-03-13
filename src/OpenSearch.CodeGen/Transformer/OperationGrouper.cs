using OpenSearch.CodeGen.OpenApi;

namespace OpenSearch.CodeGen.Transformer;

/// <summary>
/// Groups operations by x-operation-group and selects the canonical operation per group.
/// </summary>
public static class OperationGrouper
{
	/// <summary>
	/// Groups operations by x-operation-group, filtering to a specific namespace.
	/// Returns one entry per operation group with the selected HTTP method and all path variants.
	/// </summary>
	public static IReadOnlyList<OperationGroup> Group(IReadOnlyList<OpenApiOperation> operations, string targetNamespace)
	{
		var prefix = targetNamespace + ".";

		// Filter to target namespace and skip ignorable
		var filtered = operations
			.Where(op => op.OperationGroup.StartsWith(prefix, StringComparison.Ordinal) && !op.Ignorable)
			.ToList();

		// Group by x-operation-group
		var groups = filtered
			.GroupBy(op => op.OperationGroup)
			.OrderBy(g => g.Key, StringComparer.Ordinal)
			.ToList();

		var result = new List<OperationGroup>();

		foreach (var group in groups)
		{
			// Select the canonical HTTP method per CLIENT_GENERATOR_GUIDE:
			// GET + POST → prefer POST (body-bearing)
			// PUT + POST → prefer PUT
			var methods = group.Select(op => op.HttpMethod.ToLowerInvariant()).Distinct().ToHashSet();
			var selectedMethod = SelectMethod(methods);

			// Get the canonical operation (first one with the selected method)
			var canonical = group.First(op => op.HttpMethod.Equals(selectedMethod, StringComparison.OrdinalIgnoreCase));

			// Collect all unique paths
			var paths = group
				.Select(op => op.Path)
				.Distinct()
				.ToList();

			// Collect all parameters across all variants.
			// If a param appears as both path and query in different variants, prefer path.
			var allParams = new Dictionary<string, OpenApiParameter>(StringComparer.Ordinal);
			foreach (var op in group)
			{
				foreach (var param in op.Parameters)
				{
					if (allParams.TryGetValue(param.Name, out var existing))
					{
						if (param.IsPath && existing.IsQuery)
							allParams[param.Name] = param;
					}
					else
					{
						allParams[param.Name] = param;
					}
				}
			}

			result.Add(new OperationGroup
			{
				OperationGroupName = group.Key,
				CanonicalOperation = canonical,
				HttpMethod = selectedMethod,
				Paths = paths,
				AllParameters = allParams.Values.ToList()
			});
		}

		return result;
	}

	/// <summary>
	/// Selects the preferred HTTP method from a set of methods.
	/// </summary>
	private static string SelectMethod(HashSet<string> methods)
	{
		// HEAD endpoints — keep HEAD
		if (methods.Contains("head") && methods.Count == 1)
			return "head";

		// PUT + POST → prefer PUT
		if (methods.Contains("put"))
			return "put";

		// POST preferred over GET (body-bearing)
		if (methods.Contains("post"))
			return "post";

		if (methods.Contains("get"))
			return "get";

		if (methods.Contains("delete"))
			return "delete";

		if (methods.Contains("head"))
			return "head";

		return methods.First();
	}
}

/// <summary>
/// A grouped set of operations sharing the same x-operation-group.
/// </summary>
public sealed class OperationGroup
{
	public required string OperationGroupName { get; init; }
	public required OpenApiOperation CanonicalOperation { get; init; }
	public required string HttpMethod { get; init; }
	public required IReadOnlyList<string> Paths { get; init; }
	public required IReadOnlyList<OpenApiParameter> AllParameters { get; init; }
}
