using FluentAssertions;
using OpenSearch.IntegrationTests.Infrastructure;
using OpenSearch.Client;

namespace OpenSearch.IntegrationTests.Knn;

public class KnnTests : IntegrationTestBase
{
	private sealed class KnnDoc
	{
		public float[]? Embedding { get; set; }
		public string? Title { get; set; }
	}

	[SkipIfNoCluster]
	public void VectorSearchReturnsNearestNeighbours()
	{
		var index = UniqueIndex("knn");

		// A knn-enabled index with a 3-dimensional knn_vector field.
		Client.Indices.Create(new CreateIndexRequest
		{
			Index = index,
			Settings = new IndexSettings { Knn = "true", NumberOfShards = "1" },
			Mappings = new TypeMapping
			{
				Properties = new Dictionary<string, Property>
				{
					["embedding"] = Property.KnnVectorProperty(new KnnVectorProperty { Dimension = 3 }),
					["title"] = Property.TextProperty(new TextProperty()),
				},
			},
		});

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<KnnDoc> { Document = new KnnDoc { Title = "near", Embedding = [1.0f, 2.0f, 3.0f] }, Id = "1" },
				new BulkIndexOperation<KnnDoc> { Document = new KnnDoc { Title = "mid", Embedding = [2.0f, 3.0f, 4.0f] }, Id = "2" },
				new BulkIndexOperation<KnnDoc> { Document = new KnnDoc { Title = "far", Embedding = [10.0f, 10.0f, 10.0f] }, Id = "3" },
			],
		});

		var search = Client.Core.Search<KnnDoc>(new SearchRequest
		{
			Index = [index],
			Size = 2,
			Query = QueryContainer.Knn("embedding", new KnnQuery { Vector = [1.0f, 2.0f, 3.0f], K = 2 }),
		});

		search.Hits!.Hits.Should().HaveCount(2);
		// The exact-match vector is the nearest neighbour.
		search.Hits.Hits![0].Source!.Title.Should().Be("near");
	}

	[SkipIfNoCluster]
	public void StatsReturnsClusterName()
	{
		var response = Client.Knn.Stats(new StatsKnnRequest());

		response.ClusterName.Should().NotBeNullOrEmpty();
	}
}
