using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenSearch.CodeGen.Model;

namespace OpenSearch.CodeGen.Transformer;

/// <summary>
/// Naming utilities: snake_case to PascalCase, operation group to class names, etc.
/// </summary>
public static partial class NamingConventions
{
	/// <summary>
	/// Converts snake_case to PascalCase. E.g., "number_of_shards" → "NumberOfShards".
	/// </summary>
	public static string ToPascalCase(string snakeCase)
	{
		if (string.IsNullOrEmpty(snakeCase))
			return snakeCase;

		var sb = new StringBuilder(snakeCase.Length);
		var capitalizeNext = true;
		foreach (var c in snakeCase)
		{
			if (c is '_' or '.' or '-')
			{
				capitalizeNext = true;
				continue;
			}

			sb.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
			capitalizeNext = false;
		}
		var result = sb.ToString();
		// Sanitize: prepend '_' if starts with digit
		if (result.Length > 0 && char.IsDigit(result[0]))
			result = "_" + result;
		return result;
	}

	// Singularized namespace names for class name suffixes
	private static readonly Dictionary<string, string> s_namespaceSingular = new(StringComparer.OrdinalIgnoreCase)
	{
		["indices"] = "Index",
		["nodes"] = "Node",
		["tasks"] = "Task",
		["ingest"] = "Ingest",
		["cluster"] = "Cluster",
		["snapshot"] = "Snapshot",
		["cat"] = "Cat",
		["dangling_indices"] = "DanglingIndex",
		["search_pipeline"] = "SearchPipeline",
		["ubi"] = "Ubi",
		["ingestion"] = "Ingestion",
		["geospatial"] = "Geospatial",
		["ism"] = "Ism",
		["knn"] = "Knn",
		["search_relevance"] = "SearchRelevance",
		["ltr"] = "Ltr",
		["ml"] = "Ml",
		["security"] = "Security",
	};

	/// <summary>
	/// Converts an operation group to request/response class names.
	/// For <c>_core</c> namespace operations, produces bare names: "search" → "SearchRequest".
	/// For namespaced operations, appends a singularized namespace suffix:
	/// "indices.create" → "CreateIndexRequest", "indices.exists" → "ExistsIndexRequest".
	/// If the action already contains the namespace concept, the suffix is still appended
	/// for consistency and collision avoidance.
	/// </summary>
	public static (string RequestName, string ResponseName, string EndpointName) OperationGroupToNames(string operationGroup)
	{
		// Split "indices.create" → ["indices", "create"]
		var parts = operationGroup.Split('.');
		if (parts.Length < 2)
			return (ToPascalCase(parts[0]) + "Request", ToPascalCase(parts[0]) + "Response", ToPascalCase(parts[0]) + "Endpoint");

		var ns = parts[0];
		var action = ToPascalCase(parts[1]);

		// _core namespace: bare names (SearchRequest, IndexRequest, etc.)
		if (ns == "_core")
			return (action + "Request", action + "Response", action + "Endpoint");

		// Other namespaces: append singularized namespace suffix
		var suffix = SingularizeNamespace(ns);
		var baseName = action + suffix;

		return (baseName + "Request", baseName + "Response", baseName + "Endpoint");
	}

	/// <summary>
	/// Converts an operation group to a method name for the namespace client.
	/// E.g., "indices.create" → "Create", "indices.get_alias" → "GetAlias"
	/// Method names on namespace clients don't need the suffix (they're already scoped).
	/// </summary>
	public static string OperationGroupToMethodName(string operationGroup)
	{
		var parts = operationGroup.Split('.');
		return parts.Length < 2 ? ToPascalCase(parts[0]) : ToPascalCase(parts[1]);
	}

	/// <summary>
	/// Returns the singular form of a namespace name for use as a class name suffix.
	/// </summary>
	private static string SingularizeNamespace(string ns) =>
		s_namespaceSingular.GetValueOrDefault(ns, ToPascalCase(ns));

	/// <summary>
	/// Gets the namespace display name from a namespace prefix.
	/// E.g., "indices" → "Indices", "_core" → "Core"
	/// </summary>
	public static string NamespaceToClassName(string ns) => ToPascalCase(ns.TrimStart('_'));

	// Class names that collide with System types (e.g., System.Action)
	private static readonly Dictionary<string, string> s_classNameRenames = new(StringComparer.Ordinal)
	{
		["Action"] = "IndexAction",
	};

	/// <summary>
	/// Converts a schema name from the spec to a C# class name.
	/// Handles prefixed names like "indices._common.Alias" → "Alias"
	/// </summary>
	public static string SchemaNameToClassName(string schemaName)
	{
		// Take just the last part after any dots
		var lastDot = schemaName.LastIndexOf('.');
		var name = lastDot >= 0 ? schemaName[(lastDot + 1)..] : schemaName;
		var pascal = ToPascalCase(name);
		return s_classNameRenames.GetValueOrDefault(pascal, pascal);
	}

	/// <summary>
	/// Sanitizes a name to be a valid C# identifier. Prepends '_' if it starts with a digit.
	/// </summary>
	public static string SanitizeIdentifier(string name)
	{
		if (string.IsNullOrEmpty(name))
			return "_";
		if (char.IsDigit(name[0]))
			return "_" + name;
		// Replace invalid chars
		return InvalidIdentifierChars().Replace(name, "_");
	}

	/// <summary>
	/// Converts an enum value string to a PascalCase member name.
	/// E.g., "open" → "Open", "read_only" → "ReadOnly", "1xx" → "_1xx"
	/// </summary>
	public static string EnumValueToMemberName(string wireValue)
	{
		var pascal = ToPascalCase(wireValue);
		return SanitizeIdentifier(pascal);
	}

	/// <summary>
	/// Returns true if a field with the given wire name and PascalCase property name
	/// needs an explicit [JsonPropertyName] attribute. This is the case when the
	/// STJ SnakeCaseLower naming policy would not round-trip to the original wire name.
	/// </summary>
	public static bool NeedsJsonPropertyName(string wireName, string pascalName)
	{
		var expected = PascalToSnakeLower(pascalName);
		return !string.Equals(expected, wireName, StringComparison.Ordinal);
	}

	/// <summary>
	/// Converts PascalCase to snake_case_lower using STJ's built-in policy
	/// to ensure exact match with runtime serialization behavior.
	/// </summary>
	private static string PascalToSnakeLower(string pascal) =>
		string.IsNullOrEmpty(pascal) ? pascal : JsonNamingPolicy.SnakeCaseLower.ConvertName(pascal);

	/// <summary>
	/// Renames any field whose PascalCase name clashes with the enclosing class name (CS0542).
	/// </summary>
	public static void FixFieldNameClash(List<Field> fields, string className)
	{
		for (int i = 0; i < fields.Count; i++)
		{
			if (fields[i].Name == className)
			{
				fields[i] = new Field
				{
					Name = fields[i].Name + "Value",
					WireName = fields[i].WireName,
					Type = fields[i].Type,
					Required = fields[i].Required,
					Description = fields[i].Description,
					Deprecated = fields[i].Deprecated
				};
			}
		}
	}

	[GeneratedRegex(@"[^a-zA-Z0-9_]")]
	private static partial Regex InvalidIdentifierChars();
}
