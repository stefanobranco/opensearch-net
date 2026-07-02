using OpenSearch.Net;

namespace OpenSearch.Client;

/// <summary>
/// Deserializes a response body that <em>is</em> a user document (get_source), honoring a
/// configured custom source serializer. Property-level documents route through
/// <see cref="SourceConverterFactory"/>; a document at the body root has no property to carry
/// the attribute, so the routing decision is made here.
/// </summary>
internal static class SourceResponseReader
{
	public static T? Read<T>(Stream body, IOpenSearchSerializer serializer)
	{
		if (serializer is SystemTextJsonSerializer stj
			&& ContextProvider<IOpenSearchClientSettings>.Get(stj.Options) is { } settings
			&& !ReferenceEquals(settings.SourceSerializer, settings.RequestResponseSerializer))
			return settings.SourceSerializer.Deserialize<T>(body);

		return serializer.Deserialize<T>(body);
	}
}
