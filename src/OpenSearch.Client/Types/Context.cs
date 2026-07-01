using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A completion-suggester context value: either a category string or a <see cref="GeoLocation"/>.
/// Construct implicitly, e.g. <c>Context c = "en";</c> or <c>Context c = GeoLocation.FromLatLon(40.7, -74.0);</c>.
/// </summary>
[JsonConverter(typeof(ContextConverter))]
public sealed class Context
{
	/// <summary>The category form, or <c>null</c> when a location is used.</summary>
	public string? Category { get; }

	/// <summary>The location form, or <c>null</c> when a category is used.</summary>
	public GeoLocation? Location { get; }

	private Context(string category) => Category = category;
	private Context(GeoLocation location) => Location = location;

	public static implicit operator Context(string category) => new(category);
	public static implicit operator Context(GeoLocation location) => new(location);
}

public sealed class ContextConverter : JsonConverter<Context>
{
	public override Context? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.Null:
				return null;
			case JsonTokenType.String:
				return reader.GetString()!;
			case JsonTokenType.StartObject:
			case JsonTokenType.StartArray:
				return JsonSerializer.Deserialize<GeoLocation>(ref reader, options)
					?? throw new JsonException("Failed to read GeoLocation for Context.");
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for Context.");
		}
	}

	public override void Write(Utf8JsonWriter writer, Context value, JsonSerializerOptions options)
	{
		if (value.Category is not null)
			writer.WriteStringValue(value.Category);
		else
			JsonSerializer.Serialize(writer, value.Location, options);
	}
}
