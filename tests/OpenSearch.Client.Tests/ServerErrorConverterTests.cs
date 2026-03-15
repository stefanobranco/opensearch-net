using System.Text.Json;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Net;
using Xunit;

namespace OpenSearch.Client.Tests;

public class ServerErrorConverterTests
{
	private static readonly JsonSerializerOptions s_options = new()
	{
		Converters = { new ServerErrorConverter() },
	};

	[Fact]
	public void Deserialize_StructuredError_ReturnsServerErrorWithTypeAndReason()
	{
		var json = """{"error":{"type":"index_not_found_exception","reason":"no such index"},"status":404}""";

		var result = JsonSerializer.Deserialize<ServerError>(json, s_options);

		result.Should().NotBeNull();
		result!.Status.Should().Be(404);
		result.Error.Should().NotBeNull();
		result.Error!.Type.Should().Be("index_not_found_exception");
		result.Error.Reason.Should().Be("no such index");
	}

	[Fact]
	public void Deserialize_StringError_ReturnsServerErrorWithReason()
	{
		var json = """{"error":"some error","status":500}""";

		var result = JsonSerializer.Deserialize<ServerError>(json, s_options);

		result.Should().NotBeNull();
		result!.Status.Should().Be(500);
		result.Error.Should().NotBeNull();
		result.Error!.Type.Should().BeNull();
		result.Error.Reason.Should().Be("some error");
	}

	[Fact]
	public void Deserialize_WithCausedBy_ReturnsNestedError()
	{
		var json = """
		{
			"error": {
				"type": "search_phase_execution_exception",
				"reason": "all shards failed",
				"caused_by": {
					"type": "query_shard_exception",
					"reason": "No mapping found for [timestamp]"
				}
			},
			"status": 503
		}
		""";

		var result = JsonSerializer.Deserialize<ServerError>(json, s_options);

		result.Should().NotBeNull();
		result!.Status.Should().Be(503);
		result.Error.Should().NotBeNull();
		result.Error!.Type.Should().Be("search_phase_execution_exception");
		result.Error.Reason.Should().Be("all shards failed");
		result.Error.CausedBy.Should().NotBeNull();
		result.Error.CausedBy!.Type.Should().Be("query_shard_exception");
		result.Error.CausedBy.Reason.Should().Be("No mapping found for [timestamp]");
	}

	[Fact]
	public void Deserialize_Null_ReturnsNull()
	{
		var json = "null";

		var result = JsonSerializer.Deserialize<ServerError>(json, s_options);

		result.Should().BeNull();
	}

	[Fact]
	public void Deserialize_StructuredError_WithUnknownPropertiesInError()
	{
		// The "status" field inside the error object is now skipped (not a field on ErrorCause)
		var json = """{"error":{"type":"mapper_parsing_exception","reason":"failed to parse","status":400},"status":400}""";

		var result = JsonSerializer.Deserialize<ServerError>(json, s_options);

		result.Should().NotBeNull();
		result!.Status.Should().Be(400);
		result.Error.Should().NotBeNull();
		result.Error!.Type.Should().Be("mapper_parsing_exception");
		result.Error.Reason.Should().Be("failed to parse");
	}

	[Fact]
	public void Serialize_StructuredError_RoundTrips()
	{
		var original = new ServerError
		{
			Status = 404,
			Error = new ErrorCause
			{
				Type = "index_not_found_exception",
				Reason = "no such index",
			}
		};

		var json = JsonSerializer.Serialize(original, s_options);
		var result = JsonSerializer.Deserialize<ServerError>(json, s_options);

		result.Should().NotBeNull();
		result!.Status.Should().Be(404);
		result.Error.Should().NotBeNull();
		result.Error!.Type.Should().Be("index_not_found_exception");
		result.Error.Reason.Should().Be("no such index");
	}

	[Fact]
	public void Serialize_WithCausedBy_RoundTrips()
	{
		var original = new ServerError
		{
			Status = 500,
			Error = new ErrorCause
			{
				Type = "outer_exception",
				Reason = "outer reason",
				CausedBy = new ErrorCause
				{
					Type = "inner_exception",
					Reason = "inner reason",
				}
			}
		};

		var json = JsonSerializer.Serialize(original, s_options);
		var result = JsonSerializer.Deserialize<ServerError>(json, s_options);

		result.Should().NotBeNull();
		result!.Error.Should().NotBeNull();
		result.Error!.CausedBy.Should().NotBeNull();
		result.Error.CausedBy!.Type.Should().Be("inner_exception");
		result.Error.CausedBy.Reason.Should().Be("inner reason");
	}

	[Fact]
	public void Deserialize_WithUnknownProperties_IgnoresThem()
	{
		var json = """{"error":{"type":"test","reason":"msg","resource.type":"index","index":"my_index"},"status":404}""";

		var result = JsonSerializer.Deserialize<ServerError>(json, s_options);

		result.Should().NotBeNull();
		result!.Error.Should().NotBeNull();
		result.Error!.Type.Should().Be("test");
		result.Error.Reason.Should().Be("msg");
	}

	[Fact]
	public void Deserialize_WithRootCause_ParsesArray()
	{
		var json = """
		{
			"error": {
				"type": "search_phase_execution_exception",
				"reason": "all shards failed",
				"root_cause": [
					{ "type": "parsing_exception", "reason": "Bad query" },
					{ "type": "parsing_exception", "reason": "Another bad query" }
				]
			},
			"status": 400
		}
		""";

		var result = JsonSerializer.Deserialize<ServerError>(json, s_options);

		result.Should().NotBeNull();
		result!.Error.Should().NotBeNull();
		result.Error!.RootCause.Should().NotBeNull();
		result.Error.RootCause.Should().HaveCount(2);
		result.Error.RootCause![0].Type.Should().Be("parsing_exception");
		result.Error.RootCause[0].Reason.Should().Be("Bad query");
		result.Error.RootCause[1].Reason.Should().Be("Another bad query");
	}

	[Fact]
	public void Deserialize_WithStackTrace_Parsed()
	{
		var json = """{"error":{"type":"test","reason":"msg","stack_trace":"java.lang.Exception\n\tat Foo.bar()"},"status":500}""";

		var result = JsonSerializer.Deserialize<ServerError>(json, s_options);

		result.Should().NotBeNull();
		result!.Error.Should().NotBeNull();
		result.Error!.StackTrace.Should().Contain("java.lang.Exception");
	}

	[Fact]
	public void ServerError_ToString_FormatsCorrectly()
	{
		var error = new ServerError
		{
			Status = 404,
			Error = new ErrorCause
			{
				Type = "index_not_found_exception",
				Reason = "no such index",
			}
		};

		var str = error.ToString();
		str.Should().Contain("ServerError: 404");
		str.Should().Contain("index_not_found_exception");
	}

	[Fact]
	public void ErrorCause_ToString_FormatsCorrectly()
	{
		var cause = new ErrorCause
		{
			Type = "parsing_exception",
			Reason = "Bad query",
		};

		var str = cause.ToString();
		str.Should().Contain("Type: parsing_exception");
		str.Should().Contain("Reason: \"Bad query\"");
	}
}
