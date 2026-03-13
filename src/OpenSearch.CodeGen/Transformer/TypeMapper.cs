using OpenSearch.CodeGen.Model;
using OpenSearch.CodeGen.OpenApi;

namespace OpenSearch.CodeGen.Transformer;

/// <summary>
/// Maps OpenAPI schemas to TypeRef instances.
/// </summary>
public sealed class TypeMapper
{
	// Common type aliases that map to simple C# types
	private static readonly HashSet<string> s_stringAliases = new(StringComparer.OrdinalIgnoreCase)
	{
		"IndexName", "Indices", "Id", "Ids", "Name", "Names",
		"Duration", "Field", "Fields", "Routing", "Uuid",
		"IndexAlias", "ScrollId", "TaskId", "NodeId", "NodeIds",
		"PipelineName", "VersionNumber", "VersionString",
		"Percentage", "HumanReadableByteCount", "ByteCount",
		"DateTime", "DateString", "StringifiedInteger",
		"StringifiedBoolean", "StringifiedLong",
		"StringifiedEpochTimeUnitMillis", "StringifiedEpochTimeUnitSeconds",
		"EpochTimeUnitMillis", "EpochTimeUnitSeconds",
		"DurationLarge", "UriReference", "Host", "Ip",
		"ScrollIds", "StringOrStringArray", "Suggestion",
		"Type", "Username", "Password",
		"Namespace", "BulkByScrollTaskStatusOrException",
		"ExpandWildcardOptions", "WaitForActiveShards"
	};

	private readonly Dictionary<string, TypeRef> _namedTypes = new(StringComparer.Ordinal);
	private readonly Dictionary<string, EnumShape> _discoveredEnums = new(StringComparer.Ordinal);
	private readonly Dictionary<string, ObjectShape> _discoveredObjects = new(StringComparer.Ordinal);
	private readonly string _targetNamespace;

	public TypeMapper(string targetNamespace)
	{
		_targetNamespace = targetNamespace;
	}

	public IReadOnlyDictionary<string, EnumShape> DiscoveredEnums => _discoveredEnums;
	public IReadOnlyDictionary<string, ObjectShape> DiscoveredObjects => _discoveredObjects;

	/// <summary>
	/// Maps an OpenAPI schema to a TypeRef.
	/// </summary>
	public TypeRef Map(OpenApiSchema schema)
	{
		// Handle $ref first
		if (schema.Ref is not null)
		{
			var resolved = schema.Resolved();
			return MapResolved(resolved, schema.Ref);
		}

		return MapDirect(schema);
	}

	private TypeRef MapResolved(OpenApiSchema resolved, string refString)
	{
		// Extract the schema name from the ref
		var schemaName = ExtractSchemaName(refString);

		// Check if it's a common string alias
		if (s_stringAliases.Contains(schemaName))
			return TypeRef.String();

		// Check for enum
		if (resolved.EnumValues.Count > 0)
			return GetOrCreateEnum(schemaName, resolved);

		// Check for simple type (not an object with properties)
		var type = resolved.Type;
		if (type is "string")
			return TypeRef.String();
		if (type is "boolean")
			return TypeRef.Bool();
		if (type is "integer")
			return resolved.Format == "int64" ? TypeRef.Long() : TypeRef.Int();
		if (type is "number")
			return resolved.Format == "double" ? TypeRef.Double() : TypeRef.Float();

		// For objects, check if it's a dictionary type
		if (type is "object" or null)
		{
			// Object with additionalProperties but no properties → Dictionary
			if (resolved.HasAdditionalProperties && resolved.Properties.Count == 0)
			{
				var valueType = resolved.AdditionalProperties is not null
					? Map(resolved.AdditionalProperties)
					: TypeRef.Object();
				return TypeRef.DictOf(TypeRef.String(), valueType);
			}

			// Object with properties → named type
			if (resolved.Properties.Count > 0 || resolved.AllOf.Count > 0)
			{
				return GetOrCreateObject(schemaName, resolved);
			}

			// Empty object or complex type → JsonElement for MVP
			return TypeRef.JsonElement();
		}

		if (type is "array")
		{
			var itemType = resolved.Items is not null ? Map(resolved.Items) : TypeRef.Object();
			return TypeRef.ListOf(itemType);
		}

		return TypeRef.JsonElement();
	}

	private TypeRef MapDirect(OpenApiSchema schema)
	{
		var type = schema.Type;
		var nullable = schema.IsNullable;

		// oneOf → simplify to most general type for MVP
		if (schema.OneOf.Count > 0)
			return TypeRef.JsonElement();

		// allOf → flatten into object (handled during object creation)
		if (schema.AllOf.Count > 0)
			return TypeRef.JsonElement();

		// enum
		if (schema.EnumValues.Count > 0)
		{
			// Inline enum — generate a name from context
			return TypeRef.String(); // For inline enums in MVP, just use string
		}

		if (type is "string")
			return TypeRef.String().WithNullable(nullable);
		if (type is "boolean")
			return TypeRef.Bool().WithNullable(nullable);
		if (type is "integer")
			return (schema.Format == "int64" ? TypeRef.Long() : TypeRef.Int()).WithNullable(nullable);
		if (type is "number")
			return (schema.Format == "double" ? TypeRef.Double() : TypeRef.Float()).WithNullable(nullable);

		if (type is "array")
		{
			var itemType = schema.Items is not null ? Map(schema.Items) : TypeRef.Object();
			return TypeRef.ListOf(itemType);
		}

		if (type is "object" or null)
		{
			// additionalProperties with no named properties → Dictionary
			if (schema.HasAdditionalProperties && schema.Properties.Count == 0)
			{
				var valueType = schema.AdditionalProperties is not null
					? Map(schema.AdditionalProperties)
					: TypeRef.Object();
				return TypeRef.DictOf(TypeRef.String(), valueType);
			}

			// Object with properties but no $ref name → inline, return JsonElement for MVP
			if (schema.Properties.Count > 0)
				return TypeRef.JsonElement();

			return TypeRef.JsonElement();
		}

		return TypeRef.JsonElement();
	}

	private TypeRef GetOrCreateEnum(string schemaName, OpenApiSchema schema)
	{
		var className = NamingConventions.SchemaNameToClassName(schemaName);
		if (_namedTypes.TryGetValue(schemaName, out var existing))
			return existing;

		var variants = schema.EnumValues.Select(v => new EnumVariant
		{
			Name = NamingConventions.EnumValueToMemberName(v),
			WireValue = v
		}).ToList();

		var enumShape = new EnumShape
		{
			ClassName = className,
			Namespace = "Common",
			Description = schema.Description,
			Variants = variants
		};
		_discoveredEnums[schemaName] = enumShape;

		var typeRef = TypeRef.Named(schemaName, className);
		_namedTypes[schemaName] = typeRef;
		return typeRef;
	}

	private TypeRef GetOrCreateObject(string schemaName, OpenApiSchema schema)
	{
		var className = NamingConventions.SchemaNameToClassName(schemaName);
		if (_namedTypes.TryGetValue(schemaName, out var existing))
			return existing;

		// Reserve the name first to prevent infinite recursion
		var typeRef = TypeRef.Named(schemaName, className);
		_namedTypes[schemaName] = typeRef;

		var fields = new List<Field>();
		var required = new HashSet<string>(schema.Required);

		// Handle allOf by flattening
		if (schema.AllOf.Count > 0)
		{
			foreach (var allOfMember in schema.AllOf)
			{
				var resolved = allOfMember.Resolved();
				CollectFields(resolved, fields, required);
			}
		}
		else
		{
			CollectFields(schema, fields, required);
		}

		TypeRef? additionalPropsType = null;
		if (schema.AdditionalProperties is not null)
			additionalPropsType = Map(schema.AdditionalProperties);

		var objectShape = new ObjectShape
		{
			ClassName = className,
			Namespace = _targetNamespace.Equals("_common", StringComparison.OrdinalIgnoreCase) ? "Common" : NamingConventions.NamespaceToClassName(_targetNamespace),
			Description = schema.Description,
			Fields = fields,
			AdditionalPropertiesType = additionalPropsType
		};
		_discoveredObjects[schemaName] = objectShape;

		return typeRef;
	}

	private void CollectFields(OpenApiSchema schema, List<Field> fields, HashSet<string> required)
	{
		// Merge required from this schema
		foreach (var r in schema.Required)
			required.Add(r);

		var existingNames = new HashSet<string>(fields.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);

		foreach (var (name, propSchema) in schema.Properties)
		{
			// Skip dotted properties like "blocks.read_only" (flattened settings)
			if (name.Contains('.'))
				continue;

			var pascalName = NamingConventions.ToPascalCase(name);
			// Skip duplicate property names (can happen with allOf merging)
			if (!existingNames.Add(pascalName))
				continue;

			var fieldType = Map(propSchema);
			fields.Add(new Field
			{
				Name = pascalName,
				WireName = name,
				Type = fieldType,
				Required = required.Contains(name),
				Description = propSchema.Description,
				Deprecated = propSchema.Deprecated
			});
		}
	}

	private static string ExtractSchemaName(string refString)
	{
		// "../schemas/_common.yaml#/components/schemas/IndexName" → "IndexName"
		// "#/components/schemas/IndexName" → "IndexName"
		var lastSlash = refString.LastIndexOf('/');
		return lastSlash >= 0 ? refString[(lastSlash + 1)..] : refString;
	}
}
