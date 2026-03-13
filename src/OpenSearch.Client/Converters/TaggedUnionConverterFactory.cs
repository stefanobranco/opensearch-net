using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Abstract base converter for tagged union types. Generated union converters inherit
/// from this class and implement <see cref="CreateFromKind"/> and <see cref="ResolveKind"/>
/// to wire up variant resolution and construction.
/// </summary>
/// <typeparam name="TUnion">The concrete tagged union type.</typeparam>
/// <typeparam name="TKind">The enum type identifying union variants.</typeparam>
public abstract class TaggedUnionConverter<TUnion, TKind> : JsonConverter<TUnion>
	where TUnion : TaggedUnion<TKind, object>
	where TKind : struct, Enum
{
	/// <summary>
	/// Creates a <typeparamref name="TUnion"/> instance from a resolved kind and deserialized value.
	/// </summary>
	protected abstract TUnion CreateFromKind(TKind kind, object value);

	/// <summary>
	/// Maps a JSON property name to its corresponding kind and value type.
	/// Used during deserialization to determine which variant to construct.
	/// </summary>
	protected abstract (TKind Kind, Type ValueType) ResolveKind(string propertyName);

	/// <summary>
	/// Maps a <typeparamref name="TKind"/> to its JSON property name.
	/// Used during serialization to write the external tag.
	/// </summary>
	protected abstract string ResolvePropertyName(TKind kind);

	/// <inheritdoc />
	public override TUnion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
			return null;

		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException($"Expected start of object for {typeof(TUnion).Name}, got {reader.TokenType}.");

		reader.Read(); // Move past StartObject

		if (reader.TokenType != JsonTokenType.PropertyName)
			throw new JsonException($"Expected property name for {typeof(TUnion).Name} variant tag.");

		var propertyName = reader.GetString()
			?? throw new JsonException("Null property name in tagged union.");

		var (kind, valueType) = ResolveKind(propertyName);

		reader.Read(); // Move to value
		var value = JsonSerializer.Deserialize(ref reader, valueType, options)
			?? throw new JsonException($"Failed to deserialize value for {typeof(TUnion).Name} variant '{propertyName}'.");

		reader.Read(); // Move past EndObject

		return CreateFromKind(kind, value);
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, TUnion value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		var propertyName = ResolvePropertyName(value.Kind);
		writer.WritePropertyName(propertyName);
		JsonSerializer.Serialize(writer, value.Value, value.Value.GetType(), options);
		writer.WriteEndObject();
	}
}
