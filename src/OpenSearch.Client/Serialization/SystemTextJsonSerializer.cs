using System.Text.Json;
using OpenSearch.Net;

namespace OpenSearch.Client;

/// <summary><see cref="IOpenSearchSerializer"/> backed by <see cref="System.Text.Json"/>.</summary>
public sealed class SystemTextJsonSerializer : IOpenSearchSerializer
{
	private readonly JsonSerializerOptions _options;

	public SystemTextJsonSerializer(JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		_options = options;
	}

	public JsonSerializerOptions Options => _options;

	/// <inheritdoc />
	public T? Deserialize<T>(Stream stream)
	{
		if (stream.CanSeek && stream.Length == 0)
			return default;

		if (stream is MemoryStream ms && ms.TryGetBuffer(out var buffer))
			return JsonSerializer.Deserialize<T>(buffer, _options);

		return JsonSerializer.Deserialize<T>(stream, _options);
	}

	/// <inheritdoc />
	public async ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken ct = default)
	{
		if (stream.CanSeek && stream.Length == 0)
			return default;

		return await JsonSerializer.DeserializeAsync<T>(stream, _options, ct).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public void Serialize<T>(T data, Stream stream) =>
		JsonSerializer.Serialize(stream, data, _options);

	/// <inheritdoc />
	public async ValueTask SerializeAsync<T>(T data, Stream stream, CancellationToken ct = default) =>
		await JsonSerializer.SerializeAsync(stream, data, _options, ct).ConfigureAwait(false);
}
