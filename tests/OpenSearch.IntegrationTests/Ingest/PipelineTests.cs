using System.Text.Json;
using FluentAssertions;
using OpenSearch.Client.Ingest;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Ingest;

public class PipelineTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void CreateGetDeletePipelineLifecycle()
	{
		var pipelineId = $"test-pipeline-{Guid.NewGuid():N}";

		try
		{
			// Create pipeline with a "set" processor
			var putResponse = Client.Ingest.PutPipeline(new PutPipelineRequest
			{
				Id = pipelineId,
				Description = "Test pipeline for integration tests",
				Processors =
				[
					new ProcessorContainer
					{
						Set = new SetProcessor
						{
							Field = "test_field",
							Value = JsonSerializer.SerializeToElement("test_value")
						}
					}
				]
			});

			putResponse.Acknowledged.Should().BeTrue();

			// Get the pipeline
			var getResponse = Client.Ingest.GetPipeline(new GetPipelineRequest { Id = pipelineId });

			getResponse.Should().ContainKey(pipelineId);
			getResponse[pipelineId].Description.Should().Be("Test pipeline for integration tests");

			// Delete the pipeline
			var deleteResponse = Client.Ingest.DeletePipeline(new DeletePipelineRequest { Id = pipelineId });

			deleteResponse.Acknowledged.Should().BeTrue();
		}
		catch
		{
			// Best effort cleanup
			try { Client.Ingest.DeletePipeline(new DeletePipelineRequest { Id = pipelineId }); } catch { }
			throw;
		}
	}
}
