using FluentAssertions;
using OpenSearch.Client;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Pins the generated single-value convenience overloads for scalar-list descriptor setters
/// (<c>Text("...")</c>, <c>Index("...")</c>) so consumers don't wrap single values in lists.
/// </summary>
public class ScalarListOverloadTests
{
	[Fact]
	public void AnalyzeText_SingleString()
	{
		AnalyzeIndexRequest request = new AnalyzeIndexRequestDescriptor()
			.Analyzer("standard")
			.Text("the quick brown fox");

		request.Text.Should().BeEquivalentTo(["the quick brown fox"]);
	}

	[Fact]
	public void AnalyzeText_ListStillSupported()
	{
		AnalyzeIndexRequest request = new AnalyzeIndexRequestDescriptor()
			.Text(["one", "two"]);

		request.Text.Should().BeEquivalentTo(["one", "two"]);
	}

	[Fact]
	public void SearchIndex_SingleString()
	{
		SearchRequest request = new SearchRequestDescriptor().Index("logs");
		request.Index.Should().BeEquivalentTo(["logs"]);
	}
}
