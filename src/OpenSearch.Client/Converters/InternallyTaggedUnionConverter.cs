using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Abstract base converter for internally-tagged union types where the discriminator
/// is a field inside the object (e.g., Property with "type": "keyword").
/// Contrast with <see cref="TaggedUnionConverter{TUnion,TKind}"/> which uses external tagging.
/// </summary>
public abstract class InternallyTaggedUnionConverter<TUnion, TKind> : JsonConverter<TUnion>
	where TUnion : TaggedUnion<TKind, object>
	where TKind : struct, Enum
{
	/// <summary>The JSON property name used as the discriminator (e.g., "type").</summary>
	protected abstract string DiscriminatorProperty { get; }

	protected abstract TUnion CreateFromKind(TKind kind, object value);
	protected abstract (TKind Kind, Type ValueType) ResolveKind(string discriminatorValue);
	protected abstract string ResolveDiscriminatorValue(TKind kind);

	public override TUnion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
			return null;

		// Buffer the entire object so we can peek at the discriminator
		using var doc = JsonDocument.ParseValue(ref reader);
		var root = doc.RootElement;

		if (root.ValueKind != JsonValueKind.Object)
			throw new JsonException($"Expected object for {typeof(TUnion).Name}, got {root.ValueKind}.");

		if (!root.TryGetProperty(DiscriminatorProperty, out var discriminatorEl))
			throw new JsonException($"Missing discriminator property '{DiscriminatorProperty}' in {typeof(TUnion).Name}.");

		var discriminatorValue = discriminatorEl.GetString()
			?? throw new JsonException($"Null discriminator value for '{DiscriminatorProperty}' in {typeof(TUnion).Name}.");

		var (kind, valueType) = ResolveKind(discriminatorValue);
		var value = JsonSerializer.Deserialize(root.GetRawText(), valueType, options)
			?? throw new JsonException($"Failed to deserialize {typeof(TUnion).Name} variant '{discriminatorValue}'.");

		return CreateFromKind(kind, value);
	}

	public override void Write(Utf8JsonWriter writer, TUnion value, JsonSerializerOptions options)
	{
		// Serialize the variant value to a temporary buffer, then merge with discriminator
		var json = JsonSerializer.SerializeToElement(value.Value, value.Value.GetType(), options);

		writer.WriteStartObject();

		// Write discriminator first
		writer.WriteString(DiscriminatorProperty, ResolveDiscriminatorValue(value.Kind));

		// Copy all other properties from the variant value
		if (json.ValueKind == JsonValueKind.Object)
		{
			foreach (var prop in json.EnumerateObject())
			{
				// Skip the discriminator if the variant object already has it
				if (prop.Name == DiscriminatorProperty)
					continue;
				prop.WriteTo(writer);
			}
		}

		writer.WriteEndObject();
	}
}
