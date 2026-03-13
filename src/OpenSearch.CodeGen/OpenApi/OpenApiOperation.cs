using YamlDotNet.RepresentationModel;

namespace OpenSearch.CodeGen.OpenApi;

/// <summary>
/// Represents a single OpenAPI operation (one path + one HTTP method).
/// </summary>
public sealed class OpenApiOperation
{
	public required string Path { get; init; }
	public required string HttpMethod { get; init; }
	public required string OperationId { get; init; }
	public required string OperationGroup { get; init; }
	public required string? Description { get; init; }
	public required bool Deprecated { get; init; }
	public required bool Ignorable { get; init; }
	public required IReadOnlyList<OpenApiParameter> Parameters { get; init; }
	public required OpenApiSchema? RequestBody { get; init; }
	public required OpenApiSchema? ResponseSchema { get; init; }
	public required string ContextFile { get; init; }

	/// <summary>Whether the request body uses NDJSON content type (application/x-ndjson).</summary>
	public required bool IsNdjsonBody { get; init; }

	public static OpenApiOperation FromNode(
		string path,
		string httpMethod,
		YamlMappingNode node,
		RefResolver resolver,
		string contextFile)
	{
		var operationId = node.GetScalar("operationId") ?? "";
		var operationGroup = node.GetScalar("x-operation-group") ?? "";
		var description = node.GetScalar("description");
		var deprecated = node.GetScalar("deprecated") == "true";
		var ignorable = node.GetScalar("x-ignorable") == "true";

		// Parse parameters
		var parameters = new List<OpenApiParameter>();
		if (node.Children.TryGetValue(new YamlScalarNode("parameters"), out var paramsNode) && paramsNode is YamlSequenceNode paramsSeq)
		{
			foreach (var paramNode in paramsSeq)
			{
				parameters.Add(OpenApiParameter.FromNode(paramNode, resolver, contextFile));
			}
		}

		// Parse request body
		OpenApiSchema? requestBody = null;
		bool isNdjsonBody = false;
		if (node.Children.TryGetValue(new YamlScalarNode("requestBody"), out var bodyNode))
		{
			isNdjsonBody = IsNdjsonContentType(bodyNode, resolver, contextFile);
			if (!isNdjsonBody)
				requestBody = ResolveBodySchema(bodyNode, resolver, contextFile);
		}

		// Parse response (use first 2xx response)
		OpenApiSchema? responseSchema = null;
		if (node.Children.TryGetValue(new YamlScalarNode("responses"), out var responsesNode) && responsesNode is YamlMappingNode responsesMapping)
		{
			foreach (var kv in responsesMapping.Children)
			{
				var statusCode = ((YamlScalarNode)kv.Key).Value!;
				if (statusCode.StartsWith('2'))
				{
					responseSchema = ResolveBodySchema(kv.Value, resolver, contextFile);
					break;
				}
			}
		}

		return new OpenApiOperation
		{
			Path = path,
			HttpMethod = httpMethod,
			OperationId = operationId,
			OperationGroup = operationGroup,
			Description = description,
			Deprecated = deprecated,
			Ignorable = ignorable,
			Parameters = parameters,
			RequestBody = requestBody,
			ResponseSchema = responseSchema,
			ContextFile = contextFile,
			IsNdjsonBody = isNdjsonBody
		};
	}

	private static bool IsNdjsonContentType(YamlNode node, RefResolver resolver, string contextFile)
	{
		// Resolve $ref on the request body itself
		if (node is YamlMappingNode mapping && mapping.Children.TryGetValue(new YamlScalarNode("$ref"), out var refNode))
		{
			var refStr = ((YamlScalarNode)refNode).Value!;
			node = resolver.Resolve(refStr, contextFile);
			contextFile = resolver.ResolveContextFile(refStr, contextFile);
		}

		if (node is not YamlMappingNode bodyMapping)
			return false;
		if (!bodyMapping.Children.TryGetValue(new YamlScalarNode("content"), out var contentNode))
			return false;
		if (contentNode is not YamlMappingNode contentMapping)
			return false;

		// Check if the only content type (or primary content type) is NDJSON
		bool hasNdjson = contentMapping.Children.ContainsKey(new YamlScalarNode("application/x-ndjson"));
		bool hasJson = contentMapping.Children.ContainsKey(new YamlScalarNode("application/json"));

		// NDJSON-only: skip body generation. If both are present, use the JSON version.
		return hasNdjson && !hasJson;
	}

	private static OpenApiSchema? ResolveBodySchema(YamlNode node, RefResolver resolver, string contextFile)
	{
		// Handle $ref
		if (node is YamlMappingNode mapping && mapping.Children.TryGetValue(new YamlScalarNode("$ref"), out var refNode))
		{
			var refStr = ((YamlScalarNode)refNode).Value!;
			node = resolver.Resolve(refStr, contextFile);
			contextFile = resolver.ResolveContextFile(refStr, contextFile);
			mapping = (YamlMappingNode)node;
		}
		else if (node is not YamlMappingNode)
			return null;
		else
			mapping = (YamlMappingNode)node;

		// Navigate: content -> application/json -> schema
		if (!mapping.Children.TryGetValue(new YamlScalarNode("content"), out var contentNode))
			return null;
		if (contentNode is not YamlMappingNode contentMapping)
			return null;
		if (!contentMapping.Children.TryGetValue(new YamlScalarNode("application/json"), out var jsonNode))
			return null;
		if (jsonNode is not YamlMappingNode jsonMapping)
			return null;
		if (!jsonMapping.Children.TryGetValue(new YamlScalarNode("schema"), out var schemaNode))
			return null;

		return new OpenApiSchema(schemaNode, resolver, contextFile);
	}

}
