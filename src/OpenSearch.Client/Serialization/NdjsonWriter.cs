using System.Text.Json;
using OpenSearch.Net;

namespace OpenSearch.Client;

/// <summary>
/// Writes NDJSON (Newline Delimited JSON) format used by OpenSearch bulk and msearch APIs.
/// All lines are emitted through a single <see cref="Utf8JsonWriter"/> over the request stream,
/// reset between lines — one serializer entry per request instead of one per line.
/// </summary>
internal static class NdjsonWriter
{
	private static readonly byte[] s_newline = [(byte)'\n'];

	/// <summary>
	/// The NDJSON envelope (action/header lines) is client-owned wire format written directly with
	/// <see cref="Utf8JsonWriter"/>; only line payloads go through <see cref="JsonSerializerOptions"/>.
	/// The client always configures a <see cref="SystemTextJsonSerializer"/>, whose options carry the
	/// settings context that routes user documents through the source serializer. The fallback covers
	/// a bare transport without client settings, matching its serializer's write behavior.
	/// </summary>
	private static JsonSerializerOptions ResolveOptions(IOpenSearchSerializer serializer) =>
		serializer is SystemTextJsonSerializer stj ? stj.Options : OpenSearchJsonOptions.RequestSerialization;

	private static void FinishLine(Utf8JsonWriter writer, Stream stream)
	{
		writer.Flush();
		stream.Write(s_newline);
		writer.Reset();
	}

	private static async ValueTask FinishLineAsync(Utf8JsonWriter writer, Stream stream, CancellationToken ct)
	{
		await writer.FlushAsync(ct).ConfigureAwait(false);
		await stream.WriteAsync(s_newline, ct).ConfigureAwait(false);
		writer.Reset();
	}

	/// <summary>Writes bulk operations as NDJSON (action line + optional data line per operation).</summary>
	public static void Write(Stream stream, IReadOnlyList<BulkOperation> operations, IOpenSearchSerializer serializer)
	{
		var options = ResolveOptions(serializer);
		using var writer = new Utf8JsonWriter(stream);

		foreach (var op in operations)
		{
			op.WriteAction(writer);
			FinishLine(writer, stream);

			if (op.HasBody)
			{
				op.WriteBody(writer, options);
				FinishLine(writer, stream);
			}
		}
	}

	/// <summary>Asynchronously writes bulk operations as NDJSON.</summary>
	public static async ValueTask WriteAsync(Stream stream, IReadOnlyList<BulkOperation> operations, IOpenSearchSerializer serializer, CancellationToken ct = default)
	{
		var options = ResolveOptions(serializer);
		var writer = new Utf8JsonWriter(stream);
		await using (writer.ConfigureAwait(false))
		{
			foreach (var op in operations)
			{
				op.WriteAction(writer);
				await FinishLineAsync(writer, stream, ct).ConfigureAwait(false);

				if (op.HasBody)
				{
					op.WriteBody(writer, options);
					await FinishLineAsync(writer, stream, ct).ConfigureAwait(false);
				}
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
	private static void WriteHeaderBodyPairs<TItem, THeader, TBody>(
		Stream stream, IReadOnlyList<TItem> items, Func<TItem, THeader> header, Func<TItem, TBody> body, IOpenSearchSerializer serializer)
	{
		var options = ResolveOptions(serializer);
		using var writer = new Utf8JsonWriter(stream);

		foreach (var item in items)
		{
			JsonSerializer.Serialize(writer, header(item), options);
			FinishLine(writer, stream);

			JsonSerializer.Serialize(writer, body(item), options);
			FinishLine(writer, stream);
		}
	}

	/// <summary>Asynchronously writes NDJSON header + body pairs using projector functions.</summary>
	private static async ValueTask WriteHeaderBodyPairsAsync<TItem, THeader, TBody>(
		Stream stream, IReadOnlyList<TItem> items, Func<TItem, THeader> header, Func<TItem, TBody> body, IOpenSearchSerializer serializer, CancellationToken ct = default)
	{
		var options = ResolveOptions(serializer);
		var writer = new Utf8JsonWriter(stream);
		await using (writer.ConfigureAwait(false))
		{
			foreach (var item in items)
			{
				JsonSerializer.Serialize(writer, header(item), options);
				await FinishLineAsync(writer, stream, ct).ConfigureAwait(false);

				JsonSerializer.Serialize(writer, body(item), options);
				await FinishLineAsync(writer, stream, ct).ConfigureAwait(false);
			}
		}
	}
}
