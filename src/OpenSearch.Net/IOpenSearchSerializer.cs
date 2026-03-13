namespace OpenSearch.Net;

/// <summary>
/// Defines the contract for serializing and deserializing objects to and from streams.
/// The transport layer uses this interface to read response bodies and write request bodies.
/// </summary>
public interface IOpenSearchSerializer
{
	/// <summary>
	/// Deserializes an object of type <typeparamref name="T"/> from the given stream.
	/// </summary>
	T? Deserialize<T>(Stream stream);

	/// <summary>
	/// Asynchronously deserializes an object of type <typeparamref name="T"/> from the given stream.
	/// </summary>
	ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken ct = default);

	/// <summary>
	/// Serializes the given <paramref name="data"/> to the stream.
	/// </summary>
	void Serialize<T>(T data, Stream stream);

	/// <summary>
	/// Asynchronously serializes the given <paramref name="data"/> to the stream.
	/// </summary>
	ValueTask SerializeAsync<T>(T data, Stream stream, CancellationToken ct = default);
}
