using System.Text.Json;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.IntegrationTests.Infrastructure;
using OpenSearch.Net;

namespace OpenSearch.IntegrationTests.Core;

/// <summary>
/// msearch and mget answer 200 when items hit a red index. With one node and one retry, a retry
/// shows that the transport recognised the failed items and treated them like a 503.
/// </summary>
public class RedIndexFailoverTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void Msearch_OnRedIndex_MarksNodeDeadAndRetries()
	{
		var red = CreateRedIndex();

		using var client = RetryingClient();
		var response = client.Core.Msearch(Msearch(red));

		response.Responses.Should().ContainSingle().Which.Status.Should().Be(503);
		RequestsSent(response).Should().Be(2);
		response.ApiCall!.AuditTrail!.Events.Should().Contain(e => e.Type == AuditEventType.DeadNode);
	}

	[SkipIfNoCluster]
	public void Mget_OnRedIndex_MarksNodeDeadAndRetries()
	{
		var red = CreateRedIndex();

		using var client = RetryingClient();
		var response = client.Core.Mget(new MgetRequest { Index = red, Ids = ["1"] });

		response.Docs.Should().ContainSingle().Which.Error.Should().NotBeNull();
		RequestsSent(response).Should().Be(2);
	}

	[SkipIfNoCluster]
	public void Msearch_OnGreenIndex_DoesNotRetry()
	{
		var green = UniqueIndex();
		Client.Indices.Create(new OpenSearch.Client.CreateIndexRequest { Index = green });

		using var client = RetryingClient();
		var response = client.Core.Msearch(Msearch(green));

		response.Responses.Should().ContainSingle().Which.Status.Should().Be(200);
		RequestsSent(response).Should().Be(1);
	}

	[SkipIfNoCluster]
	public void Mget_OnMissingIndex_DoesNotRetry()
	{
		using var client = RetryingClient();
		var response = client.Core.Mget(new MgetRequest { Index = UniqueIndex(), Ids = ["1"] });

		response.Docs.Should().ContainSingle().Which.Error.Should().NotBeNull();
		RequestsSent(response).Should().Be(1);
	}

	private static OpenSearchClient RetryingClient() =>
		new(OpenSearchClientSettings.Create(OpenSearchCluster.Url).MaxRetries(1).Build());

	private string CreateRedIndex()
	{
		var index = UniqueIndex();
		Client.Indices.Create(new OpenSearch.Client.CreateIndexRequest
		{
			Index = index,
			WaitForActiveShards = "0",
			Settings = new IndexSettings
			{
				NumberOfShards = "1",
				NumberOfReplicas = "0",
				Routing = new IndexRouting { Allocation = new IndexRoutingAllocation { Enable = "none" } },
			},
		});
		return index;
	}

	private static MsearchRequest Msearch(string index) => new()
	{
		Searches =
		[
			new MsearchItem
			{
				Header = new MsearchHeader { Index = index },
				Body = new MsearchBody { Query = JsonSerializer.SerializeToElement(new { match_all = new { } }) }
			}
		]
	};

	private static int RequestsSent(OpenSearchResponse response) =>
		response.ApiCall!.AuditTrail!.Events.Count(e => e.Type == AuditEventType.RequestSent);
}
