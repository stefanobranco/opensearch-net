using OpenSearch.Benchmarks;

namespace OpenSearch.Benchmarks.Legacy;

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
		Check("SearchSimple hits", simple.Documents.Count == 10, ref failures);
		Check("SearchSimple source", simple.Documents.First().Id == "product-0", ref failures);

		var rich = await benchmarks.SearchRich();
		Check("SearchRich hits", rich.Documents.Count == 100, ref failures);
		Check("SearchRich terms buckets", rich.Aggregations.Terms("tags")?.Buckets.Count == 5, ref failures);
		Check("SearchRich date buckets", rich.Aggregations.DateHistogram("per_month")?.Buckets.Count == 12, ref failures);

		var bulk = await benchmarks.BulkIndex1000();
		Check("Bulk items", bulk.Items?.Count == 1000, ref failures);
		Check("Bulk errors", bulk.Errors == false, ref failures);

		var get = await benchmarks.GetDocument();
		Check("Get found", get.Found, ref failures);
		Check("Get source", get.Source?.Id == "product-1", ref failures);

		benchmarks.SerializeSearchRequest();

		var deserialized = benchmarks.DeserializeSearchResponse();
		Check("Deserialize hits", deserialized?.Documents.Count == 100, ref failures);

		Console.WriteLine(failures == 0 ? "Smoke: all scenarios valid." : $"Smoke: {failures} FAILURE(S).");
		return failures == 0 ? 0 : 1;
	}

	private static void Check(string name, bool ok, ref int failures)
	{
		if (!ok) failures++;
		Console.WriteLine($"  [{(ok ? "ok" : "FAIL")}] {name}");
	}
}
