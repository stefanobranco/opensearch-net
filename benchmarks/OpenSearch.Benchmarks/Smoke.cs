using OpenSearch.Client;

namespace OpenSearch.Benchmarks;

/// <summary>
/// Runs every benchmark scenario once and validates the parsed responses, so the harness is
/// known-good before any numbers are trusted. Invoked with <c>dotnet run -- smoke</c>.
/// </summary>
public static class Smoke
{
	public static async Task<int> RunAsync()
	{
		var benchmarks = new ClientBenchmarks();
		benchmarks.Setup();
		var failures = 0;

		var simple = await benchmarks.SearchSimple();
		Check("SearchSimple hits", simple.Documents().Count == 10, ref failures);
		Check("SearchSimple source", simple.Documents()[0].Id == "product-0", ref failures);

		var rich = await benchmarks.SearchRich();
		Check("SearchRich hits", rich.Documents().Count == 100, ref failures);
		Check("SearchRich terms buckets", rich.Aggs().Terms("tags")?.Buckets.Count == 5, ref failures);
		Check("SearchRich date buckets", rich.Aggs().DateHistogram("per_month")?.Buckets.Count == 12, ref failures);

		var bulk = await benchmarks.BulkIndex1000();
		Check("Bulk items", bulk.Items?.Count == 1000, ref failures);
		Check("Bulk errors", bulk.Errors == false, ref failures);

		var get = await benchmarks.GetDocument();
		Check("Get found", get.Found, ref failures);
		Check("Get source", get.Source?.Id == "product-1", ref failures);

		benchmarks.SerializeSearchRequest();

		var deserialized = benchmarks.DeserializeSearchResponse();
		Check("Deserialize hits", deserialized?.Documents().Count == 100, ref failures);

		var sourceBenchmarks = new SourceSerializerBenchmarks();
		sourceBenchmarks.Setup();

		foreach (var (name, search) in new (string, Func<Task<SearchResponse<Product>>>)[]
		{
			("default STJ", sourceBenchmarks.Search100_DefaultStj),
			("custom STJ source", sourceBenchmarks.Search100_CustomStjSource),
			("Newtonsoft source", sourceBenchmarks.Search100_NewtonsoftSource),
		})
		{
			var result = await search();
			Check($"SourceSerializer search ({name})", result.Documents().Count == 100
				&& result.Documents()[0].Id == "product-0" && result.Documents()[0].Tags.Count == 2, ref failures);
		}

		var newtonsoftBulk = await sourceBenchmarks.Bulk1000_NewtonsoftSource();
		Check("SourceSerializer bulk (Newtonsoft source)", newtonsoftBulk.Items?.Count == 1000, ref failures);

		Console.WriteLine(failures == 0 ? "Smoke: all scenarios valid." : $"Smoke: {failures} FAILURE(S).");
		return failures == 0 ? 0 : 1;
	}

	private static void Check(string name, bool ok, ref int failures)
	{
		if (!ok) failures++;
		Console.WriteLine($"  [{(ok ? "ok" : "FAIL")}] {name}");
	}
}
