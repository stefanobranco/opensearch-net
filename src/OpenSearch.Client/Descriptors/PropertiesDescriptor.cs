using System.Linq.Expressions;

namespace OpenSearch.Client;

/// <summary>
/// Builds a <c>Dictionary&lt;string, Property&gt;</c> of named field mappings — the fluent form of a
/// mapping's <c>properties</c>. Each method adds one field by wire name or by a <typeparamref name="TDocument"/>
/// member expression (snake-cased via <see cref="Field"/>), configuring the property through its typed
/// descriptor. Mirrors <see cref="AggregationsDictDescriptor"/> for aggregations; nest object/nested
/// fields by configuring their own <c>Properties</c> in the property descriptor.
/// </summary>
/// <typeparam name="TDocument">The document type whose members name the fields (use <see cref="object"/> for string-only naming).</typeparam>
public sealed class PropertiesDescriptor<TDocument>
{
	internal readonly Dictionary<string, Property> _dict = new();

	private PropertiesDescriptor<TDocument> Add<TDesc>(string name, Action<TDesc> configure, Func<TDesc, Property> factory)
		where TDesc : new()
	{
		var descriptor = new TDesc();
		configure(descriptor);
		_dict[name] = factory(descriptor);
		return this;
	}

	/// <summary>Adds a Binary field by name.</summary>
	public PropertiesDescriptor<TDocument> Binary(string name, Action<BinaryPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.BinaryProperty(d));
	/// <summary>Adds a Binary field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Binary(Expression<Func<TDocument, object>> field, Action<BinaryPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.BinaryProperty(d));

	/// <summary>Adds a Boolean field by name.</summary>
	public PropertiesDescriptor<TDocument> Boolean(string name, Action<BooleanPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.BooleanProperty(d));
	/// <summary>Adds a Boolean field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Boolean(Expression<Func<TDocument, object>> field, Action<BooleanPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.BooleanProperty(d));

	/// <summary>Adds a Join field by name.</summary>
	public PropertiesDescriptor<TDocument> Join(string name, Action<JoinPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.JoinProperty(d));
	/// <summary>Adds a Join field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Join(Expression<Func<TDocument, object>> field, Action<JoinPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.JoinProperty(d));

	/// <summary>Adds a Keyword field by name.</summary>
	public PropertiesDescriptor<TDocument> Keyword(string name, Action<KeywordPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.KeywordProperty(d));
	/// <summary>Adds a Keyword field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Keyword(Expression<Func<TDocument, object>> field, Action<KeywordPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.KeywordProperty(d));

	/// <summary>Adds a MatchOnlyText field by name.</summary>
	public PropertiesDescriptor<TDocument> MatchOnlyText(string name, Action<MatchOnlyTextPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.MatchOnlyTextProperty(d));
	/// <summary>Adds a MatchOnlyText field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> MatchOnlyText(Expression<Func<TDocument, object>> field, Action<MatchOnlyTextPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.MatchOnlyTextProperty(d));

	/// <summary>Adds a Percolator field by name.</summary>
	public PropertiesDescriptor<TDocument> Percolator(string name, Action<PercolatorPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.PercolatorProperty(d));
	/// <summary>Adds a Percolator field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Percolator(Expression<Func<TDocument, object>> field, Action<PercolatorPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.PercolatorProperty(d));

	/// <summary>Adds a RankFeature field by name.</summary>
	public PropertiesDescriptor<TDocument> RankFeature(string name, Action<RankFeaturePropertyDescriptor> configure) =>
		Add(name, configure, d => Property.RankFeatureProperty(d));
	/// <summary>Adds a RankFeature field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> RankFeature(Expression<Func<TDocument, object>> field, Action<RankFeaturePropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.RankFeatureProperty(d));

	/// <summary>Adds a RankFeatures field by name.</summary>
	public PropertiesDescriptor<TDocument> RankFeatures(string name, Action<RankFeaturesPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.RankFeaturesProperty(d));
	/// <summary>Adds a RankFeatures field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> RankFeatures(Expression<Func<TDocument, object>> field, Action<RankFeaturesPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.RankFeaturesProperty(d));

	/// <summary>Adds a SearchAsYouType field by name.</summary>
	public PropertiesDescriptor<TDocument> SearchAsYouType(string name, Action<SearchAsYouTypePropertyDescriptor> configure) =>
		Add(name, configure, d => Property.SearchAsYouTypeProperty(d));
	/// <summary>Adds a SearchAsYouType field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> SearchAsYouType(Expression<Func<TDocument, object>> field, Action<SearchAsYouTypePropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.SearchAsYouTypeProperty(d));

	/// <summary>Adds a Text field by name.</summary>
	public PropertiesDescriptor<TDocument> Text(string name, Action<TextPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.TextProperty(d));
	/// <summary>Adds a Text field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Text(Expression<Func<TDocument, object>> field, Action<TextPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.TextProperty(d));

	/// <summary>Adds a Version field by name.</summary>
	public PropertiesDescriptor<TDocument> Version(string name, Action<VersionPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.VersionProperty(d));
	/// <summary>Adds a Version field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Version(Expression<Func<TDocument, object>> field, Action<VersionPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.VersionProperty(d));

	/// <summary>Adds a Wildcard field by name.</summary>
	public PropertiesDescriptor<TDocument> Wildcard(string name, Action<WildcardPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.WildcardProperty(d));
	/// <summary>Adds a Wildcard field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Wildcard(Expression<Func<TDocument, object>> field, Action<WildcardPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.WildcardProperty(d));

	/// <summary>Adds a DateNanos field by name.</summary>
	public PropertiesDescriptor<TDocument> DateNanos(string name, Action<DateNanosPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.DateNanosProperty(d));
	/// <summary>Adds a DateNanos field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> DateNanos(Expression<Func<TDocument, object>> field, Action<DateNanosPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.DateNanosProperty(d));

	/// <summary>Adds a Date field by name.</summary>
	public PropertiesDescriptor<TDocument> Date(string name, Action<DatePropertyDescriptor> configure) =>
		Add(name, configure, d => Property.DateProperty(d));
	/// <summary>Adds a Date field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Date(Expression<Func<TDocument, object>> field, Action<DatePropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.DateProperty(d));

	/// <summary>Adds a AggregateMetricDouble field by name.</summary>
	public PropertiesDescriptor<TDocument> AggregateMetricDouble(string name, Action<AggregateMetricDoublePropertyDescriptor> configure) =>
		Add(name, configure, d => Property.AggregateMetricDoubleProperty(d));
	/// <summary>Adds a AggregateMetricDouble field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> AggregateMetricDouble(Expression<Func<TDocument, object>> field, Action<AggregateMetricDoublePropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.AggregateMetricDoubleProperty(d));

	/// <summary>Adds a FlatObject field by name.</summary>
	public PropertiesDescriptor<TDocument> FlatObject(string name, Action<FlatObjectPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.FlatObjectProperty(d));
	/// <summary>Adds a FlatObject field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> FlatObject(Expression<Func<TDocument, object>> field, Action<FlatObjectPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.FlatObjectProperty(d));

	/// <summary>Adds a Nested field by name.</summary>
	public PropertiesDescriptor<TDocument> Nested(string name, Action<NestedPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.NestedProperty(d));
	/// <summary>Adds a Nested field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Nested(Expression<Func<TDocument, object>> field, Action<NestedPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.NestedProperty(d));

	/// <summary>Adds a Object field by name.</summary>
	public PropertiesDescriptor<TDocument> Object(string name, Action<ObjectPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.ObjectProperty(d));
	/// <summary>Adds a Object field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Object(Expression<Func<TDocument, object>> field, Action<ObjectPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.ObjectProperty(d));

	/// <summary>Adds a Completion field by name.</summary>
	public PropertiesDescriptor<TDocument> Completion(string name, Action<CompletionPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.CompletionProperty(d));
	/// <summary>Adds a Completion field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Completion(Expression<Func<TDocument, object>> field, Action<CompletionPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.CompletionProperty(d));

	/// <summary>Adds a ConstantKeyword field by name.</summary>
	public PropertiesDescriptor<TDocument> ConstantKeyword(string name, Action<ConstantKeywordPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.ConstantKeywordProperty(d));
	/// <summary>Adds a ConstantKeyword field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> ConstantKeyword(Expression<Func<TDocument, object>> field, Action<ConstantKeywordPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.ConstantKeywordProperty(d));

	/// <summary>Adds a FieldAlias field by name.</summary>
	public PropertiesDescriptor<TDocument> FieldAlias(string name, Action<FieldAliasPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.FieldAliasProperty(d));
	/// <summary>Adds a FieldAlias field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> FieldAlias(Expression<Func<TDocument, object>> field, Action<FieldAliasPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.FieldAliasProperty(d));

	/// <summary>Adds a Histogram field by name.</summary>
	public PropertiesDescriptor<TDocument> Histogram(string name, Action<HistogramPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.HistogramProperty(d));
	/// <summary>Adds a Histogram field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Histogram(Expression<Func<TDocument, object>> field, Action<HistogramPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.HistogramProperty(d));

	/// <summary>Adds a Ip field by name.</summary>
	public PropertiesDescriptor<TDocument> Ip(string name, Action<IpPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.IpProperty(d));
	/// <summary>Adds a Ip field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Ip(Expression<Func<TDocument, object>> field, Action<IpPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.IpProperty(d));

	/// <summary>Adds a Murmur3Hash field by name.</summary>
	public PropertiesDescriptor<TDocument> Murmur3Hash(string name, Action<Murmur3HashPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.Murmur3HashProperty(d));
	/// <summary>Adds a Murmur3Hash field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Murmur3Hash(Expression<Func<TDocument, object>> field, Action<Murmur3HashPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.Murmur3HashProperty(d));

	/// <summary>Adds a TokenCount field by name.</summary>
	public PropertiesDescriptor<TDocument> TokenCount(string name, Action<TokenCountPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.TokenCountProperty(d));
	/// <summary>Adds a TokenCount field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> TokenCount(Expression<Func<TDocument, object>> field, Action<TokenCountPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.TokenCountProperty(d));

	/// <summary>Adds a GeoPoint field by name.</summary>
	public PropertiesDescriptor<TDocument> GeoPoint(string name, Action<GeoPointPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.GeoPointProperty(d));
	/// <summary>Adds a GeoPoint field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> GeoPoint(Expression<Func<TDocument, object>> field, Action<GeoPointPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.GeoPointProperty(d));

	/// <summary>Adds a GeoShape field by name.</summary>
	public PropertiesDescriptor<TDocument> GeoShape(string name, Action<GeoShapePropertyDescriptor> configure) =>
		Add(name, configure, d => Property.GeoShapeProperty(d));
	/// <summary>Adds a GeoShape field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> GeoShape(Expression<Func<TDocument, object>> field, Action<GeoShapePropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.GeoShapeProperty(d));

	/// <summary>Adds a XyPoint field by name.</summary>
	public PropertiesDescriptor<TDocument> XyPoint(string name, Action<XyPointPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.XyPointProperty(d));
	/// <summary>Adds a XyPoint field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> XyPoint(Expression<Func<TDocument, object>> field, Action<XyPointPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.XyPointProperty(d));

	/// <summary>Adds a XyShape field by name.</summary>
	public PropertiesDescriptor<TDocument> XyShape(string name, Action<XyShapePropertyDescriptor> configure) =>
		Add(name, configure, d => Property.XyShapeProperty(d));
	/// <summary>Adds a XyShape field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> XyShape(Expression<Func<TDocument, object>> field, Action<XyShapePropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.XyShapeProperty(d));

	/// <summary>Adds a ByteNumber field by name.</summary>
	public PropertiesDescriptor<TDocument> ByteNumber(string name, Action<ByteNumberPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.ByteNumberProperty(d));
	/// <summary>Adds a ByteNumber field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> ByteNumber(Expression<Func<TDocument, object>> field, Action<ByteNumberPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.ByteNumberProperty(d));

	/// <summary>Adds a DoubleNumber field by name.</summary>
	public PropertiesDescriptor<TDocument> DoubleNumber(string name, Action<DoubleNumberPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.DoubleNumberProperty(d));
	/// <summary>Adds a DoubleNumber field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> DoubleNumber(Expression<Func<TDocument, object>> field, Action<DoubleNumberPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.DoubleNumberProperty(d));

	/// <summary>Adds a FloatNumber field by name.</summary>
	public PropertiesDescriptor<TDocument> FloatNumber(string name, Action<FloatNumberPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.FloatNumberProperty(d));
	/// <summary>Adds a FloatNumber field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> FloatNumber(Expression<Func<TDocument, object>> field, Action<FloatNumberPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.FloatNumberProperty(d));

	/// <summary>Adds a HalfFloatNumber field by name.</summary>
	public PropertiesDescriptor<TDocument> HalfFloatNumber(string name, Action<HalfFloatNumberPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.HalfFloatNumberProperty(d));
	/// <summary>Adds a HalfFloatNumber field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> HalfFloatNumber(Expression<Func<TDocument, object>> field, Action<HalfFloatNumberPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.HalfFloatNumberProperty(d));

	/// <summary>Adds a IntegerNumber field by name.</summary>
	public PropertiesDescriptor<TDocument> IntegerNumber(string name, Action<IntegerNumberPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.IntegerNumberProperty(d));
	/// <summary>Adds a IntegerNumber field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> IntegerNumber(Expression<Func<TDocument, object>> field, Action<IntegerNumberPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.IntegerNumberProperty(d));

	/// <summary>Adds a LongNumber field by name.</summary>
	public PropertiesDescriptor<TDocument> LongNumber(string name, Action<LongNumberPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.LongNumberProperty(d));
	/// <summary>Adds a LongNumber field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> LongNumber(Expression<Func<TDocument, object>> field, Action<LongNumberPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.LongNumberProperty(d));

	/// <summary>Adds a ScaledFloatNumber field by name.</summary>
	public PropertiesDescriptor<TDocument> ScaledFloatNumber(string name, Action<ScaledFloatNumberPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.ScaledFloatNumberProperty(d));
	/// <summary>Adds a ScaledFloatNumber field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> ScaledFloatNumber(Expression<Func<TDocument, object>> field, Action<ScaledFloatNumberPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.ScaledFloatNumberProperty(d));

	/// <summary>Adds a Semantic field by name.</summary>
	public PropertiesDescriptor<TDocument> Semantic(string name, Action<SemanticPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.SemanticProperty(d));
	/// <summary>Adds a Semantic field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> Semantic(Expression<Func<TDocument, object>> field, Action<SemanticPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.SemanticProperty(d));

	/// <summary>Adds a ShortNumber field by name.</summary>
	public PropertiesDescriptor<TDocument> ShortNumber(string name, Action<ShortNumberPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.ShortNumberProperty(d));
	/// <summary>Adds a ShortNumber field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> ShortNumber(Expression<Func<TDocument, object>> field, Action<ShortNumberPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.ShortNumberProperty(d));

	/// <summary>Adds a UnsignedLongNumber field by name.</summary>
	public PropertiesDescriptor<TDocument> UnsignedLongNumber(string name, Action<UnsignedLongNumberPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.UnsignedLongNumberProperty(d));
	/// <summary>Adds a UnsignedLongNumber field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> UnsignedLongNumber(Expression<Func<TDocument, object>> field, Action<UnsignedLongNumberPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.UnsignedLongNumberProperty(d));

	/// <summary>Adds a DateRange field by name.</summary>
	public PropertiesDescriptor<TDocument> DateRange(string name, Action<DateRangePropertyDescriptor> configure) =>
		Add(name, configure, d => Property.DateRangeProperty(d));
	/// <summary>Adds a DateRange field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> DateRange(Expression<Func<TDocument, object>> field, Action<DateRangePropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.DateRangeProperty(d));

	/// <summary>Adds a DoubleRange field by name.</summary>
	public PropertiesDescriptor<TDocument> DoubleRange(string name, Action<DoubleRangePropertyDescriptor> configure) =>
		Add(name, configure, d => Property.DoubleRangeProperty(d));
	/// <summary>Adds a DoubleRange field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> DoubleRange(Expression<Func<TDocument, object>> field, Action<DoubleRangePropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.DoubleRangeProperty(d));

	/// <summary>Adds a FloatRange field by name.</summary>
	public PropertiesDescriptor<TDocument> FloatRange(string name, Action<FloatRangePropertyDescriptor> configure) =>
		Add(name, configure, d => Property.FloatRangeProperty(d));
	/// <summary>Adds a FloatRange field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> FloatRange(Expression<Func<TDocument, object>> field, Action<FloatRangePropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.FloatRangeProperty(d));

	/// <summary>Adds a IntegerRange field by name.</summary>
	public PropertiesDescriptor<TDocument> IntegerRange(string name, Action<IntegerRangePropertyDescriptor> configure) =>
		Add(name, configure, d => Property.IntegerRangeProperty(d));
	/// <summary>Adds a IntegerRange field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> IntegerRange(Expression<Func<TDocument, object>> field, Action<IntegerRangePropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.IntegerRangeProperty(d));

	/// <summary>Adds a IpRange field by name.</summary>
	public PropertiesDescriptor<TDocument> IpRange(string name, Action<IpRangePropertyDescriptor> configure) =>
		Add(name, configure, d => Property.IpRangeProperty(d));
	/// <summary>Adds a IpRange field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> IpRange(Expression<Func<TDocument, object>> field, Action<IpRangePropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.IpRangeProperty(d));

	/// <summary>Adds a LongRange field by name.</summary>
	public PropertiesDescriptor<TDocument> LongRange(string name, Action<LongRangePropertyDescriptor> configure) =>
		Add(name, configure, d => Property.LongRangeProperty(d));
	/// <summary>Adds a LongRange field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> LongRange(Expression<Func<TDocument, object>> field, Action<LongRangePropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.LongRangeProperty(d));

	/// <summary>Adds a KnnVector field by name.</summary>
	public PropertiesDescriptor<TDocument> KnnVector(string name, Action<KnnVectorPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.KnnVectorProperty(d));
	/// <summary>Adds a KnnVector field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> KnnVector(Expression<Func<TDocument, object>> field, Action<KnnVectorPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.KnnVectorProperty(d));

	/// <summary>Adds a IcuCollationKeyword field by name.</summary>
	public PropertiesDescriptor<TDocument> IcuCollationKeyword(string name, Action<IcuCollationKeywordPropertyDescriptor> configure) =>
		Add(name, configure, d => Property.IcuCollationKeywordProperty(d));
	/// <summary>Adds a IcuCollationKeyword field selected by an expression.</summary>
	public PropertiesDescriptor<TDocument> IcuCollationKeyword(Expression<Func<TDocument, object>> field, Action<IcuCollationKeywordPropertyDescriptor> configure) =>
		Add(Field.ResolveName<TDocument>(field), configure, d => Property.IcuCollationKeywordProperty(d));
}
