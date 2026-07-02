using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A completion-suggester context filter: a bare <see cref="OpenSearch.Client.Context"/>
/// (category string or geo location), or the object form with boost/prefix (category contexts)
/// and precision/neighbours (geo contexts). Construct implicitly from a string or a
/// <see cref="OpenSearch.Client.Context"/> for the bare form.
/// </summary>
[JsonConverter(typeof(CompletionContextConverter))]
public sealed class CompletionContext
{
	/// <summary>The context value to filter or boost on.</summary>
	public Context? Context { get; set; }

	/// <summary>The boost factor for suggestions matching this context.</summary>
	public double? Boost { get; set; }

	/// <summary>Whether the context is matched as a prefix (category contexts).</summary>
	public bool? Prefix { get; set; }

	/// <summary>The geohash precision of the geo context (geo contexts).</summary>
	public GeoHashPrecision? Precision { get; set; }

	/// <summary>Neighbouring geohash precisions to also match (geo contexts).</summary>
	public List<GeoHashPrecision>? Neighbours { get; set; }

	public static implicit operator CompletionContext(string category) =>
		new() { Context = category };

	public static implicit operator CompletionContext(Context context) =>
		new() { Context = context };

	private bool IsBareContext =>
		Boost is null && Prefix is null && Precision is null && Neighbours is null;

	internal void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options)
	{
		if (IsBareContext)
		{
			JsonSerializer.Serialize(writer, Context, options);
			return;
		}

		writer.WriteStartObject();
		writer.WritePropertyName("context");
		JsonSerializer.Serialize(writer, Context, options);
		if (Boost is { } boost)
			writer.WriteNumber("boost", boost);
		if (Prefix is { } prefix)
			writer.WriteBoolean("prefix", prefix);
		if (Precision is { } precision)
		{
			writer.WritePropertyName("precision");
			JsonSerializer.Serialize(writer, precision, options);
		}
		if (Neighbours is { } neighbours)
		{
			writer.WritePropertyName("neighbours");
			JsonSerializer.Serialize(writer, neighbours, options);
		}
		writer.WriteEndObject();
	}
}

public sealed class CompletionContextConverter : JsonConverter<CompletionContext>
{
	public override CompletionContext Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		// Strings, arrays (lat/lon), and objects without a "context" property are the bare
		// Context form; the boosted object form always carries "context".
		if (reader.TokenType != JsonTokenType.StartObject)
			return JsonSerializer.Deserialize<Context>(ref reader, options)!;

		using var doc = JsonDocument.ParseValue(ref reader);
		var root = doc.RootElement;
		if (!root.TryGetProperty("context", out var contextEl))
			return root.Deserialize<Context>(options)!;

		return new CompletionContext
		{
			Context = contextEl.Deserialize<Context>(options),
			Boost = root.TryGetProperty("boost", out var boost) ? boost.GetDouble() : null,
			Prefix = root.TryGetProperty("prefix", out var prefix) ? prefix.GetBoolean() : null,
			Precision = root.TryGetProperty("precision", out var precision)
				? (GeoHashPrecision?)precision.Deserialize<GeoHashPrecision>(options)
				: null,
			Neighbours = root.TryGetProperty("neighbours", out var neighbours)
				? neighbours.Deserialize<List<GeoHashPrecision>>(options)
				: null,
		};
	}

	public override void Write(Utf8JsonWriter writer, CompletionContext value, JsonSerializerOptions options) =>
		value.WriteTo(writer, options);
}
