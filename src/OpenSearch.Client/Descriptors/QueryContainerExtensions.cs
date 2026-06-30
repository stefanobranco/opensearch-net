namespace OpenSearch.Client;

/// <summary>
/// Static convenience methods for constructing <see cref="QueryContainer"/> instances
/// without building the underlying value objects by hand.
/// </summary>
public static class QueryContainerExtensions
{
	/// <summary>
	/// Creates a range query with typed parameters.
	/// Usage: <c>QueryContainerExtensions.Range("field", gt: "now-1M")</c>
	/// </summary>
	public static QueryContainer Range(string field,
		object? gt = null, object? gte = null,
		object? lt = null, object? lte = null,
		string? format = null, string? timeZone = null,
		float? boost = null) =>
		QueryContainer.Range(field, new RangeQuery
		{
			Gt = gt,
			Gte = gte,
			Lt = lt,
			Lte = lte,
			Format = format,
			TimeZone = timeZone,
			Boost = boost,
		});
}
