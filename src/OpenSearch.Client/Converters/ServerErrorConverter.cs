using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Represents a structured error returned by OpenSearch.
/// </summary>
public sealed class ServerError
{
	/// <summary>
	/// The error type (e.g., <c>"index_not_found_exception"</c>).
	/// </summary>
	public string? Type { get; set; }

	/// <summary>
	/// A human-readable reason for the error.
	/// </summary>
	public string? Reason { get; set; }

	/// <summary>
	/// The HTTP status code associated with this error, if provided in the error body.
	/// </summary>
	public int? Status { get; set; }

	/// <summary>
	/// The underlying cause of the error, if available.
	/// </summary>
	public ServerError? CausedBy { get; set; }

	/// <inheritdoc />
	public override string ToString() =>
		$"Type: {Type ?? "(none)"}, Reason: \"{Reason ?? "(none)"}\"";
}

/// <summary>
/// Represents the top-level error response envelope returned by OpenSearch.
/// The <c>error</c> field may be a structured object or a plain string.
/// </summary>
public sealed class ErrorResponse
{
	/// <summary>
	/// The structured error, if the server returned an object.
	/// </summary>
	public ServerError? Error { get; set; }

	/// <summary>
	/// The HTTP status code from the response envelope.
	/// </summary>
	public int Status { get; set; }
}

/// <summary>
/// Handles deserialization of OpenSearch error responses, which can appear in two forms:
/// <list type="bullet">
///   <item><c>{ "error": { "type": "...", "reason": "..." }, "status": 404 }</c></item>
///   <item><c>{ "error": "string message", "status": 404 }</c></item>
/// </list>
/// </summary>
public sealed class ServerErrorConverter : JsonConverter<ErrorResponse>
{
	/// <inheritdoc />
	public override ErrorResponse? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
			return null;

		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException($"Expected start of object for {nameof(ErrorResponse)}, got {reader.TokenType}.");

		var response = new ErrorResponse();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName)
				continue;

			var propertyName = reader.GetString();
			reader.Read(); // Move to value

			switch (propertyName)
			{
				case "error":
					response.Error = ReadError(ref reader, options);
					break;

				case "status":
					response.Status = reader.GetInt32();
					break;

				default:
					reader.Skip();
					break;
			}
		}

		return response;
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, ErrorResponse value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();

		if (value.Error is not null)
		{
			writer.WritePropertyName("error");
			WriteServerError(writer, value.Error, options);
		}

		writer.WriteNumber("status", value.Status);
		writer.WriteEndObject();
	}

	private static ServerError? ReadError(ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.String:
			{
				// Plain string error: { "error": "some message" }
				var message = reader.GetString();
				return new ServerError { Reason = message };
			}

			case JsonTokenType.StartObject:
			{
				return ReadServerErrorObject(ref reader, options);
			}

			default:
				reader.Skip();
				return null;
		}
	}

	private static ServerError ReadServerErrorObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		var error = new ServerError();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName)
				continue;

			var prop = reader.GetString();
			reader.Read();

			switch (prop)
			{
				case "type":
					error.Type = reader.GetString();
					break;

				case "reason":
					error.Reason = reader.GetString();
					break;

				case "status":
					error.Status = reader.GetInt32();
					break;

				case "caused_by":
					error.CausedBy = reader.TokenType == JsonTokenType.StartObject
						? ReadServerErrorObject(ref reader, options)
						: null;
					break;

				default:
					reader.Skip();
					break;
			}
		}

		return error;
	}

	private static void WriteServerError(Utf8JsonWriter writer, ServerError error, JsonSerializerOptions options)
	{
		writer.WriteStartObject();

		if (error.Type is not null)
			writer.WriteString("type", error.Type);

		if (error.Reason is not null)
			writer.WriteString("reason", error.Reason);

		if (error.Status is not null)
			writer.WriteNumber("status", error.Status.Value);

		if (error.CausedBy is not null)
		{
			writer.WritePropertyName("caused_by");
			WriteServerError(writer, error.CausedBy, options);
		}

		writer.WriteEndObject();
	}
}
