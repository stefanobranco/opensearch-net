using YamlDotNet.RepresentationModel;

namespace OpenSearch.CodeGen.OpenApi;

/// <summary>
/// Wraps a YAML node representing an OpenAPI schema, providing typed accessors.
/// Parsed results are lazily cached to avoid repeated allocations.
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

	/// <summary>
	/// Returns the discriminator property name (e.g., "type" for Property union).
	/// </summary>
	public string? DiscriminatorPropertyName
	{
		get
		{
			if (!TryGetNode("discriminator", out var node) || node is not YamlMappingNode mapping)
				return null;
			var key = new YamlScalarNode("propertyName");
			if (mapping.Children.TryGetValue(key, out var child) && child is YamlScalarNode scalar)
				return scalar.Value;
			return null;
		}
	}
	public string? Const => TryGetString("const", out var v) ? v : null;
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

	// ── Cached accessors ──

	private IReadOnlyList<string>? _required;
	public IReadOnlyList<string> Required => _required ??= ParseRequired();

	private IReadOnlyList<string>? _enumValues;
	public IReadOnlyList<string> EnumValues => _enumValues ??= ParseEnumValues();

	private IReadOnlyList<(string Name, OpenApiSchema Schema)>? _properties;
	public IReadOnlyList<(string Name, OpenApiSchema Schema)> Properties => _properties ??= ParseProperties();

	private IReadOnlyList<OpenApiSchema>? _oneOf;
	public IReadOnlyList<OpenApiSchema> OneOf => _oneOf ??= ParseSequenceSchemas("oneOf");

	private IReadOnlyList<OpenApiSchema>? _anyOf;
	public IReadOnlyList<OpenApiSchema> AnyOf => _anyOf ??= ParseSequenceSchemas("anyOf");

	private IReadOnlyList<OpenApiSchema>? _allOf;
	public IReadOnlyList<OpenApiSchema> AllOf => _allOf ??= ParseSequenceSchemas("allOf");

	private OpenApiSchema? _items;
	private bool _itemsParsed;
	public OpenApiSchema? Items
	{
		get
		{
			if (!_itemsParsed)
			{
				_items = ParseItems();
				_itemsParsed = true;
			}
			return _items;
		}
	}

	private OpenApiSchema? _additionalProperties;
	private bool _additionalPropertiesParsed;
	public OpenApiSchema? AdditionalProperties
	{
		get
		{
			if (!_additionalPropertiesParsed)
			{
				_additionalProperties = ParseAdditionalProperties();
				_additionalPropertiesParsed = true;
			}
			return _additionalProperties;
		}
	}

	/// <summary>
	/// Whether additionalProperties is present (even if untyped).
	/// </summary>
	public bool HasAdditionalProperties =>
		TryGetNode("additionalProperties", out _);

	// ── Parsing helpers ──

	private IReadOnlyList<string> ParseRequired()
	{
		if (!TryGetNode("required", out var node) || node is not YamlSequenceNode seq)
			return [];
		return seq.Children.OfType<YamlScalarNode>().Select(s => s.Value!).ToList();
	}

	private IReadOnlyList<string> ParseEnumValues()
	{
		if (!TryGetNode("enum", out var node) || node is not YamlSequenceNode seq)
			return [];
		return seq.Children.OfType<YamlScalarNode>().Select(s => s.Value!).ToList();
	}

	private IReadOnlyList<(string Name, OpenApiSchema Schema)> ParseProperties()
	{
		if (!TryGetNode("properties", out var node) || node is not YamlMappingNode mapping)
			return [];
		return mapping.Children
			.Select(kv => (
				Name: ((YamlScalarNode)kv.Key).Value!,
				Schema: new OpenApiSchema(kv.Value, _resolver, _contextFile)))
			.ToList();
	}

	private IReadOnlyList<OpenApiSchema> ParseSequenceSchemas(string key)
	{
		if (!TryGetNode(key, out var node) || node is not YamlSequenceNode seq)
			return [];
		return seq.Children
			.Select(c => new OpenApiSchema(c, _resolver, _contextFile))
			.ToList();
	}

	private OpenApiSchema? ParseItems()
	{
		if (!TryGetNode("items", out var node))
			return null;
		return new OpenApiSchema(node, _resolver, _contextFile);
	}

	private OpenApiSchema? ParseAdditionalProperties()
	{
		if (!TryGetNode("additionalProperties", out var node))
			return null;
		if (node is YamlScalarNode scalar && scalar.Value == "true")
			return null; // `additionalProperties: true` means untyped
		if (node is YamlMappingNode mapping && mapping.Children.Count == 0)
			return null; // `additionalProperties: {}` means untyped
		return new OpenApiSchema(node, _resolver, _contextFile);
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
