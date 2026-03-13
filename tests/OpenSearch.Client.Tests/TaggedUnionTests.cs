using FluentAssertions;
using OpenSearch.Client;
using Xunit;

namespace OpenSearch.Client.Tests;

public class TaggedUnionTests
{
	private enum TestKind
	{
		StringVariant,
		IntVariant,
	}

	private sealed class TestUnion : TaggedUnion<TestKind, object>
	{
		public TestUnion(TestKind kind, object value) : base(kind, value) { }

		public static TestUnion FromString(string value) => new(TestKind.StringVariant, value);
		public static TestUnion FromInt(int value) => new(TestKind.IntVariant, value);
	}

	[Fact]
	public void Kind_ReturnsCorrectKind_ForStringVariant()
	{
		var union = TestUnion.FromString("hello");

		union.Kind.Should().Be(TestKind.StringVariant);
	}

	[Fact]
	public void Kind_ReturnsCorrectKind_ForIntVariant()
	{
		var union = TestUnion.FromInt(42);

		union.Kind.Should().Be(TestKind.IntVariant);
	}

	[Fact]
	public void Get_ReturnsTypedValue_ForStringVariant()
	{
		var union = TestUnion.FromString("world");

		union.Get<string>().Should().Be("world");
	}

	[Fact]
	public void Get_ReturnsTypedValue_ForIntVariant()
	{
		var union = TestUnion.FromInt(99);

		union.Get<int>().Should().Be(99);
	}

	[Fact]
	public void Is_ReturnsTrue_ForMatchingKind()
	{
		var union = TestUnion.FromString("test");

		union.Is(TestKind.StringVariant).Should().BeTrue();
	}

	[Fact]
	public void Is_ReturnsFalse_ForNonMatchingKind()
	{
		var union = TestUnion.FromString("test");

		union.Is(TestKind.IntVariant).Should().BeFalse();
	}

	[Fact]
	public void Get_ThrowsInvalidOperationException_ForWrongType()
	{
		var union = TestUnion.FromString("text");

		var act = () => union.Get<int>();

		act.Should().Throw<InvalidOperationException>()
			.Which.Message.Should().Contain("Int32");
	}

	[Fact]
	public void ToString_IncludesKindAndValue()
	{
		var union = TestUnion.FromString("hello");

		union.ToString().Should().Be("StringVariant: hello");
	}

	[Fact]
	public void ToString_IncludesKindAndValue_ForInt()
	{
		var union = TestUnion.FromInt(42);

		union.ToString().Should().Be("IntVariant: 42");
	}
}
