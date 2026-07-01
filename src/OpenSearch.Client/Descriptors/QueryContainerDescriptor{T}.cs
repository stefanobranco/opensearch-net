using System.Linq.Expressions;

namespace OpenSearch.Client;

/// <summary>
/// Hand-written companion to the generated <see cref="QueryContainerDescriptor{TDocument}"/> partial
/// (Generated/Common/Descriptors/QueryContainerDescriptor.Generic.cs). The generated partial owns
/// <c>_value</c>, the implicit conversion, and every variant the generator can emit; this partial
/// supplies the ones it can't: compound queries that thread <c>TDocument</c> into a nested-query
/// sub-descriptor (bool / constant_score / nested), plus a few expression-based convenience overloads
/// (exists by field, zero-arg match_all / match_none, and the terms <c>params</c> forms).
/// </summary>
public sealed partial class QueryContainerDescriptor<TDocument>
{
	// ── Compound queries that propagate <TDocument> into nested clauses ──

	public QueryContainerDescriptor<TDocument> Bool(Action<BoolQueryDescriptor<TDocument>> configure)
	{
		var descriptor = new BoolQueryDescriptor<TDocument>();
		configure(descriptor);
		_value = QueryContainer.Bool((BoolQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Bool(BoolQuery value)
	{
		_value = QueryContainer.Bool(value);
		return this;
	}

	public QueryContainerDescriptor<TDocument> ConstantScore(Action<ConstantScoreQueryDescriptor<TDocument>> configure)
	{
		var descriptor = new ConstantScoreQueryDescriptor<TDocument>();
		configure(descriptor);
		_value = QueryContainer.ConstantScore((ConstantScoreQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> ConstantScore(ConstantScoreQuery value)
	{
		_value = QueryContainer.ConstantScore(value);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Nested(Action<NestedQueryDescriptor<TDocument>> configure)
	{
		var descriptor = new NestedQueryDescriptor<TDocument>();
		configure(descriptor);
		_value = QueryContainer.Nested((NestedQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Nested(NestedQuery value)
	{
		_value = QueryContainer.Nested(value);
		return this;
	}

	// ── Range: its value descriptor (RangeQueryDescriptor) is hand-written, so the generator can't
	//    emit the Action-based overloads; the generated partial supplies only the value forms. ──

	public QueryContainerDescriptor<TDocument> Range(Expression<Func<TDocument, object>> field, Action<RangeQueryDescriptor> configure)
	{
		var descriptor = new RangeQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Range(Field.ResolveName<TDocument>(field), (RangeQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Range(string field, Action<RangeQueryDescriptor> configure)
	{
		var descriptor = new RangeQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Range(field, (RangeQuery)descriptor);
		return this;
	}

	// ── Expression-based convenience overloads the generator doesn't produce ──

	/// <summary>Creates an exists query for a field selected by an expression.</summary>
	public QueryContainerDescriptor<TDocument> Exists(Expression<Func<TDocument, object>> field)
	{
		_value = QueryContainer.Exists(new ExistsQuery { Field = Field.ResolveName<TDocument>(field) });
		return this;
	}

	/// <summary>Creates a match_all query with default settings.</summary>
	public QueryContainerDescriptor<TDocument> MatchAll()
	{
		_value = QueryContainer.MatchAll(new MatchAllQuery());
		return this;
	}

	/// <summary>Creates a match_none query with default settings.</summary>
	public QueryContainerDescriptor<TDocument> MatchNone()
	{
		_value = QueryContainer.MatchNone(new MatchNoneQuery());
		return this;
	}

	/// <summary>Creates a terms query with string values for a field selected by an expression.</summary>
	public QueryContainerDescriptor<TDocument> Terms(Expression<Func<TDocument, object>> field, params string[] values) =>
		TermsByField(Field.ResolveName<TDocument>(field), values);

	/// <summary>Creates a terms query with typed values for a field selected by an expression.</summary>
	public QueryContainerDescriptor<TDocument> Terms<TValue>(Expression<Func<TDocument, object>> field, params TValue[] values) =>
		TermsByField(Field.ResolveName<TDocument>(field), values);

	/// <summary>Creates a terms query with string values for a field name.</summary>
	public QueryContainerDescriptor<TDocument> Terms(string field, params string[] values) =>
		TermsByField(field, values);

	/// <summary>Creates a terms query with string values for a <see cref="Field"/> (supports <c>Suffix()</c>).</summary>
	public QueryContainerDescriptor<TDocument> Terms(Field field, params string[] values) =>
		TermsByField(field.Name, values);

	private QueryContainerDescriptor<TDocument> TermsByField<TValue>(string field, TValue[] values)
	{
		var query = new TermsQuery();
		query.ExtensionData ??= new();
		query.ExtensionData[field] = System.Text.Json.JsonSerializer.SerializeToElement(values);
		_value = QueryContainer.Terms(query);
		return this;
	}
}
