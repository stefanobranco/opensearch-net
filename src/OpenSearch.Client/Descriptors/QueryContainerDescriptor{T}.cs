using System.Linq.Expressions;
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

	public QueryContainerDescriptor<TDocument> MatchNone(Action<MatchNoneQueryDescriptor> configure)
	{
		var descriptor = new MatchNoneQueryDescriptor();
		configure(descriptor);
		_value = QueryContainer.MatchNone((MatchNoneQuery)descriptor);
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

	public static implicit operator QueryContainer?(QueryContainerDescriptor<TDocument> d) => d._value;
}
