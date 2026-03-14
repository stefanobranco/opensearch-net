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
		"PipelineName", "VersionString",
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

	// Common type aliases that map to long (numeric types serialized as JSON numbers)
	private static readonly HashSet<string> s_longAliases = new(StringComparer.OrdinalIgnoreCase)
	{
		"VersionNumber"
	};

	// Known property-based union types (all properties optional, only one set at a time)
	private static readonly HashSet<string> s_knownTaggedUnions = new(StringComparer.OrdinalIgnoreCase)
	{
		"QueryContainer"
	};

	private readonly Dictionary<string, TypeRef> _namedTypes = new(StringComparer.Ordinal);
	private readonly Dictionary<string, EnumShape> _discoveredEnums = new(StringComparer.Ordinal);
	private readonly Dictionary<string, ObjectShape> _discoveredObjects = new(StringComparer.Ordinal);
	private readonly Dictionary<string, TaggedUnionShape> _discoveredTaggedUnions = new(StringComparer.Ordinal);
	private string _targetNamespace;

	public TypeMapper(string targetNamespace)
	{
		_targetNamespace = targetNamespace;
	}

	/// <summary>
	/// Updates the target namespace for subsequent type discovery.
	/// Used when a shared TypeMapper processes multiple namespaces.
	/// </summary>
	public void SetTargetNamespace(string targetNamespace) => _targetNamespace = targetNamespace;

	public IReadOnlyDictionary<string, EnumShape> DiscoveredEnums => _discoveredEnums;
	public IReadOnlyDictionary<string, ObjectShape> DiscoveredObjects => _discoveredObjects;
	public IReadOnlyDictionary<string, TaggedUnionShape> DiscoveredTaggedUnions => _discoveredTaggedUnions;

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

		// Check if it's a numeric alias
		if (s_longAliases.Contains(schemaName))
			return TypeRef.Long();

		// Check for generic type parameter (e.g., TDocument, TBucket)
		if (resolved.IsGenericTypeParameter)
			return TypeRef.GenericParam(schemaName);

		// Determine the correct namespace from the ref path (not always _targetNamespace)
		var nsFromRef = ExtractNamespaceFromRef(refString);

		// Check for enum
		if (resolved.EnumValues.Count > 0)
			return GetOrCreateEnum(schemaName, resolved, nsFromRef);

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
				return GetOrCreateObject(schemaName, resolved, nsFromRef);
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

		// allOf → follow the first $ref member if available, otherwise JsonElement
		if (schema.AllOf.Count > 0)
		{
			// Try to find a $ref member — use that as the primary type
			foreach (var member in schema.AllOf)
			{
				if (member.Ref is not null)
					return Map(member);
			}
			return TypeRef.JsonElement();
		}

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

	private TypeRef GetOrCreateEnum(string schemaName, OpenApiSchema schema, string? namespaceOverride = null)
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
			Namespace = namespaceOverride ?? "Common",
			Description = schema.Description,
			Variants = variants
		};
		_discoveredEnums[schemaName] = enumShape;

		var typeRef = TypeRef.Named(schemaName, className, isEnum: true);
		_namedTypes[schemaName] = typeRef;
		return typeRef;
	}

	private TypeRef GetOrCreateObject(string schemaName, OpenApiSchema schema, string? namespaceOverride = null)
	{
		var className = NamingConventions.SchemaNameToClassName(schemaName);
		if (_namedTypes.TryGetValue(schemaName, out var existing))
			return existing;

		// Check if this is a known tagged union type
		if (s_knownTaggedUnions.Contains(schemaName))
			return GetOrCreateTaggedUnion(schemaName, schema, namespaceOverride);

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

		var ns = ResolveNamespace(namespaceOverride);

		NamingConventions.FixFieldNameClash(fields, className);

		// Discover generic type parameters referenced by any field
		var typeParams = CollectGenericParams(fields);

		var objectShape = new ObjectShape
		{
			ClassName = className,
			Namespace = ns,
			Description = schema.Description,
			Fields = fields,
			AdditionalPropertiesType = additionalPropsType,
			TypeParameters = typeParams
		};
		_discoveredObjects[schemaName] = objectShape;

		// Update the TypeRef CSharpName to include generic parameters (e.g., "Hit" → "Hit<TDocument>")
		if (typeParams.Count > 0)
		{
			var genericSuffix = "<" + string.Join(", ", typeParams) + ">";
			_namedTypes[schemaName] = TypeRef.Named(schemaName, className + genericSuffix);
		}

		return _namedTypes[schemaName];
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

	private TypeRef GetOrCreateTaggedUnion(string schemaName, OpenApiSchema schema, string? namespaceOverride)
	{
		var className = NamingConventions.SchemaNameToClassName(schemaName);
		var kindEnumName = className.Replace("Container", "") + "Kind";

		// Reserve the name
		var typeRef = TypeRef.Named(schemaName, className);
		_namedTypes[schemaName] = typeRef;

		var variants = new List<UnionVariant>();
		foreach (var (name, propSchema) in schema.Properties)
		{
			if (name.Contains('.'))
				continue;

			var variantType = Map(propSchema);
			variants.Add(new UnionVariant
			{
				Name = NamingConventions.ToPascalCase(name),
				WireName = name,
				Type = variantType,
				Description = propSchema.Description
			});
		}

		var ns = ResolveNamespace(namespaceOverride);

		var unionShape = new TaggedUnionShape
		{
			ClassName = className,
			Namespace = ns,
			Description = schema.Description,
			KindEnumName = kindEnumName,
			Variants = variants
		};
		_discoveredTaggedUnions[schemaName] = unionShape;

		return typeRef;
	}

	/// <summary>
	/// Collects generic type parameter names from a list of fields.
	/// Checks both direct GenericParameter TypeRefs and Named types whose
	/// referenced ObjectShape is itself generic.
	/// </summary>
	internal IReadOnlyList<string> CollectGenericParams(IReadOnlyList<Field> fields)
	{
		var result = new List<string>();
		var seen = new HashSet<string>();
		foreach (var field in fields)
			CollectGenericParamsFromType(field.Type, result, seen);
		return result;
	}

	private void CollectGenericParamsFromType(TypeRef type, List<string> result, HashSet<string> seen)
	{
		if (type.IsGenericParameter && seen.Add(type.Name))
			result.Add(type.Name);
		// Check if this is a Named type that references a generic object
		if (type.Kind == TypeRefKind.Named && !type.IsEnum
			&& _discoveredObjects.TryGetValue(type.Name, out var genObj) && genObj.IsGeneric)
		{
			foreach (var tp in genObj.TypeParameters)
			{
				if (seen.Add(tp))
					result.Add(tp);
			}
		}
		if (type.ItemType is not null)
			CollectGenericParamsFromType(type.ItemType, result, seen);
		if (type.KeyType is not null)
			CollectGenericParamsFromType(type.KeyType, result, seen);
		if (type.ValueType is not null)
			CollectGenericParamsFromType(type.ValueType, result, seen);
	}

	/// <summary>
	/// Propagates generic type parameters from child objects to parent objects,
	/// then updates all field TypeRefs to include the correct generic CSharpNames.
	/// Must be called after all types are discovered.
	/// </summary>
	public void PropagateGenericParams()
	{
		// Phase 1: Propagate generic params upward
		bool changed = true;
		while (changed)
		{
			changed = false;
			foreach (var kvp in _discoveredObjects.ToList())
			{
				var obj = kvp.Value;
				if (obj.IsGeneric) continue;

				var typeParams = CollectGenericParams(obj.Fields);
				if (typeParams.Count > 0)
				{
					_discoveredObjects[kvp.Key] = new ObjectShape
					{
						ClassName = obj.ClassName,
						Namespace = obj.Namespace,
						Description = obj.Description,
						Fields = obj.Fields,
						AdditionalPropertiesType = obj.AdditionalPropertiesType,
						TypeParameters = typeParams
					};

					var genericSuffix = "<" + string.Join(", ", typeParams) + ">";
					_namedTypes[kvp.Key] = TypeRef.Named(kvp.Key, obj.ClassName + genericSuffix);
					changed = true;
				}
			}
		}

		// Phase 2: Update all field TypeRefs to reference the final generic CSharpNames
		foreach (var obj in _discoveredObjects.Values)
		{
			UpdateFieldTypeRefs((List<Field>)obj.Fields);
		}
	}

	private void UpdateFieldTypeRefs(List<Field> fields)
	{
		for (int i = 0; i < fields.Count; i++)
		{
			var updated = UpdateTypeRef(fields[i].Type);
			if (updated != fields[i].Type)
			{
				fields[i] = new Field
				{
					Name = fields[i].Name,
					WireName = fields[i].WireName,
					Type = updated,
					Required = fields[i].Required,
					Description = fields[i].Description,
					Deprecated = fields[i].Deprecated
				};
			}
		}
	}

	private TypeRef UpdateTypeRef(TypeRef type)
	{
		if (type.Kind == TypeRefKind.Named && !type.IsEnum && _namedTypes.TryGetValue(type.Name, out var latest))
		{
			if (latest.CSharpName != type.CSharpName)
				return latest;
		}
		if (type.Kind == TypeRefKind.List && type.ItemType is not null)
		{
			var updatedItem = UpdateTypeRef(type.ItemType);
			if (updatedItem != type.ItemType)
				return TypeRef.ListOf(updatedItem);
		}
		if (type.Kind == TypeRefKind.Dictionary && type.ValueType is not null)
		{
			var updatedValue = UpdateTypeRef(type.ValueType);
			if (updatedValue != type.ValueType)
				return TypeRef.DictOf(type.KeyType!, updatedValue);
		}
		return type;
	}

	private string ResolveNamespace(string? namespaceOverride) =>
		namespaceOverride
		?? (_targetNamespace.Equals("_common", StringComparison.OrdinalIgnoreCase)
			? "Common"
			: NamingConventions.NamespaceToClassName(_targetNamespace));

	/// <summary>
	/// Extracts the C# namespace from a $ref file path.
	/// E.g., "../schemas/_common.yaml#/..." → "Common"
	///       "../schemas/_common.query_dsl.yaml#/..." → "Common"
	///       "../schemas/_core.search.yaml#/..." → "Core"
	///       "#/components/schemas/..." → null (local ref, use current namespace)
	/// </summary>
	private string? ExtractNamespaceFromRef(string refString)
	{
		var hashIndex = refString.IndexOf('#');
		if (hashIndex <= 0) return null; // local ref → use current namespace

		var filePart = refString[..hashIndex];
		var fileName = Path.GetFileNameWithoutExtension(filePart);
		// "_common.query_dsl" → first segment is "_common"
		// "_core.search" → first segment is "_core"
		var firstSegment = fileName.Split('.')[0];
		return NamingConventions.NamespaceToClassName(firstSegment);
	}
}
