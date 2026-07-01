using FluentAssertions;
using OpenSearch.IntegrationTests.Infrastructure;
using OpenSearch.Client;

namespace OpenSearch.IntegrationTests.SearchPipeline;

public class SearchPipelineTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void PutGetDeletePipelineLifecycle()
	{
		var pipelineId = $"test-search-pipeline-{Guid.NewGuid():N}";

		try
		{
			var putResponse = Client.SearchPipeline.Put(new PutSearchPipelineRequest
			{
				Id = pipelineId,
				Description = "Integration test search pipeline",
				RequestProcessors =
				[
					RequestProcessor.FilterQuery(new FilterQueryRequestProcessor
					{
						Query = QueryContainer.MatchAll(new MatchAllQuery()),
					}),
				],
			});

			putResponse.Acknowledged.Should().BeTrue();

			var getResponse = Client.SearchPipeline.Get(new GetSearchPipelineRequest { Id = pipelineId });

			getResponse.Should().ContainKey(pipelineId);

			var deleteResponse = Client.SearchPipeline.Delete(new DeleteSearchPipelineRequest { Id = pipelineId });

			deleteResponse.Acknowledged.Should().BeTrue();
		}
		catch
		{
			try { Client.SearchPipeline.Delete(new DeleteSearchPipelineRequest { Id = pipelineId }); } catch { }
			throw;
		}
	}
}
