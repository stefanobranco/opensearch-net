using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenSearch.Net;

namespace OpenSearch.Benchmarks;

/// <summary>
/// A Newtonsoft.Json-backed <see cref="IOpenSearchSerializer"/> for user document types — the
/// adapter consumers plug in via <c>OpenSearchClientSettings.Builder.SourceSerializer(...)</c>
/// when their documents rely on Newtonsoft attributes or converters. API types stay on the
/// built-in System.Text.Json serializer either way.
/// </summary>
public sealed class NewtonsoftSourceSerializer : IOpenSearchSerializer
{
	private static readonly JsonSerializer s_serializer = JsonSerializer.Create(new JsonSerializerSettings
	{
		ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
		NullValueHandling = NullValueHandling.Ignore,
	});

	public T? Deserialize<T>(Stream stream)
	{
		using var reader = new StreamReader(stream, leaveOpen: true);
		using var json = new JsonTextReader(reader);
		return s_serializer.Deserialize<T>(json);
	}

	public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken ct = default) =>
		new(Deserialize<T>(stream));

	public void Serialize<T>(T data, Stream stream)
	{
		using var writer = new StreamWriter(stream, leaveOpen: true);
		using var json = new JsonTextWriter(writer);
		s_serializer.Serialize(json, data);
		json.Flush();
	}

	public ValueTask SerializeAsync<T>(T data, Stream stream, CancellationToken ct = default)
	{
		Serialize(data, stream);
		return ValueTask.CompletedTask;
	}
}
