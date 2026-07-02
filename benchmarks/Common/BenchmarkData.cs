using System.Globalization;
using System.Text;

namespace OpenSearch.Benchmarks;

/// <summary>
/// The document type used by all benchmark scenarios. Property names are single words so the
/// v2 client (snake_case) and the legacy client (camelCase) produce identical wire field names,
/// letting both deserialize the same response fixtures.
/// </summary>
public sealed class Product
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public string Description { get; set; } = "";
	public double Price { get; set; }
	public int Stock { get; set; }
	public DateTime Created { get; set; }
	public List<string> Tags { get; set; } = [];
}

/// <summary>
/// Deterministic documents and canned response bodies shared by the v2 and legacy benchmark
/// projects, so both clients are measured against byte-identical wire payloads.
/// </summary>
public static class BenchmarkData
{
	private static readonly string[] TagPool = ["alpha", "beta", "gamma", "delta", "epsilon"];
	private static readonly DateTime BaseDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public static List<Product> Products(int count)
	{
		var products = new List<Product>(count);
		for (var i = 0; i < count; i++)
			products.Add(Product(i));
		return products;
	}

	private static Product Product(int i) => new()
	{
		Id = $"product-{i}",
		Name = $"Widget {i}",
		Description = $"A very useful widget, model {i}, with extended warranty and premium finish.",
		Price = 9.99 + i % 90,
		Stock = i % 250,
		Created = BaseDate.AddDays(i % 365),
		Tags = [TagPool[i % TagPool.Length], TagPool[(i + 2) % TagPool.Length]],
	};

	private static void AppendSource(StringBuilder sb, int i)
	{
		var p = Product(i);
		sb.Append("{\"id\":\"").Append(p.Id)
			.Append("\",\"name\":\"").Append(p.Name)
			.Append("\",\"description\":\"").Append(p.Description)
			.Append("\",\"price\":").Append(p.Price.ToString(CultureInfo.InvariantCulture))
			.Append(",\"stock\":").Append(p.Stock)
			.Append(",\"created\":\"").Append(p.Created.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture))
			.Append("\",\"tags\":[\"").Append(p.Tags[0]).Append("\",\"").Append(p.Tags[1])
			.Append("\"]}");
	}

	public static string SearchResponseJson(int hits, bool withAggregations)
	{
		var sb = new StringBuilder(hits * 320);
		sb.Append("{\"took\":12,\"timed_out\":false,")
			.Append("\"_shards\":{\"total\":1,\"successful\":1,\"skipped\":0,\"failed\":0},")
			.Append("\"hits\":{\"total\":{\"value\":1234,\"relation\":\"eq\"},\"max_score\":1.5,\"hits\":[");

		for (var i = 0; i < hits; i++)
		{
			if (i > 0) sb.Append(',');
			sb.Append("{\"_index\":\"products\",\"_id\":\"product-").Append(i)
				.Append("\",\"_score\":1.5,\"_source\":");
			AppendSource(sb, i);
			sb.Append('}');
		}

		sb.Append("]}");

		if (withAggregations)
		{
			sb.Append(",\"aggregations\":{")
				.Append("\"tags\":{\"doc_count_error_upper_bound\":0,\"sum_other_doc_count\":0,\"buckets\":[");
			for (var i = 0; i < TagPool.Length; i++)
			{
				if (i > 0) sb.Append(',');
				sb.Append("{\"key\":\"").Append(TagPool[i])
					.Append("\",\"doc_count\":").Append(200 + i * 17)
					.Append(",\"avg_price\":{\"value\":").Append((30.5 + i * 4.25).ToString(CultureInfo.InvariantCulture))
					.Append("}}");
			}
			sb.Append("]},\"per_month\":{\"buckets\":[");
			for (var month = 0; month < 12; month++)
			{
				if (month > 0) sb.Append(',');
				var key = new DateTimeOffset(BaseDate.AddMonths(month)).ToUnixTimeMilliseconds();
				sb.Append("{\"key_as_string\":\"")
					.Append(BaseDate.AddMonths(month).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture))
					.Append("\",\"key\":").Append(key)
					.Append(",\"doc_count\":").Append(80 + month * 3)
					.Append('}');
			}
			sb.Append("]}}");
		}

		sb.Append('}');
		return sb.ToString();
	}

	public static string BulkResponseJson(int items)
	{
		var sb = new StringBuilder(items * 190);
		sb.Append("{\"took\":30,\"errors\":false,\"items\":[");
		for (var i = 0; i < items; i++)
		{
			if (i > 0) sb.Append(',');
			sb.Append("{\"index\":{\"_index\":\"products\",\"_id\":\"product-").Append(i)
				.Append("\",\"_version\":1,\"result\":\"created\",")
				.Append("\"_shards\":{\"total\":2,\"successful\":1,\"failed\":0},")
				.Append("\"_seq_no\":").Append(i).Append(",\"_primary_term\":1,\"status\":201}}");
		}
		sb.Append("]}");
		return sb.ToString();
	}

	public static string GetResponseJson()
	{
		var sb = new StringBuilder(400);
		sb.Append("{\"_index\":\"products\",\"_id\":\"product-1\",\"_version\":1,")
			.Append("\"_seq_no\":0,\"_primary_term\":1,\"found\":true,\"_source\":");
		AppendSource(sb, 1);
		sb.Append('}');
		return sb.ToString();
	}
}
