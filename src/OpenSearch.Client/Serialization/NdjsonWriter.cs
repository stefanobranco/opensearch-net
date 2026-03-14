using OpenSearch.Client.Core;
using OpenSearch.Net;

namespace OpenSearch.Client;

/// <summary>Writes NDJSON (Newline Delimited JSON) format used by OpenSearch bulk and msearch APIs.</summary>
internal static class NdjsonWriter
{
	private static readonly byte[] s_newline = [(byte)'\n'];

	/// <summary>Writes bulk operations as NDJSON (action line + optional data line per operation).</summary>
	public static void Write(Stream stream, IReadOnlyList<BulkOperation> operations, IOpenSearchSerializer serializer)
	{
		foreach (var op in operations)
		{
			serializer.Serialize(op.GetActionObject(), stream);
			stream.Write(s_newline);

			if (op.GetBody() is { } body)
			{
				serializer.Serialize(body, stream);
				stream.Write(s_newline);
			}
		}
	}

	/// <summary>Asynchronously writes bulk operations as NDJSON.</summary>
	public static async ValueTask WriteAsync(Stream stream, IReadOnlyList<BulkOperation> operations, IOpenSearchSerializer serializer, CancellationToken ct = default)
	{
		foreach (var op in operations)
		{
			await serializer.SerializeAsync(op.GetActionObject(), stream, ct).ConfigureAwait(false);
			await stream.WriteAsync(s_newline, ct).ConfigureAwait(false);

			if (op.GetBody() is { } body)
			{
				await serializer.SerializeAsync(body, stream, ct).ConfigureAwait(false);
				await stream.WriteAsync(s_newline, ct).ConfigureAwait(false);
			}
		}
	}

	/// <summary>Writes msearch items as NDJSON (header line + body line per item).</summary>
	public static void WriteMsearch(Stream stream, IReadOnlyList<MsearchItem> items, IOpenSearchSerializer serializer) =>
		WriteHeaderBodyPairs(stream, items, static i => i.Header, static i => i.Body, serializer);

	/// <summary>Asynchronously writes msearch items as NDJSON (header line + body line per item).</summary>
	public static ValueTask WriteMsearchAsync(Stream stream, IReadOnlyList<MsearchItem> items, IOpenSearchSerializer serializer, CancellationToken ct = default) =>
		WriteHeaderBodyPairsAsync(stream, items, static i => i.Header, static i => i.Body, serializer, ct);

	/// <summary>Writes msearch_template items as NDJSON (header line + template body line per item).</summary>
	public static void WriteMsearchTemplate(Stream stream, IReadOnlyList<MsearchTemplateItem> items, IOpenSearchSerializer serializer) =>
		WriteHeaderBodyPairs(stream, items, static i => i.Header, static i => i.Body, serializer);

	/// <summary>Asynchronously writes msearch_template items as NDJSON.</summary>
	public static ValueTask WriteMsearchTemplateAsync(Stream stream, IReadOnlyList<MsearchTemplateItem> items, IOpenSearchSerializer serializer, CancellationToken ct = default) =>
		WriteHeaderBodyPairsAsync(stream, items, static i => i.Header, static i => i.Body, serializer, ct);

	/// <summary>Writes NDJSON header + body pairs using projector functions.</summary>
	private static void WriteHeaderBodyPairs<T>(Stream stream, IReadOnlyList<T> items, Func<T, object> header, Func<T, object> body, IOpenSearchSerializer serializer)
	{
		foreach (var item in items)
		{
			serializer.Serialize(header(item), stream);
			stream.Write(s_newline);

			serializer.Serialize(body(item), stream);
			stream.Write(s_newline);
		}
	}

	/// <summary>Asynchronously writes NDJSON header + body pairs using projector functions.</summary>
	private static async ValueTask WriteHeaderBodyPairsAsync<T>(Stream stream, IReadOnlyList<T> items, Func<T, object> header, Func<T, object> body, IOpenSearchSerializer serializer, CancellationToken ct = default)
	{
		foreach (var item in items)
		{
			await serializer.SerializeAsync(header(item), stream, ct).ConfigureAwait(false);
			await stream.WriteAsync(s_newline, ct).ConfigureAwait(false);

			await serializer.SerializeAsync(body(item), stream, ct).ConfigureAwait(false);
			await stream.WriteAsync(s_newline, ct).ConfigureAwait(false);
		}
	}
}
