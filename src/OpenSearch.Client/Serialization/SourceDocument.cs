using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Marks a request payload as a user document on the write side, so it serializes through the configured
/// <see cref="IOpenSearchClientSettings.SourceSerializer"/> — the write-side counterpart of
/// <see cref="SourceConverter{T}"/>. Wrapped at the points where user documents enter a request body
/// (the <c>index</c>/<c>create</c> document body and bulk NDJSON operations).
/// </summary>
[JsonConverter(typeof(SourceDocumentConverter))]
internal sealed class SourceDocument
{
	internal SourceDocument(object value) => Value = value;

	internal object Value { get; }

	internal static SourceDocument Wrap(object value) => new(value);
}

internal sealed class SourceDocumentConverter : JsonConverter<SourceDocument>
{
	public override SourceDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		throw new NotSupportedException($"{nameof(SourceDocument)} is a write-only wrapper.");

	public override void Write(Utf8JsonWriter writer, SourceDocument value, JsonSerializerOptions options)
	{
		// A custom source serializer can only be configured through client settings, so no settings
		// context — or a source serializer that IS the request/response serializer — means default
		// behavior: serialize in place.
		var settings = ContextProvider<IOpenSearchClientSettings>.Get(options);
		if (settings is null || ReferenceEquals(settings.SourceSerializer, settings.RequestResponseSerializer))
		{
			JsonSerializer.Serialize(writer, value.Value, value.Value.GetType(), options);
			return;
		}

		using var stream = new MemoryStream();
		settings.SourceSerializer.Serialize(value.Value, stream);
		stream.Position = 0;

		using var doc = JsonDocument.Parse(stream);
		doc.WriteTo(writer);
	}
}
