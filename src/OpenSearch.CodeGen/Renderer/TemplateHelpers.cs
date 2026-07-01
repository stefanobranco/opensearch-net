using OpenSearch.CodeGen.Model;
using OpenSearch.CodeGen.Transformer;
using Scriban.Runtime;

namespace OpenSearch.CodeGen.Renderer;

/// <summary>
/// Builds Scriban template context data from shape model objects.
/// </summary>
public static class TemplateHelpers
{
	public static ScriptObject BuildRequestContext(RequestShape request)
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
		obj["raw_body_type"] = RenderRawBodyType(request.RawBodyType);
		obj["is_head"] = request.IsHead;
		obj["is_plain_text_response"] = request.Response.IsPlainTextResponse;

		// Index-style operations: POST when no Id, PUT when Id is present
		var hasPathWithId = request.HttpPaths.Any(p => p.ParameterNames.Contains("id"));
		var hasPathWithoutId = request.HttpPaths.Any(p => !p.ParameterNames.Contains("id"));
		obj["method_varies_by_id"] = hasPathWithId && hasPathWithoutId && request.IsRawBody;

		obj["path_params"] = BuildFieldArray(request.PathParams);
		obj["query_params"] = BuildQueryParamArray(request.QueryParams);
		obj["body_fields"] = BuildFieldArray(request.BodyFields);
		obj["paths"] = BuildPathArray(request.HttpPaths, request.PathParams);

		// Generic endpoint support: if the response is generic, the endpoint must be generic too
		obj["response_type_parameters"] = request.Response.IsGeneric
			? new ScriptArray(request.Response.TypeParameters.Cast<object>())
			: null;

		// Cat namespace: inject format=json default so users get structured JSON responses
		obj["is_cat"] = request.Namespace == "Cat";

		return obj;
	}

	public static ScriptObject BuildResponseContext(ResponseShape response)
	{
		var obj = new ScriptObject();
		obj["namespace"] = response.Namespace;
		obj["class_name"] = response.ClassName;
		obj["description"] = SanitizeDescription(response.Description);
		obj["fields"] = BuildFieldArray(response.Fields);
		obj["type_parameters"] = response.IsGeneric
			? new ScriptArray(response.TypeParameters.Cast<object>())
			: null;
		obj["has_additional_properties"] = false;
		obj["is_plain_text_response"] = response.IsPlainTextResponse;
		obj["is_response"] = true;
		return obj;
	}

	public static ScriptObject BuildDictionaryResponseContext(ResponseShape response)
	{
		var obj = new ScriptObject();
		obj["namespace"] = response.Namespace;
		obj["class_name"] = response.ClassName;
		obj["description"] = SanitizeDescription(response.Description);
		obj["value_type"] = response.DictionaryValueType!.ToCSharpPropertyType();
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

	public static ScriptObject BuildObjectContext(ObjectShape objectShape)
	{
		var obj = new ScriptObject();
		obj["namespace"] = objectShape.Namespace;
		obj["class_name"] = objectShape.ClassName;
		obj["description"] = SanitizeDescription(objectShape.Description);
		obj["fields"] = BuildFieldArray(objectShape.Fields);
		obj["type_parameters"] = objectShape.IsGeneric
			? new ScriptArray(objectShape.TypeParameters.Cast<object>())
			: null;
		obj["has_additional_properties"] = objectShape.AdditionalPropertiesType is not null;
		obj["is_plain_text_response"] = false;
		obj["is_response"] = false;
		return obj;
	}

	public static ScriptObject BuildTaggedUnionContext(TaggedUnionShape union)
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

			PopulateFieldKeyedProperties(v, variant);
			variants.Add(v);
		}
		obj["variants"] = variants;
		obj["discriminator_property"] = union.DiscriminatorProperty;
		obj["sibling_fields"] = BuildFieldArray(union.SiblingFields);

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
	/// <summary>The C# type of the raw <c>Body</c> property: a specific nullable type for array/union/scalar
	/// bodies, or <c>object?</c> for a bare user document.</summary>
	private static string RenderRawBodyType(TypeRef? rawBodyType) =>
		rawBodyType is { } t
			? t.CSharpName + (t.CSharpName.EndsWith("?") ? "" : "?")
			: "object?";

	private static string ComputePropertyType(Field field)
	{
		var baseName = field.Type.CSharpName;

		if (field.Type.IsValueType)
		{
			// Required AND non-nullable value types stay non-nullable; otherwise get ?
			// A field can be required (always present) but nullable (oneOf: [number, null]).
			return (field.Required && !field.Type.IsNullable) ? baseName : baseName + "?";
		}

		// Reference types are always nullable in DTOs (no constructor enforcement)
		return baseName + "?";
	}

	private static ScriptArray BuildPathArray(IReadOnlyList<HttpPath> paths, IReadOnlyList<Field> pathParams)
	{
		var paramLookup = pathParams.ToDictionary(p => p.WireName, p => p.Name);
		var paramFieldLookup = pathParams.ToDictionary(p => p.WireName, p => p);

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
				var isListParam = paramFieldLookup.TryGetValue(pname, out var paramField) && paramField.Type.Kind == TypeRefKind.List;
				var valueExpr = isListParam
					? "string.Join(\",\", r." + csharpName + "!)"
					: "r." + csharpName + "!.ToString()!";
				interpolated = interpolated.Replace(
					"{" + pname + "}",
					"{Uri.EscapeDataString(" + valueExpr + ")}");
			}
			p["interpolated"] = interpolated;
			arr.Add(p);
		}
		return arr;
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
		var typeParams = objectShape.IsGeneric
			? new ScriptArray(objectShape.TypeParameters.Cast<object>())
			: null;
		var ctx = BuildDescriptorContext(objectShape.Namespace, objectShape.ClassName, objectShape.Fields, typeParams,
			isRawBody: false, allObjects, allUnions);
		ctx["has_additional_properties"] = objectShape.AdditionalPropertiesType is not null;
		return ctx;
	}

	/// <summary>
	/// The set of query/span types that need a generic (<c>&lt;TDocument&gt;</c>) descriptor so
	/// <see cref="Field"/> expressions thread all the way through nested clauses. Seeded with the
	/// <c>QueryContainer</c> and <c>SpanQuery</c> unions, then the fixpoint of every type reachable
	/// from their variants that (transitively) nests one of those unions.
	/// </summary>
	public static HashSet<string> ComputeGenericQueryTypes(
		IReadOnlyDictionary<string, ObjectShape> allObjects,
		IReadOnlyDictionary<string, TaggedUnionShape> allUnions)
	{
		var roots = new[] { "QueryContainer", "SpanQuery" }.Where(allUnions.ContainsKey).ToList();

		// 1. Everything reachable downward from those unions' variants — the query object graph.
		var reachable = new HashSet<string>(StringComparer.Ordinal);
		var stack = new Stack<string>(roots);
		while (stack.Count > 0)
		{
			var name = stack.Pop();
			if (!reachable.Add(name))
				continue;
			if (allUnions.TryGetValue(name, out var u))
				foreach (var variant in u.Variants)
					foreach (var t in TypeNames(variant.Type))
						stack.Push(t);
			if (allObjects.TryGetValue(name, out var o))
				foreach (var f in o.Fields)
					foreach (var t in TypeNames(f.Type))
						stack.Push(t);
		}

		// 2. Within the graph, a type is generic if it (transitively) nests a query/span union.
		var generic = new HashSet<string>(roots, StringComparer.Ordinal);
		bool changed = true;
		while (changed)
		{
			changed = false;
			foreach (var name in reachable)
			{
				if (generic.Contains(name) || !allObjects.TryGetValue(name, out var o))
					continue;
				if (o.Fields.Any(f => TypeNames(f.Type).Any(generic.Contains)))
					changed |= generic.Add(name);
			}
		}
		return generic;
	}

	/// <summary>The type names referenced by a <see cref="TypeRef"/> (itself plus element/key/value types).</summary>
	private static IEnumerable<string> TypeNames(TypeRef type)
	{
		yield return type.CSharpName;
		if (type.ItemType is not null)
			yield return type.ItemType.CSharpName;
		if (type.ValueType is not null)
			yield return type.ValueType.CSharpName;
	}

	/// <summary>Appends <c>&lt;TDocument&gt;</c> to a descriptor name when the described type has a generic descriptor.</summary>
	private static string GenericName(string descriptorName, string typeName, IReadOnlySet<string>? genericTypes) =>
		genericTypes is not null && genericTypes.Contains(typeName) ? descriptorName + "<TDocument>" : descriptorName;

	public static ScriptObject BuildTaggedUnionDescriptorContext(TaggedUnionShape union, IReadOnlyDictionary<string, ObjectShape> allObjects, IReadOnlyDictionary<string, TaggedUnionShape> allUnions, IReadOnlySet<string>? genericTypes = null)
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
				var dn = GetDescriptorName(variant.Type);
				v["descriptor_name"] = dn;
				v["generic_descriptor_name"] = GenericName(dn, variant.Type.CSharpName, genericTypes);
			}
			else
			{
				v["variant_kind"] = "simple";
				v["descriptor_name"] = "";
				v["generic_descriptor_name"] = "";
			}

			PopulateFieldKeyedProperties(v, variant, allObjects, allUnions, genericTypes);

			variants.Add(v);
		}
		obj["variants"] = variants;
		return obj;
	}

	public static ScriptObject BuildRequestDescriptorContext(RequestShape request, IReadOnlyDictionary<string, ObjectShape> allObjects, IReadOnlyDictionary<string, TaggedUnionShape> allUnions)
	{
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

		return BuildDescriptorContext(request.Namespace, request.ClassName, allFields, typeParameters: null,
			request.IsRawBody, allObjects, allUnions, rawBodyType: RenderRawBodyType(request.RawBodyType));
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

	/// <summary>
	/// Builds a generic (<c>&lt;TDocument&gt;</c>) descriptor context for an object that nests a query,
	/// so its query/span-typed fields build via the generic sub-descriptors and thread Field expressions.
	/// </summary>
	public static ScriptObject BuildGenericObjectDescriptorContext(ObjectShape objectShape, IReadOnlyDictionary<string, ObjectShape> allObjects, IReadOnlyDictionary<string, TaggedUnionShape> allUnions, IReadOnlySet<string> genericTypes)
	{
		var typeParams = new ScriptArray { "TDocument" };
		var ctx = BuildDescriptorContext(objectShape.Namespace, objectShape.ClassName, objectShape.Fields, typeParams,
			isRawBody: false, allObjects, allUnions, genericTypes);
		ctx["has_additional_properties"] = objectShape.AdditionalPropertiesType is not null;
		// The descriptor is generic (<TDocument>) but the value type it builds is not — only its
		// nested query/span fields carry TDocument (via the generic sub-descriptors).
		ctx["value_type"] = objectShape.ClassName;
		return ctx;
	}

	private static ScriptObject BuildDescriptorContext(
		string ns, string className, IReadOnlyList<Field> fields, ScriptArray? typeParameters,
		bool isRawBody, IReadOnlyDictionary<string, ObjectShape> allObjects, IReadOnlyDictionary<string, TaggedUnionShape> allUnions,
		IReadOnlySet<string>? genericTypes = null, string rawBodyType = "object?")
	{
		var obj = new ScriptObject();
		obj["namespace"] = ns;
		obj["class_name"] = className;
		obj["descriptor_name"] = className + "Descriptor";
		obj["fields"] = BuildDescriptorFieldArray(fields, allObjects, allUnions, genericTypes);
		obj["type_parameters"] = typeParameters;
		// By default the value type carries the same type parameters as the descriptor (e.g. Hit<TDocument>);
		// BuildGenericObjectDescriptorContext overrides this where the value type is non-generic.
		obj["value_type"] = className + (typeParameters is { Count: > 0 } ? "<" + string.Join(", ", typeParameters) + ">" : "");
		obj["is_raw_body"] = isRawBody;
		obj["raw_body_type"] = rawBodyType;
		obj["has_additional_properties"] = false;
		return obj;
	}

	private static ScriptArray BuildDescriptorFieldArray(IReadOnlyList<Field> fields, IReadOnlyDictionary<string, ObjectShape> allObjects, IReadOnlyDictionary<string, TaggedUnionShape> allUnions, IReadOnlySet<string>? genericTypes = null)
	{
		var arr = new ScriptArray();
		foreach (var field in fields)
		{
			var f = new ScriptObject();
			f["name"] = field.Name;
			f["description"] = SanitizeDescription(field.Description);
			f["poco_type"] = ComputePropertyType(field);
			f["descriptor_kind"] = ComputeDescriptorKind(field, allObjects, allUnions);

			if (HasDescriptor(field.Type, allObjects, allUnions))
				f["descriptor_name"] = GenericName(GetDescriptorName(field.Type), field.Type.CSharpName, genericTypes);
			else
				f["descriptor_name"] = "";

			// For list types with descriptors, provide the item descriptor name and item type
			if (field.Type.Kind == TypeRefKind.List && field.Type.ItemType is not null && HasDescriptor(field.Type.ItemType, allObjects, allUnions))
			{
				f["item_descriptor_name"] = GenericName(GetDescriptorName(field.Type.ItemType), field.Type.ItemType.CSharpName, genericTypes);
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
	/// Populates field-keyed variant properties on a ScriptObject.
	/// A variant is "field-keyed" when its type is Dictionary&lt;string, T&gt;.
	/// Always sets is_field_keyed and field_value_type.
	/// When allObjects/allUnions are provided, also sets field_has_descriptor and field_value_descriptor_name.
	/// </summary>
	private static void PopulateFieldKeyedProperties(
		ScriptObject v, UnionVariant variant,
		IReadOnlyDictionary<string, ObjectShape>? allObjects = null,
		IReadOnlyDictionary<string, TaggedUnionShape>? allUnions = null,
		IReadOnlySet<string>? genericTypes = null)
	{
		if (variant.Type.Kind == TypeRefKind.Dictionary
			&& variant.Type.KeyType?.Name == "string"
			&& variant.Type.ValueType is not null)
		{
			v["is_field_keyed"] = true;
			v["field_value_type"] = variant.Type.ValueType.CSharpName;

			if (allObjects is not null && allUnions is not null
				&& HasDescriptor(variant.Type.ValueType, allObjects, allUnions))
			{
				var dn = GetDescriptorName(variant.Type.ValueType);
				v["field_has_descriptor"] = true;
				v["field_value_descriptor_name"] = dn;
				v["field_value_generic_descriptor_name"] = GenericName(dn, variant.Type.ValueType.CSharpName, genericTypes);
			}
			else
			{
				v["field_has_descriptor"] = false;
				v["field_value_descriptor_name"] = "";
				v["field_value_generic_descriptor_name"] = "";
			}
		}
		else
		{
			v["is_field_keyed"] = false;
			v["field_value_type"] = "";
			v["field_has_descriptor"] = false;
			v["field_value_descriptor_name"] = "";
			v["field_value_generic_descriptor_name"] = "";
		}
	}

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
