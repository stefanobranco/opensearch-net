using System.Text.Json;
using OpenSearch.Net;

namespace OpenSearch.Client;

/// <summary>
/// Extends transport configuration with client-level serialization settings.
/// Provides both a request/response serializer (for API types) and a source serializer
/// (for user document types like <c>_source</c> fields).
/// </summary>
public interface IOpenSearchClientSettings : ITransportConfiguration
{
	/// <summary>
	/// The serializer used for OpenSearch request and response types.
	/// This serializer is configured with converters for API enums, tagged unions,
	/// server errors, and other generated types.
	/// </summary>
	IOpenSearchSerializer RequestResponseSerializer { get; }

	/// <summary>
	/// The serializer used for user document types (e.g., <c>_source</c>, <c>_fields</c>).
	/// Defaults to <see cref="RequestResponseSerializer"/> but can be replaced
	/// to use a different serializer (e.g., Newtonsoft.Json) for user types.
	/// </summary>
	IOpenSearchSerializer SourceSerializer { get; }

	/// <summary>
	/// The <see cref="JsonSerializerOptions"/> used by <see cref="RequestResponseSerializer"/>.
	/// Exposed so converters and generated code can access it.
	/// </summary>
	JsonSerializerOptions RequestResponseOptions { get; }
}
