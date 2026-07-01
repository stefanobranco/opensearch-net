using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A tokenizer in an analysis chain: either a built-in tokenizer name (e.g. <c>"standard"</c>) or an inline
/// <see cref="TokenizerDefinition"/>. Construct implicitly from a string or a definition, e.g.
/// <c>Tokenizer t = "standard";</c> or <c>Tokenizer t = TokenizerDefinition.NGramTokenizer(new() {...});</c>.
/// </summary>
[JsonConverter(typeof(TokenizerConverter))]
public sealed class Tokenizer
{
	/// <summary>The built-in tokenizer name, or <c>null</c> when this is an inline definition.</summary>
	public string? Name { get; }

	/// <summary>The inline definition, or <c>null</c> when this is a built-in name.</summary>
	public TokenizerDefinition? Definition { get; }

	private Tokenizer(string name) => Name = name;
	private Tokenizer(TokenizerDefinition definition) => Definition = definition;

	/// <summary>References a built-in tokenizer by name.</summary>
	public static Tokenizer Builtin(string name) => new(name);

	public static implicit operator Tokenizer(string name) => new(name);
	public static implicit operator Tokenizer(TokenizerDefinition definition) => new(definition);
}

public sealed class TokenizerConverter : StringOrDefinitionConverter<Tokenizer, TokenizerDefinition>
{
	protected override Tokenizer Create(string name) => Tokenizer.Builtin(name);
	protected override Tokenizer Create(TokenizerDefinition definition) => definition;
	protected override string? NameOf(Tokenizer value) => value.Name;
	protected override TokenizerDefinition? DefinitionOf(Tokenizer value) => value.Definition;
}
