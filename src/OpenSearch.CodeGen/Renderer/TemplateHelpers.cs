using OpenSearch.CodeGen.Model;
using OpenSearch.CodeGen.Transformer;
using Scriban.Runtime;

namespace OpenSearch.CodeGen.Renderer;

/// <summary>
/// Builds Scriban template context data from shape model objects.
/// </summary>
public static class TemplateHelpers
{
	public static ScriptObject BuildRequestContext(RequestShape request, IReadOnlyDictionary<string, ObjectShape> allObjects)
	{
		var obj = new ScriptObject();
		obj["namespace"] = request.Namespace;
		obj["class_name"] = request.ClassName;
		obj["description"] = SanitizeDescription(request.Description);
		obj["endpoint_name"] = request.EndpointName;
		obj["response_name"] = request.Response.ClassName;
		obj["http_method"] = request.HttpMethod;
		obj["has_body"] = request.HasBody;
		obj["is_raw_body"] = request.IsRawBody;
		obj["is_head"] = request.IsHead;

		// Index-style operations: POST when no Id, PUT when Id is present
		var hasPathWithId = request.HttpPaths.Any(p => p.ParameterNames.Contains("id"));
		var hasPathWithoutId = request.HttpPaths.Any(p => !p.ParameterNames.Contains("id"));
		obj["method_varies_by_id"] = hasPathWithId && hasPathWithoutId && request.IsRawBody;

		obj["path_params"] = BuildFieldArray(request.PathParams);
		obj["query_params"] = BuildQueryParamArray(request.QueryParams);
		obj["body_fields"] = BuildFieldArray(request.BodyFields);
		obj["paths"] = BuildPathArray(request.HttpPaths, request.PathParams);
		obj["extra_usings"] = ComputeExtraUsings(request.Namespace, request.BodyFields, allObjects);

		// Generic endpoint support: if the response is generic, the endpoint must be generic too
		obj["response_type_parameters"] = request.Response.IsGeneric
			? new ScriptArray(request.Response.TypeParameters.Cast<object>())
			: null;

		// Cat namespace: inject format=json default so users get structured JSON responses
		obj["is_cat"] = request.Namespace == "Cat";

		return obj;
	}

	public static ScriptObject BuildResponseContext(ResponseShape response, IReadOnlyDictionary<string, ObjectShape> allObjects)
	{
		var obj = new ScriptObject();
		obj["namespace"] = response.Namespace;
		obj["class_name"] = response.ClassName;
		obj["description"] = SanitizeDescription(response.Description);
		obj["fields"] = BuildFieldArray(response.Fields);
		obj["extra_usings"] = ComputeExtraUsings(response.Namespace, response.Fields, allObjects);
		obj["type_parameters"] = response.IsGeneric
			? new ScriptArray(response.TypeParameters.Cast<object>())
			: null;
		return obj;
	}

	public static ScriptObject BuildDictionaryResponseContext(ResponseShape response, IReadOnlyDictionary<string, ObjectShape> allObjects)
	{
		var obj = new ScriptObject();
		obj["namespace"] = response.Namespace;
		obj["class_name"] = response.ClassName;
		obj["description"] = SanitizeDescription(response.Description);
		obj["value_type"] = response.DictionaryValueType!.ToCSharpPropertyType();
		obj["extra_usings"] = ComputeExtraUsings(response.Namespace, [response.DictionaryValueType!], allObjects);
		return obj;
	}

	public static ScriptObject BuildEnumContext(EnumShape enumShape)
	{
		var obj = new ScriptObject();
		obj["class_name"] = enumShape.ClassName;
		obj["description"] = SanitizeDescription(enumShape.Description);

		var variants = new ScriptArray();
		foreach (var variant in enumShape.Variants)
		{
			var v = new ScriptObject();
			v["name"] = variant.Name;
			v["wire_value"] = variant.WireValue;
			variants.Add(v);
		}
		obj["variants"] = variants;
		return obj;
	}

	public static ScriptObject BuildObjectContext(ObjectShape objectShape, IReadOnlyDictionary<string, ObjectShape> allObjects)
	{
		var obj = new ScriptObject();
		obj["namespace"] = objectShape.Namespace;
		obj["class_name"] = objectShape.ClassName;
		obj["description"] = SanitizeDescription(objectShape.Description);
		obj["fields"] = BuildFieldArray(objectShape.Fields);
		obj["extra_usings"] = ComputeExtraUsings(objectShape.Namespace, objectShape.Fields, allObjects);
		obj["type_parameters"] = objectShape.IsGeneric
			? new ScriptArray(objectShape.TypeParameters.Cast<object>())
			: null;
		return obj;
	}

	public static ScriptObject BuildNamespaceClientContext(string namespaceName, IReadOnlyList<RequestShape> requests)
	{
		// Legacy overload without descriptor support — delegates with empty lookups
		return BuildNamespaceClientContext(namespaceName, requests,
			new Dictionary<string, ObjectShape>(StringComparer.Ordinal),
			new Dictionary<string, TaggedUnionShape>(StringComparer.Ordinal));
	}

	public static ScriptObject BuildTaggedUnionContext(TaggedUnionShape union, IReadOnlyDictionary<string, ObjectShape> allObjects)
	{
		var obj = new ScriptObject();
		obj["namespace"] = union.Namespace;
		obj["class_name"] = union.ClassName;
		obj["description"] = SanitizeDescription(union.Description);
		obj["kind_enum_name"] = union.KindEnumName;

		var variants = new ScriptArray();
		foreach (var variant in union.Variants)
		{
			var v = new ScriptObject();
			v["name"] = variant.Name;
			v["wire_name"] = variant.WireName;
			v["type"] = variant.Type.CSharpName;
			v["type_name"] = variant.Type.CSharpName;
			v["description"] = SanitizeDescription(variant.Description);
			variants.Add(v);
		}
		obj["variants"] = variants;
		obj["extra_usings"] = ComputeExtraUsings(union.Namespace, union.Variants.Select(v => v.Type).ToList(), allObjects);

		return obj;
	}

	public static ScriptObject BuildClientExtensionContext(string namespaceName)
	{
		var obj = new ScriptObject();
		obj["namespace"] = namespaceName;
		obj["namespace_lower"] = namespaceName[..1].ToLowerInvariant() + namespaceName[1..];
		return obj;
	}

	private static ScriptArray BuildFieldArray(IReadOnlyList<Field> fields)
	{
		var arr = new ScriptArray();
		foreach (var field in fields)
		{
			var f = new ScriptObject();
			f["name"] = field.Name;
			f["wire_name"] = field.WireName;
			// Compute the full property type — non-required is always nullable,
			// required value types are non-nullable, required reference types are also nullable
			// to avoid CS8618 in DTOs (no constructors)
			f["type"] = ComputePropertyType(field);
			f["required"] = field.Required;
			f["description"] = SanitizeDescription(field.Description);
			f["deprecated"] = field.Deprecated;
			// Emit [JsonPropertyName] when the wire name won't round-trip through SnakeCaseLower
			f["needs_json_property_name"] = NamingConventions.NeedsJsonPropertyName(field.WireName, field.Name);
			arr.Add(f);
		}
		return arr;
	}

	private static ScriptArray BuildQueryParamArray(IReadOnlyList<Field> fields)
	{
		var arr = new ScriptArray();
		foreach (var field in fields)
		{
			var f = new ScriptObject();
			f["name"] = field.Name;
			f["wire_name"] = field.WireName;
			// Query params are always nullable — the template generates `is not null` checks
			f["type"] = ComputeQueryParamType(field);
			f["required"] = false;
			f["description"] = SanitizeDescription(field.Description);
			f["deprecated"] = field.Deprecated;
			f["value_expr"] = ComputeQueryValueExpr(field);
			f["needs_json_property_name"] = NamingConventions.NeedsJsonPropertyName(field.WireName, field.Name);
			arr.Add(f);
		}
		return arr;
	}

	/// <summary>
	/// Returns the C# expression to convert a query parameter value to a string
	/// suitable for Uri.EscapeDataString. Handles enums, bools, and general types.
	/// </summary>
	private static string ComputeQueryValueExpr(Field field)
	{
		if (field.Type.Name == "string")
			return $"r.{field.Name}!";

		if (field.Type.Name == "bool")
			return $"(r.{field.Name}.Value ? \"true\" : \"false\")";

		if (field.Type.IsEnum)
			return $"QueryParamSerializer.Serialize(r.{field.Name}!.Value)";

		if (field.Type.Kind == Model.TypeRefKind.List)
			return $"string.Join(\",\", r.{field.Name}!)";

		// int, long, float, double, JsonElement, etc.
		return $"r.{field.Name}.ToString()!";
	}

	/// <summary>
	/// Returns the C# type for a query parameter — always nullable since query params are optional on the wire.
	/// </summary>
	private static string ComputeQueryParamType(Field field) =>
		field.Type.CSharpName + (field.Type.CSharpName.EndsWith("?") ? "" : "?");

	/// <summary>
	/// Returns the full C# property type string, including ? suffix where needed.
	/// For DTOs: all properties are nullable except required value types.
	/// </summary>
	private static string ComputePropertyType(Field field)
	{
		var baseName = field.Type.CSharpName;

		if (field.Type.IsValueType)
		{
			// Required value types stay non-nullable; optional value types get ?
			return field.Required ? baseName : baseName + "?";
		}

		// Reference types are always nullable in DTOs (no constructor enforcement)
		return baseName + "?";
	}

	private static ScriptArray BuildPathArray(IReadOnlyList<HttpPath> paths, IReadOnlyList<Field> pathParams)
	{
		var paramLookup = pathParams.ToDictionary(p => p.WireName, p => p.Name);

		// Deduplicate paths that have the same parameter set (e.g., /_alias/{name} and /_aliases/{name}).
		// Keep only the first (most specific) path for each unique combination of parameters.
		var seenParamSets = new HashSet<string>();

		var arr = new ScriptArray();
		foreach (var path in paths)
		{
			var paramSetKey = string.Join(",", path.ParameterNames.OrderBy(n => n, StringComparer.Ordinal));
			if (!seenParamSets.Add(paramSetKey))
				continue;
			var p = new ScriptObject();
			p["template"] = path.Template;

			var paramNames = new ScriptArray();
			foreach (var pname in path.ParameterNames)
			{
				paramNames.Add(paramLookup.GetValueOrDefault(pname, NamingConventions.ToPascalCase(pname)));
			}
			p["parameter_names"] = paramNames;

			// Build interpolated string: /{index}/_settings → /{Uri.EscapeDataString(r.Index)}/_settings
			var interpolated = path.Template;
			foreach (var pname in path.ParameterNames)
			{
				var csharpName = paramLookup.GetValueOrDefault(pname, NamingConventions.ToPascalCase(pname));
				interpolated = interpolated.Replace(
					"{" + pname + "}",
					"{Uri.EscapeDataString(r." + csharpName + "!.ToString()!)}");
			}
			p["interpolated"] = interpolated;
			arr.Add(p);
		}
		return arr;
	}

	/// <summary>
	/// Computes extra using directives needed for cross-namespace type references.
	/// Returns a ScriptArray of namespace names (e.g., ["Common", "Core"]).
	/// </summary>
	private static ScriptArray ComputeExtraUsings(string currentNamespace, IReadOnlyList<Field> fields, IReadOnlyDictionary<string, ObjectShape> allObjects)
	{
		var namespaces = new HashSet<string>(StringComparer.Ordinal);
		CollectReferencedNamespaces(fields, allObjects, namespaces);

		// Remove current namespace and "Common" enums (they're in the root OpenSearch.Client namespace)
		namespaces.Remove(currentNamespace);

		var arr = new ScriptArray();
		foreach (var ns in namespaces.OrderBy(n => n, StringComparer.Ordinal))
			arr.Add(ns);
		return arr;
	}

	private static ScriptArray ComputeExtraUsings(string currentNamespace, IReadOnlyList<TypeRef> types, IReadOnlyDictionary<string, ObjectShape> allObjects)
	{
		var namespaces = new HashSet<string>(StringComparer.Ordinal);
		foreach (var type in types)
			CollectTypeNamespaces(type, allObjects, namespaces);
		namespaces.Remove(currentNamespace);
		var arr = new ScriptArray();
		foreach (var ns in namespaces.OrderBy(n => n, StringComparer.Ordinal))
			arr.Add(ns);
		return arr;
	}

	private static void CollectReferencedNamespaces(IReadOnlyList<Field> fields, IReadOnlyDictionary<string, ObjectShape> allObjects, HashSet<string> namespaces)
	{
		foreach (var field in fields)
			CollectTypeNamespaces(field.Type, allObjects, namespaces);
	}

	private static void CollectTypeNamespaces(TypeRef type, IReadOnlyDictionary<string, ObjectShape> allObjects, HashSet<string> namespaces)
	{
		if (type.Kind == TypeRefKind.Named && !type.IsEnum)
		{
			// Look up by schema name first, then by class name (dictionary may be keyed by either)
			if (allObjects.TryGetValue(type.Name, out var shape) || allObjects.TryGetValue(type.CSharpName, out shape))
				namespaces.Add(shape.Namespace);
		}
		if (type.ItemType is not null)
			CollectTypeNamespaces(type.ItemType, allObjects, namespaces);
		if (type.KeyType is not null)
			CollectTypeNamespaces(type.KeyType, allObjects, namespaces);
		if (type.ValueType is not null)
			CollectTypeNamespaces(type.ValueType, allObjects, namespaces);
	}

	// ───────────────── Descriptor context builders ─────────────────

	/// <summary>
	/// Returns true if the type has a corresponding descriptor (Named, non-enum, found in objects or unions).
	/// </summary>
	public static bool HasDescriptor(TypeRef type, IReadOnlyDictionary<string, ObjectShape> allObjects, IReadOnlyDictionary<string, TaggedUnionShape> allUnions)
	{
		if (type.Kind != TypeRefKind.Named || type.IsEnum)
			return false;
		// Unions with no variants don't get descriptors
		if (allUnions.TryGetValue(type.CSharpName, out var union) && union.Variants.Count == 0)
			return false;
		return allObjects.ContainsKey(type.CSharpName) || allUnions.ContainsKey(type.CSharpName);
	}

	/// <summary>
	/// Returns the descriptor class name for a given type (e.g., "BoolQuery" → "BoolQueryDescriptor").
	/// </summary>
	public static string GetDescriptorName(TypeRef type)
	{
		var baseName = type.CSharpName;
		// Strip generic suffixes like <TDocument>
		var angleIdx = baseName.IndexOf('<');
		if (angleIdx >= 0) baseName = baseName[..angleIdx];
		return baseName + "Descriptor";
	}

	/// <summary>
	/// Classifies a field for descriptor method generation.
	/// </summary>
	public static string ComputeDescriptorKind(Field field, IReadOnlyDictionary<string, ObjectShape> allObjects, IReadOnlyDictionary<string, TaggedUnionShape> allUnions)
	{
		var type = field.Type;
		if (type.Kind == TypeRefKind.List && type.ItemType is not null)
		{
			if (HasDescriptor(type.ItemType, allObjects, allUnions))
				return allUnions.ContainsKey(type.ItemType.CSharpName) ? "list_union" : "list_object";
			return "list_simple";
		}
		if (HasDescriptor(type, allObjects, allUnions))
			return allUnions.ContainsKey(type.CSharpName) ? "union" : "object";
		return "simple";
	}

	public static ScriptObject BuildObjectDescriptorContext(ObjectShape objectShape, IReadOnlyDictionary<string, ObjectShape> allObjects, IReadOnlyDictionary<string, TaggedUnionShape> allUnions)
	{
		var obj = new ScriptObject();
		obj["namespace"] = objectShape.Namespace;
		obj["class_name"] = objectShape.ClassName;
		obj["descriptor_name"] = objectShape.ClassName + "Descriptor";
		obj["fields"] = BuildDescriptorFieldArray(objectShape.Fields, allObjects, allUnions);
		obj["extra_usings"] = ComputeExtraUsings(objectShape.Namespace, objectShape.Fields, allObjects);
		obj["type_parameters"] = objectShape.IsGeneric
			? new ScriptArray(objectShape.TypeParameters.Cast<object>())
			: null;
		return obj;
	}

	public static ScriptObject BuildTaggedUnionDescriptorContext(TaggedUnionShape union, IReadOnlyDictionary<string, ObjectShape> allObjects, IReadOnlyDictionary<string, TaggedUnionShape> allUnions)
	{
		var obj = new ScriptObject();
		obj["namespace"] = union.Namespace;
		obj["class_name"] = union.ClassName;
		obj["descriptor_name"] = union.ClassName + "Descriptor";

		var variants = new ScriptArray();
		foreach (var variant in union.Variants)
		{
			var v = new ScriptObject();
			v["name"] = variant.Name;
			v["type"] = variant.Type.CSharpName;
			v["type_name"] = variant.Type.CSharpName;

			// Classify variant: does its type have a descriptor?
			if (HasDescriptor(variant.Type, allObjects, allUnions))
			{
				v["variant_kind"] = allUnions.ContainsKey(variant.Type.CSharpName) ? "union" : "object";
				v["descriptor_name"] = GetDescriptorName(variant.Type);
			}
			else
			{
				v["variant_kind"] = "simple";
				v["descriptor_name"] = "";
			}
			variants.Add(v);
		}
		obj["variants"] = variants;
		obj["extra_usings"] = ComputeExtraUsings(union.Namespace, union.Variants.Select(v => v.Type).ToList(), allObjects);
		return obj;
	}

	public static ScriptObject BuildRequestDescriptorContext(RequestShape request, IReadOnlyDictionary<string, ObjectShape> allObjects, IReadOnlyDictionary<string, TaggedUnionShape> allUnions)
	{
		var obj = new ScriptObject();
		obj["namespace"] = request.Namespace;
		obj["class_name"] = request.ClassName;
		obj["descriptor_name"] = request.ClassName + "Descriptor";
		obj["is_raw_body"] = request.IsRawBody;

		// Query params are always nullable in the POCO (ComputeQueryParamType always adds ?),
		// so force Required=false so ComputePropertyType matches.
		var queryParamsAsOptional = request.QueryParams.Select(qp => new Field
		{
			Name = qp.Name,
			WireName = qp.WireName,
			Type = qp.Type,
			Required = false,
			Description = qp.Description,
			Deprecated = qp.Deprecated
		}).ToList();

		var allFields = new List<Field>();
		allFields.AddRange(request.PathParams);
		allFields.AddRange(queryParamsAsOptional);
		allFields.AddRange(request.BodyFields);

		obj["fields"] = BuildDescriptorFieldArray(allFields, allObjects, allUnions);
		obj["extra_usings"] = ComputeExtraUsings(request.Namespace, allFields, allObjects);

		// Request descriptors are always non-generic
		obj["type_parameters"] = null;

		return obj;
	}

	public static ScriptObject BuildNamespaceClientContext(string namespaceName, IReadOnlyList<RequestShape> requests, IReadOnlyDictionary<string, ObjectShape> allObjects, IReadOnlyDictionary<string, TaggedUnionShape> allUnions)
	{
		var obj = new ScriptObject();
		obj["namespace"] = namespaceName;

		var ops = new ScriptArray();
		foreach (var req in requests)
		{
			var op = new ScriptObject();
			op["class_name"] = req.ClassName;
			op["response_name"] = req.Response.ClassName;
			op["endpoint_name"] = req.EndpointName;
			op["method_name"] = NamingConventions.OperationGroupToMethodName(req.OperationGroup);
			op["description"] = SanitizeDescription(req.Description) ?? req.OperationGroup;
			op["is_generic"] = req.Response.IsGeneric;
			op["type_parameters"] = req.Response.IsGeneric
				? new ScriptArray(req.Response.TypeParameters.Cast<object>())
				: null;
			op["descriptor_name"] = req.ClassName + "Descriptor";
			ops.Add(op);
		}
		obj["operations"] = ops;
		return obj;
	}

	private static ScriptArray BuildDescriptorFieldArray(IReadOnlyList<Field> fields, IReadOnlyDictionary<string, ObjectShape> allObjects, IReadOnlyDictionary<string, TaggedUnionShape> allUnions)
	{
		var arr = new ScriptArray();
		foreach (var field in fields)
		{
			var f = new ScriptObject();
			f["name"] = field.Name;
			f["description"] = SanitizeDescription(field.Description);

			// Use the same type computation as for POCO properties — always nullable for descriptors
			f["poco_type"] = ComputeDescriptorParamType(field);
			f["descriptor_kind"] = ComputeDescriptorKind(field, allObjects, allUnions);

			if (HasDescriptor(field.Type, allObjects, allUnions))
				f["descriptor_name"] = GetDescriptorName(field.Type);
			else
				f["descriptor_name"] = "";

			// For list types with descriptors, provide the item descriptor name and item type
			if (field.Type.Kind == TypeRefKind.List && field.Type.ItemType is not null && HasDescriptor(field.Type.ItemType, allObjects, allUnions))
			{
				f["item_descriptor_name"] = GetDescriptorName(field.Type.ItemType);
				f["item_type"] = field.Type.ItemType.CSharpName;
			}
			else
			{
				f["item_descriptor_name"] = "";
				f["item_type"] = "";
			}

			arr.Add(f);
		}
		return arr;
	}

	/// <summary>
	/// Returns the type string matching the POCO property type — descriptor setters must be assignment-compatible.
	/// </summary>
	private static string ComputeDescriptorParamType(Field field) => ComputePropertyType(field);

	private static string? SanitizeDescription(string? desc)
	{
		if (desc is null) return null;
		// Collapse multiline descriptions to single line for XML doc comments
		desc = desc.Replace("\r\n", " ").Replace("\n", " ").Replace("  ", " ").Trim();
		// XML-escape to prevent CS1570 warnings in generated doc comments
		return desc
			.Replace("&", "&amp;")
			.Replace("<", "&lt;")
			.Replace(">", "&gt;");
	}
}
