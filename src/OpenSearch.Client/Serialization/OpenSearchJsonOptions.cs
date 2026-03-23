using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> instances used throughout the client
/// for ad-hoc deserialization of response fragments (aggregation buckets, suggest entries,
/// multi-search items, etc.). Avoids duplicating identical options objects across files.
/// </summary>
public static class OpenSearchJsonOptions
{
	/// <summary>
	/// Standard options for deserializing OpenSearch response fragments:
	/// snake_case property names, numbers readable from strings.
	/// </summary>
	public static readonly JsonSerializerOptions Default = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
		MaxDepth = 256,
		Converters = { new JsonEnumConverterFactory() },
	};

	/// <summary>
	/// Options for serializing request fragments (e.g., multi-search body construction):
	/// snake_case, null-skipping, enum conversion.
	/// </summary>
	public static readonly JsonSerializerOptions RequestSerialization = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
		MaxDepth = 256,
		Converters = { new JsonEnumConverterFactory() },
	};
}
