using System.Text.Json;
using OpenSearch.Client.Core;

namespace OpenSearch.Client;

/// <summary>
/// Typed accessors for <see cref="MgetResponse"/>.
/// </summary>
public static class MgetResponseExtensions
{
	private static JsonSerializerOptions DefaultOptions => OpenSearchJsonOptions.Default;

	/// <summary>
	/// Deserializes the <c>_source</c> of each doc into <typeparamref name="T"/>.
	/// Returns items with <see cref="MgetHit{T}.Found"/>, <see cref="MgetHit{T}.Id"/>,
	/// and <see cref="MgetHit{T}.Source"/>.
	/// </summary>
	public static IReadOnlyList<MgetHit<T>> GetDocs<T>(this MgetResponse response, JsonSerializerOptions? options = null)
	{
		if (response.Docs is null) return [];
		var opts = options ?? DefaultOptions;
		return response.Docs
			.Select(item => new MgetHit<T>
			{
				Index = item.Index,
				Id = item.Id,
				Found = item.Found,
				Version = item.Version,
				SeqNo = item.SeqNo,
				PrimaryTerm = item.PrimaryTerm,
				Routing = item.Routing,
				Source = item.Source is { } src && src.ValueKind != JsonValueKind.Null && src.ValueKind != JsonValueKind.Undefined
					? JsonSerializer.Deserialize<T>(src.GetRawText(), opts)
					: default,
			})
			.ToList();
	}

	/// <summary>
	/// Returns typed docs that match the given IDs, preserving the ID order.
	/// Equivalent to NEST's <c>response.GetMany&lt;T&gt;(ids)</c>.
	/// </summary>
	public static IReadOnlyList<MgetHit<T>> GetMany<T>(this MgetResponse response, IEnumerable<string> ids, JsonSerializerOptions? options = null)
	{
		var docs = response.GetDocs<T>(options);
		var byId = docs.ToDictionary(d => d.Id ?? "", d => d);
		return ids
			.Select(id => byId.TryGetValue(id, out var hit) ? hit : new MgetHit<T> { Id = id, Found = false })
			.ToList();
	}
}
