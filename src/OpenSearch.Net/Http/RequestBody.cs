using System.Text;

namespace OpenSearch.Net;

/// <summary>
/// Represents the body of an HTTP request. Use the factory methods to create instances.
/// </summary>
public abstract class RequestBody
{
	/// <summary>
	/// The MIME content type of this body.
	/// </summary>
	public abstract string ContentType { get; }

	/// <summary>
	/// Writes the body content to the given stream using the provided serializer.
	/// </summary>
	public abstract void WriteTo(Stream stream, IOpenSearchSerializer serializer);

	/// <summary>
	/// Asynchronously writes the body content to the given stream using the provided serializer.
	/// </summary>
	public abstract ValueTask WriteToAsync(Stream stream, IOpenSearchSerializer serializer, CancellationToken ct = default);

	/// <summary>
	/// Creates a JSON body that will be serialized using the transport's serializer.
	/// </summary>
	public static RequestBody Json<T>(T value) => new JsonBody<T>(value);

	/// <summary>
	/// Creates a raw byte body with the specified content type.
	/// </summary>
	public static RequestBody Raw(ReadOnlyMemory<byte> bytes, string contentType = "application/octet-stream") =>
		new RawBody(bytes, contentType);

	/// <summary>
	/// Creates a string body with the specified content type.
	/// </summary>
	public static RequestBody String(string content, string contentType = "text/plain") =>
		new StringBody(content, contentType);

	/// <summary>
	/// Creates a custom body with the specified content type and write delegates.
	/// </summary>
	public static RequestBody Custom(
		string contentType,
		Action<Stream, IOpenSearchSerializer> writeTo,
		Func<Stream, IOpenSearchSerializer, CancellationToken, ValueTask> writeToAsync) =>
		new CustomBody(contentType, writeTo, writeToAsync);

	private sealed class JsonBody<T> : RequestBody
	{
		private readonly T _value;

		public JsonBody(T value) => _value = value;

		public override string ContentType => "application/json";

		public override void WriteTo(Stream stream, IOpenSearchSerializer serializer) =>
			serializer.Serialize(_value, stream);

		public override ValueTask WriteToAsync(Stream stream, IOpenSearchSerializer serializer, CancellationToken ct) =>
			serializer.SerializeAsync(_value, stream, ct);
	}

	private sealed class RawBody : RequestBody
	{
		private readonly ReadOnlyMemory<byte> _bytes;
		private readonly string _contentType;

		public RawBody(ReadOnlyMemory<byte> bytes, string contentType)
		{
			_bytes = bytes;
			_contentType = contentType;
		}

		public override string ContentType => _contentType;

		public override void WriteTo(Stream stream, IOpenSearchSerializer serializer) =>
			stream.Write(_bytes.Span);

		public override async ValueTask WriteToAsync(Stream stream, IOpenSearchSerializer serializer, CancellationToken ct) =>
			await stream.WriteAsync(_bytes, ct).ConfigureAwait(false);
	}

	private sealed class StringBody : RequestBody
	{
		private readonly byte[] _bytes;
		private readonly string _contentType;

		public StringBody(string content, string contentType)
		{
			_bytes = Encoding.UTF8.GetBytes(content);
			_contentType = contentType;
		}

		public override string ContentType => _contentType;

		public override void WriteTo(Stream stream, IOpenSearchSerializer serializer) =>
			stream.Write(_bytes);

		public override async ValueTask WriteToAsync(Stream stream, IOpenSearchSerializer serializer, CancellationToken ct) =>
			await stream.WriteAsync(_bytes.AsMemory(), ct).ConfigureAwait(false);
	}

	private sealed class CustomBody : RequestBody
	{
		private readonly string _contentType;
		private readonly Action<Stream, IOpenSearchSerializer> _writeTo;
		private readonly Func<Stream, IOpenSearchSerializer, CancellationToken, ValueTask> _writeToAsync;

		public CustomBody(
			string contentType,
			Action<Stream, IOpenSearchSerializer> writeTo,
			Func<Stream, IOpenSearchSerializer, CancellationToken, ValueTask> writeToAsync)
		{
			_contentType = contentType;
			_writeTo = writeTo;
			_writeToAsync = writeToAsync;
		}

		public override string ContentType => _contentType;

		public override void WriteTo(Stream stream, IOpenSearchSerializer serializer) =>
			_writeTo(stream, serializer);

		public override ValueTask WriteToAsync(Stream stream, IOpenSearchSerializer serializer, CancellationToken ct) =>
			_writeToAsync(stream, serializer, ct);
	}
}
