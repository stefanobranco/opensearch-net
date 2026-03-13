using YamlDotNet.RepresentationModel;

namespace OpenSearch.CodeGen.OpenApi;

/// <summary>
/// Represents an OpenAPI parameter (path or query).
/// </summary>
public sealed class OpenApiParameter
{
	public required string Name { get; init; }
	public required string In { get; init; }
	public required bool Required { get; init; }
	public required bool Deprecated { get; init; }
	public required OpenApiSchema Schema { get; init; }
	public string? Description { get; init; }

	public bool IsPath => In == "path";
	public bool IsQuery => In == "query";

	public static OpenApiParameter FromNode(YamlNode node, RefResolver resolver, string contextFile)
	{
		// Handle $ref parameters
		if (node is YamlMappingNode mapping && mapping.Children.TryGetValue(new YamlScalarNode("$ref"), out var refNode))
		{
			var refStr = ((YamlScalarNode)refNode).Value!;
			node = resolver.Resolve(refStr, contextFile);
			// Update context file if ref is cross-file
			var hashIndex = refStr.IndexOf('#');
			if (hashIndex > 0)
			{
				var relPath = refStr[..hashIndex];
				var contextDir = Path.GetDirectoryName(contextFile) ?? "";
				contextFile = Path.GetFullPath(Path.Combine(contextDir, relPath));
			}
		}

		var m = (YamlMappingNode)node;
		var name = GetScalar(m, "name")!;
		var inValue = GetScalar(m, "in")!;
		var required = GetScalar(m, "required") == "true";
		var deprecated = GetScalar(m, "deprecated") == "true";
		var description = GetScalar(m, "description");

		OpenApiSchema schema;
		if (m.Children.TryGetValue(new YamlScalarNode("schema"), out var schemaNode))
			schema = new OpenApiSchema(schemaNode, resolver, contextFile);
		else
			schema = new OpenApiSchema(new YamlMappingNode(), resolver, contextFile);

		return new OpenApiParameter
		{
			Name = name,
			In = inValue,
			Required = required,
			Deprecated = deprecated,
			Schema = schema,
			Description = description
		};
	}

	private static string? GetScalar(YamlMappingNode node, string key)
	{
		if (node.Children.TryGetValue(new YamlScalarNode(key), out var child) && child is YamlScalarNode scalar)
			return scalar.Value;
		return null;
	}
}
