using System.Text.Json;
using FluentAssertions;
using OpenSearch.Client.Common;
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
			var putResponse = Client.Ingest.PutPipeline(new PutPipelineIngestRequest
			{
				Id = pipelineId,
				Description = "Test pipeline for integration tests",
				Processors =
				[
					ProcessorContainer.Set(new SetProcessor
					{
						Field = "test_field",
						Value = JsonSerializer.SerializeToElement("test_value")
					})
				]
			});

			putResponse.Acknowledged.Should().BeTrue();

			// Get the pipeline
			var getResponse = Client.Ingest.GetPipeline(new GetPipelineIngestRequest { Id = pipelineId });

			getResponse.Should().ContainKey(pipelineId);
			getResponse[pipelineId].Description.Should().Be("Test pipeline for integration tests");

			// Delete the pipeline
			var deleteResponse = Client.Ingest.DeletePipeline(new DeletePipelineIngestRequest { Id = pipelineId });

			deleteResponse.Acknowledged.Should().BeTrue();
		}
		catch
		{
			// Best effort cleanup
			try { Client.Ingest.DeletePipeline(new DeletePipelineIngestRequest { Id = pipelineId }); } catch { }
			throw;
		}
	}
}
