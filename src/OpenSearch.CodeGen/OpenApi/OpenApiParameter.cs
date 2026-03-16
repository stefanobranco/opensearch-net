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
			contextFile = resolver.ResolveContextFile(refStr, contextFile);
		}

		var m = (YamlMappingNode)node;
		var name = m.GetScalar("name")!;
		var inValue = m.GetScalar("in")!;
		var required = m.GetScalar("required") == "true";
		var deprecated = m.GetScalar("deprecated") == "true";
		var description = m.GetScalar("description");

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

}
