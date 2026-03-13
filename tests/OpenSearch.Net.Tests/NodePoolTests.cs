using FluentAssertions;
using OpenSearch.Net;
using Xunit;

namespace OpenSearch.Net.Tests;

public class NodePoolTests
{
	private static readonly Uri Uri1 = new("http://node1:9200");
	private static readonly Uri Uri2 = new("http://node2:9200");
	private static readonly Uri Uri3 = new("http://node3:9200");

	[Fact]
	public void RoundRobin_CyclesThroughNodes()
	{
		var pool = new NodePool([Uri1, Uri2, Uri3]);

		var selected = new List<Uri>();
		for (var i = 0; i < 6; i++)
			selected.Add(pool.SelectNode().Host);

		// Should cycle through all three nodes twice.
		selected.Should().HaveCount(6);
		selected.Distinct().Should().HaveCount(3);
		selected[0].Should().Be(selected[3]);
		selected[1].Should().Be(selected[4]);
		selected[2].Should().Be(selected[5]);
	}

	[Fact]
	public void SelectNode_SkipsDeadNode()
	{
		var pool = new NodePool([Uri1, Uri2, Uri3]);

		var node2 = pool.Nodes.First(n => n.Host == Uri2);
		pool.MarkDead(node2);

		// Select enough times to verify that node2 is never returned.
		var selected = new HashSet<Uri>();
		for (var i = 0; i < 20; i++)
			selected.Add(pool.SelectNode().Host);

		selected.Should().NotContain(Uri2);
		selected.Should().Contain(Uri1);
		selected.Should().Contain(Uri3);
	}

	[Fact]
	public void NodeMarkedDeadThenAlive_BecomesSelectable()
	{
		var pool = new NodePool([Uri1, Uri2]);

		var node1 = pool.Nodes.First(n => n.Host == Uri1);
		pool.MarkDead(node1);

		// While dead, only node2 is selected.
		for (var i = 0; i < 5; i++)
			pool.SelectNode().Host.Should().Be(Uri2);

		pool.MarkAlive(node1);

		// After revival, node1 should appear again.
		var selected = new HashSet<Uri>();
		for (var i = 0; i < 20; i++)
			selected.Add(pool.SelectNode().Host);

		selected.Should().Contain(Uri1);
	}

	[Fact]
	public void AllNodesDead_SelectsClosestToRevival()
	{
		var pool = new NodePool([Uri1, Uri2, Uri3]);

		// Mark all dead. node1 first, then node2, then node3.
		// node1 was marked dead first so it has the earliest DeadUntilTicks (closest to revival).
		var node1 = pool.Nodes.First(n => n.Host == Uri1);
		var node2 = pool.Nodes.First(n => n.Host == Uri2);
		var node3 = pool.Nodes.First(n => n.Host == Uri3);

		pool.MarkDead(node1);
		pool.MarkDead(node2);
		pool.MarkDead(node3);

		// When all are dead with same attempt count, they all have ~same DeadUntilTicks.
		// The first one marked should have the lowest DeadUntilTicks.
		var selected = pool.SelectNode();
		selected.Host.Should().Be(Uri1);
	}

	[Fact]
	public void Constructor_ThrowsOnEmptyNodes()
	{
		var act = () => new NodePool(Array.Empty<Node>());
		act.Should().Throw<ArgumentException>()
			.WithMessage("*At least one node*");
	}

	[Fact]
	public void Constructor_ThrowsOnNull()
	{
		var act = () => new NodePool((IEnumerable<Node>)null!);
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void SingleNode_AlwaysReturnsSameNode()
	{
		var pool = new NodePool([Uri1]);

		for (var i = 0; i < 10; i++)
			pool.SelectNode().Host.Should().Be(Uri1);
	}

	[Fact]
	public void SingleNode_ReturnedEvenWhenDead()
	{
		var pool = new NodePool([Uri1]);
		var node = pool.Nodes[0];
		pool.MarkDead(node);

		// Single-node fast path always returns the only node.
		pool.SelectNode().Host.Should().Be(Uri1);
	}

	[Fact]
	public void ThreadSafety_ParallelCallsDoNotCrash()
	{
		var pool = new NodePool([Uri1, Uri2, Uri3]);

		// Run many parallel operations that mix SelectNode, MarkDead, and MarkAlive.
		Parallel.For(0, 1000, i =>
		{
			var node = pool.SelectNode();
			node.Should().NotBeNull();

			if (i % 3 == 0)
				pool.MarkDead(node);
			else if (i % 3 == 1)
				pool.MarkAlive(node);
		});

		// No crash = success. Also verify we can still select a node.
		var selected = pool.SelectNode();
		selected.Should().NotBeNull();
	}
}
