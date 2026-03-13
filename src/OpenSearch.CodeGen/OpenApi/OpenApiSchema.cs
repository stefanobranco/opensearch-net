using YamlDotNet.RepresentationModel;

namespace OpenSearch.CodeGen.OpenApi;

/// <summary>
/// Wraps a YAML node representing an OpenAPI schema, providing typed accessors.
/// </summary>
public sealed class OpenApiSchema
{
	private readonly YamlNode _node;
	private readonly RefResolver _resolver;
	private readonly string _contextFile;

	public OpenApiSchema(YamlNode node, RefResolver resolver, string contextFile)
	{
		_node = node is YamlMappingNode ? node : new YamlMappingNode();
		_resolver = resolver;
		_contextFile = contextFile;
	}

	private YamlMappingNode Mapping => (YamlMappingNode)_node;

	/// <summary>
	/// If this schema is a $ref, resolves and returns the referenced schema.
	/// Otherwise returns this schema.
	/// </summary>
	public OpenApiSchema Resolved()
	{
		if (TryGetString("$ref", out var refStr))
		{
			var resolved = _resolver.Resolve(refStr, _contextFile);
			// Determine the file the ref points to
			var hashIndex = refStr.IndexOf('#');
			string targetFile;
			if (hashIndex <= 0)
				targetFile = _contextFile;
			else
			{
				var relPath = refStr[..hashIndex];
				var contextDir = Path.GetDirectoryName(_contextFile) ?? "";
				targetFile = Path.GetFullPath(Path.Combine(contextDir, relPath));
			}
			return new OpenApiSchema(resolved, _resolver, targetFile);
		}
		return this;
	}

	public string? Ref => TryGetString("$ref", out var v) ? v : null;
	public string? Type => GetTypeString();
	public string? Format => TryGetString("format", out var v) ? v : null;
	public string? Description => TryGetString("description", out var v) ? v : null;
	public string? Title => TryGetString("title", out var v) ? v : null;
	public bool Deprecated => TryGetString("deprecated", out var v) && v == "true";
	public string? XOperationGroup => TryGetString("x-operation-group", out var v) ? v : null;
	public bool XIgnorable => TryGetString("x-ignorable", out var v) && v == "true";

	/// <summary>
	/// Whether this schema represents a generic type parameter (x-is-generic-type-parameter: true).
	/// Used for schemas like TDocument that should map to a C# generic type parameter.
	/// </summary>
	public bool IsGenericTypeParameter => TryGetString("x-is-generic-type-parameter", out var v) && v == "true";

	/// <summary>
	/// Gets the type, handling OpenAPI 3.1 type arrays like [string, "null"].
	/// </summary>
	private string? GetTypeString()
	{
		if (!TryGetNode("type", out var typeNode))
			return null;

		if (typeNode is YamlScalarNode scalar)
			return scalar.Value;

		if (typeNode is YamlSequenceNode seq)
		{
			// Return the first non-null type
			foreach (var item in seq)
			{
				if (item is YamlScalarNode s && s.Value != "null")
					return s.Value;
			}
		}

		return null;
	}

	/// <summary>
	/// Whether this type is nullable (type array contains "null").
	/// </summary>
	public bool IsNullable
	{
		get
		{
			if (!TryGetNode("type", out var typeNode))
				return false;
			if (typeNode is YamlSequenceNode seq)
				return seq.Children.Any(c => c is YamlScalarNode s && s.Value == "null");
			return false;
		}
	}

	public IReadOnlyList<string> Required
	{
		get
		{
			if (!TryGetNode("required", out var node) || node is not YamlSequenceNode seq)
				return [];
			return seq.Children.OfType<YamlScalarNode>().Select(s => s.Value!).ToList();
		}
	}

	public IReadOnlyList<string> EnumValues
	{
		get
		{
			if (!TryGetNode("enum", out var node) || node is not YamlSequenceNode seq)
				return [];
			return seq.Children.OfType<YamlScalarNode>().Select(s => s.Value!).ToList();
		}
	}

	/// <summary>
	/// Returns property schemas as (name, schema) pairs.
	/// </summary>
	public IReadOnlyList<(string Name, OpenApiSchema Schema)> Properties
	{
		get
		{
			if (!TryGetNode("properties", out var node) || node is not YamlMappingNode mapping)
				return [];
			return mapping.Children
				.Select(kv => (
					Name: ((YamlScalarNode)kv.Key).Value!,
					Schema: new OpenApiSchema(kv.Value, _resolver, _contextFile)))
				.ToList();
		}
	}

	/// <summary>
	/// Returns the items schema for array types.
	/// </summary>
	public OpenApiSchema? Items
	{
		get
		{
			if (!TryGetNode("items", out var node))
				return null;
			return new OpenApiSchema(node, _resolver, _contextFile);
		}
	}

	/// <summary>
	/// Returns the additionalProperties schema.
	/// </summary>
	public OpenApiSchema? AdditionalProperties
	{
		get
		{
			if (!TryGetNode("additionalProperties", out var node))
				return null;
			if (node is YamlScalarNode scalar && scalar.Value == "true")
				return null; // `additionalProperties: true` means untyped
			if (node is YamlMappingNode mapping && mapping.Children.Count == 0)
				return null; // `additionalProperties: {}` means untyped
			return new OpenApiSchema(node, _resolver, _contextFile);
		}
	}

	/// <summary>
	/// Whether additionalProperties is present (even if untyped).
	/// </summary>
	public bool HasAdditionalProperties =>
		TryGetNode("additionalProperties", out _);

	/// <summary>
	/// Returns oneOf schemas.
	/// </summary>
	public IReadOnlyList<OpenApiSchema> OneOf
	{
		get
		{
			if (!TryGetNode("oneOf", out var node) || node is not YamlSequenceNode seq)
				return [];
			return seq.Children
				.Select(c => new OpenApiSchema(c, _resolver, _contextFile))
				.ToList();
		}
	}

	/// <summary>
	/// Returns allOf schemas.
	/// </summary>
	public IReadOnlyList<OpenApiSchema> AllOf
	{
		get
		{
			if (!TryGetNode("allOf", out var node) || node is not YamlSequenceNode seq)
				return [];
			return seq.Children
				.Select(c => new OpenApiSchema(c, _resolver, _contextFile))
				.ToList();
		}
	}

	private bool TryGetString(string key, out string value)
	{
		value = "";
		if (_node is not YamlMappingNode mapping)
			return false;
		var scalarKey = new YamlScalarNode(key);
		if (!mapping.Children.TryGetValue(scalarKey, out var child))
			return false;
		if (child is not YamlScalarNode scalar || scalar.Value is null)
			return false;
		value = scalar.Value;
		return true;
	}

	private bool TryGetNode(string key, out YamlNode node)
	{
		node = new YamlScalarNode();
		if (_node is not YamlMappingNode mapping)
			return false;
		return mapping.Children.TryGetValue(new YamlScalarNode(key), out node!);
	}
}
