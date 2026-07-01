using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Wire-format fixtures for field mappings — the <see cref="Property"/> union and <see cref="TypeMapping"/>.
/// Unlike the query/aggregation unions (externally tagged: <c>{ "&lt;kind&gt;": { ... } }</c>), <c>Property</c>
/// is <em>internally</em> tagged: the discriminator is a <c>"type"</c> member inside the object
/// (<c>{ "type": "text", "analyzer": ... }</c>). These fixtures validate that embedded-discriminator
/// shape per property kind, the read path (deserialize back), nested-property and multi-field recursion,
/// and a full <see cref="TypeMapping"/> — all through the production serializer.
/// </summary>
public class MappingSerializationTests : SerializationTestBase
{
	/// <summary>
	/// Round-trips a single <see cref="Property"/> and asserts it serialized as an internally-tagged object
	/// with exactly one <c>type</c> member equal to <paramref name="expectedType"/>. Returns the parsed object.
	/// </summary>
	private static JsonElement PropertyBody(Property property, string expectedType)
	{
		var root = AssertRoundTrips(property);
		root.ValueKind.Should().Be(JsonValueKind.Object);
		root.GetProperty("type").GetString().Should().Be(expectedType);
		// The discriminator must appear exactly once — the internally-tagged converter writes it from the
		// union Kind and must skip the value type's own `type` property, not emit both.
		root.EnumerateObject().Count(p => p.Name == "type").Should().Be(1, "the `type` discriminator must not be double-written");
		return root;
	}

	[Fact]
	public void Text_property_serializes_internally_tagged_and_round_trips()
	{
		var body = PropertyBody(Property.TextProperty(new TextProperty
		{
			Analyzer = "standard",
			Boost = 1.5,
			Index = true,
			IndexOptions = IndexOptions.Offsets,
		}), "text");

		body.GetProperty("analyzer").GetString().Should().Be("standard");
		body.GetProperty("boost").GetDouble().Should().Be(1.5);
		body.GetProperty("index").GetBoolean().Should().BeTrue();
		body.GetProperty("index_options").GetString().Should().Be("offsets");
	}

	[Fact]
	public void Keyword_property_serializes_and_round_trips()
	{
		var body = PropertyBody(Property.KeywordProperty(new KeywordProperty
		{
			IgnoreAbove = 256,
			Normalizer = "lowercase",
			DocValues = true,
		}), "keyword");

		body.GetProperty("ignore_above").GetInt32().Should().Be(256);
		body.GetProperty("normalizer").GetString().Should().Be("lowercase");
		body.GetProperty("doc_values").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void Date_property_serializes_and_round_trips()
	{
		var body = PropertyBody(Property.DateProperty(new DateProperty
		{
			Format = "yyyy-MM-dd",
			IgnoreMalformed = true,
		}), "date");

		body.GetProperty("format").GetString().Should().Be("yyyy-MM-dd");
		body.GetProperty("ignore_malformed").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void Integer_property_serializes_and_round_trips()
	{
		var body = PropertyBody(Property.IntegerNumberProperty(new IntegerNumberProperty
		{
			NullValue = 0,
			Coerce = false,
		}), "integer");

		body.GetProperty("null_value").GetInt32().Should().Be(0);
		body.GetProperty("coerce").GetBoolean().Should().BeFalse();
	}

	[Fact]
	public void ScaledFloat_property_serializes_and_round_trips()
	{
		var body = PropertyBody(Property.ScaledFloatNumberProperty(new ScaledFloatNumberProperty
		{
			ScalingFactor = 100.0,
		}), "scaled_float");

		body.GetProperty("scaling_factor").GetDouble().Should().Be(100.0);
	}

	[Fact]
	public void Boolean_property_serializes_and_round_trips()
	{
		var body = PropertyBody(Property.BooleanProperty(new BooleanProperty { NullValue = false }), "boolean");
		body.GetProperty("null_value").GetBoolean().Should().BeFalse();
	}

	[Fact]
	public void Ip_property_serializes_and_round_trips()
	{
		var body = PropertyBody(Property.IpProperty(new IpProperty { NullValue = "127.0.0.1" }), "ip");
		body.GetProperty("null_value").GetString().Should().Be("127.0.0.1");
	}

	[Fact]
	public void GeoPoint_property_serializes_typed_null_value_and_round_trips()
	{
		var body = PropertyBody(Property.GeoPointProperty(new GeoPointProperty
		{
			NullValue = GeoLocation.FromLatLon(0.0, 0.0),
			IgnoreMalformed = true,
		}), "geo_point");

		body.GetProperty("null_value").GetProperty("lat").GetDouble().Should().Be(0.0);
		body.GetProperty("ignore_malformed").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void Completion_property_serializes_and_round_trips()
	{
		var body = PropertyBody(Property.CompletionProperty(new CompletionProperty
		{
			Analyzer = "simple",
			MaxInputLength = 50,
			PreserveSeparators = true,
		}), "completion");

		body.GetProperty("analyzer").GetString().Should().Be("simple");
		body.GetProperty("max_input_length").GetInt32().Should().Be(50);
	}

	[Fact]
	public void KnnVector_property_serializes_and_round_trips()
	{
		var body = PropertyBody(Property.KnnVectorProperty(new KnnVectorProperty
		{
			Dimension = 768,
			SpaceType = "l2",
			DataType = "float",
		}), "knn_vector");

		body.GetProperty("dimension").GetInt32().Should().Be(768);
		body.GetProperty("space_type").GetString().Should().Be("l2");
	}

	[Fact]
	public void Object_property_with_nested_properties_serializes_and_round_trips()
	{
		// object property recursing into sub-properties, each itself internally tagged.
		var body = PropertyBody(Property.ObjectProperty(new ObjectProperty
		{
			Properties = new Dictionary<string, Property>
			{
				["name"] = Property.TextProperty(new TextProperty { Analyzer = "standard" }),
				["age"] = Property.IntegerNumberProperty(new IntegerNumberProperty()),
			},
		}), "object");

		var props = body.GetProperty("properties");
		props.GetProperty("name").GetProperty("type").GetString().Should().Be("text");
		props.GetProperty("name").GetProperty("analyzer").GetString().Should().Be("standard");
		props.GetProperty("age").GetProperty("type").GetString().Should().Be("integer");
	}

	[Fact]
	public void Nested_property_with_nested_properties_serializes_and_round_trips()
	{
		var body = PropertyBody(Property.NestedProperty(new NestedProperty
		{
			IncludeInParent = true,
			Properties = new Dictionary<string, Property>
			{
				["value"] = Property.DoubleNumberProperty(new DoubleNumberProperty()),
			},
		}), "nested");

		body.GetProperty("include_in_parent").GetBoolean().Should().BeTrue();
		body.GetProperty("properties").GetProperty("value").GetProperty("type").GetString().Should().Be("double");
	}

	[Fact]
	public void Text_property_with_multi_fields_serializes_and_round_trips()
	{
		// The classic text-with-keyword-subfield multi-field, recursing via `fields`.
		var body = PropertyBody(Property.TextProperty(new TextProperty
		{
			Analyzer = "standard",
			Fields = new Dictionary<string, Property>
			{
				["raw"] = Property.KeywordProperty(new KeywordProperty { IgnoreAbove = 256 }),
			},
		}), "text");

		var raw = body.GetProperty("fields").GetProperty("raw");
		raw.GetProperty("type").GetString().Should().Be("keyword");
		raw.GetProperty("ignore_above").GetInt32().Should().Be(256);
	}

	[Fact]
	public void Property_reads_back_from_internally_tagged_json()
	{
		// Explicit read-path check: deserialize a mapping property from its wire JSON and confirm the
		// converter dispatches on the embedded `type` and materializes the correct variant.
		const string json = """{"type":"keyword","ignore_above":128,"doc_values":false}""";

		var property = Deserialize<Property>(json);

		property.Should().NotBeNull();
		property!.Kind.Should().Be(PropertyKind.KeywordProperty);
		var keyword = property.Value.Should().BeOfType<KeywordProperty>().Subject;
		keyword.IgnoreAbove.Should().Be(128);
		keyword.DocValues.Should().BeFalse();
	}

	[Fact]
	public void TypeMapping_with_properties_dictionary_serializes_and_round_trips()
	{
		var mapping = new TypeMapping
		{
			Dynamic = "strict",
			Properties = new Dictionary<string, Property>
			{
				["title"] = Property.TextProperty(new TextProperty
				{
					Analyzer = "standard",
					Fields = new Dictionary<string, Property> { ["raw"] = Property.KeywordProperty(new KeywordProperty()) },
				}),
				["created"] = Property.DateProperty(new DateProperty { Format = "epoch_millis" }),
				["tags"] = Property.KeywordProperty(new KeywordProperty()),
			},
		};

		var root = AssertRoundTrips(mapping);
		root.GetProperty("dynamic").GetString().Should().Be("strict");

		var props = root.GetProperty("properties");
		props.GetProperty("title").GetProperty("type").GetString().Should().Be("text");
		props.GetProperty("title").GetProperty("fields").GetProperty("raw").GetProperty("type").GetString().Should().Be("keyword");
		props.GetProperty("created").GetProperty("type").GetString().Should().Be("date");
		props.GetProperty("created").GetProperty("format").GetString().Should().Be("epoch_millis");
		props.GetProperty("tags").GetProperty("type").GetString().Should().Be("keyword");
	}
}
