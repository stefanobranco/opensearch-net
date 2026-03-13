using OpenSearch.CodeGen.Model;
using OpenSearch.CodeGen.OpenApi;

namespace OpenSearch.CodeGen.Transformer;

/// <summary>
/// Transforms OpenAPI operations for a namespace into the shape model.
/// </summary>
public sealed class SpecTransformer
{
	private readonly OpenApiSpecification _spec;

	public SpecTransformer(OpenApiSpecification spec) => _spec = spec;

	/// <summary>
	/// Transforms all operations in the given namespace into shapes.
	/// </summary>
	public TransformResult Transform(string targetNamespace)
	{
		var typeMapper = new TypeMapper(targetNamespace);
		var groups = OperationGrouper.Group(_spec.Operations, targetNamespace);
		var nsName = NamingConventions.NamespaceToClassName(targetNamespace);

		var requests = new List<RequestShape>();

		foreach (var group in groups)
		{
			var (requestName, responseName, endpointName) = NamingConventions.OperationGroupToNames(group.OperationGroupName);

			// Parse HTTP paths
			var httpPaths = group.Paths
				.Select(HttpPath.Parse)
				.OrderByDescending(p => p.Specificity) // Most specific first
				.ToList();

			// Map HTTP method to C# HttpMethod enum name
			var httpMethod = MapHttpMethod(group.HttpMethod);

			// Collect path parameters
			var pathParams = new List<Field>();
			var queryParams = new List<Field>();

			foreach (var param in group.AllParameters)
			{
				// Skip deprecated master_timeout in favor of cluster_manager_timeout
				if (param.Name == "master_timeout" && param.Deprecated)
					continue;

				var fieldType = typeMapper.Map(param.Schema);

				// Path params are always stringified in URLs — never use JsonElement
				if (param.IsPath && fieldType.CSharpName == "System.Text.Json.JsonElement")
					fieldType = TypeRef.String();

				var field = new Field
				{
					Name = NamingConventions.ToPascalCase(param.Name),
					WireName = param.Name,
					Type = fieldType,
					Required = param.Required,
					Description = param.Description,
					Deprecated = param.Deprecated
				};

				if (param.IsPath)
					pathParams.Add(field);
				else if (param.IsQuery)
					queryParams.Add(field);
			}

			// Collect body fields (excluding any that clash with path/query params)
			var bodyFields = new List<Field>();
			var bodySchema = group.CanonicalOperation.RequestBody;
			if (bodySchema is not null)
			{
				var resolved = bodySchema.Ref is not null ? bodySchema.Resolved() : bodySchema;
				CollectBodyFields(resolved, bodyFields, typeMapper);

				var existingNames = new HashSet<string>(
					pathParams.Select(p => p.Name).Concat(queryParams.Select(p => p.Name)),
					StringComparer.OrdinalIgnoreCase);
				bodyFields.RemoveAll(f => existingNames.Contains(f.Name));
			}

			// Build response shape
			var responseShape = BuildResponseShape(
				responseName, nsName, group, typeMapper);

			var requestShape = new RequestShape
			{
				ClassName = requestName,
				Namespace = nsName,
				Description = group.CanonicalOperation.Description,
				OperationGroup = group.OperationGroupName,
				HttpPaths = httpPaths,
				HttpMethod = httpMethod,
				PathParams = pathParams,
				QueryParams = queryParams,
				BodyFields = bodyFields,
				EndpointName = endpointName,
				Response = responseShape
			};

			requests.Add(requestShape);
		}

		return new TransformResult
		{
			Namespace = nsName,
			Requests = requests,
			Enums = typeMapper.DiscoveredEnums.Values.ToList(),
			Objects = typeMapper.DiscoveredObjects.Values.ToList()
		};
	}

	private ResponseShape BuildResponseShape(
		string responseName, string nsName,
		OperationGroup group, TypeMapper typeMapper)
	{
		// HEAD requests have no body — response has a single Exists bool
		if (group.HttpMethod == "head")
		{
			return new ResponseShape
			{
				ClassName = responseName,
				Namespace = nsName,
				Description = group.CanonicalOperation.Description,
				Fields = [
					new Field
					{
						Name = "Exists",
						WireName = "exists",
						Type = TypeRef.Bool(),
						Required = true,
						Description = "Whether the resource exists."
					}
				],
				IsHeadResponse = true
			};
		}

		var responseSchema = group.CanonicalOperation.ResponseSchema;
		if (responseSchema is null)
		{
			return new ResponseShape
			{
				ClassName = responseName,
				Namespace = nsName,
				Description = group.CanonicalOperation.Description,
				Fields = [],
				IsHeadResponse = false
			};
		}

		var resolved = responseSchema.Ref is not null ? responseSchema.Resolved() : responseSchema;

		// Check if it's a dictionary response (object with additionalProperties, no named properties)
		if (resolved.HasAdditionalProperties && resolved.Properties.Count == 0)
		{
			var valueType = resolved.AdditionalProperties is not null
				? typeMapper.Map(resolved.AdditionalProperties)
				: TypeRef.Object();

			return new ResponseShape
			{
				ClassName = responseName,
				Namespace = nsName,
				Description = resolved.Description ?? group.CanonicalOperation.Description,
				Fields = [],
				DictionaryValueType = valueType,
				IsHeadResponse = false
			};
		}

		// Regular response with fields
		var fields = new List<Field>();
		var required = new HashSet<string>(resolved.Required);

		// Handle allOf
		if (resolved.AllOf.Count > 0)
		{
			foreach (var allOfMember in resolved.AllOf)
			{
				var memberResolved = allOfMember.Resolved();
				foreach (var r in memberResolved.Required)
					required.Add(r);
				CollectResponseFields(memberResolved, fields, required, typeMapper);
			}
		}
		else
		{
			CollectResponseFields(resolved, fields, required, typeMapper);
		}

		return new ResponseShape
		{
			ClassName = responseName,
			Namespace = nsName,
			Description = resolved.Description ?? group.CanonicalOperation.Description,
			Fields = fields,
			IsHeadResponse = false
		};
	}

	private void CollectBodyFields(OpenApiSchema schema, List<Field> fields, TypeMapper typeMapper, HashSet<string>? parentRequired = null)
	{
		var required = new HashSet<string>(schema.Required);
		if (parentRequired is not null)
		{
			foreach (var r in parentRequired)
				required.Add(r);
		}

		// Handle allOf in body schemas
		if (schema.AllOf.Count > 0)
		{
			foreach (var allOfMember in schema.AllOf)
			{
				var resolved = allOfMember.Resolved();
				CollectBodyFields(resolved, fields, typeMapper, required);
			}
			return;
		}

		foreach (var (name, propSchema) in schema.Properties)
		{
			if (name.Contains('.'))
				continue;
			var fieldType = typeMapper.Map(propSchema);
			fields.Add(new Field
			{
				Name = NamingConventions.ToPascalCase(name),
				WireName = name,
				Type = fieldType,
				Required = required.Contains(name),
				Description = propSchema.Description,
				Deprecated = propSchema.Deprecated
			});
		}
	}

	private static void CollectResponseFields(OpenApiSchema schema, List<Field> fields, HashSet<string> required, TypeMapper typeMapper)
	{
		foreach (var (name, propSchema) in schema.Properties)
		{
			if (name.Contains('.'))
				continue;
			fields.Add(new Field
			{
				Name = NamingConventions.ToPascalCase(name),
				WireName = name,
				Type = typeMapper.Map(propSchema),
				Required = required.Contains(name),
				Description = propSchema.Description,
				Deprecated = propSchema.Deprecated
			});
		}
	}

	private static string MapHttpMethod(string method) => method.ToLowerInvariant() switch
	{
		"get" => "Get",
		"post" => "Post",
		"put" => "Put",
		"delete" => "Delete",
		"head" => "Head",
		"patch" => "Patch",
		_ => "Post"
	};
}
