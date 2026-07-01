using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A character filter in an analysis chain: either a built-in filter name (e.g. <c>"html_strip"</c>) or an
/// inline <see cref="CharFilterDefinition"/>. Construct implicitly from a string or a definition, e.g.
/// <c>CharFilter f = "html_strip";</c> or <c>CharFilter f = CharFilterDefinition.MappingCharFilter(new() {...});</c>.
/// </summary>
[JsonConverter(typeof(CharFilterConverter))]
public sealed class CharFilter
{
	/// <summary>The built-in filter name, or <c>null</c> when this is an inline definition.</summary>
	public string? Name { get; }

	/// <summary>The inline definition, or <c>null</c> when this is a built-in name.</summary>
	public CharFilterDefinition? Definition { get; }

	private CharFilter(string name) => Name = name;
	private CharFilter(CharFilterDefinition definition) => Definition = definition;

	/// <summary>References a built-in character filter by name.</summary>
	public static CharFilter Builtin(string name) => new(name);

	public static implicit operator CharFilter(string name) => new(name);
	public static implicit operator CharFilter(CharFilterDefinition definition) => new(definition);
}

public sealed class CharFilterConverter : StringOrDefinitionConverter<CharFilter, CharFilterDefinition>
{
	protected override CharFilter Create(string name) => CharFilter.Builtin(name);
	protected override CharFilter Create(CharFilterDefinition definition) => definition;
	protected override string? NameOf(CharFilter value) => value.Name;
	protected override CharFilterDefinition? DefinitionOf(CharFilter value) => value.Definition;
}
