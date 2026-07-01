using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Fixtures for the search_pipeline processor unions. <c>RequestProcessor</c>/<c>ResponseProcessor</c>/
/// <c>PhaseResultsProcessor</c> are externally-tagged unions (<c>{ "&lt;processor&gt;": { ... } }</c>) — the
/// same wire shape as <see cref="QueryContainer"/> — that the generator previously emitted as
/// <c>JsonElement</c>. These prove the typed unions round-trip through the production serializer.
/// </summary>
public class SearchPipelineSerializationTests : SerializationTestBase
{
	[Fact]
	public void RequestProcessor_serializes_externally_tagged_by_wrapper_name()
	{
		RequestProcessor processor = RequestProcessor.Oversample(new OversampleRequestProcessor
		{
			SampleFactor = 2.5f,
			ContentPrefix = "doc",
		});

		var root = AssertRoundTrips(processor);

		root.ValueKind.Should().Be(JsonValueKind.Object);
		var oversample = root.GetProperty("oversample");
		oversample.GetProperty("sample_factor").GetSingle().Should().Be(2.5f);
		oversample.GetProperty("content_prefix").GetString().Should().Be("doc");
		root.EnumerateObject().Count().Should().Be(1, "an externally-tagged union writes exactly one wrapper key");
	}

	[Fact]
	public void ResponseProcessor_serializes_externally_tagged_by_wrapper_name()
	{
		ResponseProcessor processor = ResponseProcessor.RenameField(new RenameFieldResponseProcessor
		{
			Field = "title",
			TargetField = "name",
		});

		var root = AssertRoundTrips(processor);

		var rename = root.GetProperty("rename_field");
		rename.GetProperty("field").GetString().Should().Be("title");
		rename.GetProperty("target_field").GetString().Should().Be("name");
	}

	[Fact]
	public void RequestProcessor_reads_back_to_the_right_kind()
	{
		var processor = Deserialize<RequestProcessor>("""{"oversample":{"sample_factor":3}}""");

		processor!.Kind.Should().Be(RequestProcessorKind.Oversample);
	}

	[Fact]
	public void PutSearchPipeline_request_serializes_processor_lists()
	{
		PutSearchPipelineRequest request = new()
		{
			Id = "my_pipeline", // [JsonIgnore] path param
			Description = "hybrid search pipeline",
			RequestProcessors = [RequestProcessor.Oversample(new OversampleRequestProcessor { SampleFactor = 2f })],
			ResponseProcessors = [ResponseProcessor.RenameField(new RenameFieldResponseProcessor { Field = "a", TargetField = "b" })],
		};

		var root = Parse(Serialize(request));

		root.TryGetProperty("id", out _).Should().BeFalse("id is a URL param, not body");
		root.GetProperty("description").GetString().Should().Be("hybrid search pipeline");

		var requestProcessors = root.GetProperty("request_processors");
		requestProcessors.GetArrayLength().Should().Be(1);
		requestProcessors[0].GetProperty("oversample").GetProperty("sample_factor").GetSingle().Should().Be(2f);

		var responseProcessors = root.GetProperty("response_processors");
		responseProcessors[0].GetProperty("rename_field").GetProperty("target_field").GetString().Should().Be("b");
	}
}
