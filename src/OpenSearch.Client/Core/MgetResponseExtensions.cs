using System.Text.Json;
using System.Text.Json.Serialization;
using OpenSearch.Client.Core;

namespace OpenSearch.Client;

/// <summary>
/// Typed accessors for <see cref="MgetResponse"/>.
/// </summary>
public static class MgetResponseExtensions
{
	private static readonly JsonSerializerOptions s_options = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
	};

	/// <summary>
	/// Deserializes the raw docs into typed <see cref="MgetHit{T}"/> objects.
	/// </summary>
	public static IReadOnlyList<MgetHit<T>> GetDocs<T>(this MgetResponse response, JsonSerializerOptions? options = null)
	{
		if (response.Docs is null) return [];
		var opts = options ?? s_options;
		return response.Docs
			.Select(el => JsonSerializer.Deserialize<MgetHit<T>>(el.GetRawText(), opts))
			.Where(h => h is not null)
			.Select(h => h!)
			.ToList();
	}
}
