using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// The <c>filters</c> of a <c>filters</c> aggregation: either named filters (a map of bucket name to
/// query, producing keyed result buckets) or anonymous filters (a query list, producing ordered result
/// buckets). Construct implicitly, e.g.
/// <c>Filters = new Dictionary&lt;string, QueryContainer&gt; { ["errors"] = ..., ["warnings"] = ... }</c>.
/// </summary>
[JsonConverter(typeof(BucketsQueryContainerConverter))]
public sealed class BucketsQueryContainer
{
	/// <summary>The named-filters form, or <c>null</c> when anonymous filters are used.</summary>
	public IReadOnlyDictionary<string, QueryContainer>? Named { get; }

	/// <summary>The anonymous-filters form, or <c>null</c> when named filters are used.</summary>
	public IReadOnlyList<QueryContainer>? Anonymous { get; }

	private BucketsQueryContainer(IReadOnlyDictionary<string, QueryContainer> named) => Named = named;
	private BucketsQueryContainer(IReadOnlyList<QueryContainer> anonymous) => Anonymous = anonymous;

	public static implicit operator BucketsQueryContainer(Dictionary<string, QueryContainer> named) => new(named);
	public static implicit operator BucketsQueryContainer(QueryContainer[] anonymous) => new(anonymous);
	public static implicit operator BucketsQueryContainer(List<QueryContainer> anonymous) => new(anonymous);
}

public sealed class BucketsQueryContainerConverter : JsonConverter<BucketsQueryContainer>
{
	public override BucketsQueryContainer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.Null:
				return null;
			case JsonTokenType.StartObject:
				return JsonSerializer.Deserialize<Dictionary<string, QueryContainer>>(ref reader, options)!;
			case JsonTokenType.StartArray:
				return JsonSerializer.Deserialize<List<QueryContainer>>(ref reader, options)!;
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for BucketsQueryContainer.");
		}
	}

	public override void Write(Utf8JsonWriter writer, BucketsQueryContainer value, JsonSerializerOptions options)
	{
		if (value.Named is not null)
			JsonSerializer.Serialize(writer, value.Named, options);
		else
			JsonSerializer.Serialize(writer, value.Anonymous, options);
	}
}
