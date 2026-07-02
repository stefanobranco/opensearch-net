using System.Text.Json;
using FluentAssertions;
using OpenSearch.Client;
using Xunit;

namespace OpenSearch.Client.Tests;

public class SourceConfigParamTests
{
	[Fact]
	public void Bool_BuildsQueryString()
	{
		var request = new MgetRequest { Index = "docs", Source = false };
		var url = MgetEndpoint.Instance.RequestUrl(request);
		url.Should().Be("/docs/_mget?_source=false");
	}

	[Fact]
	public void SingleField_BuildsQueryString()
	{
		var request = new MgetRequest { Index = "docs", Source = "title" };
		var url = MgetEndpoint.Instance.RequestUrl(request);
		url.Should().Be("/docs/_mget?_source=title");
	}

	[Fact]
	public void FieldList_BuildsCommaSeparatedQueryString()
	{
		var request = new MgetRequest { Index = "docs", Source = new[] { "title", "price" } };
		var url = MgetEndpoint.Instance.RequestUrl(request);
		url.Should().Be("/docs/_mget?_source=title%2Cprice");
	}

	[Fact]
	public void Unset_OmitsQueryParameter()
	{
		var request = new MgetRequest { Index = "docs" };
		var url = MgetEndpoint.Instance.RequestUrl(request);
		url.Should().Be("/docs/_mget");
	}

	[Fact]
	public void Bool_SerializesAsJsonBool()
	{
		SourceConfigParam value = true;
		JsonSerializer.Serialize(value).Should().Be("true");
	}

	[Fact]
	public void FieldList_SerializesAsJsonArray()
	{
		SourceConfigParam value = new List<string> { "title", "price" };
		JsonSerializer.Serialize(value).Should().Be("""["title","price"]""");
	}

	[Fact]
	public void Deserialize_AllWireForms()
	{
		JsonSerializer.Deserialize<SourceConfigParam>("false")!.Enabled.Should().BeFalse();
		JsonSerializer.Deserialize<SourceConfigParam>("\"title\"")!.Fields.Should().BeEquivalentTo(["title"]);
		JsonSerializer.Deserialize<SourceConfigParam>("""["a","b"]""")!.Fields.Should().BeEquivalentTo(["a", "b"]);
	}
}
