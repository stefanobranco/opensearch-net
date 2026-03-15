using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class HighlightDescriptorTests
{
	private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

	private static JsonSerializerOptions CreateOptions()
	{
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			NumberHandling = JsonNumberHandling.AllowReadingFromString,
		};
		options.Converters.Add(new JsonEnumConverterFactory());
		return options;
	}

	[Fact]
	public void Fluent_Fields_Builder()
	{
		SearchRequest request = new SearchRequestDescriptor()
			.Highlight(h => h
				.PreTags(["<em>"]).PostTags(["</em>"])
				.Fields(
					f => f.Field("title").FragmentSize(150).NumberOfFragments(3),
					f => f.Field("body").Type("fvh").NumberOfFragments(0)
				)
			);

		request.Highlight.Should().NotBeNull();
		request.Highlight!.PreTags.Should().BeEquivalentTo(["<em>"]);
		request.Highlight.PostTags.Should().BeEquivalentTo(["</em>"]);
		request.Highlight.Fields.Should().HaveCount(2);
		request.Highlight.Fields.Should().ContainKey("title");
		request.Highlight.Fields.Should().ContainKey("body");
		request.Highlight.Fields!["title"].FragmentSize.Should().Be(150);
		request.Highlight.Fields["title"].NumberOfFragments.Should().Be(3);
		request.Highlight.Fields["body"].Type.Should().Be("fvh");
		request.Highlight.Fields["body"].NumberOfFragments.Should().Be(0);
	}

	[Fact]
	public void Serialization_Produces_Correct_Json()
	{
		SearchRequest request = new SearchRequestDescriptor()
			.Highlight(h => h
				.PreTags(["<em>"]).PostTags(["</em>"])
				.Fields(
					f => f.Field("title").FragmentSize(150),
					f => f.Field("body").Type("fvh")
				)
			);

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var highlight = doc.RootElement.GetProperty("highlight");
		highlight.GetProperty("pre_tags").GetArrayLength().Should().Be(1);
		highlight.GetProperty("post_tags").GetArrayLength().Should().Be(1);

		var fields = highlight.GetProperty("fields");
		fields.TryGetProperty("title", out var title).Should().BeTrue();
		title.GetProperty("fragment_size").GetInt32().Should().Be(150);

		fields.TryGetProperty("body", out var body).Should().BeTrue();
		body.GetProperty("type").GetString().Should().Be("fvh");
	}

	[Fact]
	public void Field_Without_Name_Throws()
	{
		var act = () =>
		{
			SearchRequest _ = new SearchRequestDescriptor()
				.Highlight(h => h
					.Fields(f => f.FragmentSize(100))
				);
		};

		act.Should().Throw<InvalidOperationException>()
			.Which.Message.Should().Contain("Field(name)");
	}

	[Fact]
	public void Single_Field()
	{
		SearchRequest request = new SearchRequestDescriptor()
			.Highlight(h => h
				.Fields(f => f.Field("title"))
			);

		request.Highlight!.Fields.Should().HaveCount(1);
		request.Highlight.Fields.Should().ContainKey("title");
	}

	[Fact]
	public void Field_With_MatchedFields()
	{
		SearchRequest request = new SearchRequestDescriptor()
			.Highlight(h => h
				.Fields(
					f => f.Field("content")
						.MatchedFields(["content", "content.plain"])
						.Type("fvh")
				)
			);

		var contentField = request.Highlight!.Fields!["content"];
		contentField.MatchedFields.Should().BeEquivalentTo(["content", "content.plain"]);
		contentField.Type.Should().Be("fvh");
	}
}
