using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Base converter for the analysis-chain wrappers (<see cref="CharFilter"/>, <see cref="TokenFilter"/>,
/// <see cref="Tokenizer"/>) that are <c>oneOf[string, &lt;definition&gt;]</c> — a built-in component name
/// or an inline definition. Writes a bare string for the name form; otherwise delegates to the definition
/// type's own converter. Reads a JSON string as the name and a JSON object as the definition.
/// </summary>
public abstract class StringOrDefinitionConverter<TWrapper, TDefinition> : JsonConverter<TWrapper>
	where TWrapper : class
	where TDefinition : class
{
	protected abstract TWrapper Create(string name);
	protected abstract TWrapper Create(TDefinition definition);
	protected abstract string? NameOf(TWrapper value);
	protected abstract TDefinition? DefinitionOf(TWrapper value);

	public override TWrapper? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.Null:
				return null;
			case JsonTokenType.String:
				return Create(reader.GetString()!);
			case JsonTokenType.StartObject:
				var definition = JsonSerializer.Deserialize<TDefinition>(ref reader, options)
					?? throw new JsonException($"Failed to read {typeof(TDefinition).Name}.");
				return Create(definition);
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for {typeof(TWrapper).Name}.");
		}
	}

	public override void Write(Utf8JsonWriter writer, TWrapper value, JsonSerializerOptions options)
	{
		var name = NameOf(value);
		if (name is not null)
			writer.WriteStringValue(name);
		else
			JsonSerializer.Serialize(writer, DefinitionOf(value), options);
	}
}
