using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A token filter in an analysis chain: either a built-in filter name (e.g. <c>"lowercase"</c>) or an inline
/// <see cref="TokenFilterDefinition"/>. Construct implicitly from a string or a definition, e.g.
/// <c>TokenFilter f = "lowercase";</c> or <c>TokenFilter f = TokenFilterDefinition.StopTokenFilter(new() {...});</c>.
/// </summary>
[JsonConverter(typeof(TokenFilterConverter))]
public sealed class TokenFilter
{
	/// <summary>The built-in filter name, or <c>null</c> when this is an inline definition.</summary>
	public string? Name { get; }

	/// <summary>The inline definition, or <c>null</c> when this is a built-in name.</summary>
	public TokenFilterDefinition? Definition { get; }

	private TokenFilter(string name) => Name = name;
	private TokenFilter(TokenFilterDefinition definition) => Definition = definition;

	/// <summary>References a built-in token filter by name.</summary>
	public static TokenFilter Builtin(string name) => new(name);

	public static implicit operator TokenFilter(string name) => new(name);
	public static implicit operator TokenFilter(TokenFilterDefinition definition) => new(definition);
}

public sealed class TokenFilterConverter : StringOrDefinitionConverter<TokenFilter, TokenFilterDefinition>
{
	protected override TokenFilter Create(string name) => TokenFilter.Builtin(name);
	protected override TokenFilter Create(TokenFilterDefinition definition) => definition;
	protected override string? NameOf(TokenFilter value) => value.Name;
	protected override TokenFilterDefinition? DefinitionOf(TokenFilter value) => value.Definition;
}
