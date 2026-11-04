using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// msearch and mget return 200 when items hit a red index; the failed items must still make the
/// transport fail over. Error items are as returned by OpenSearch 3.9.0 for a red index.
/// </summary>
public class MultiItemFailoverTests
{
	private const string HealthyMsearch =
		"""{"took":1,"responses":[{"took":1,"timed_out":false,"_shards":{"total":1,"successful":1,"skipped":0,"failed":0},"hits":{"total":{"value":0,"relation":"eq"},"max_score":null,"hits":[]},"status":200}]}""";

	private const string RedIndexMsearch =
		"""{"took":4,"responses":[{"took":4,"timed_out":false,"_shards":{"total":1,"successful":1,"skipped":0,"failed":0},"hits":{"total":{"value":1,"relation":"eq"},"max_score":1.0,"hits":[]},"status":200},{"error":{"root_cause":[],"type":"search_phase_execution_exception","reason":"all shards failed","phase":"query","grouped":true,"failed_shards":[]},"status":503}]}""";

	private const string BadQueryMsearch =
		"""{"took":1,"responses":[{"error":{"root_cause":[{"type":"parsing_exception","reason":"unknown query [nope]"}],"type":"parsing_exception","reason":"unknown query [nope]"},"status":400}]}""";

	private const string HealthyMget =
		"""{"docs":[{"_index":"green","_id":"1","_version":1,"_seq_no":0,"_primary_term":1,"found":true,"_source":{"t":"hello"}}]}""";

	private const string RedIndexMget =
		"""{"docs":[{"_index":"green","_id":"1","_version":1,"_seq_no":0,"_primary_term":1,"found":true,"_source":{"t":"hello"}},{"_index":"red","_id":"1","error":{"root_cause":[{"type":"no_shard_available_action_exception","reason":"No shard available for [org.opensearch.action.get.MultiGetShardRequest@4a85d4cf]"}],"type":"no_shard_available_action_exception","reason":"No shard available for [org.opensearch.action.get.MultiGetShardRequest@4a85d4cf]"}}]}""";

	private const string MissingIndexMget =
		"""{"docs":[{"_index":"nope","_id":"1","error":{"root_cause":[{"type":"index_not_found_exception","reason":"no such index [nope]","index":"nope"}],"type":"index_not_found_exception","reason":"no such index [nope]","index":"nope"}}]}""";

	[Fact]
	public void Msearch_ItemWith503_FailsOverToNextNode()
	{
		var handler = new SequenceHandler(RedIndexMsearch, HealthyMsearch);
		using var client = CreateClient(handler);

		var response = client.Core.Msearch(MsearchRequest());

		handler.Hosts.Should().HaveCount(2).And.OnlyHaveUniqueItems();
		response.Responses.Should().ContainSingle().Which.Status.Should().Be(200);
	}

	[Fact]
	public async Task MsearchAsync_ItemWith503_FailsOverToNextNode()
	{
		var handler = new SequenceHandler(RedIndexMsearch, HealthyMsearch);
		using var client = CreateClient(handler);

		var response = await client.Core.MsearchAsync(MsearchRequest());

		handler.Hosts.Should().HaveCount(2).And.OnlyHaveUniqueItems();
		response.Responses.Should().ContainSingle().Which.Status.Should().Be(200);
	}

	[Fact]
	public void Msearch_ItemWith400_DoesNotFailOver()
	{
		var handler = new SequenceHandler(BadQueryMsearch, HealthyMsearch);
		using var client = CreateClient(handler);

		var response = client.Core.Msearch(MsearchRequest());

		handler.Hosts.Should().HaveCount(1);
		response.Responses.Should().ContainSingle().Which.Status.Should().Be(400);
	}

	[Fact]
	public void Mget_NoShardAvailable_FailsOverToNextNode()
	{
		var handler = new SequenceHandler(RedIndexMget, HealthyMget);
		using var client = CreateClient(handler);

		var response = client.Core.Mget(new MgetRequest { Index = "green", Ids = ["1"] });

		handler.Hosts.Should().HaveCount(2).And.OnlyHaveUniqueItems();
		response.Docs.Should().ContainSingle().Which.Error.Should().BeNull();
	}

	[Fact]
	public void Mget_IndexNotFound_DoesNotFailOver()
	{
		var handler = new SequenceHandler(MissingIndexMget, HealthyMget);
		using var client = CreateClient(handler);

		var response = client.Core.Mget(new MgetRequest { Index = "nope", Ids = ["1"] });

		handler.Hosts.Should().HaveCount(1);
		response.Docs.Should().ContainSingle().Which.Error.Should().NotBeNull();
	}

	private static MsearchRequest MsearchRequest() => new()
	{
		Searches =
		[
			new MsearchItem
			{
				Header = new MsearchHeader { Index = "green" },
				Body = new MsearchBody { Query = JsonSerializer.SerializeToElement(new { match_all = new { } }) }
			}
		]
	};

	private static OpenSearchClient CreateClient(SequenceHandler handler) =>
		new(OpenSearchClientSettings
			.Create(new Uri("http://node1:9200"), new Uri("http://node2:9200"))
			.HttpMessageHandlerFactory(_ => handler)
			.Build());

	private sealed class SequenceHandler(params string[] bodies) : HttpMessageHandler
	{
		private int _calls;

		public List<string> Hosts { get; } = [];

		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken ct)
		{
			Hosts.Add(request.RequestUri!.Host);
			var body = bodies[Math.Min(_calls++, bodies.Length - 1)];
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(body, Encoding.UTF8, "application/json")
			};
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
			Task.FromResult(Send(request, ct));
	}
}
