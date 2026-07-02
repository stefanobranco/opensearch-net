namespace OpenSearch.Client;

/// <summary>
/// The result of a <c>max_bucket</c> / <c>min_bucket</c> pipeline aggregation: the extreme value
/// plus the key(s) of the bucket(s) where it occurred (multiple on ties).
/// </summary>
public sealed class BucketMetricValueResult
{
	internal BucketMetricValueResult(double? value, string? valueAsString, IReadOnlyList<string> keys)
	{
		Value = value;
		ValueAsString = valueAsString;
		Keys = keys;
	}

	public double? Value { get; }
	public string? ValueAsString { get; }

	/// <summary>The bucket key(s) holding the extreme value; more than one on ties.</summary>
	public IReadOnlyList<string> Keys { get; }
}
