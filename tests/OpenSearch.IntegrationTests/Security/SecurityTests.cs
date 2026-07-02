using FluentAssertions;
using OpenSearch.IntegrationTests.Infrastructure;
using OpenSearch.Client;

namespace OpenSearch.IntegrationTests.Security;

/// <summary>
/// Runs against the security-enabled cluster (HTTPS + basic auth + self-signed demo certs), so
/// every test here also exercises the TLS and authentication transport paths that the plain
/// cluster jobs never touch.
/// </summary>
public class SecurityTests : SecuredIntegrationTestBase
{
	[SkipIfNoSecuredCluster]
	public void AuthInfoReturnsAuthenticatedIdentity()
	{
		var response = Client.Security.Authinfo(new AuthinfoSecurityRequest());

		response.IsValid.Should().BeTrue();
		response.UserName.Should().Be("admin");
		response.Roles.Should().NotBeNullOrEmpty();
	}

	[SkipIfNoSecuredCluster]
	public void WrongPasswordIsRejectedWith401()
	{
		using var badClient = new OpenSearchClient(CreateSettings(password: "Wrong#Password1"));

		var response = badClient.Security.Authinfo(new AuthinfoSecurityRequest());

		response.IsValid.Should().BeFalse();
		response.ApiCall!.HttpStatusCode.Should().Be(401);
	}

	[SkipIfNoSecuredCluster]
	public void UserLifecycle_CreateGetDelete()
	{
		var username = $"it-user-{Guid.NewGuid():N}";

		var create = Client.Security.CreateUser(new CreateUserSecurityRequest
		{
			Username = username,
			Password = "UserLife#2026x",
			BackendRoles = ["it-backend"],
			Attributes = new Dictionary<string, string> { ["team"] = "integration" },
		});
		create.IsValid.Should().BeTrue(create.ApiCall?.ToString());

		try
		{
			var get = Client.Security.GetUser(new GetUserSecurityRequest { Username = username });
			get.Should().ContainKey(username);
			get[username].BackendRoles.Should().Contain("it-backend");
			get[username].Attributes.Should().ContainKey("team");
		}
		finally
		{
			var delete = Client.Security.DeleteUser(new DeleteUserSecurityRequest { Username = username });
			delete.IsValid.Should().BeTrue();
		}

		var after = Client.Security.GetUser(new GetUserSecurityRequest { Username = username });
		after.Should().NotContainKey(username);
	}

	[SkipIfNoSecuredCluster]
	public void RoleLifecycle_CreateMapGetDelete()
	{
		var roleName = $"it-role-{Guid.NewGuid():N}";

		var createRole = Client.Security.CreateRole(new CreateRoleSecurityRequest
		{
			Role = roleName,
			Description = "integration test role",
			ClusterPermissions = ["cluster_monitor"],
			IndexPermissions =
			[
				new IndexPermission
				{
					IndexPatterns = ["sectest-*"],
					AllowedActions = ["read"],
				}
			],
		});
		createRole.IsValid.Should().BeTrue(createRole.ApiCall?.ToString());

		try
		{
			var getRole = Client.Security.GetRole(new GetRoleSecurityRequest { Role = roleName });
			getRole.Should().ContainKey(roleName);
			getRole[roleName].ClusterPermissions.Should().Contain("cluster_monitor");
			getRole[roleName].IndexPermissions.Should().NotBeNullOrEmpty();

			var mapping = Client.Security.CreateRoleMapping(new CreateRoleMappingSecurityRequest
			{
				Role = roleName,
				BackendRoles = ["it-backend"],
			});
			mapping.IsValid.Should().BeTrue(mapping.ApiCall?.ToString());

			var getMapping = Client.Security.GetRoleMapping(new GetRoleMappingSecurityRequest { Role = roleName });
			getMapping.Should().ContainKey(roleName);
		}
		finally
		{
			Client.Security.DeleteRoleMapping(new DeleteRoleMappingSecurityRequest { Role = roleName });
			var deleteRole = Client.Security.DeleteRole(new DeleteRoleSecurityRequest { Role = roleName });
			deleteRole.IsValid.Should().BeTrue();
		}
	}

	[SkipIfNoSecuredCluster]
	public void DocumentRoundTripOverTls()
	{
		var index = UniqueIndex();
		Client.Indices.Create(new CreateIndexRequest { Index = index });
		Client.Core.Index(new IndexRequest
		{
			Index = index,
			Id = "doc-1",
			Refresh = "true",
			Body = new SecureDoc { Title = "over tls", Count = 1 },
		});

		var search = Client.Search<SecureDoc>(s => s
			.Index(index)
			.Query(q => q.Match(f => f.Title!, m => m.Query("tls"))));

		search.IsValid.Should().BeTrue();
		search.Documents().Should().ContainSingle(d => d.Title == "over tls");
	}

	private sealed class SecureDoc
	{
		public string? Title { get; set; }
		public int? Count { get; set; }
	}
}
