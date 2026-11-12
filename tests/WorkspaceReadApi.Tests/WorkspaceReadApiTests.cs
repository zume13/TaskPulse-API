using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api;
using Api.Controllers;
using Api.Middleware;
using Application;
using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Workspace = Domain.Aggregates.Collaboration.Workspaces.Workspace.Workspace;
using IWorkspaceRepository = Domain.Aggregates.Collaboration.Workspaces.Workspace.IWorkspaceRepository;

namespace WorkspaceReadApi.Tests;

// Real routing, JWT validation, authorization, controller, MediatR, validation and JSON.
// Only persistence is substituted; these tests do not connect to PostgreSQL or run the seeder.
public class WorkspaceReadApiTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Anonymous_requests_are_unauthorized(bool detail)
    {
        using var api = new TestApi();
        var response = await api.Client.GetAsync(Path(detail));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, api.Repository.Reads);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Missing_malformed_or_empty_subject_is_unauthorized(string? subject)
    {
        using var api = new TestApi();
        api.Authenticate(subject);
        foreach (var path in new[] { Path(false), Path(true) })
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync(path)).StatusCode);
        Assert.Equal(0, api.Repository.Reads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Invalid_signature_is_rejected(bool detail)
    {
        using var api = new TestApi();
        api.Authenticate(Guid.NewGuid().ToString(), invalidSignature: true);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync(Path(detail))).StatusCode);
        Assert.Equal(0, api.Repository.Reads);
    }

    [Theory]
    [InlineData("Employee")]
    [InlineData("Admin")]
    public async Task No_memberships_returns_an_empty_array(string globalRole)
    {
        var otherWorkspace = CreateWorkspace(Guid.NewGuid(), WorkspaceRole.Owner);
        using var api = new TestApi(otherWorkspace);
        api.Authenticate(Guid.NewGuid().ToString(), globalRole);
        var response = await api.Client.GetAsync(Path(false));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await Json(response)).EnumerateArray());
    }

    [Fact]
    public async Task List_returns_only_memberships_including_archived_with_correct_counts_and_roles()
    {
        var userId = Guid.NewGuid();
        var owned = CreateWorkspace(userId, WorkspaceRole.Owner);
        var extraMember = Guid.NewGuid();
        Assert.True(owned.InviteMember(extraMember, userId, WorkspaceRole.Member, DateTime.UtcNow).IsSuccess);
        Assert.True(owned.AcceptInvitation(owned.Invitations.Last().Id, extraMember, DateTime.UtcNow).IsSuccess);
        var administered = CreateWorkspace(userId, WorkspaceRole.Admin);
        var archived = CreateWorkspace(userId, WorkspaceRole.Member);
        Assert.True(archived.Archive(archived.CreatedBy, DateTime.UtcNow).IsSuccess);
        var privateWorkspace = CreateWorkspace(Guid.NewGuid(), WorkspaceRole.Owner);
        using var api = new TestApi(owned, administered, archived, privateWorkspace);
        api.Authenticate(userId.ToString());

        // A client-supplied userId must never replace the verified subject.
        var response = await api.Client.GetAsync(Path(false) + "?userId=" + privateWorkspace.CreatedBy);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rows = (await Json(response)).EnumerateArray().ToArray();
        Assert.Equal(3, rows.Length);
        AssertRow(rows.Single(row => row.GetProperty("id").GetGuid() == owned.Id), owned, "Owner");
        AssertRow(rows.Single(row => row.GetProperty("id").GetGuid() == administered.Id), administered, "Admin");
        AssertRow(rows.Single(row => row.GetProperty("id").GetGuid() == archived.Id), archived, "Member");
        Assert.DoesNotContain(rows, row => row.GetProperty("id").GetGuid() == privateWorkspace.Id);
        Assert.Equal(userId, api.Repository.LastUserId);
        Assert.Equal(1, api.Repository.Reads);
    }

    [Theory]
    [InlineData(WorkspaceRole.Owner, false)]
    [InlineData(WorkspaceRole.Admin, false)]
    [InlineData(WorkspaceRole.Member, false)]
    [InlineData(WorkspaceRole.Owner, true)]
    [InlineData(WorkspaceRole.Admin, true)]
    [InlineData(WorkspaceRole.Member, true)]
    public async Task Detail_supports_every_membership_role_and_archive_state(WorkspaceRole role, bool archived)
    {
        var userId = Guid.NewGuid();
        var workspace = CreateWorkspace(userId, role);
        if (archived) Assert.True(workspace.Archive(workspace.CreatedBy, DateTime.UtcNow).IsSuccess);
        using var api = new TestApi(workspace);
        // A conflicting global role cannot change the persisted workspace role.
        api.Authenticate(userId.ToString(), globalRole: "Admin");
        var response = await api.Client.GetAsync($"/api/workspaces/{workspace.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertRow(await Json(response), workspace, role.ToString());
    }

    [Theory]
    [InlineData("Employee")]
    [InlineData("Admin")]
    public async Task Nonmember_and_missing_workspace_have_the_same_not_found_response(string globalRole)
    {
        var privateWorkspace = CreateWorkspace(Guid.NewGuid(), WorkspaceRole.Owner);
        using var api = new TestApi(privateWorkspace);
        api.Authenticate(Guid.NewGuid().ToString(), globalRole);
        var denied = await api.Client.GetAsync($"/api/workspaces/{privateWorkspace.Id}");
        var missing = await api.Client.GetAsync(Path(true));
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var deniedProblem = await Json(denied);
        var missingProblem = await Json(missing);
        foreach (var field in new[] { "status", "title", "detail" })
            Assert.Equal(missingProblem.GetProperty(field).ToString(), deniedProblem.GetProperty(field).ToString());
        Assert.DoesNotContain(privateWorkspace.WorkspaceName.Value, await denied.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("not-a-guid", HttpStatusCode.NotFound)]
    [InlineData("00000000-0000-0000-0000-000000000000", HttpStatusCode.BadRequest)]
    public async Task Invalid_workspace_ids_are_rejected_without_repository_access(string id, HttpStatusCode status)
    {
        using var api = new TestApi();
        api.Authenticate(Guid.NewGuid().ToString());
        Assert.Equal(status, (await api.Client.GetAsync($"/api/workspaces/{id}")).StatusCode);
        Assert.Equal(0, api.Repository.Reads);
    }

    private static string Path(bool detail) => detail ? $"/api/workspaces/{Guid.NewGuid()}" : "/api/workspaces";

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static void AssertRow(JsonElement row, Workspace workspace, string role)
    {
        Assert.Equal(workspace.Id, row.GetProperty("id").GetGuid());
        Assert.Equal(workspace.WorkspaceName.Value, row.GetProperty("name").GetString());
        Assert.Equal(role, row.GetProperty("myRole").GetString());
        Assert.Equal(workspace.Members.Count, row.GetProperty("memberCount").GetInt32());
        Assert.Equal(workspace.IsArchived, row.GetProperty("isArchived").GetBoolean());
        Assert.Equal(5, row.EnumerateObject().Count()); // No membership roster or unrelated fields.
    }

    private static Workspace CreateWorkspace(Guid userId, WorkspaceRole role)
    {
        var ownerId = role == WorkspaceRole.Owner ? userId : Guid.NewGuid();
        var workspace = Workspace.Create("Workspace " + Guid.NewGuid().ToString("N"), ownerId).value;
        var invitee = role == WorkspaceRole.Owner ? Guid.NewGuid() : userId;
        var invitedRole = role == WorkspaceRole.Owner ? WorkspaceRole.Member : role;
        Assert.True(workspace.InviteMember(invitee, ownerId, invitedRole, DateTime.UtcNow).IsSuccess);
        Assert.True(workspace.AcceptInvitation(workspace.Invitations.Single().Id, invitee, DateTime.UtcNow).IsSuccess);
        return workspace;
    }

    private sealed class TestApi : IDisposable
    {
        // Ephemeral test-only signing key. No credentials or runtime config are read.
        private readonly string secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        private readonly TestServer server;
        public HttpClient Client { get; }
        public FakeWorkspaceRepository Repository { get; }

        public TestApi(params Workspace[] workspaces)
        {
            Repository = new FakeWorkspaceRepository(workspaces);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = secret,
                ["Jwt:Issuer"] = "workspace-tests",
                ["Jwt:Audience"] = "workspace-tests"
            }).Build();
            server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddApplication();
                    services.AddPresentation(configuration);
                    services.AddSingleton<IWorkspaceRepository>(Repository);
                    services.AddExceptionHandler<GlobalExceptionHandler>();
                    services.AddProblemDetails();
                    services.AddControllers().AddApplicationPart(typeof(WorkspacesController).Assembly)
                        .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
                            new System.Text.Json.Serialization.JsonStringEnumConverter()));
                })
                .Configure(app =>
                {
                    app.UseExceptionHandler();
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }));
            Client = server.CreateClient();
        }

        public void Authenticate(string? subject, string globalRole = "Employee", bool invalidSignature = false)
        {
            var claims = new List<Claim> { new("role", globalRole) };
            if (subject is not null) claims.Add(new Claim("sub", subject));
            var signingSecret = invalidSignature ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)) : secret;
            var token = new JwtSecurityToken("workspace-tests", "workspace-tests", claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingSecret)), SecurityAlgorithms.HmacSha256));
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        }

        public void Dispose()
        {
            Client.Dispose();
            server.Dispose();
        }
    }

    private sealed class FakeWorkspaceRepository(Workspace[] workspaces) : IWorkspaceRepository
    {
        public int Reads { get; private set; }
        public Guid LastUserId { get; private set; }

        public Task<List<Workspace>> GetByMemberIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            Reads++;
            LastUserId = userId;
            return Task.FromResult(workspaces.Where(workspace => workspace.IsMember(userId)).ToList());
        }

        public Task<Workspace?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult(workspaces.FirstOrDefault(workspace => workspace.Id == id));
        }

        public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Workspace>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Workspace workspace, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Update(Workspace workspace) => throw new NotSupportedException();
        public void Remove(Workspace workspace) => throw new NotSupportedException();
        public Task<Workspace?> GetByInvitationIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Workspace?> GetByIdWithInvitationsAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Workspace?> GetByIdWithMembersAndInvitationsAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
