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
		obj["is_head"] = request.IsHead;

		obj["path_params"] = BuildFieldArray(request.PathParams);
		obj["query_params"] = BuildQueryParamArray(request.QueryParams);
		obj["body_fields"] = BuildFieldArray(request.BodyFields);
		obj["paths"] = BuildPathArray(request.HttpPaths, request.PathParams);

		return obj;
	}

	public static ScriptObject BuildResponseContext(ResponseShape response)
	{
		var obj = new ScriptObject();
		obj["namespace"] = response.Namespace;
		obj["class_name"] = response.ClassName;
		obj["description"] = SanitizeDescription(response.Description);
		obj["fields"] = BuildFieldArray(response.Fields);
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
		return obj;
	}

	public static ScriptObject BuildNamespaceClientContext(string namespaceName, IReadOnlyList<RequestShape> requests)
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
			ops.Add(op);
		}
		obj["operations"] = ops;
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
			f["type"] = ComputePropertyType(field);
			f["required"] = field.Required;
			f["description"] = SanitizeDescription(field.Description);
			f["deprecated"] = field.Deprecated;
			// All non-string types need .ToString() for Uri.EscapeDataString
			f["to_string"] = field.Type.Name != "string" ? ".ToString()" : "";
			arr.Add(f);
		}
		return arr;
	}

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

		var arr = new ScriptArray();
		foreach (var path in paths)
		{
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

	private static string? SanitizeDescription(string? desc)
	{
		if (desc is null) return null;
		// Collapse multiline descriptions to single line for XML doc comments
		return desc.Replace("\r\n", " ").Replace("\n", " ").Replace("  ", " ").Trim();
	}
}
