using OpenSearch.Client.Common;

namespace OpenSearch.Client.Core;

/// <summary>
/// Generic companion to <see cref="ConstantScoreQueryDescriptor"/> that propagates
/// <typeparamref name="TDocument"/> to the Filter clause.
/// </summary>
public sealed class ConstantScoreQueryDescriptor<TDocument>
{
	internal ConstantScoreQuery _value = new();

	public ConstantScoreQueryDescriptor<TDocument> Boost(float? value) { _value.Boost = value; return this; }
	public ConstantScoreQueryDescriptor<TDocument> Name(string? value) { _value.Name = value; return this; }

	public ConstantScoreQueryDescriptor<TDocument> Filter(
		Action<QueryContainerDescriptor<TDocument>> configure)
	{
		var descriptor = new QueryContainerDescriptor<TDocument>();
		configure(descriptor);
		_value.Filter = descriptor;
		return this;
	}

	public ConstantScoreQueryDescriptor<TDocument> Filter(QueryContainer? value)
	{
		_value.Filter = value;
		return this;
	}

	public static implicit operator ConstantScoreQuery(ConstantScoreQueryDescriptor<TDocument> d) => d._value;
}
