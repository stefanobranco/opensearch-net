using System.Text.Json;

namespace OpenSearch.Client.Common;

/// <summary>
/// Static convenience methods for constructing <see cref="QueryContainer"/> instances
/// without requiring manual <c>JsonSerializer.SerializeToElement</c> calls.
/// </summary>
public static class QueryContainerExtensions
{
	/// <summary>
	/// Creates a range query with typed parameters.
	/// Usage: <c>QueryContainers.Range("field", gt: "now-1M")</c>
	/// </summary>
	public static QueryContainer Range(string field,
		object? gt = null, object? gte = null,
		object? lt = null, object? lte = null,
		string? format = null, string? timeZone = null,
		float? boost = null)
	{
		var props = new Dictionary<string, object>();
		if (gt is not null) props["gt"] = gt;
		if (gte is not null) props["gte"] = gte;
		if (lt is not null) props["lt"] = lt;
		if (lte is not null) props["lte"] = lte;
		if (format is not null) props["format"] = format;
		if (timeZone is not null) props["time_zone"] = timeZone;
		if (boost is not null) props["boost"] = boost.Value;
		return QueryContainer.Range(field, JsonSerializer.SerializeToElement(props));
	}
}
