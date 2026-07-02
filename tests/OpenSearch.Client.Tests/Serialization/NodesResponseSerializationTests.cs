using System.Text;
using FluentAssertions;
using OpenSearch.Net;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Response-deserialization fixtures for the per-node dictionaries recovered from the
/// <c>_nodes</c>/<c>nodes</c> PascalCase collision (nodes.info, nodes.stats, cluster.stats,
/// knn.stats), the freeform cluster.state body, and the typed get_source document.
/// </summary>
public class NodesResponseSerializationTests : SerializationTestBase
{
	[Fact]
	public void Nodes_info_response_deserializes_per_node_details()
	{
		const string json = """
		{
		  "_nodes": { "total": 1, "successful": 1, "failed": 0 },
		  "cluster_name": "opensearch-cluster",
		  "nodes": {
		    "node-id-1": {
		      "name": "node-1",
		      "transport_address": "127.0.0.1:9300",
		      "host": "127.0.0.1",
		      "ip": "127.0.0.1",
		      "version": "3.4.0",
		      "build_type": "tar",
		      "build_hash": "abc123",
		      "roles": ["cluster_manager", "data", "ingest"],
		      "os": { "name": "Linux", "arch": "aarch64", "available_processors": 8, "allocated_processors": 8 },
		      "jvm": { "pid": 1, "version": "21.0.1", "vm_name": "OpenJDK 64-Bit Server VM" }
		    }
		  }
		}
		""";

		var response = Deserialize<InfoNodeResponse>(json);

		response!.ClusterName.Should().Be("opensearch-cluster");
		response.NodeStatistics!.Total.Should().Be(1);
		response.Nodes.Should().ContainKey("node-id-1");

		var node = response.Nodes!["node-id-1"];
		node.Name.Should().Be("node-1");
		node.Version.Should().Be("3.4.0");
		node.Roles.Should().Contain("data");
		node.Os!.AvailableProcessors.Should().Be(8);
		node.Jvm!.Version.Should().Be("21.0.1");
	}

	[Fact]
	public void Nodes_stats_response_deserializes_per_node_details()
	{
		const string json = """
		{
		  "_nodes": { "total": 1, "successful": 1, "failed": 0 },
		  "cluster_name": "opensearch-cluster",
		  "nodes": {
		    "node-id-1": {
		      "name": "node-1",
		      "timestamp": 1719900000000,
		      "indices": {
		        "docs": { "count": 1234, "deleted": 5 },
		        "store": { "size_in_bytes": 987654 }
		      },
		      "jvm": {
		        "uptime_in_millis": 3600000,
		        "mem": { "heap_used_in_bytes": 268435456, "heap_used_percent": 25, "heap_max_in_bytes": 1073741824 }
		      }
		    }
		  }
		}
		""";

		var response = Deserialize<StatsNodeResponse>(json);

		response!.NodeStatistics!.Total.Should().Be(1);
		var node = response.Nodes!["node-id-1"];
		node.Name.Should().Be("node-1");
		node.Indices!.Docs!.Count.Should().Be(1234);
		node.Indices.Store!.SizeInBytes.Should().Be(987654);
		node.Jvm!.Mem!.HeapUsedInBytes.Should().Be(268435456);
		node.Jvm.Mem.HeapMaxInBytes.Should().Be(1073741824);
	}

	[Fact]
	public void Cluster_stats_response_deserializes_nodes_section()
	{
		const string json = """
		{
		  "_nodes": { "total": 3, "successful": 3, "failed": 0 },
		  "cluster_name": "opensearch-cluster",
		  "cluster_uuid": "uuid-1",
		  "status": "green",
		  "nodes": {
		    "count": { "total": 3, "data": 3, "ingest": 3 },
		    "versions": ["3.4.0"],
		    "jvm": { "max_uptime_in_millis": 7200000 }
		  }
		}
		""";

		var response = Deserialize<StatsClusterResponse>(json);

		response!.NodeStatistics!.Total.Should().Be(3);
		response.Nodes!.Count!.Total.Should().Be(3);
		response.Nodes.Versions.Should().Contain("3.4.0");
	}

	[Fact]
	public void Cluster_state_response_captures_freeform_body()
	{
		const string json = """
		{
		  "cluster_name": "opensearch-cluster",
		  "cluster_uuid": "uuid-1",
		  "metadata": { "indices": { "products": { "state": "open" } } },
		  "nodes": { "node-id-1": { "name": "node-1" } }
		}
		""";

		var response = Deserialize<StateClusterResponse>(json);

		response!.ExtensionData.Should().NotBeNull();
		response.ExtensionData!["cluster_name"].GetString().Should().Be("opensearch-cluster");
		response.ExtensionData["metadata"].GetProperty("indices").GetProperty("products")
			.GetProperty("state").GetString().Should().Be("open");
	}

	private sealed class Product
	{
		public string? Title { get; set; }
		public double? Price { get; set; }
	}

	[Fact]
	public void Get_source_response_deserializes_typed_document()
	{
		const string json = """{ "title": "Widget", "price": 9.99 }""";
		using var body = new MemoryStream(Encoding.UTF8.GetBytes(json));

		var response = GetSourceEndpoint<Product>.Instance.DeserializeResponse(200, "application/json", body, Serializer);

		response.Body!.Title.Should().Be("Widget");
		response.Body.Price.Should().Be(9.99);
	}

	private sealed class UpperCaseSourceSerializer : IOpenSearchSerializer
	{
		public T? Deserialize<T>(Stream stream)
		{
			using var reader = new StreamReader(stream);
			var raw = reader.ReadToEnd();
			return System.Text.Json.JsonSerializer.Deserialize<T>(raw.Replace("\"widget\"", "\"WIDGET\""));
		}

		public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken ct = default) =>
			new(Deserialize<T>(stream));

		public void Serialize<T>(T data, Stream stream) =>
			System.Text.Json.JsonSerializer.Serialize(stream, data);

		public ValueTask SerializeAsync<T>(T data, Stream stream, CancellationToken ct = default)
		{
			Serialize(data, stream);
			return ValueTask.CompletedTask;
		}
	}

	[Fact]
	public void Get_source_response_routes_through_custom_source_serializer()
	{
		var settings = OpenSearchClientSettings
			.Create(new Uri("http://localhost:9200"))
			.SourceSerializer(() => new UpperCaseSourceSerializer())
			.Build();

		const string json = """{ "Title": "widget", "Price": 1 }""";
		using var body = new MemoryStream(Encoding.UTF8.GetBytes(json));

		var response = GetSourceEndpoint<Product>.Instance
			.DeserializeResponse(200, "application/json", body, settings.RequestResponseSerializer);

		response.Body!.Title.Should().Be("WIDGET", "the custom source serializer must deserialize the body");
	}

	[Fact]
	public void Percolate_query_serializes_both_name_properties()
	{
		var query = new PercolateQuery
		{
			Field = "query_field",
			QueryName = "outer",
			Name = "slot_a",
		};

		var root = Parse(Serialize(query));

		root.GetProperty("_name").GetString().Should().Be("outer");
		root.GetProperty("name").GetString().Should().Be("slot_a");
		root.GetProperty("field").GetString().Should().Be("query_field");
	}
}
