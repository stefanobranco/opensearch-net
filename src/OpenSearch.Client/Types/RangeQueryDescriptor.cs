namespace OpenSearch.Client;

/// <summary>
/// Fluent builder for <see cref="RangeQuery"/> values used in <c>QueryContainer.Range</c>.
/// </summary>
public sealed class RangeQueryDescriptor
{
	private readonly RangeQuery _query = new();

	public RangeQueryDescriptor Gte(object value) { _query.Gte = value; return this; }
	public RangeQueryDescriptor Gt(object value) { _query.Gt = value; return this; }
	public RangeQueryDescriptor Lte(object value) { _query.Lte = value; return this; }
	public RangeQueryDescriptor Lt(object value) { _query.Lt = value; return this; }
	public RangeQueryDescriptor Format(string value) { _query.Format = value; return this; }
	public RangeQueryDescriptor TimeZone(string value) { _query.TimeZone = value; return this; }
	public RangeQueryDescriptor Boost(float value) { _query.Boost = value; return this; }
	public RangeQueryDescriptor Relation(RangeRelation value) { _query.Relation = value; return this; }

	public static implicit operator RangeQuery(RangeQueryDescriptor d) => d._query;
}
