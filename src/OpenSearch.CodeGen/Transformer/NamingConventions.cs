using System.Text;
using System.Text.RegularExpressions;

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
			if (c == '_' || c == '.')
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

	/// <summary>
	/// Converts an operation group to request/response class names.
	/// E.g., "indices.create" → ("CreateIndexRequest", "CreateIndexResponse")
	/// "indices.get_alias" → ("GetAliasRequest", "GetAliasResponse")
	/// "indices.exists" → ("ExistsIndexRequest", "ExistsIndexResponse")
	/// </summary>
	public static (string RequestName, string ResponseName, string EndpointName) OperationGroupToNames(string operationGroup)
	{
		// Split "indices.create" → ["indices", "create"]
		var parts = operationGroup.Split('.');
		if (parts.Length < 2)
			return (ToPascalCase(parts[0]) + "Request", ToPascalCase(parts[0]) + "Response", ToPascalCase(parts[0]) + "Endpoint");

		var action = ToPascalCase(parts[1]);
		var baseName = action;

		return (baseName + "Request", baseName + "Response", baseName + "Endpoint");
	}

	/// <summary>
	/// Converts an operation group to a method name for the namespace client.
	/// E.g., "indices.create" → "Create", "indices.get_alias" → "GetAlias"
	/// </summary>
	public static string OperationGroupToMethodName(string operationGroup)
	{
		var parts = operationGroup.Split('.');
		return parts.Length < 2 ? ToPascalCase(parts[0]) : ToPascalCase(parts[1]);
	}

	/// <summary>
	/// Gets the namespace display name from a namespace prefix.
	/// E.g., "indices" → "Indices"
	/// </summary>
	public static string NamespaceToClassName(string ns) => ToPascalCase(ns);

	/// <summary>
	/// Converts a schema name from the spec to a C# class name.
	/// Handles prefixed names like "indices._common.Alias" → "Alias"
	/// </summary>
	public static string SchemaNameToClassName(string schemaName)
	{
		// Take just the last part after any dots
		var lastDot = schemaName.LastIndexOf('.');
		var name = lastDot >= 0 ? schemaName[(lastDot + 1)..] : schemaName;
		return ToPascalCase(name);
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

	[GeneratedRegex(@"[^a-zA-Z0-9_]")]
	private static partial Regex InvalidIdentifierChars();
}
