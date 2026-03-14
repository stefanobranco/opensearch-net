using FluentAssertions;
using OpenSearch.Net;
using Xunit;

namespace OpenSearch.Net.Tests;

public class TransportConfigurationTests
{
	private static readonly Uri Node1 = new("http://node1:9200");
	private static readonly Uri Node2 = new("http://node2:9200");
	private static readonly Uri Node3 = new("http://node3:9200");

	[Fact]
	public void Defaults_RequestTimeoutIs60Seconds()
	{
		var config = TransportConfiguration.Create(Node1).Build();
		config.RequestTimeout.Should().Be(TimeSpan.FromSeconds(60));
	}

	[Fact]
	public void Defaults_DnsRefreshTimeoutIs5Minutes()
	{
		var config = TransportConfiguration.Create(Node1).Build();
		config.DnsRefreshTimeout.Should().Be(TimeSpan.FromMinutes(5));
	}

	[Fact]
	public void Defaults_MaxRetriesIsNodeCountMinus1()
	{
		var config = TransportConfiguration.Create(Node1, Node2, Node3).Build();
		config.MaxRetries.Should().Be(2);
	}

	[Fact]
	public void Defaults_MaxRetriesIsZeroForSingleNode()
	{
		var config = TransportConfiguration.Create(Node1).Build();
		config.MaxRetries.Should().Be(0);
	}

	[Fact]
	public void Defaults_CompressionDisabled()
	{
		var config = TransportConfiguration.Create(Node1).Build();
		config.EnableHttpCompression.Should().BeFalse();
	}

	[Fact]
	public void Defaults_NoAuthentication()
	{
		var config = TransportConfiguration.Create(Node1).Build();
		config.BasicAuth.Should().BeNull();
		config.ApiKeyAuth.Should().BeNull();
	}

	[Fact]
	public void Override_RequestTimeout()
	{
		var config = TransportConfiguration.Create(Node1)
			.RequestTimeout(TimeSpan.FromSeconds(30))
			.Build();

		config.RequestTimeout.Should().Be(TimeSpan.FromSeconds(30));
	}

	[Fact]
	public void Override_DnsRefreshTimeout()
	{
		var config = TransportConfiguration.Create(Node1)
			.DnsRefreshTimeout(TimeSpan.FromMinutes(10))
			.Build();

		config.DnsRefreshTimeout.Should().Be(TimeSpan.FromMinutes(10));
	}

	[Fact]
	public void Override_MaxRetries()
	{
		var config = TransportConfiguration.Create(Node1)
			.MaxRetries(5)
			.Build();

		config.MaxRetries.Should().Be(5);
	}

	[Fact]
	public void Override_EnableHttpCompression()
	{
		var config = TransportConfiguration.Create(Node1)
			.EnableHttpCompression()
			.Build();

		config.EnableHttpCompression.Should().BeTrue();
	}

	[Fact]
	public void CreateFromUris_ProducesCorrectNodePool()
	{
		var config = TransportConfiguration.Create(Node1, Node2).Build();

		config.NodePool.Nodes.Should().HaveCount(2);
		config.NodePool.Nodes[0].Host.Should().Be(Node1);
		config.NodePool.Nodes[1].Host.Should().Be(Node2);
	}

	[Fact]
	public void CreateFromNodePool_UsesGivenPool()
	{
		var pool = new NodePool([Node1, Node2, Node3]);
		var config = TransportConfiguration.Create(pool).Build();

		config.NodePool.Should().BeSameAs(pool);
		config.NodePool.Nodes.Should().HaveCount(3);
	}

	[Fact]
	public void BasicAuth_ClearsApiKey()
	{
		var config = TransportConfiguration.Create(Node1)
			.Authentication(new ApiKeyCredentials("id", "key"))
			.Authentication(new BasicAuthCredentials("user", "pass"))
			.Build();

		config.BasicAuth.Should().NotBeNull();
		config.BasicAuth!.Username.Should().Be("user");
		config.BasicAuth.Password.Should().Be("pass");
		config.ApiKeyAuth.Should().BeNull();
	}

	[Fact]
	public void ApiKeyAuth_ClearsBasic()
	{
		var config = TransportConfiguration.Create(Node1)
			.Authentication(new BasicAuthCredentials("user", "pass"))
			.Authentication(new ApiKeyCredentials("id", "key"))
			.Build();

		config.ApiKeyAuth.Should().NotBeNull();
		config.ApiKeyAuth!.Id.Should().Be("id");
		config.ApiKeyAuth.ApiKey.Should().Be("key");
		config.BasicAuth.Should().BeNull();
	}

	[Fact]
	public void Proxy_SetsProxyAddress()
	{
		var proxyUri = new Uri("http://proxy:8080");
		var config = TransportConfiguration.Create(Node1)
			.Proxy(proxyUri, "user", "pass")
			.Build();

		config.ProxyAddress.Should().Be(proxyUri);
		config.ProxyUsername.Should().Be("user");
		config.ProxyPassword.Should().Be("pass");
	}

	[Fact]
	public void DisableAutomaticProxyDetection_SetsFlag()
	{
		var config = TransportConfiguration.Create(Node1)
			.DisableAutomaticProxyDetection()
			.Build();

		config.DisableAutomaticProxyDetection.Should().BeTrue();
	}

	[Fact]
	public void OnRequestCreated_SetsCallback()
	{
		Action<HttpRequestMessage> callback = _ => { };
		var config = TransportConfiguration.Create(Node1)
			.OnRequestCreated(callback)
			.Build();

		config.OnRequestCreated.Should().BeSameAs(callback);
	}

	[Fact]
	public void Serializer_SetsCustomSerializer()
	{
		var serializer = new StubSerializer();
		var config = TransportConfiguration.Create(Node1)
			.Serializer(serializer)
			.Build();

		config.Serializer.Should().BeSameAs(serializer);
	}

	[Fact]
	public void Builder_SetsAllProperties()
	{
		var proxyUri = new Uri("http://proxy:8080");
		Action<HttpRequestMessage> callback = _ => { };
		Func<HttpMessageHandler, HttpMessageHandler> handlerFactory = h => h;
		var serializer = new StubSerializer();

		var config = TransportConfiguration.Create(Node1, Node2, Node3)
			.Serializer(serializer)
			.RequestTimeout(TimeSpan.FromSeconds(30))
			.DnsRefreshTimeout(TimeSpan.FromMinutes(10))
			.MaxRetries(5)
			.EnableHttpCompression()
			.Authentication(new BasicAuthCredentials("user", "pass"))
			.DisableAutomaticProxyDetection()
			.Proxy(proxyUri, "puser", "ppass")
			.OnRequestCreated(callback)
			.HttpMessageHandlerFactory(handlerFactory)
			.Build();

		config.NodePool.Nodes.Should().HaveCount(3);
		config.Serializer.Should().BeSameAs(serializer);
		config.RequestTimeout.Should().Be(TimeSpan.FromSeconds(30));
		config.DnsRefreshTimeout.Should().Be(TimeSpan.FromMinutes(10));
		config.MaxRetries.Should().Be(5);
		config.EnableHttpCompression.Should().BeTrue();
		config.BasicAuth!.Username.Should().Be("user");
		config.DisableAutomaticProxyDetection.Should().BeTrue();
		config.ProxyAddress.Should().Be(proxyUri);
		config.ProxyUsername.Should().Be("puser");
		config.ProxyPassword.Should().Be("ppass");
		config.OnRequestCreated.Should().BeSameAs(callback);
		config.HttpMessageHandlerFactory.Should().BeSameAs(handlerFactory);
	}

	[Fact]
	public void RequestTimeout_ThrowsOnZero()
	{
		var act = () => TransportConfiguration.Create(Node1)
			.RequestTimeout(TimeSpan.Zero);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	private sealed class StubSerializer : IOpenSearchSerializer
	{
		public T? Deserialize<T>(Stream stream) => default;
		public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken ct = default) => default;
		public void Serialize<T>(T data, Stream stream) { }
		public ValueTask SerializeAsync<T>(T data, Stream stream, CancellationToken ct = default) => default;
	}
}
