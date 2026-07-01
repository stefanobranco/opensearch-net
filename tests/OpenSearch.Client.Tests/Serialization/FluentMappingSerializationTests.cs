using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// End-to-end fixtures for the fluent mapping API as users write it:
/// <c>new CreateIndexRequestDescriptor("i").Mappings(m =&gt; m.Properties(p =&gt; p.Text("title", t =&gt; ...)))</c>.
/// Exercises <see cref="PropertiesDescriptor{TDocument}"/> (name- and expression-based field declaration),
/// the property descriptors, and the descriptor→<see cref="Property"/> casts — through the production serializer.
/// </summary>
public class FluentMappingSerializationTests : SerializationTestBase
{
	private sealed class Doc
	{
		public string? Title { get; set; }
		public int Age { get; set; }
		public System.DateTime CreatedAt { get; set; }
		public Meta? Meta { get; set; }
	}

	private sealed class Meta
	{
		public string? Author { get; set; }
	}

	/// <summary>Builds a TypeMapping via the fluent descriptor and returns its serialized <c>properties</c>.</summary>
	private static JsonElement Properties<TDocument>(System.Action<PropertiesDescriptor<TDocument>> build)
	{
		var descriptor = new TypeMappingDescriptor().Properties(build);
		TypeMapping mapping = descriptor;
		return Parse(Serialize(mapping)).GetProperty("properties");
	}

	[Fact]
	public void Text_by_name_serializes_internally_tagged()
	{
		var props = Properties<object>(p => p.Text("title", t => t.Analyzer("standard").Index(true)));

		var title = props.GetProperty("title");
		title.GetProperty("type").GetString().Should().Be("text");
		title.GetProperty("analyzer").GetString().Should().Be("standard");
		title.GetProperty("index").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void Multiple_property_types_by_name_serialize()
	{
		var props = Properties<object>(p => p
			.Text("title", t => t.Analyzer("standard"))
			.Keyword("status", k => k.IgnoreAbove(64))
			.IntegerNumber("age", i => i.NullValue(0))
			.Date("created", d => d.Format("epoch_millis"))
			.Boolean("active", b => b.NullValue(true)));

		props.GetProperty("title").GetProperty("type").GetString().Should().Be("text");
		props.GetProperty("status").GetProperty("type").GetString().Should().Be("keyword");
		props.GetProperty("status").GetProperty("ignore_above").GetInt32().Should().Be(64);
		props.GetProperty("age").GetProperty("type").GetString().Should().Be("integer");
		props.GetProperty("created").GetProperty("type").GetString().Should().Be("date");
		props.GetProperty("active").GetProperty("type").GetString().Should().Be("boolean");
	}

	[Fact]
	public void Field_expression_names_fields_snake_cased()
	{
		var props = Properties<Doc>(p => p
			.Text(d => d.Title!, t => t.Analyzer("standard"))
			.IntegerNumber(d => d.Age, i => i.Index(true))
			.Date(d => d.CreatedAt, _ => { }));

		props.GetProperty("title").GetProperty("type").GetString().Should().Be("text");
		props.GetProperty("age").GetProperty("type").GetString().Should().Be("integer");
		// Field.ResolveName snake-cases the member: CreatedAt → created_at.
		props.GetProperty("created_at").GetProperty("type").GetString().Should().Be("date");
	}

	[Fact]
	public void Nested_member_expression_resolves_dotted_path()
	{
		var props = Properties<Doc>(p => p.Keyword(d => d.Meta!.Author!, k => k.IgnoreAbove(256)));

		// Meta.Author → "meta.author".
		props.GetProperty("meta.author").GetProperty("type").GetString().Should().Be("keyword");
	}

	[Fact]
	public void Text_with_keyword_multi_field_serializes()
	{
		// The classic text field with a keyword sub-field, built fluently via the nested descriptor's Fields.
		var props = Properties<Doc>(p => p.Text(d => d.Title!, t => t
			.Analyzer("standard")
			.Fields(new Dictionary<string, Property> { ["raw"] = Property.KeywordProperty(new KeywordProperty { IgnoreAbove = 256 }) })));

		var title = props.GetProperty("title");
		title.GetProperty("type").GetString().Should().Be("text");
		var raw = title.GetProperty("fields").GetProperty("raw");
		raw.GetProperty("type").GetString().Should().Be("keyword");
		raw.GetProperty("ignore_above").GetInt32().Should().Be(256);
	}

	[Fact]
	public void Object_property_with_nested_fluent_properties_serializes()
	{
		var props = Properties<object>(p => p.Object("author", o => o
			.Properties(new Dictionary<string, Property>
			{
				["name"] = Property.TextProperty(new TextProperty()),
				["id"] = Property.KeywordProperty(new KeywordProperty()),
			})));

		var author = props.GetProperty("author");
		author.GetProperty("type").GetString().Should().Be("object");
		author.GetProperty("properties").GetProperty("name").GetProperty("type").GetString().Should().Be("text");
		author.GetProperty("properties").GetProperty("id").GetProperty("type").GetString().Should().Be("keyword");
	}

	[Fact]
	public void GeoPoint_and_knn_vector_fluent_serialize()
	{
		var props = Properties<object>(p => p
			.GeoPoint("location", g => g.IgnoreMalformed(true))
			.KnnVector("embedding", k => k.Dimension(768).SpaceType("l2")));

		props.GetProperty("location").GetProperty("type").GetString().Should().Be("geo_point");
		props.GetProperty("embedding").GetProperty("type").GetString().Should().Be("knn_vector");
		props.GetProperty("embedding").GetProperty("dimension").GetInt32().Should().Be(768);
	}

	[Fact]
	public void Mappings_on_create_index_request_serialize_end_to_end()
	{
		// Full path: CreateIndexRequest → mappings → properties, as a user configures an index.
		CreateIndexRequest request = new CreateIndexRequestDescriptor()
			.Index("products")
			.Mappings(m => m
				.Dynamic("strict")
				.Properties<Doc>(p => p
					.Text(d => d.Title!, t => t.Analyzer("standard"))
					.IntegerNumber(d => d.Age, _ => { })));

		var root = Parse(Serialize(request));
		var mappings = root.GetProperty("mappings");
		mappings.GetProperty("dynamic").GetString().Should().Be("strict");
		mappings.GetProperty("properties").GetProperty("title").GetProperty("type").GetString().Should().Be("text");
		mappings.GetProperty("properties").GetProperty("age").GetProperty("type").GetString().Should().Be("integer");
	}
}
