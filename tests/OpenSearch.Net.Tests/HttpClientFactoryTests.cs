using FluentAssertions;
using OpenSearch.Net;
using Xunit;

namespace OpenSearch.Net.Tests;

public class HttpClientFactoryTests : IDisposable
{
	private readonly List<HttpClientFactory> _factories = [];

	public void Dispose()
	{
		foreach (var f in _factories)
			f.Dispose();
	}

	[Fact]
	public void GetClient_ReturnsNonNullClient()
	{
		var factory = Track(new HttpClientFactory(TimeSpan.FromHours(1)));
		var client = factory.GetClient();
		client.Should().NotBeNull();
	}

	[Fact]
	public void GetClient_BeforeExpiry_ReturnsSameClient()
	{
		var factory = Track(new HttpClientFactory(TimeSpan.FromHours(1)));
		var client1 = factory.GetClient();
		var client2 = factory.GetClient();
		client1.Should().BeSameAs(client2);
	}

	[Fact]
	public void GetClient_AfterExpiry_ReturnsDifferentClient()
	{
		// Use a very short lifetime to force expiry
		var factory = Track(new HttpClientFactory(
			TimeSpan.FromMilliseconds(1),
			() => new HttpClientHandler()));
		var client1 = factory.GetClient();
		// Wait for expiry
		Thread.Sleep(10);
		var client2 = factory.GetClient();
		client2.Should().NotBeSameAs(client1);
	}

	[Fact]
	public void Dispose_PreventsGetClient()
	{
		var factory = new HttpClientFactory(TimeSpan.FromHours(1));
		factory.Dispose();
		var act = () => factory.GetClient();
		act.Should().Throw<ObjectDisposedException>();
	}

	[Fact]
	public async Task GetClient_ThreadSafety()
	{
		var factory = Track(new HttpClientFactory(TimeSpan.FromHours(1)));
		var clients = new System.Collections.Concurrent.ConcurrentBag<System.Net.Http.HttpClient>();
		var tasks = Enumerable.Range(0, 10).Select(_ =>
			Task.Run(() => clients.Add(factory.GetClient()))).ToArray();
		await Task.WhenAll(tasks);
		clients.Should().HaveCount(10);
		// All should be the same instance (not expired)
		clients.Distinct().Should().HaveCount(1);
	}

	private HttpClientFactory Track(HttpClientFactory f) { _factories.Add(f); return f; }
}
