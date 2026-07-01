using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Applied by generated code to properties that hold a user document (e.g. <c>Hit&lt;TDocument&gt;.Source</c>).
/// Creates a <see cref="SourcePropertyConverter{T}"/> for the concrete document type at runtime, routing
/// the property through the configured <see cref="IOpenSearchClientSettings.SourceSerializer"/>.
/// </summary>
public sealed class SourceConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert) => true;

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
		(JsonConverter)Activator.CreateInstance(typeof(SourcePropertyConverter<>).MakeGenericType(typeToConvert))!;
}

/// <summary>
/// The property-attribute counterpart of <see cref="SourceConverter{T}"/>. Because it is selected via a
/// property attribute (never type-level resolution), it can safely fall back to in-place (de)serialization
/// with the ambient options — which is both what keeps generated types usable as plain POCOs outside a
/// client, and what keeps the default path (no custom source serializer) free of buffering overhead.
/// </summary>
internal sealed class SourcePropertyConverter<T> : JsonConverter<T>
{
	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		// A custom source serializer can only be configured through client settings, so no settings
		// context — or a source serializer that IS the request/response serializer — means default
		// behavior: deserialize in place.
		var settings = ContextProvider<IOpenSearchClientSettings>.Get(options);
		if (settings is null || ReferenceEquals(settings.SourceSerializer, settings.RequestResponseSerializer))
			return JsonSerializer.Deserialize<T>(ref reader, options);

		using var doc = JsonDocument.ParseValue(ref reader);
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream))
		{
			doc.WriteTo(writer);
		}

		stream.Position = 0;
		return settings.SourceSerializer.Deserialize<T>(stream);
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		var settings = ContextProvider<IOpenSearchClientSettings>.Get(options);
		if (settings is null || ReferenceEquals(settings.SourceSerializer, settings.RequestResponseSerializer))
		{
			JsonSerializer.Serialize(writer, value, options);
			return;
		}

		using var stream = new MemoryStream();
		settings.SourceSerializer.Serialize(value, stream);
		stream.Position = 0;

		using var doc = JsonDocument.Parse(stream);
		doc.WriteTo(writer);
	}
}
