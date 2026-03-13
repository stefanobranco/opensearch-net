using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Net;

/// <summary>
/// A minimal JSON serializer using System.Text.Json with snake_case naming.
/// Used as a fallback when no custom serializer is configured.
/// The full-featured serializer lives in OpenSearch.Client.
/// </summary>
internal sealed class DefaultJsonSerializer : IOpenSearchSerializer
{
	private static readonly JsonSerializerOptions Options = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public T? Deserialize<T>(Stream stream)
	{
		if (stream is null || stream == Stream.Null || !stream.CanRead)
			return default;

		if (stream is MemoryStream ms && ms.TryGetBuffer(out var buffer))
		{
			if (buffer.Count == 0)
				return default;

			return JsonSerializer.Deserialize<T>(buffer.AsSpan(), Options);
		}

		return JsonSerializer.Deserialize<T>(stream, Options);
	}

	public async ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken ct = default)
	{
		if (stream is null || stream == Stream.Null || !stream.CanRead)
			return default;

		return await JsonSerializer.DeserializeAsync<T>(stream, Options, ct).ConfigureAwait(false);
	}

	public void Serialize<T>(T data, Stream stream)
	{
		JsonSerializer.Serialize(stream, data, Options);
		stream.Flush();
	}

	public async ValueTask SerializeAsync<T>(T data, Stream stream, CancellationToken ct = default)
	{
		await JsonSerializer.SerializeAsync(stream, data, Options, ct).ConfigureAwait(false);
		await stream.FlushAsync(ct).ConfigureAwait(false);
	}
}
