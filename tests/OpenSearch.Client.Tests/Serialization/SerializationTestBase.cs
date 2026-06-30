using System.Text;
using System.Text.Json;
using FluentAssertions;
using OpenSearch.Net;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Shared scaffolding for serialization fixtures. Serializes through the <em>production</em>
/// request/response serializer built by <see cref="OpenSearchClientSettings"/> — i.e. the real
/// <see cref="SystemTextJsonSerializer"/> with its <c>ContextProvider</c>, enum factory and error
/// converters wired exactly as a live client configures them — so a green fixture means the actual
/// serialization pipeline is correct, not just a hand-rolled options bag.
/// </summary>
public abstract class SerializationTestBase
{
	private static readonly OpenSearchClientSettings Settings =
		OpenSearchClientSettings.Create(new Uri("http://localhost:9200")).Build();

	/// <summary>The production request/response serializer.</summary>
	protected static readonly IOpenSearchSerializer Serializer = Settings.RequestResponseSerializer;

	/// <summary>The exact <see cref="JsonSerializerOptions"/> the production serializer uses.</summary>
	protected static readonly JsonSerializerOptions Json = Settings.RequestResponseOptions;

	protected static string Serialize<T>(T value)
	{
		using var stream = new MemoryStream();
		Serializer.Serialize(value, stream);
		return Encoding.UTF8.GetString(stream.ToArray());
	}

	protected static T? Deserialize<T>(string json)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
		return Serializer.Deserialize<T>(stream);
	}

	protected static JsonElement Parse(string json)
	{
		using var doc = JsonDocument.Parse(json);
		return doc.RootElement.Clone();
	}

	/// <summary>Wraps a CLR value as a <see cref="JsonElement"/> for properties typed as JsonElement.</summary>
	protected static JsonElement Element(object? value) => JsonSerializer.SerializeToElement(value, Json);

	/// <summary>
	/// Serializes, deserializes, and re-serializes <paramref name="value"/>, asserting the two
	/// JSON renderings are byte-identical (canonical round-trip), and returns the parsed JSON.
	/// </summary>
	protected static JsonElement AssertRoundTrips<T>(T value)
	{
		var first = Serialize(value);
		var back = Deserialize<T>(first);
		var second = Serialize(back);
		second.Should().Be(first, "deserialize→serialize should reproduce the original JSON");
		return Parse(first);
	}

	/// <summary>
	/// Asserts a field-keyed query shape — <c>{ "&lt;kind&gt;": { "&lt;field&gt;": { ... } } }</c> — and
	/// returns the inner value object for further assertions.
	/// </summary>
	protected static JsonElement AssertFieldKeyed(JsonElement root, string kind, string field)
	{
		root.TryGetProperty(kind, out var kindEl).Should().BeTrue($"expected a '{kind}' query wrapper");
		kindEl.TryGetProperty(field, out var fieldEl).Should().BeTrue($"expected field key '{field}' under '{kind}'");
		return fieldEl;
	}
}
