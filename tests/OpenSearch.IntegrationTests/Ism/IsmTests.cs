using FluentAssertions;
using OpenSearch.IntegrationTests.Infrastructure;
using OpenSearch.Client;

namespace OpenSearch.IntegrationTests.Ism;

public class IsmTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void PolicyLifecycle()
	{
		var policyId = $"test-policy-{Guid.NewGuid():N}";

		try
		{
			var put = Client.Ism.PutPolicy(new PutPolicyIsmRequest
			{
				PolicyId = policyId,
				Policy = new Policy
				{
					Description = "Integration test ISM policy",
					DefaultState = "hot",
					States = [new States { Name = "hot", Actions = [], Transitions = [] }],
				},
			});
			put.Id.Should().Be(policyId);

			var get = Client.Ism.GetPolicy(new GetPolicyIsmRequest { PolicyId = policyId });
			get.Policy!.DefaultState.Should().Be("hot");
		}
		finally
		{
			try { Client.Ism.DeletePolicy(new DeletePolicyIsmRequest { PolicyId = policyId }); } catch { }
		}
	}
}
