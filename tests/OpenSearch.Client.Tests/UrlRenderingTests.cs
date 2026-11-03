using System.Globalization;
using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

public class UrlRenderingTests
{
	[Fact]
	public void EnumListPathParam_UsesWireNames()
	{
		var request = new StateClusterRequest { Metric = [ClusterStateMetric.All, ClusterStateMetric.ClusterManagerNode] };
		var url = StateClusterEndpoint.Instance.RequestUrl(request);
		url.Should().Be("/_cluster/state/_all%2Ccluster_manager_node");
	}

	[Fact]
	public void EnumPathParam_UsesWireName()
	{
		var request = new ExecuteAlgorithmMlRequest { AlgorithmName = FunctionName.AdLibsvm };
		var url = ExecuteAlgorithmMlEndpoint.Instance.RequestUrl(request);
		url.Should().Be("/_plugins/_ml/_execute/ad_libsvm");
	}

	[Theory]
	[InlineData("de-DE")]
	[InlineData("sv-SE")]
	public void FloatQueryParam_FormatsInvariant(string culture)
	{
		var request = new UpdateByQueryRequest { Index = ["docs"], RequestsPerSecond = 0.5f };
		var url = WithCulture(culture, () => UpdateByQueryEndpoint.Instance.RequestUrl(request));
		url.Should().Be("/docs/_update_by_query?requests_per_second=0.5");
	}

	[Fact]
	public void NegativeLongPathParam_FormatsInvariant()
	{
		var request = new ChunkModelMlRequest { ModelId = "m", ChunkNumber = -1 };
		var url = WithCulture("sv-SE", () => ChunkModelMlEndpoint.Instance.RequestUrl(request));
		url.Should().Be("/_plugins/_ml/models/m/chunk/-1");
	}

	private static string WithCulture(string culture, Func<string> action)
	{
		var original = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo(culture);
		try
		{
			return action();
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}
}
