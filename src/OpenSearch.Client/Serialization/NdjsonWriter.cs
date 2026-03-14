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
	public static void WriteMsearch(Stream stream, IReadOnlyList<MsearchItem> items, IOpenSearchSerializer serializer)
	{
		foreach (var item in items)
		{
			serializer.Serialize(item.Header, stream);
			stream.Write(s_newline);

			serializer.Serialize(item.Body, stream);
			stream.Write(s_newline);
		}
	}

	/// <summary>Asynchronously writes msearch items as NDJSON (header line + body line per item).</summary>
	public static async ValueTask WriteMsearchAsync(Stream stream, IReadOnlyList<MsearchItem> items, IOpenSearchSerializer serializer, CancellationToken ct = default)
	{
		foreach (var item in items)
		{
			await serializer.SerializeAsync(item.Header, stream, ct).ConfigureAwait(false);
			await stream.WriteAsync(s_newline, ct).ConfigureAwait(false);

			await serializer.SerializeAsync(item.Body, stream, ct).ConfigureAwait(false);
			await stream.WriteAsync(s_newline, ct).ConfigureAwait(false);
		}
	}
}
