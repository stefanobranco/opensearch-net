using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using OpenSearch.CodeGen.Coverage;
using OpenSearch.CodeGen.OpenApi;
using Xunit;
using Xunit.Abstractions;

namespace OpenSearch.CodeGen.Tests;

/// <summary>
/// Enforces the committed <c>API_COVERAGE.md</c> gap map: regenerates it from the vendored spec
/// plus the vendored opensearch-java namespace list and fails if the committed copy drifts. Also
/// guards that the generator's wired-namespace list and the regenerate script stay in sync.
/// </summary>
public class CoverageReportTests
{
	private readonly ITestOutputHelper _output;

	public CoverageReportTests(ITestOutputHelper output) => _output = output;

	[Fact]
	public void Coverage_map_is_up_to_date()
	{
		var repoRoot = RepoRoot();
		var rendered = BuildCoverageMarkdown(repoRoot);

		var committedPath = Path.Combine(repoRoot, "API_COVERAGE.md");

		if (Environment.GetEnvironmentVariable("UPDATE_COVERAGE") is { Length: > 0 })
		{
			File.WriteAllText(committedPath, rendered);
			_output.WriteLine($"Updated {committedPath}");
			return;
		}

		File.Exists(committedPath).Should().BeTrue(
			$"API_COVERAGE.md should exist — run `UPDATE_COVERAGE=1 dotnet test tests/OpenSearch.CodeGen.Tests` to create it");

		var committed = Normalize(File.ReadAllText(committedPath));
		Normalize(rendered).Should().Be(committed,
			"API_COVERAGE.md is stale — run `UPDATE_COVERAGE=1 dotnet test tests/OpenSearch.CodeGen.Tests` to refresh it");
	}

	[Fact]
	public void Regenerate_script_matches_canonical_namespace_list()
	{
		var repoRoot = RepoRoot();
		var script = File.ReadAllText(Path.Combine(repoRoot, "build", "regenerate.sh"));

		var match = Regex.Match(script, "namespaces=\"(?<list>[^\"]*)\"");
		match.Success.Should().BeTrue("build/regenerate.sh should declare namespaces=\"...\"");

		var scriptNamespaces = match.Groups["list"].Value
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		scriptNamespaces.Should().BeEquivalentTo(GeneratedNamespaces.All,
			"build/regenerate.sh must pass exactly the canonical GeneratedNamespaces.All set");
	}

	// ---------------------------------------------------------------------

	private static string BuildCoverageMarkdown(string repoRoot)
	{
		var spec = OpenApiSpecification.Load(Path.Combine(repoRoot, "src", "OpenSearch.CodeGen", "Spec"));

		var javaFile = Path.Combine(repoRoot, "tests", "OpenSearch.CodeGen.Tests", "Coverage", "opensearch-java-namespaces.txt");
		var javaNamespaces = ReadListFile(javaFile);
		var javaProvenance = ReadProvenance(javaFile);

		var handWritten = ScanHandWrittenRequestNames(
			Path.Combine(repoRoot, "src", "OpenSearch.Client"));

		var report = CoverageAnalyzer.Analyze(spec, GeneratedNamespaces.All, javaNamespaces, handWritten);
		return CoverageMarkdownRenderer.Render(report, javaProvenance);
	}

	/// <summary>Hand-written <c>*Request</c> type names outside the generated tree (credits NDJSON endpoints).</summary>
	private static IReadOnlyCollection<string> ScanHandWrittenRequestNames(string clientSrcDir)
	{
		var names = new HashSet<string>(StringComparer.Ordinal);
		var classRegex = new Regex(@"\bclass\s+(?<name>\w+Request)\b");

		foreach (var file in Directory.EnumerateFiles(clientSrcDir, "*.cs", SearchOption.AllDirectories))
		{
			var rel = Path.GetRelativePath(clientSrcDir, file).Replace('\\', '/');
			if (rel.StartsWith("Generated/", StringComparison.Ordinal)
				|| rel.Contains("/obj/", StringComparison.Ordinal)
				|| rel.Contains("/bin/", StringComparison.Ordinal))
				continue;

			foreach (Match m in classRegex.Matches(File.ReadAllText(file)))
				names.Add(m.Groups["name"].Value);
		}

		return names;
	}

	private static IReadOnlyCollection<string> ReadListFile(string path) =>
		File.ReadAllLines(path)
			.Select(l => l.Trim())
			.Where(l => l.Length > 0 && !l.StartsWith('#'))
			.ToList();

	private static string ReadProvenance(string path)
	{
		var line = File.ReadAllLines(path).FirstOrDefault(l => l.TrimStart('#', ' ').StartsWith("Source:", StringComparison.Ordinal));
		return line is null ? "opensearch-java" : line.TrimStart('#', ' ').Substring("Source:".Length).Trim();
	}

	private static string RepoRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "OpenSearch.sln")))
			dir = dir.Parent;

		dir.Should().NotBeNull("the test must run inside the repository (no OpenSearch.sln found walking up)");
		return dir!.FullName;
	}

	private static string Normalize(string s) => s.Replace("\r\n", "\n");
}
