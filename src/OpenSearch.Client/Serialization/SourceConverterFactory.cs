using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Applied by generated code to properties that hold a user document (e.g. <c>Hit&lt;TDocument&gt;.Source</c>).
/// A custom source serializer can only be configured through client settings, which are carried by the
/// options via <see cref="ContextProvider{TContext}"/> and fixed before first use — so the routing decision
/// is made once here, when STJ resolves the property's converter. Without a distinct source serializer the
/// document type's own resolved converter is returned, keeping the default path free of any wrapper;
/// otherwise a buffering <see cref="SourceConverter{T}"/> routes the property through
/// <see cref="IOpenSearchClientSettings.SourceSerializer"/>.
/// </summary>
public sealed class SourceConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert) => true;

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var settings = ContextProvider<IOpenSearchClientSettings>.Get(options);
		if (settings is null || ReferenceEquals(settings.SourceSerializer, settings.RequestResponseSerializer))
			return options.GetConverter(typeToConvert);

		return (JsonConverter)Activator.CreateInstance(typeof(SourceConverter<>).MakeGenericType(typeToConvert))!;
	}
}
