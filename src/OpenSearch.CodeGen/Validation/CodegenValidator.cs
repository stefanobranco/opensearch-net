using OpenSearch.CodeGen.Model;
using OpenSearch.CodeGen.Transformer;

namespace OpenSearch.CodeGen.Validation;

/// <summary>
/// Validates codegen output quality: detects stub types, empty types, and unresolved oneOf fallbacks.
/// Returns exit code 1 if critical types fail minimum field thresholds.
/// </summary>
public static class CodegenValidator
{
	/// <summary>
	/// Minimum field counts for critical object/request types.
	/// </summary>
	private static readonly Dictionary<string, int> s_criticalObjectMinimums = new(StringComparer.OrdinalIgnoreCase)
	{
		["BoolQuery"] = 5,
		["SearchRequest"] = 10,
		["IndexSettings"] = 15,
	};

	/// <summary>
	/// Minimum variant counts for critical tagged union types.
	/// </summary>
	private static readonly Dictionary<string, int> s_criticalUnionMinimums = new(StringComparer.OrdinalIgnoreCase)
	{
		["AggregationContainer"] = 30,
	};

	/// <summary>
	/// Types that are expected to have 0 fields (CAT responses, marker types).
	/// </summary>
	private static readonly HashSet<string> s_expectedEmpties = new(StringComparer.OrdinalIgnoreCase)
	{
		"AliasesCatResponse", "AllocationCatResponse", "CountCatResponse",
		"FielddataCatResponse", "HealthCatResponse", "HelpCatResponse",
		"IndicesCatResponse", "MasterCatResponse", "NodeattrsCatResponse",
		"NodesCatResponse", "PendingTasksCatResponse", "PluginsCatResponse",
		"RecoveryCatResponse", "RepositoriesCatResponse", "SegmentsCatResponse",
		"ShardsCatResponse", "SnapshotsCatResponse", "TasksCatResponse",
		"TemplatesCatResponse", "ThreadPoolCatResponse",
	};

	/// <summary>
	/// Runs all validation checks. Returns true if validation passes.
	/// </summary>
	public static bool Validate(
		IReadOnlyList<TransformResult> allResults,
		TypeMapper typeMapper)
	{
		var hasCriticalFailure = false;

		// Collect all shapes across namespaces into lookup dictionaries
		var objectsByName = new Dictionary<string, ObjectShape>(StringComparer.OrdinalIgnoreCase);
		var unionsByName = new Dictionary<string, TaggedUnionShape>(StringComparer.OrdinalIgnoreCase);
		var requestsByName = new Dictionary<string, RequestShape>(StringComparer.OrdinalIgnoreCase);
		var stubTypes = new List<string>();
		var emptyTypes = new List<string>();

		foreach (var result in allResults)
		{
			foreach (var obj in result.Objects)
			{
				objectsByName.TryAdd(obj.ClassName, obj);

				// Check 1: JsonElement ratio
				if (obj.Fields.Count > 0)
				{
					var jsonElementCount = obj.Fields.Count(f =>
						f.Type.CSharpName == "System.Text.Json.JsonElement");
					var ratio = (double)jsonElementCount / obj.Fields.Count;
					if (ratio > 0.5)
						stubTypes.Add($"  {obj.Namespace}.{obj.ClassName}: {jsonElementCount}/{obj.Fields.Count} fields are JsonElement ({ratio:P0})");
				}

				// Check 2: Empty types
				if (obj.Fields.Count == 0 && !s_expectedEmpties.Contains(obj.ClassName))
					emptyTypes.Add($"  {obj.Namespace}.{obj.ClassName}");
			}
			foreach (var union in result.TaggedUnions)
				unionsByName.TryAdd(union.ClassName, union);
			foreach (var req in result.Requests)
				requestsByName.TryAdd(req.ClassName, req);
		}

		Console.WriteLine("\n=== Codegen Validation ===");

		if (stubTypes.Count > 0)
		{
			Console.WriteLine($"[WARN] {stubTypes.Count} types have >50% JsonElement fields (likely stubs):");
			foreach (var s in stubTypes.Take(20))
				Console.WriteLine(s);
			if (stubTypes.Count > 20)
				Console.WriteLine($"  ... and {stubTypes.Count - 20} more");
		}

		if (emptyTypes.Count > 0)
		{
			Console.WriteLine($"[WARN] {emptyTypes.Count} types have 0 fields:");
			foreach (var s in emptyTypes.Take(20))
				Console.WriteLine(s);
			if (emptyTypes.Count > 20)
				Console.WriteLine($"  ... and {emptyTypes.Count - 20} more");
		}

		// Check 3a: Critical object/request type minimums
		foreach (var (typeName, minCount) in s_criticalObjectMinimums)
		{
			if (objectsByName.TryGetValue(typeName, out var obj))
			{
				if (obj.Fields.Count < minCount)
				{
					Console.WriteLine($"[FAIL] {typeName} has {obj.Fields.Count} fields (minimum: {minCount})");
					hasCriticalFailure = true;
				}
				else
				{
					Console.WriteLine($"[OK] {typeName}: {obj.Fields.Count} fields (minimum: {minCount})");
				}
			}
			else
			{
				// Fall back to request shapes (field count = path + query + body)
				var reqName = typeName.EndsWith("Request", StringComparison.OrdinalIgnoreCase)
					? typeName : typeName + "Request";
				if (requestsByName.TryGetValue(reqName, out var req))
				{
					var totalFields = req.PathParams.Count + req.QueryParams.Count + req.BodyFields.Count;
					if (totalFields < minCount)
					{
						Console.WriteLine($"[FAIL] {reqName} has {totalFields} total fields (minimum: {minCount})");
						hasCriticalFailure = true;
					}
					else
					{
						Console.WriteLine($"[OK] {reqName}: {totalFields} total fields (minimum: {minCount})");
					}
				}
				else
				{
					Console.WriteLine($"[WARN] Critical type '{typeName}' not found");
				}
			}
		}

		// Check 3b: Critical tagged union minimums
		foreach (var (unionName, minCount) in s_criticalUnionMinimums)
		{
			if (unionsByName.TryGetValue(unionName, out var union))
			{
				if (union.Variants.Count < minCount)
				{
					Console.WriteLine($"[FAIL] {unionName} has {union.Variants.Count} variants (minimum: {minCount})");
					hasCriticalFailure = true;
				}
				else
				{
					Console.WriteLine($"[OK] {unionName}: {union.Variants.Count} variants (minimum: {minCount})");
				}
			}
			else
			{
				Console.WriteLine($"[FAIL] Critical union '{unionName}' not found");
				hasCriticalFailure = true;
			}
		}

		// Check 4: Unresolved oneOf fallbacks
		var fallbacks = typeMapper.OneOfFallbacks;
		if (fallbacks.Count > 0)
		{
			Console.WriteLine($"\n[INFO] {fallbacks.Count} oneOf schemas fell back to JsonElement:");
			foreach (var f in fallbacks.Take(30))
				Console.WriteLine($"  {f}");
			if (fallbacks.Count > 30)
				Console.WriteLine($"  ... and {fallbacks.Count - 30} more");
		}
		else
		{
			Console.WriteLine("\n[OK] No oneOf fallbacks to JsonElement");
		}

		Console.WriteLine($"\n=== Validation {(hasCriticalFailure ? "FAILED" : "PASSED")} ===\n");
		return !hasCriticalFailure;
	}
}
