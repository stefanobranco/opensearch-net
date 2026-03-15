using OpenSearch.Client.Common;

namespace OpenSearch.Client.Core;

/// <summary>
/// Generic companion to <see cref="NestedQueryDescriptor"/> that propagates
/// <typeparamref name="TDocument"/> to the Query clause.
/// </summary>
public sealed class NestedQueryDescriptor<TDocument>
{
	internal NestedQuery _value = new();

	public NestedQueryDescriptor<TDocument> Boost(float? value) { _value.Boost = value; return this; }
	public NestedQueryDescriptor<TDocument> Name(string? value) { _value.Name = value; return this; }
	public NestedQueryDescriptor<TDocument> Path(string? value) { _value.Path = value; return this; }
	public NestedQueryDescriptor<TDocument> ScoreMode(ChildScoreMode? value) { _value.ScoreMode = value; return this; }
	public NestedQueryDescriptor<TDocument> IgnoreUnmapped(bool? value) { _value.IgnoreUnmapped = value; return this; }
	public NestedQueryDescriptor<TDocument> InnerHits(InnerHits? value) { _value.InnerHits = value; return this; }

	public NestedQueryDescriptor<TDocument> Query(
		Action<QueryContainerDescriptor<TDocument>> configure)
	{
		var descriptor = new QueryContainerDescriptor<TDocument>();
		configure(descriptor);
		_value.Query = descriptor;
		return this;
	}

	public NestedQueryDescriptor<TDocument> Query(QueryContainer? value)
	{
		_value.Query = value;
		return this;
	}

	public static implicit operator NestedQuery(NestedQueryDescriptor<TDocument> d) => d._value;
}
