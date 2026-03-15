using OpenSearch.Client.Common;

namespace OpenSearch.Client.Core;

/// <summary>
/// Generic companion to <see cref="SearchRequestDescriptor"/> that provides
/// expression-based field selection through <see cref="QueryContainerDescriptor{TDocument}"/>.
/// </summary>
public sealed class SearchRequestDescriptor<TDocument>
{
	internal SearchRequest _value = new();

	// ── Body properties with generic descriptor support ──

	public SearchRequestDescriptor<TDocument> Query(
		Action<QueryContainerDescriptor<TDocument>> configure)
	{
		var descriptor = new QueryContainerDescriptor<TDocument>();
		configure(descriptor);
		_value.Query = descriptor;
		return this;
	}

	public SearchRequestDescriptor<TDocument> Query(QueryContainer? value)
	{
		_value.Query = value;
		return this;
	}

	public SearchRequestDescriptor<TDocument> PostFilter(
		Action<QueryContainerDescriptor<TDocument>> configure)
	{
		var descriptor = new QueryContainerDescriptor<TDocument>();
		configure(descriptor);
		_value.PostFilter = descriptor;
		return this;
	}

	public SearchRequestDescriptor<TDocument> PostFilter(QueryContainer? value)
	{
		_value.PostFilter = value;
		return this;
	}

	public SearchRequestDescriptor<TDocument> Aggregations(
		Action<AggregationsDictDescriptor> configure)
	{
		var desc = new AggregationsDictDescriptor();
		configure(desc);
		_value.Aggregations = desc;
		return this;
	}

	public SearchRequestDescriptor<TDocument> Aggregations(Dictionary<string, AggregationContainer>? value)
	{
		_value.Aggregations = value;
		return this;
	}

	public SearchRequestDescriptor<TDocument> Highlight(Action<HighlightDescriptor> configure)
	{
		var descriptor = new HighlightDescriptor();
		configure(descriptor);
		_value.Highlight = descriptor;
		return this;
	}

	public SearchRequestDescriptor<TDocument> Highlight(Highlight? value)
	{
		_value.Highlight = value;
		return this;
	}

	public SearchRequestDescriptor<TDocument> Sort(List<SortOptions>? value) { _value.Sort = value; return this; }
	public SearchRequestDescriptor<TDocument> Sort(params SortOptions[] sorts) { _value.Sort = sorts.ToList(); return this; }

	public SearchRequestDescriptor<TDocument> Source(SourceConfig? value) { _value.Source = value; return this; }
	public SearchRequestDescriptor<TDocument> Source(bool fetch) { _value.Source = SourceConfig.Enabled(fetch); return this; }

	// ── Common body properties ──

	public SearchRequestDescriptor<TDocument> Size(int? value) { _value.Size = value; return this; }
	public SearchRequestDescriptor<TDocument> From(int? value) { _value.From = value; return this; }
	public SearchRequestDescriptor<TDocument> Explain(bool? value) { _value.Explain = value; return this; }
	public SearchRequestDescriptor<TDocument> MinScore(float? value) { _value.MinScore = value; return this; }
	public SearchRequestDescriptor<TDocument> Profile(bool? value) { _value.Profile = value; return this; }
	public SearchRequestDescriptor<TDocument> TrackScores(bool? value) { _value.TrackScores = value; return this; }
	public SearchRequestDescriptor<TDocument> TrackTotalHits(System.Text.Json.JsonElement? value) { _value.TrackTotalHits = value; return this; }
	public SearchRequestDescriptor<TDocument> Version(bool? value) { _value.Version = value; return this; }
	public SearchRequestDescriptor<TDocument> SeqNoPrimaryTerm(bool? value) { _value.SeqNoPrimaryTerm = value; return this; }
	public SearchRequestDescriptor<TDocument> Timeout(string? value) { _value.Timeout = value; return this; }
	public SearchRequestDescriptor<TDocument> TerminateAfter(int? value) { _value.TerminateAfter = value; return this; }
	public SearchRequestDescriptor<TDocument> IncludeNamedQueriesScore(bool? value) { _value.IncludeNamedQueriesScore = value; return this; }
	public SearchRequestDescriptor<TDocument> SearchPipeline(string? value) { _value.SearchPipeline = value; return this; }
	public SearchRequestDescriptor<TDocument> StoredFields(List<string>? value) { _value.StoredFields = value; return this; }
	public SearchRequestDescriptor<TDocument> IndicesBoost(List<Dictionary<string, float>>? value) { _value.IndicesBoost = value; return this; }
	public SearchRequestDescriptor<TDocument> ScriptFields(Dictionary<string, ScriptField>? value) { _value.ScriptFields = value; return this; }
	public SearchRequestDescriptor<TDocument> Stats(List<string>? value) { _value.Stats = value; return this; }
	public SearchRequestDescriptor<TDocument> Ext(Dictionary<string, object>? value) { _value.Ext = value; return this; }
	public SearchRequestDescriptor<TDocument> Derived(Dictionary<string, DerivedField>? value) { _value.Derived = value; return this; }

	public SearchRequestDescriptor<TDocument> Rescore(params Action<RescoreDescriptor>[] configure)
	{
		var list = new List<Rescore>();
		foreach (var action in configure)
		{
			var descriptor = new RescoreDescriptor();
			action(descriptor);
			list.Add(descriptor);
		}
		_value.Rescore = list;
		return this;
	}

	public SearchRequestDescriptor<TDocument> Collapse(Action<FieldCollapseDescriptor> configure)
	{
		var descriptor = new FieldCollapseDescriptor();
		configure(descriptor);
		_value.Collapse = descriptor;
		return this;
	}

	public SearchRequestDescriptor<TDocument> Suggest(Action<SuggesterDescriptor> configure)
	{
		var descriptor = new SuggesterDescriptor();
		configure(descriptor);
		_value.Suggest = descriptor;
		return this;
	}

	public SearchRequestDescriptor<TDocument> Pit(Action<PointInTimeReferenceDescriptor> configure)
	{
		var descriptor = new PointInTimeReferenceDescriptor();
		configure(descriptor);
		_value.Pit = descriptor;
		return this;
	}

	// ── Query-string parameters ──

	public SearchRequestDescriptor<TDocument> Index(List<string>? value) { _value.Index = value; return this; }
	public SearchRequestDescriptor<TDocument> Scroll(string? value) { _value.Scroll = value; return this; }
	public SearchRequestDescriptor<TDocument> Routing(System.Text.Json.JsonElement? value) { _value.Routing = value; return this; }
	public SearchRequestDescriptor<TDocument> Preference(string? value) { _value.Preference = value; return this; }
	public SearchRequestDescriptor<TDocument> AllowNoIndices(bool? value) { _value.AllowNoIndices = value; return this; }
	public SearchRequestDescriptor<TDocument> IgnoreUnavailable(bool? value) { _value.IgnoreUnavailable = value; return this; }
	public SearchRequestDescriptor<TDocument> ExpandWildcards(List<string>? value) { _value.ExpandWildcards = value; return this; }
	public SearchRequestDescriptor<TDocument> TypedKeys(bool? value) { _value.TypedKeys = value; return this; }

	public static implicit operator SearchRequest(SearchRequestDescriptor<TDocument> d) => d._value;
}
