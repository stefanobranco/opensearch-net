using OpenSearch.Client.Common;

namespace OpenSearch.Client.Core;

/// <summary>
/// Wraps <see cref="HighlightField"/> with a field name setter for use in the
/// <see cref="HighlightDescriptorExtensions.Fields"/> fluent builder.
/// </summary>
public sealed class HighlightFieldEntryDescriptor
{
	internal string? _fieldName;
	internal HighlightField _value = new();

	public HighlightFieldEntryDescriptor Field(string name) { _fieldName = name; return this; }
	public HighlightFieldEntryDescriptor Type(string? value) { _value.Type = value; return this; }
	public HighlightFieldEntryDescriptor FragmentSize(int? value) { _value.FragmentSize = value; return this; }
	public HighlightFieldEntryDescriptor NumberOfFragments(int? value) { _value.NumberOfFragments = value; return this; }
	public HighlightFieldEntryDescriptor MatchedFields(List<string>? value) { _value.MatchedFields = value; return this; }
	public HighlightFieldEntryDescriptor PreTags(List<string>? value) { _value.PreTags = value; return this; }
	public HighlightFieldEntryDescriptor PostTags(List<string>? value) { _value.PostTags = value; return this; }
	public HighlightFieldEntryDescriptor BoundaryChars(string? value) { _value.BoundaryChars = value; return this; }
	public HighlightFieldEntryDescriptor BoundaryMaxScan(int? value) { _value.BoundaryMaxScan = value; return this; }
	public HighlightFieldEntryDescriptor BoundaryScanner(BoundaryScanner? value) { _value.BoundaryScanner = value; return this; }
	public HighlightFieldEntryDescriptor BoundaryScannerLocale(string? value) { _value.BoundaryScannerLocale = value; return this; }
	public HighlightFieldEntryDescriptor ForceSource(bool? value) { _value.ForceSource = value; return this; }
	public HighlightFieldEntryDescriptor Fragmenter(HighlighterFragmenter? value) { _value.Fragmenter = value; return this; }
	public HighlightFieldEntryDescriptor FragmentOffset(int? value) { _value.FragmentOffset = value; return this; }
	public HighlightFieldEntryDescriptor HighlightFilter(bool? value) { _value.HighlightFilter = value; return this; }
	public HighlightFieldEntryDescriptor HighlightQuery(QueryContainer? value) { _value.HighlightQuery = value; return this; }
	public HighlightFieldEntryDescriptor MaxFragmentLength(int? value) { _value.MaxFragmentLength = value; return this; }
	public HighlightFieldEntryDescriptor MaxAnalyzerOffset(int? value) { _value.MaxAnalyzerOffset = value; return this; }
	public HighlightFieldEntryDescriptor NoMatchSize(int? value) { _value.NoMatchSize = value; return this; }
	public HighlightFieldEntryDescriptor Order(HighlighterOrder? value) { _value.Order = value; return this; }
	public HighlightFieldEntryDescriptor PhraseLimit(int? value) { _value.PhraseLimit = value; return this; }
	public HighlightFieldEntryDescriptor RequireFieldMatch(bool? value) { _value.RequireFieldMatch = value; return this; }
	public HighlightFieldEntryDescriptor TagsSchema(HighlighterTagsSchema? value) { _value.TagsSchema = value; return this; }

	public static implicit operator HighlightField(HighlightFieldEntryDescriptor d) => d._value;
}
