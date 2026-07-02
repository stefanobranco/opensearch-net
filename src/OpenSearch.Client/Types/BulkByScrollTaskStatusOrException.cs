using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A per-slice entry in a bulk-by-scroll task's status: either the slice's
/// <see cref="BulkByScrollTaskStatus"/> or the <see cref="OpenSearch.Net.ErrorCause"/> that
/// failed it. The wire form has no discriminator; an object carrying <c>type</c> and
/// <c>reason</c> is an error.
/// </summary>
[JsonConverter(typeof(BulkByScrollTaskStatusOrExceptionConverter))]
public sealed class BulkByScrollTaskStatusOrException
{
	private BulkByScrollTaskStatusOrException(BulkByScrollTaskStatus? status, OpenSearch.Net.ErrorCause? exception)
	{
		Status = status;
		Exception = exception;
	}

	/// <summary>The slice status, or <c>null</c> when the slice failed.</summary>
	public BulkByScrollTaskStatus? Status { get; }

	/// <summary>The failure cause, or <c>null</c> when the slice succeeded.</summary>
	public OpenSearch.Net.ErrorCause? Exception { get; }

	public static BulkByScrollTaskStatusOrException OfStatus(BulkByScrollTaskStatus status) => new(status, null);
	public static BulkByScrollTaskStatusOrException OfException(OpenSearch.Net.ErrorCause exception) => new(null, exception);
}

public sealed class BulkByScrollTaskStatusOrExceptionConverter : JsonConverter<BulkByScrollTaskStatusOrException>
{
	public override BulkByScrollTaskStatusOrException Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using var doc = JsonDocument.ParseValue(ref reader);
		var root = doc.RootElement;
		if (root.TryGetProperty("type", out _) && root.TryGetProperty("reason", out _))
			return BulkByScrollTaskStatusOrException.OfException(
				root.Deserialize<OpenSearch.Net.ErrorCause>(options)!);
		return BulkByScrollTaskStatusOrException.OfStatus(
			root.Deserialize<BulkByScrollTaskStatus>(options)!);
	}

	public override void Write(Utf8JsonWriter writer, BulkByScrollTaskStatusOrException value, JsonSerializerOptions options)
	{
		if (value.Exception is { } exception)
			JsonSerializer.Serialize(writer, exception, options);
		else
			JsonSerializer.Serialize(writer, value.Status, options);
	}
}
