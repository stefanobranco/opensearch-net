using YamlDotNet.RepresentationModel;

namespace OpenSearch.CodeGen.OpenApi;

/// <summary>
/// Loads and indexes all OpenAPI operations from the multi-file spec.
/// </summary>
public sealed class OpenApiSpecification
{
	public RefResolver Resolver { get; }
	public IReadOnlyList<OpenApiOperation> Operations { get; }
	public IReadOnlyList<string> NamespaceFiles { get; }

	private OpenApiSpecification(RefResolver resolver, List<OpenApiOperation> operations, List<string> namespaceFiles)
	{
		Resolver = resolver;
		Operations = operations;
		NamespaceFiles = namespaceFiles;
	}

	public static OpenApiSpecification Load(string specDir)
	{
		var resolver = new RefResolver(specDir);
		var operations = new List<OpenApiOperation>();

		var namespacesDir = Path.Combine(specDir, "namespaces");
		var namespaceFiles = Directory.GetFiles(namespacesDir, "*.yaml")
			.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
			.ToList();

		foreach (var file in namespaceFiles)
		{
			var relativePath = Path.GetRelativePath(specDir, file);
			var root = resolver.LoadFile(relativePath);
			var contextFile = Path.GetFullPath(file);

			if (!root.Children.TryGetValue(new YamlScalarNode("paths"), out var pathsNode))
				continue;
			if (pathsNode is not YamlMappingNode pathsMapping)
				continue;

			foreach (var pathKv in pathsMapping.Children)
			{
				var path = ((YamlScalarNode)pathKv.Key).Value!;
				if (pathKv.Value is not YamlMappingNode methodsMapping)
					continue;

				foreach (var methodKv in methodsMapping.Children)
				{
					var method = ((YamlScalarNode)methodKv.Key).Value!;
					// Skip non-HTTP-method keys (parameters, summary, etc.)
					if (!IsHttpMethod(method))
						continue;

					if (methodKv.Value is not YamlMappingNode operationNode)
						continue;

					operations.Add(OpenApiOperation.FromNode(path, method, operationNode, resolver, contextFile));
				}
			}
		}

		return new OpenApiSpecification(resolver, operations, namespaceFiles);
	}

	private static bool IsHttpMethod(string value) =>
		value is "get" or "post" or "put" or "delete" or "head" or "patch" or "options";
}
