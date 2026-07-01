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
			var request = new PutPolicyIsmRequest
			{
				PolicyId = policyId,
				Policy = new Policy
				{
					Description = "Integration test ISM policy",
					DefaultState = "hot",
					States = [new States { Name = "hot", Actions = [], Transitions = [] }],
				},
			};

			// The first ISM write after cluster start can race the plugin's own config-index
			// bootstrap (both TFMs' test runs share one cluster), so retry briefly before failing.
			var put = Client.Ism.PutPolicy(request);
			for (var attempt = 0; !put.IsValid && attempt < 10; attempt++)
			{
				Thread.Sleep(500);
				put = Client.Ism.PutPolicy(request);
			}

			put.IsValid.Should().BeTrue($"put_policy should succeed: {put.DebugInformation}");
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
