using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Response-deserialization fixtures for the ingest namespace — proves the client parses a real
/// <c>_ingest/pipeline</c> payload into the dictionary-shaped <see cref="GetPipelineIngestResponse"/>,
/// including the externally-tagged <see cref="ProcessorContainer"/> union on the read path.
/// </summary>
public class IngestResponseSerializationTests : SerializationTestBase
{
	[Fact]
	public void Get_pipeline_response_deserializes_pipeline_with_processors()
	{
		const string json = """
		{
		  "my-pipeline": {
		    "description": "grok then set",
		    "version": 2,
		    "processors": [
		      { "set": { "field": "environment", "value": "production" } },
		      { "lowercase": { "field": "message" } }
		    ]
		  }
		}
		""";

		var response = Deserialize<GetPipelineIngestResponse>(json);

		response!.Should().ContainKey("my-pipeline");
		var pipeline = response["my-pipeline"];
		pipeline.Description.Should().Be("grok then set");
		pipeline.Version.Should().Be(2);

		pipeline.Processors.Should().HaveCount(2);
		pipeline.Processors![0].Kind.Should().Be(ProcessorKind.Set);
		pipeline.Processors![1].Kind.Should().Be(ProcessorKind.Lowercase);
	}
}
