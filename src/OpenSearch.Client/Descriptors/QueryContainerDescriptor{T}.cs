using System.Linq.Expressions;
using System.Text.Json;
using OpenSearch.Client.Core;

namespace OpenSearch.Client.Common;

/// <summary>
/// Generic companion to <see cref="QueryContainerDescriptor"/> that provides
/// expression-based field selection for document-typed queries.
/// </summary>
public sealed class QueryContainerDescriptor<TDocument>
{
	internal QueryContainer? _value;

	// ── Field-keyed queries with expression overloads ──

	public QueryContainerDescriptor<TDocument> Term(
		Expression<Func<TDocument, object>> field, Action<TermQueryDescriptor> configure)
	{
		var descriptor = new TermQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Term(Field.ResolveName<TDocument>(field), (TermQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Term(
		string field, Action<TermQueryDescriptor> configure)
	{
		var descriptor = new TermQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Term(field, (TermQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Match(
		Expression<Func<TDocument, object>> field, Action<MatchQueryDescriptor> configure)
	{
		var descriptor = new MatchQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Match(Field.ResolveName<TDocument>(field), (MatchQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Match(
		string field, Action<MatchQueryDescriptor> configure)
	{
		var descriptor = new MatchQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Match(field, (MatchQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> MatchPhrase(
		Expression<Func<TDocument, object>> field, Action<MatchPhraseQueryDescriptor> configure)
	{
		var descriptor = new MatchPhraseQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.MatchPhrase(Field.ResolveName<TDocument>(field), (MatchPhraseQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> MatchPhrase(
		string field, Action<MatchPhraseQueryDescriptor> configure)
	{
		var descriptor = new MatchPhraseQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.MatchPhrase(field, (MatchPhraseQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Prefix(
		Expression<Func<TDocument, object>> field, Action<PrefixQueryDescriptor> configure)
	{
		var descriptor = new PrefixQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Prefix(Field.ResolveName<TDocument>(field), (PrefixQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Prefix(
		string field, Action<PrefixQueryDescriptor> configure)
	{
		var descriptor = new PrefixQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Prefix(field, (PrefixQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Wildcard(
		Expression<Func<TDocument, object>> field, Action<WildcardQueryDescriptor> configure)
	{
		var descriptor = new WildcardQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Wildcard(Field.ResolveName<TDocument>(field), (WildcardQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Wildcard(
		string field, Action<WildcardQueryDescriptor> configure)
	{
		var descriptor = new WildcardQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Wildcard(field, (WildcardQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Regexp(
		Expression<Func<TDocument, object>> field, Action<RegexpQueryDescriptor> configure)
	{
		var descriptor = new RegexpQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Regexp(Field.ResolveName<TDocument>(field), (RegexpQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Regexp(
		string field, Action<RegexpQueryDescriptor> configure)
	{
		var descriptor = new RegexpQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Regexp(field, (RegexpQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Fuzzy(
		Expression<Func<TDocument, object>> field, Action<FuzzyQueryDescriptor> configure)
	{
		var descriptor = new FuzzyQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Fuzzy(Field.ResolveName<TDocument>(field), (FuzzyQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Fuzzy(
		string field, Action<FuzzyQueryDescriptor> configure)
	{
		var descriptor = new FuzzyQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Fuzzy(field, (FuzzyQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> MatchPhrasePrefix(
		Expression<Func<TDocument, object>> field, Action<MatchPhrasePrefixQueryDescriptor> configure)
	{
		var descriptor = new MatchPhrasePrefixQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.MatchPhrasePrefix(Field.ResolveName<TDocument>(field), (MatchPhrasePrefixQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> MatchPhrasePrefix(
		string field, Action<MatchPhrasePrefixQueryDescriptor> configure)
	{
		var descriptor = new MatchPhrasePrefixQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.MatchPhrasePrefix(field, (MatchPhrasePrefixQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Range(
		Expression<Func<TDocument, object>> field, Action<RangeQueryDescriptor> configure)
	{
		var descriptor = new RangeQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Range(Field.ResolveName<TDocument>(field), (JsonElement)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Range(
		string field, Action<RangeQueryDescriptor> configure)
	{
		var descriptor = new RangeQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Range(field, (JsonElement)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Exists(
		Expression<Func<TDocument, object>> field)
	{
		_value = QueryContainer.Exists(new ExistsQuery { Field = Field.ResolveName<TDocument>(field) });
		return this;
	}

	public QueryContainerDescriptor<TDocument> Exists(ExistsQuery value)
	{
		_value = QueryContainer.Exists(value);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Exists(Action<ExistsQueryDescriptor> configure)
	{
		var descriptor = new ExistsQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Exists((ExistsQuery)descriptor);
		return this;
	}

	// ── Compound queries that propagate <TDocument> ──

	public QueryContainerDescriptor<TDocument> Bool(
		Action<BoolQueryDescriptor<TDocument>> configure)
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

	public QueryContainerDescriptor<TDocument> ConstantScore(
		Action<ConstantScoreQueryDescriptor<TDocument>> configure)
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

	public QueryContainerDescriptor<TDocument> Nested(
		Action<NestedQueryDescriptor<TDocument>> configure)
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

	// ── Non-field queries (no <T> needed) ──

	public QueryContainerDescriptor<TDocument> MatchAll(Action<MatchAllQueryDescriptor> configure)
	{
		var descriptor = new MatchAllQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.MatchAll((MatchAllQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> MatchAll(MatchAllQuery value)
	{
		_value = QueryContainer.MatchAll(value);
		return this;
	}

	/// <summary>Creates a match_all query with default settings.</summary>
	public QueryContainerDescriptor<TDocument> MatchAll()
	{
		_value = QueryContainer.MatchAll(new MatchAllQuery());
		return this;
	}

	public QueryContainerDescriptor<TDocument> MatchNone(Action<MatchNoneQueryDescriptor> configure)
	{
		var descriptor = new MatchNoneQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.MatchNone((MatchNoneQuery)descriptor);
		return this;
	}

	/// <summary>Creates a match_none query with default settings.</summary>
	public QueryContainerDescriptor<TDocument> MatchNone()
	{
		_value = QueryContainer.MatchNone(new MatchNoneQuery());
		return this;
	}

	public QueryContainerDescriptor<TDocument> Ids(Action<IdsQueryDescriptor> configure)
	{
		var descriptor = new IdsQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Ids((IdsQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Ids(IdsQuery value)
	{
		_value = QueryContainer.Ids(value);
		return this;
	}

	public QueryContainerDescriptor<TDocument> MultiMatch(Action<MultiMatchQueryDescriptor> configure)
	{
		var descriptor = new MultiMatchQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.MultiMatch((MultiMatchQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> QueryString(Action<QueryStringQueryDescriptor> configure)
	{
		var descriptor = new QueryStringQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.QueryString((QueryStringQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> SimpleQueryString(Action<SimpleQueryStringQueryDescriptor> configure)
	{
		var descriptor = new SimpleQueryStringQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.SimpleQueryString((SimpleQueryStringQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Terms(Action<TermsQueryDescriptor> configure)
	{
		var descriptor = new TermsQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Terms((TermsQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Terms(TermsQuery value)
	{
		_value = QueryContainer.Terms(value);
		return this;
	}

	public QueryContainerDescriptor<TDocument> FunctionScore(
		Action<FunctionScoreQueryDescriptor> configure)
	{
		var descriptor = new FunctionScoreQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.FunctionScore((FunctionScoreQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> FunctionScore(FunctionScoreQuery value)
	{
		_value = QueryContainer.FunctionScore(value);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Intervals(
		Expression<Func<TDocument, object>> field, Action<IntervalsQueryDescriptor> configure)
	{
		var descriptor = new IntervalsQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Intervals(Field.ResolveName<TDocument>(field), (IntervalsQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Intervals(
		string field, Action<IntervalsQueryDescriptor> configure)
	{
		var descriptor = new IntervalsQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Intervals(field, (IntervalsQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Intervals(
		Expression<Func<TDocument, object>> field, IntervalsQuery value)
	{
		_value = QueryContainer.Intervals(Field.ResolveName<TDocument>(field), value);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Intervals(
		string field, IntervalsQuery value)
	{
		_value = QueryContainer.Intervals(field, value);
		return this;
	}

	// ── Terms query (field via expression) ──

	/// <summary>Creates a terms query with string values using an expression-based field.</summary>
	public QueryContainerDescriptor<TDocument> Terms(
		Expression<Func<TDocument, object>> field, params string[] values)
	{
		var query = new TermsQuery();
		query.ExtensionData ??= new();
		query.ExtensionData[Field.ResolveName<TDocument>(field)] =
			System.Text.Json.JsonSerializer.SerializeToElement(values);
		_value = QueryContainer.Terms(query);
		return this;
	}

	/// <summary>Creates a terms query with typed values using an expression-based field.</summary>
	public QueryContainerDescriptor<TDocument> Terms<TValue>(
		Expression<Func<TDocument, object>> field, params TValue[] values)
	{
		var query = new TermsQuery();
		query.ExtensionData ??= new();
		query.ExtensionData[Field.ResolveName<TDocument>(field)] =
			System.Text.Json.JsonSerializer.SerializeToElement(values);
		_value = QueryContainer.Terms(query);
		return this;
	}

	/// <summary>Creates a terms query with string values using a string field name.</summary>
	public QueryContainerDescriptor<TDocument> Terms(
		string field, params string[] values)
	{
		var query = new TermsQuery();
		query.ExtensionData ??= new();
		query.ExtensionData[field] =
			System.Text.Json.JsonSerializer.SerializeToElement(values);
		_value = QueryContainer.Terms(query);
		return this;
	}

	// ── Pass-through queries (no field expression needed) ──

	public QueryContainerDescriptor<TDocument> MoreLikeThis(MoreLikeThisQuery value)
	{
		_value = QueryContainer.MoreLikeThis(value);
		return this;
	}

	public QueryContainerDescriptor<TDocument> MoreLikeThis(
		Action<MoreLikeThisQueryDescriptor> configure)
	{
		var descriptor = new MoreLikeThisQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.MoreLikeThis((MoreLikeThisQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Script(ScriptQuery value)
	{
		_value = QueryContainer.Script(value);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Script(
		Action<ScriptQueryDescriptor> configure)
	{
		var descriptor = new ScriptQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Script((ScriptQuery)descriptor);
		return this;
	}

	// ── kNN query ──

	public QueryContainerDescriptor<TDocument> Knn(
		Expression<Func<TDocument, object>> field, Action<KnnQueryDescriptor> configure)
	{
		var descriptor = new KnnQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Knn(Field.ResolveName<TDocument>(field), (KnnQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Knn(
		string field, Action<KnnQueryDescriptor> configure)
	{
		var descriptor = new KnnQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.Knn(field, (KnnQuery)descriptor);
		return this;
	}

	public QueryContainerDescriptor<TDocument> Knn(string field, KnnQuery value)
	{
		_value = QueryContainer.Knn(field, value);
		return this;
	}

	/// <summary>Creates a terms query with string values using a string field name (for field expressions with Suffix).</summary>
	public QueryContainerDescriptor<TDocument> Terms(
		Field field, params string[] values)
	{
		var query = new TermsQuery();
		query.ExtensionData ??= new();
		query.ExtensionData[field.Name] =
			System.Text.Json.JsonSerializer.SerializeToElement(values);
		_value = QueryContainer.Terms(query);
		return this;
	}

	public static implicit operator QueryContainer?(QueryContainerDescriptor<TDocument> d) => d._value;
}
