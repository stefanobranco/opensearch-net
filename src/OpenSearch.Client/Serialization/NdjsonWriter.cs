using OpenSearch.Client.Core;
using OpenSearch.Net;

namespace OpenSearch.Client;

/// <summary>
/// Writes NDJSON (Newline Delimited JSON) format used by OpenSearch bulk and msearch APIs.
/// Each JSON value is followed by a newline character.
/// </summary>
internal static class NdjsonWriter
{
	private static readonly byte[] s_newline = [(byte)'\n'];

	/// <summary>
	/// Writes a sequence of action/data pairs in NDJSON format.
	/// Each <see cref="BulkOperation"/> contributes one or two lines:
	/// the action line (always) and the optional data line.
	/// </summary>
	public static void Write(Stream stream, IReadOnlyList<BulkOperation> operations, IOpenSearchSerializer serializer)
	{
		foreach (var op in operations)
		{
			// Action line: { "index": { "_index": "...", "_id": "..." } }
			serializer.Serialize(op.GetActionObject(), stream);
			stream.Write(s_newline);

			// Data line (if applicable — delete has no body)
			if (op.GetBody() is { } body)
			{
				serializer.Serialize(body, stream);
				stream.Write(s_newline);
			}
		}
	}

	/// <summary>
	/// Asynchronously writes a sequence of action/data pairs in NDJSON format.
	/// </summary>
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
}
