using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A <see cref="JsonConverter{T}"/> that delegates to the source serializer
/// configured in <see cref="IOpenSearchClientSettings"/>. This is used for user
/// document types (e.g., the <c>_source</c> field in search hits) so that users
/// can bring their own serializer (Newtonsoft.Json, etc.) for their domain objects
/// while the rest of the request/response is handled by System.Text.Json.
/// </summary>
/// <typeparam name="T">The user document type.</typeparam>
public sealed class SourceConverter<T> : JsonConverter<T>
{
	/// <inheritdoc />
	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var settings = ContextProvider<IOpenSearchClientSettings>.Get(options)
			?? throw new InvalidOperationException(
				$"No {nameof(IOpenSearchClientSettings)} found in JsonSerializerOptions. " +
				$"Ensure a ContextProvider<{nameof(IOpenSearchClientSettings)}> has been added to the converters.");

		// Read the raw JSON for this value into a buffer, then hand it to the source serializer.
		using var doc = JsonDocument.ParseValue(ref reader);
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream))
		{
			doc.WriteTo(writer);
		}

		stream.Position = 0;
		return settings.SourceSerializer.Deserialize<T>(stream);
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		var settings = ContextProvider<IOpenSearchClientSettings>.Get(options)
			?? throw new InvalidOperationException(
				$"No {nameof(IOpenSearchClientSettings)} found in JsonSerializerOptions. " +
				$"Ensure a ContextProvider<{nameof(IOpenSearchClientSettings)}> has been added to the converters.");

		using var stream = new MemoryStream();
		settings.SourceSerializer.Serialize(value, stream);
		stream.Position = 0;

		using var doc = JsonDocument.Parse(stream);
		doc.WriteTo(writer);
	}
}
