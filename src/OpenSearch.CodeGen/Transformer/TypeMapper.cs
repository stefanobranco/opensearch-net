using OpenSearch.CodeGen.Model;
using OpenSearch.CodeGen.OpenApi;

namespace OpenSearch.CodeGen.Transformer;

/// <summary>
/// Maps OpenAPI schemas to TypeRef instances.
/// </summary>
public sealed class TypeMapper
{
	// Explicit numeric aliases that can't be auto-detected from schema shape
	// (VersionNumber is type: integer in the spec but we want to map it to long)
	private static readonly HashSet<string> s_longAliases = new(StringComparer.OrdinalIgnoreCase)
	{
		"VersionNumber"
	};

	/// <summary>
	/// Minimum number of optional properties for a schema to be considered a property-keyed tagged union.
	/// </summary>
	private const int PropertyKeyedUnionMinProperties = 10;

	/// <summary>
	/// Schemas that are known to be property-keyed tagged unions but can't be
	/// reliably auto-detected (e.g., because they have inline object properties
	/// that look like regular object fields to the heuristic).
	/// </summary>
	private static readonly HashSet<string> s_knownTaggedUnions = new(StringComparer.OrdinalIgnoreCase)
	{
		"QueryContainer"
	};

	/// <summary>
	/// Extra sibling fields to inject into specific tagged unions.
	/// Used when the spec defines fields on variant base types (e.g., BucketAggregationBase)
	/// that we want on the container for simplicity.
	/// </summary>
	private static readonly Dictionary<string, List<Field>> s_additionalSiblingFields = new(StringComparer.OrdinalIgnoreCase)
	{
		["AggregationContainer"] =
		[
			new Field
			{
				Name = "Aggregations",
				WireName = "aggregations",
				Type = TypeRef.DictOf(TypeRef.String(), TypeRef.Named("AggregationContainer", "AggregationContainer")),
				Required = false,
				Description = "Sub-aggregations for this aggregation."
			},
			new Field
			{
				Name = "Aggs",
				WireName = "aggs",
				Type = TypeRef.DictOf(TypeRef.String(), TypeRef.Named("AggregationContainer", "AggregationContainer")),
				Required = false,
				Description = "Sub-aggregations for this aggregation (alias for 'aggregations')."
			}
		]
	};

	/// <summary>
	/// Schema names that map to hand-written C# types. When Map() encounters one of these,
	/// it returns the override TypeRef instead of processing the schema.
	/// </summary>
	private static readonly Dictionary<string, TypeRef> s_typeOverrides = new(StringComparer.OrdinalIgnoreCase)
	{
		["SortCombinations"] = TypeRef.Named("SortOptions", "SortOptions"),
		["SortOptions"] = TypeRef.Named("SortOptions", "SortOptions"),
		["Sort"] = TypeRef.ListOf(TypeRef.Named("SortOptions", "SortOptions")),
		["SourceConfig"] = TypeRef.Named("SourceConfig", "SourceConfig"),
		["HighlightFields"] = TypeRef.DictOf(TypeRef.String(), TypeRef.Named("HighlightField", "HighlightField")),
		["TotalHits"] = TypeRef.Named("TotalHits", "TotalHits"),
		["ErrorCause"] = TypeRef.Named("ErrorCause", "OpenSearch.Net.ErrorCause"),
		["ResponseItem"] = TypeRef.Named("MgetResponseItem", "MgetResponseItem"), // oneOf GetResult|MultiGetError → hand-written
		["Aggregate"] = TypeRef.Named("Aggregate", "Aggregate"), // non-generic; TDocument only matters for top_hits (lazily deserialized)
	};

	/// <summary>
	/// Schema names whose generated types are replaced by hand-written types.
	/// Includes all override keys plus schemas referenced only by overridden types.
	/// </summary>
	/// <summary>
	/// Schema names that should emit [JsonExtensionData] even though the spec doesn't declare additionalProperties.
	/// Aggregate needs it to capture sub-aggregation results as extra JSON properties.
	/// </summary>
	private static readonly HashSet<string> s_forceAdditionalProperties = new(StringComparer.OrdinalIgnoreCase)
	{
		"Aggregate"
	};

	private static readonly HashSet<string> s_skipGeneration = new(
		s_typeOverrides.Keys.Concat(["SortOptions", "FieldSort", "SourceFilter", "TotalHits", "TotalHitsRelation"]),
		StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, TypeRef> _namedTypes = new(StringComparer.Ordinal);
	private readonly Dictionary<string, EnumShape> _discoveredEnums = new(StringComparer.Ordinal);
	private readonly Dictionary<string, ObjectShape> _discoveredObjects = new(StringComparer.Ordinal);
	private readonly Dictionary<string, TaggedUnionShape> _discoveredTaggedUnions = new(StringComparer.Ordinal);
	private readonly List<string> _oneOfFallbacks = new();
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
	public IReadOnlyList<string> OneOfFallbacks => _oneOfFallbacks;

	/// <summary>
	/// Maps an OpenAPI schema to a TypeRef.
	/// </summary>
	public TypeRef Map(OpenApiSchema schema)
	{
		// Handle $ref first — use QualifiedRef so local refs (#/...) carry their source file
		// context, enabling correct namespace resolution for cross-file type deduplication.
		if (schema.Ref is not null)
		{
			var resolved = schema.Resolved();
			return MapResolved(resolved, schema.QualifiedRef!);
		}

		return MapDirect(schema);
	}

	private TypeRef MapResolved(OpenApiSchema resolved, string refString)
	{
		// Extract the schema name from the ref
		var schemaName = ExtractSchemaName(refString);

		// Check for hand-written type overrides before any other processing.
		// Still walk the schema to discover referenced sub-types (e.g., ScoreSort from SortOptions).
		if (s_typeOverrides.TryGetValue(schemaName, out var overrideType))
		{
			if (_namedTypes.TryAdd(schemaName, overrideType))
				DiscoverReferencedTypes(resolved);
			return overrideType;
		}

		// Check if it's a numeric alias (explicit override)
		if (s_longAliases.Contains(schemaName))
			return TypeRef.Long();

		// Auto-detect string aliases: a $ref to a schema that is type: string
		// with no enum values and no properties is a string alias
		if (IsStringAlias(resolved))
			return TypeRef.String();

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

			// Named schema that IS a oneOf (e.g., DecayPlacement, RangeQuery, Script)
			if (resolved.OneOf.Count > 0)
				return MapOneOf(resolved.OneOf, schemaName, nsFromRef, resolved.DiscriminatorPropertyName);

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

		// oneOf → dispatch to pattern-matching resolver
		if (schema.OneOf.Count > 0)
			return MapOneOf(schema.OneOf);

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

	/// <summary>
	/// Pattern-matches oneOf alternatives and returns the best TypeRef.
	/// When called from MapResolved, schemaName and ns are available for creating named types.
	/// When called from MapDirect (inline oneOf), they are null.
	/// </summary>
	private TypeRef MapOneOf(IReadOnlyList<OpenApiSchema> schemas, string? schemaName = null, string? ns = null, string? discriminatorProperty = null)
	{
		// Pattern 1: Nullable wrapper — 2 members, one is type: 'null'
		if (schemas.Count == 2)
		{
			var nullIndex = -1;
			for (int i = 0; i < 2; i++)
			{
				if (schemas[i].Type == "null")
				{
					nullIndex = i;
					break;
				}
			}
			if (nullIndex >= 0)
			{
				var nonNull = schemas[1 - nullIndex];
				return Map(nonNull).WithNullable(true);
			}
		}

		// Pattern 2: Single-or-array — $ref + array with same items.$ref
		if (schemas.Count == 2)
		{
			OpenApiSchema? singleSchema = null;
			OpenApiSchema? arraySchema = null;
			for (int i = 0; i < 2; i++)
			{
				if (schemas[i].Type == "array")
					arraySchema = schemas[i];
				else
					singleSchema = schemas[i];
			}
			if (singleSchema is not null && arraySchema?.Items is not null)
			{
				var singleRef = singleSchema.Ref;
				var arrayItemRef = arraySchema.Items.Ref;
				if (singleRef is not null && arrayItemRef is not null
					&& string.Equals(singleRef, arrayItemRef, StringComparison.Ordinal))
				{
					var itemType = Map(singleSchema);
					return TypeRef.ListOf(itemType);
				}
			}
		}

		// Pattern 2b: String-or-array — type:string + type:array → List<string>
		// (e.g., StringOrStringArray — accept a single string or array of strings)
		if (schemas.Count == 2)
		{
			bool hasString = false;
			bool hasArray = false;
			for (int i = 0; i < 2; i++)
			{
				if (schemas[i].Type == "string") hasString = true;
				if (schemas[i].Type == "array") hasArray = true;
			}
			if (hasString && hasArray)
				return TypeRef.ListOf(TypeRef.String());
		}

		// Pattern 3: Primitive union — all members are inline primitives, string among them
		{
			var allPrimitives = true;
			var hasString = false;
			foreach (var s in schemas)
			{
				var t = s.Type;
				if (t is not ("string" or "integer" or "number" or "boolean" or "null"))
				{
					allPrimitives = false;
					break;
				}
				if (t == "string") hasString = true;
			}
			if (allPrimitives && hasString)
				return TypeRef.String();
		}

		// Pattern 3b: Ref + primitive — one $ref member + one inline primitive type.
		// Uses Map() to fully resolve the ref (follows $ref chains, string aliases, etc.)
		// then decides based on the resolved TypeRef.
		if (schemas.Count == 2)
		{
			OpenApiSchema? refSchema = null;
			OpenApiSchema? inlineSchema = null;
			for (int i = 0; i < 2; i++)
			{
				if (schemas[i].Ref is not null) refSchema = schemas[i];
				else inlineSchema = schemas[i];
			}
			if (refSchema is not null && inlineSchema is not null)
			{
				var inlineType = inlineSchema.Type;
				var refType = Map(refSchema);

				// Ref resolves to string + inline is string → string
				// (e.g., HighlighterType: oneOf[$ref:BuiltinHighlighterType, type:string])
				if (refType.Name == "string" && inlineType == "string")
					return TypeRef.String();

				// Ref resolves to enum + inline is string → string (enum values are valid strings)
				// (e.g., SimpleQueryStringFlags: oneOf[$ref:SimpleQueryStringFlag, type:string])
				if (refType.IsEnum && inlineType == "string")
					return TypeRef.String();

				// Ref resolves to a primitive (int, long, etc.) + inline is string → string
				// (e.g., DateTime: oneOf[type:string, $ref:EpochTimeUnitMillis → long])
				if (refType.IsValueType && inlineType == "string")
					return TypeRef.String();

				// Both resolve to primitives → use the wider type
				if (refType.Kind == TypeRefKind.Primitive && inlineType is "string" or "boolean" or "integer" or "number")
				{
					if (refType.Name == "string" || inlineType == "string")
						return TypeRef.String();
				}

				// Ref resolves to a named object type + inline is integer/number → use the named type
				// (e.g., TotalHits: oneOf[$ref:TotalHits, type:integer] — bare integer is shorthand)
				if (refType.Kind == TypeRefKind.Named && !refType.IsEnum
					&& inlineType is "integer" or "number")
					return refType;
			}
		}

		// Pattern 4: Shorthand-or-expanded — 2 members: simple type + allOf with properties
		if (schemas.Count == 2)
		{
			OpenApiSchema? expanded = null;
			for (int i = 0; i < 2; i++)
			{
				if (schemas[i].AllOf.Count > 0)
				{
					expanded = schemas[i];
					break;
				}
			}
			if (expanded is not null)
			{
				if (schemaName is not null)
				{
					// Create a named object from the expanded allOf form
					return GetOrCreateObject(schemaName, expanded, ns);
				}
				// No name available — follow the first $ref in allOf
				foreach (var member in expanded.AllOf)
				{
					if (member.Ref is not null)
						return Map(member);
				}
			}
		}

		// Pattern 5: All-refs union — all members are $ref to named types.
		// Only fire when a discriminator is present (internally-tagged unions like Property, Analyzer).
		// Structural unions without discriminators (SortCombinations, GeoBounds, DecayPlacement)
		// fall through to Pattern 6 → JsonElement and are handled by hand-written types.
		if (discriminatorProperty is not null && schemas.Count >= 2 && schemaName is not null)
		{
			var allRefs = true;
			foreach (var s in schemas)
			{
				if (s.Ref is null)
				{
					allRefs = false;
					break;
				}
			}
			if (allRefs)
				return GetOrCreateOneOfTaggedUnion(schemaName, schemas, ns, discriminatorProperty);
		}

		// Pattern 6: Fallback → JsonElement + record warning
		var context = schemaName ?? "inline";
		var memberDescs = string.Join(", ", schemas.Select(s =>
			s.Ref is not null ? $"$ref:{ExtractSchemaName(s.Ref)}" :
			s.Type is not null ? $"type:{s.Type}" :
			s.AllOf.Count > 0 ? "allOf" : "unknown"));
		_oneOfFallbacks.Add($"{context}: oneOf[{memberDescs}]");
		return TypeRef.JsonElement();
	}

	/// <summary>
	/// Creates a TaggedUnion from a oneOf where all members are $ref to named types.
	/// When a discriminator property is present (e.g., "type" for Property), extracts
	/// the actual wire value from each variant's schema instead of using the class name.
	/// </summary>
	private TypeRef GetOrCreateOneOfTaggedUnion(string schemaName, IReadOnlyList<OpenApiSchema> oneOfSchemas, string? namespaceOverride, string? discriminatorProperty = null)
	{
		if (_namedTypes.TryGetValue(schemaName, out var existing))
			return existing;

		var variants = new List<UnionVariant>();
		foreach (var member in oneOfSchemas)
		{
			if (member.Ref is null) continue;
			var variantSchemaName = ExtractSchemaName(member.Ref);
			var variantType = Map(member);
			var title = member.Title;

			// When a discriminator is present, extract the wire value from the variant's
			// discriminator field enum (e.g., KeywordProperty's "type" enum: ["keyword"])
			string wireName;
			if (discriminatorProperty is not null)
			{
				wireName = ExtractDiscriminatorValue(member.Resolved(), discriminatorProperty)
					?? title ?? variantSchemaName;
			}
			else
			{
				wireName = title ?? variantSchemaName;
			}

			variants.Add(new UnionVariant
			{
				Name = NamingConventions.ToPascalCase(title ?? variantSchemaName),
				WireName = wireName,
				Type = variantType,
				Description = member.Description
			});
		}

		EraseGenericVariants(variants);
		return RegisterTaggedUnion(schemaName, namespaceOverride, null, variants, discriminatorProperty);
	}

	/// <summary>
	/// Extracts the discriminator field's enum value from a variant schema.
	/// Searches the schema's allOf chain for a member that defines the discriminator
	/// property with a single enum value (e.g., type: { enum: ["keyword"] }).
	/// </summary>
	private static string? ExtractDiscriminatorValue(OpenApiSchema variantSchema, string discriminatorProperty)
	{
		// Check direct properties first
		var value = FindDiscriminatorEnum(variantSchema, discriminatorProperty);
		if (value is not null) return value;

		// Search allOf members
		foreach (var allOfMember in variantSchema.AllOf)
		{
			var resolved = allOfMember.Resolved();
			value = FindDiscriminatorEnum(resolved, discriminatorProperty);
			if (value is not null) return value;

			// Recurse into nested allOf
			value = ExtractDiscriminatorValue(resolved, discriminatorProperty);
			if (value is not null) return value;
		}

		return null;
	}

	private static string? FindDiscriminatorEnum(OpenApiSchema schema, string discriminatorProperty)
	{
		foreach (var (name, propSchema) in schema.Properties)
		{
			if (name != discriminatorProperty) continue;
			var resolved = propSchema.Resolved();
			var enumValues = resolved.EnumValues;
			if (enumValues.Count == 1)
				return enumValues[0];
			// Also check for const value
			if (resolved.Const is not null)
				return resolved.Const;
		}
		return null;
	}

	/// <summary>
	/// Detects allOf schemas where one member contains a oneOf (e.g., AggregationContainer).
	/// Extracts variants and builds a TaggedUnionShape.
	/// </summary>
	private bool TryExtractAllOfOneOfUnion(string schemaName, OpenApiSchema schema, string? ns, out TypeRef typeRef)
	{
		typeRef = TypeRef.JsonElement();

		if (schema.AllOf.Count == 0) return false;

		// Find an allOf member with oneOf
		OpenApiSchema? oneOfMember = null;
		foreach (var member in schema.AllOf)
		{
			var resolved = member.Resolved();
			if (resolved.OneOf.Count > 0)
			{
				oneOfMember = resolved;
				break;
			}
		}

		if (oneOfMember is null) return false;

		// Collect sibling properties from non-oneOf allOf members (e.g., meta, aggregations)
		var siblingFields = new List<Field>();
		foreach (var member in schema.AllOf)
		{
			var resolved = member.Resolved();
			if (resolved.OneOf.Count > 0) continue; // skip the oneOf member
			CollectFields(resolved, siblingFields, new HashSet<string>());
		}

		// Inject additional sibling fields for specific unions
		if (s_additionalSiblingFields.TryGetValue(schemaName, out var additional))
			siblingFields.AddRange(additional);

		var variants = new List<UnionVariant>();

		foreach (var variant in oneOfMember.OneOf)
		{
			if (variant.Ref is not null)
			{
				// Direct $ref variant — drill into the referenced schema to find the wire name
				var resolved = variant.Resolved();
				ExtractVariantFromRefSchema(resolved, variants);
			}
			else if (variant.Properties.Count > 0)
			{
				// Inline properties variant — the property name IS the wire name
				foreach (var (propName, propSchema) in variant.Properties)
				{
					var variantType = Map(propSchema);
					variants.Add(new UnionVariant
					{
						Name = NamingConventions.ToPascalCase(propName),
						WireName = propName,
						Type = variantType,
						Description = propSchema.Description
					});
					break; // Only the first property
				}
			}
		}

		EraseGenericVariants(variants);
		typeRef = RegisterTaggedUnion(schemaName, ns, schema.Description, variants, siblingFields: siblingFields);
		return true;
	}

	/// <summary>
	/// Extracts the wire name and variant type from a $ref schema used in an allOf+oneOf union.
	/// Skips $ref base type members (like BucketAggregationBase) and looks at inline members
	/// that have a required property — the required property is the discriminating wire name.
	/// </summary>
	private void ExtractVariantFromRefSchema(OpenApiSchema resolved, List<UnionVariant> variants)
	{
		if (resolved.AllOf.Count > 0)
		{
			foreach (var member in resolved.AllOf)
			{
				if (member.Ref is not null) continue; // Skip base types
				if (TryAddVariantFromRequired(member.Resolved(), variants))
					return;
			}
		}

		// Fallback: check direct properties with required
		TryAddVariantFromRequired(resolved, variants);
	}

	/// <summary>
	/// If the schema has both properties and a required list, adds a variant
	/// using the first required property as the wire name.
	/// </summary>
	private bool TryAddVariantFromRequired(OpenApiSchema schema, List<UnionVariant> variants)
	{
		var required = schema.Required;
		if (required.Count == 0) return false;

		var properties = schema.Properties;
		if (properties.Count == 0) return false;

		var requiredName = required[0];
		var matchingProp = properties.FirstOrDefault(p => p.Name == requiredName);
		if (matchingProp.Schema is null) return false;

		variants.Add(new UnionVariant
		{
			Name = NamingConventions.ToPascalCase(requiredName),
			WireName = requiredName,
			Type = Map(matchingProp.Schema),
			Description = matchingProp.Schema.Description
		});
		return true;
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

		// Skip generation for schemas that have hand-written replacements.
		// Still process the schema's properties so referenced types (e.g., ScoreSort from SortOptions)
		// are discovered. Only suppress emitting the skipped type itself.
		if (s_skipGeneration.Contains(schemaName))
		{
			var skipRef = s_typeOverrides.TryGetValue(schemaName, out var ov) ? ov : TypeRef.JsonElement();
			_namedTypes[schemaName] = skipRef;
			DiscoverReferencedTypes(schema);
			return skipRef;
		}

		// Detect property-keyed tagged unions:
		// either explicitly known, or auto-detected from schema shape.
		if (s_knownTaggedUnions.Contains(schemaName) || IsPropertyKeyedUnion(schema))
			return GetOrCreateTaggedUnion(schemaName, schema, namespaceOverride);

		// Detect allOf+oneOf unions (e.g., AggregationContainer)
		if (TryExtractAllOfOneOfUnion(schemaName, schema, namespaceOverride, out var unionRef))
			return unionRef;

		// Reserve the name first to prevent infinite recursion
		var typeRef = TypeRef.Named(schemaName, className);
		_namedTypes[schemaName] = typeRef;

		var fields = new List<Field>();
		var required = new HashSet<string>(schema.Required);
		TypeRef? additionalPropsType = null;

		// Handle allOf by flattening
		if (schema.AllOf.Count > 0)
		{
			foreach (var allOfMember in schema.AllOf)
			{
				var resolved = allOfMember.Resolved();
				CollectFields(resolved, fields, required);

				// Capture additionalProperties from allOf members (e.g., DecayFunction)
				if (additionalPropsType is null && resolved.AdditionalProperties is not null)
					additionalPropsType = Map(resolved.AdditionalProperties);
			}
		}
		else
		{
			CollectFields(schema, fields, required);
		}

		// Top-level additionalProperties takes precedence
		if (schema.AdditionalProperties is not null)
			additionalPropsType = Map(schema.AdditionalProperties);

		// Force additionalProperties on types that need [JsonExtensionData] for sub-aggregation capture
		if (additionalPropsType is null && s_forceAdditionalProperties.Contains(schemaName))
			additionalPropsType = TypeRef.JsonElement();

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

	private void CollectFields(OpenApiSchema schema, List<Field> fields, HashSet<string> required, HashSet<string>? existingNames = null)
	{
		// Merge required from this schema
		foreach (var r in schema.Required)
			required.Add(r);

		// Recursively flatten allOf members first (handles inheritance chains
		// like IntegerNumberProperty → NumberPropertyBase → DocValuesPropertyBase → ...)
		if (schema.AllOf.Count > 0)
		{
			foreach (var allOfMember in schema.AllOf)
			{
				var resolved = allOfMember.Resolved();
				CollectFields(resolved, fields, required, existingNames);
			}
		}

		// Flatten anyOf variant properties as optional (e.g., MetricAggregationBase
		// uses anyOf for field/script — include all variant properties without propagating required)
		if (schema.AnyOf.Count > 0)
		{
			foreach (var anyOfMember in schema.AnyOf)
			{
				var resolved = anyOfMember.Resolved();
				CollectFields(resolved, fields, new HashSet<string>(), existingNames);
			}
		}

		// Initialize or reuse the existing names set for deduplication
		existingNames ??= new HashSet<string>(fields.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);

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

	/// <summary>
	/// Detects property-keyed tagged unions: objects with many optional properties,
	/// no required list, where nearly all properties are $ref to distinct types.
	/// Examples: QueryContainer (query DSL), Property (mappings).
	/// Objects like IndexSettings are excluded because they have a significant mix of
	/// $ref and inline primitive/string properties.
	/// </summary>
	private static bool IsPropertyKeyedUnion(OpenApiSchema schema)
	{
		// Must be an object type with enough properties
		if (schema.Properties.Count < PropertyKeyedUnionMinProperties)
			return false;

		// Must have no required properties (all optional = only one set at a time)
		if (schema.Required.Count > 0)
			return false;

		// Count $ref properties vs total. True unions like QueryContainer have nearly
		// all $ref properties (with maybe 1-2 deprecated inline exceptions).
		// Config objects like IndexSettings have many inline string/primitive properties.
		var refCount = 0;
		var inlineStringOrPrimitiveCount = 0;
		foreach (var (_, propSchema) in schema.Properties)
		{
			if (propSchema.Ref is not null)
			{
				refCount++;
			}
			else
			{
				// Inline properties that are simple types (string, boolean, integer, etc.)
				// strongly indicate a config object, not a union.
				var resolved = propSchema.Resolved();
				if (resolved.Type is "string" or "boolean" or "integer" or "number")
					inlineStringOrPrimitiveCount++;
			}
		}

		// Reject if there are any inline primitive/string properties — config objects
		// always have at least some. True unions only have inline $ref or inline objects.
		if (inlineStringOrPrimitiveCount > 0)
			return false;

		// Require at least 90% $ref properties
		return refCount * 10 >= schema.Properties.Count * 9;
	}

	/// <summary>
	/// Returns true if the resolved schema is a simple string alias:
	/// type: string with no enum values, no properties, and no allOf/oneOf.
	/// </summary>
	private static bool IsStringAlias(OpenApiSchema resolved) =>
		resolved.Type is "string"
		&& resolved.EnumValues.Count == 0
		&& resolved.Properties.Count == 0
		&& resolved.AllOf.Count == 0
		&& resolved.OneOf.Count == 0;

	private static string ExtractSchemaName(string refString)
	{
		// "../schemas/_common.yaml#/components/schemas/IndexName" → "IndexName"
		// "#/components/schemas/IndexName" → "IndexName"
		var lastSlash = refString.LastIndexOf('/');
		return lastSlash >= 0 ? refString[(lastSlash + 1)..] : refString;
	}

	private TypeRef GetOrCreateTaggedUnion(string schemaName, OpenApiSchema schema, string? namespaceOverride)
	{
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

		return RegisterTaggedUnion(schemaName, namespaceOverride, schema.Description, variants);
	}

	/// <summary>
	/// Shared helper: reserves the name, builds the TaggedUnionShape, registers it, and returns the TypeRef.
	/// Used by all three union-creation paths (property-keyed, oneOf-refs, allOf+oneOf).
	/// </summary>
	private TypeRef RegisterTaggedUnion(string schemaName, string? namespaceOverride, string? description, List<UnionVariant> variants, string? discriminatorProperty = null, List<Field>? siblingFields = null)
	{
		var className = NamingConventions.SchemaNameToClassName(schemaName);
		var kindEnumName = className.Replace("Container", "") + "Kind";

		var typeRef = TypeRef.Named(schemaName, className);
		_namedTypes[schemaName] = typeRef;

		var ns = ResolveNamespace(namespaceOverride);
		_discoveredTaggedUnions[schemaName] = new TaggedUnionShape
		{
			ClassName = className,
			Namespace = ns,
			Description = description,
			KindEnumName = kindEnumName,
			Variants = variants,
			DiscriminatorProperty = discriminatorProperty,
			SiblingFields = siblingFields ?? []
		};

		return typeRef;
	}

	/// <summary>
	/// Returns true if a TypeRef references generic type parameters (structurally, not via string matching).
	/// </summary>
	/// <summary>
	/// Erases generic type params on union variants — the union itself isn't generic,
	/// so factory methods can't reference undeclared type params like T.
	/// </summary>
	private void EraseGenericVariants(List<UnionVariant> variants)
	{
		for (int i = 0; i < variants.Count; i++)
		{
			if (IsGenericTypeRef(variants[i].Type))
			{
				variants[i] = new UnionVariant
				{
					Name = variants[i].Name,
					WireName = variants[i].WireName,
					Type = TypeRef.JsonElement(),
					Description = variants[i].Description
				};
			}
		}
	}

	private bool IsGenericTypeRef(TypeRef type) =>
		type.IsGenericParameter
		|| (type.Kind == TypeRefKind.Named && !type.IsEnum
			&& _discoveredObjects.TryGetValue(type.Name, out var obj) && obj.IsGeneric)
		|| (type.ItemType is not null && IsGenericTypeRef(type.ItemType))
		|| (type.ValueType is not null && IsGenericTypeRef(type.ValueType));

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

	/// <summary>
	/// Walks a schema's properties, allOf, and oneOf members to discover referenced types,
	/// without creating an object shape for the schema itself.
	/// Used for overridden/skipped schemas whose sub-types still need generation.
	/// </summary>
	private void DiscoverReferencedTypes(OpenApiSchema schema)
	{
		foreach (var (_, propSchema) in schema.Properties)
			Map(propSchema);
		if (schema.AdditionalProperties is not null)
			Map(schema.AdditionalProperties);
		if (schema.Items is not null)
			Map(schema.Items);
		foreach (var allOfMember in schema.AllOf)
		{
			var resolved = allOfMember.Resolved();
			DiscoverReferencedTypes(resolved);
		}
		foreach (var oneOfMember in schema.OneOf)
		{
			if (oneOfMember.Ref is not null)
				Map(oneOfMember);
			else
				DiscoverReferencedTypes(oneOfMember.Resolved());
		}
	}

	private string ResolveNamespace(string? namespaceOverride) =>
		namespaceOverride
		?? (_targetNamespace.Equals("_common", StringComparison.OrdinalIgnoreCase)
			? "Common"
			: NamingConventions.NamespaceToClassName(_targetNamespace));

	/// <summary>
	/// Extracts the C# namespace from a $ref file path.
	/// Types from any <c>_common</c> schema file (e.g., <c>_common.yaml</c>,
	/// <c>_common.query_dsl.yaml</c>, <c>indices._common.yaml</c>) always map to
	/// <c>Common</c> to avoid duplicate types across namespaces.
	/// </summary>
	private string? ExtractNamespaceFromRef(string refString)
	{
		var hashIndex = refString.IndexOf('#');
		if (hashIndex <= 0) return null; // local ref → use current namespace

		var filePart = refString[..hashIndex];
		var fileName = Path.GetFileNameWithoutExtension(filePart);

		// Any file containing "_common" (e.g., "_common.yaml", "_common.query_dsl.yaml",
		// "indices._common.yaml") maps to Common to prevent cross-namespace duplicates.
		if (fileName.Contains("_common", StringComparison.OrdinalIgnoreCase))
			return "Common";

		var firstSegment = fileName.Split('.')[0];
		return NamingConventions.NamespaceToClassName(firstSegment);
	}
}
