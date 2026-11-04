using System.Text.Json;

namespace OpenSearch.Client;

public partial class MgetResponse
{
	/// <summary>
	/// mget items carry no status; a document on a shard without any available copy fails with
	/// the exception a single get returns as 503, while the mget itself still returns 200.
	/// </summary>
	protected override bool HasRetryableFailure() =>
		Docs?.Any(d => d.Error is { ValueKind: JsonValueKind.Object } error
			&& error.TryGetProperty("type", out var type)
			&& type.ValueKind == JsonValueKind.String
			&& type.GetString() == "no_shard_available_action_exception") == true;
}
