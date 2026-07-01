using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Fixtures for the generator's request-body handling. Bodies that are not a plain object —
/// <c>$ref</c>-alias chains, JSON arrays, unions, and scalars — were previously dropped
/// (<c>GetBody =&gt; null</c>, an un-sendable request). These prove each shape now produces the
/// correct wire body through the production serializer.
/// </summary>
public class RequestBodySerializationTests : SerializationTestBase
{
	[Fact]
	public void Security_patch_body_serializes_as_json_patch_array()
	{
		// security.patch_roles' body is `array<PatchOperation>` — a JSON array, not an object.
		// The request carries it as a typed List<PatchOperation> Body (the endpoint serializes r.Body).
		PatchRolesSecurityRequest request = new()
		{
			Body =
			[
				new PatchOperation { Op = "add", Path = "/my_role", Value = Element(new { cluster_permissions = new[] { "cluster_all" } }) },
				new PatchOperation { Op = "remove", Path = "/old_role" },
			],
		};

		var root = Parse(Serialize(request.Body));

		root.ValueKind.Should().Be(JsonValueKind.Array);
		root.GetArrayLength().Should().Be(2);
		root[0].GetProperty("op").GetString().Should().Be("add");
		root[0].GetProperty("path").GetString().Should().Be("/my_role");
		root[0].GetProperty("value").GetProperty("cluster_permissions")[0].GetString().Should().Be("cluster_all");
		root[1].GetProperty("op").GetString().Should().Be("remove");
	}

	[Fact]
	public void Ism_put_policy_flattens_ref_alias_body()
	{
		// ism.put_policy's body is a $ref-alias chain (PutPolicyRequest -> PolicyEnvelope -> {policy}).
		// Following the chain flattens it, so the request carries a typed Policy field (not a raw body).
		PutPolicyIsmRequest request = new()
		{
			PolicyId = "rollover", // [JsonIgnore] path param — must not appear in the body
			Policy = new Policy { Description = "rollover after 7d", DefaultState = "hot" },
		};

		var root = Parse(Serialize(request));

		root.TryGetProperty("policy_id", out _).Should().BeFalse("policy_id is a URL param, not body");
		var policy = root.GetProperty("policy");
		policy.GetProperty("description").GetString().Should().Be("rollover after 7d");
		policy.GetProperty("default_state").GetString().Should().Be("hot");
	}

	[Fact]
	public void Search_relevance_union_body_is_sendable_raw_json()
	{
		// search_relevance.put_experiments' body is an anyOf of 3 experiment shapes with no clean
		// discriminator — modelled as a sendable raw JsonElement body (the caller supplies verbatim
		// JSON) rather than dropped.
		PutExperimentsSearchRelevanceRequest request = new()
		{
			Body = Parse("""{"querySetId":"qs1","type":"PAIRWISE_COMPARISON","size":10}"""),
		};

		var root = Parse(Serialize(request.Body));

		root.GetProperty("querySetId").GetString().Should().Be("qs1");
		root.GetProperty("type").GetString().Should().Be("PAIRWISE_COMPARISON");
		root.GetProperty("size").GetInt32().Should().Be(10);
	}

	[Fact]
	public void Create_snapshot_request_body_round_trips()
	{
		CreateSnapshotRequest request = new()
		{
			Repository = "backups", // [JsonIgnore] path param
			Snapshot = "snap-1",    // [JsonIgnore] path param
			Indices = ["logs-*", "metrics-*"],
			IgnoreUnavailable = true,
			IncludeGlobalState = false,
			Partial = false,
		};

		var root = AssertRoundTrips(request);

		root.TryGetProperty("repository", out _).Should().BeFalse("repository is a URL param");
		root.GetProperty("indices").GetArrayLength().Should().Be(2);
		root.GetProperty("ignore_unavailable").GetBoolean().Should().BeTrue();
		root.GetProperty("include_global_state").GetBoolean().Should().BeFalse();
	}

	[Fact]
	public void Restore_snapshot_request_body_round_trips()
	{
		RestoreSnapshotRequest request = new()
		{
			Repository = "backups",
			Snapshot = "snap-1",
			Indices = ["logs-2024"],
			RenamePattern = "logs-(.+)",
			RenameReplacement = "restored-logs-$1",
			IncludeAliases = false,
		};

		var root = AssertRoundTrips(request);

		root.GetProperty("rename_pattern").GetString().Should().Be("logs-(.+)");
		root.GetProperty("rename_replacement").GetString().Should().Be("restored-logs-$1");
		root.GetProperty("include_aliases").GetBoolean().Should().BeFalse();
	}
}
