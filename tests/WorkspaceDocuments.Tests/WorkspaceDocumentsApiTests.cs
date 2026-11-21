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
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.WorkspaceDocuments;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Workspace = Domain.Aggregates.Collaboration.Workspaces.Workspace.Workspace;

namespace WorkspaceDocuments.Tests;

// Real production JWT validation, routing, controllers, MediatR, validation and serialization.
// Persistence/clock are substituted. No Program startup, Google calls or PostgreSQL connection.
public class WorkspaceDocumentsApiTests
{
    private const string FileId = "AbCdEfGhIjKlMnOpQrStUvWxYz1234567890_-";

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Anonymous_requests_require_authentication(string method)
    {
        using var api = new TestApi();
        using var request = api.Request(method);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.SendAsync(request, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(0, api.Persistence.Saves);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Signed_tokens_with_invalid_subject_are_unauthorized(string? subject)
    {
        using var api = new TestApi();
        api.Authenticate(subject);
        foreach (var method in new[] { "GET", "POST", "PATCH", "DELETE" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.SendAsync(api.Request(method), TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(0, api.Persistence.Saves);
    }

    [Fact]
    public async Task Invalid_signature_is_rejected()
    {
        using var api = new TestApi();
        api.Authenticate(api.Creator.ToString(), invalidSignature: true);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync(api.Collection, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Member_lists_only_this_workspaces_records_including_when_archived(bool archived)
    {
        using var api = new TestApi();
        if (archived) api.Archive();
        api.Authenticate(api.OtherMember.ToString());
        api.Documents.Items.Add(CreateDocument(Guid.NewGuid(), api.Creator));
        var response = await api.Client.GetAsync(api.Collection, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rows = (await Json(response)).EnumerateArray().ToArray();
        Assert.Single(rows);
        Assert.Equal(api.Document.Id, rows[0].GetProperty("id").GetGuid());
        Assert.Equal(FileId, rows[0].GetProperty("googleFileId").GetString());
        Assert.Equal("Manual", rows[0].GetProperty("sourceType").GetString());
    }

    [Fact]
    public async Task Empty_library_returns_empty_array()
    {
        using var api = new TestApi();
        api.Documents.Items.Clear();
        api.Authenticate(api.Creator.ToString());
        Assert.Empty((await Json(await api.Client.GetAsync(api.Collection, TestContext.Current.CancellationToken))).EnumerateArray());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Global_admin_nonmember_cannot_access_workspace(string method)
    {
        using var api = new TestApi();
        api.Authenticate(Guid.NewGuid().ToString(), "Admin");
        var denied = await api.Client.SendAsync(api.Request(method), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        Assert.Equal("Workspace.NotFound", (await Json(denied)).GetProperty("title").GetString());
        Assert.Equal(0, api.Persistence.Saves);
    }

    [Fact]
    public async Task Missing_and_inaccessible_workspaces_have_equivalent_errors()
    {
        using var api = new TestApi();
        api.Authenticate(Guid.NewGuid().ToString());
        var denied = await Json(await api.Client.GetAsync(api.Collection, TestContext.Current.CancellationToken));
        var missing = await Json(await api.Client.GetAsync($"/api/workspaces/{Guid.NewGuid()}/documents", TestContext.Current.CancellationToken));
        foreach (var key in new[] { "status", "title", "detail" })
            Assert.Equal(denied.GetProperty(key).ToString(), missing.GetProperty(key).ToString());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Removed_creator_loses_access_even_with_global_admin_role(string method)
    {
        using var api = new TestApi();
        Assert.True(api.Workspace.RemoveMember(api.Owner, api.Creator, TestClock.Now.UtcDateTime).IsSuccess);
        api.Authenticate(api.Creator.ToString(), "Admin");
        var response = await api.Client.SendAsync(api.Request(method), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Workspace.NotFound", (await Json(response)).GetProperty("title").GetString());
        Assert.Equal(0, api.Persistence.Saves);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Archived_workspace_rejects_every_write(string method)
    {
        using var api = new TestApi();
        api.Archive();
        api.Authenticate(api.Creator.ToString());
        var response = await api.Client.SendAsync(api.Request(method), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Workspace.DeletedOrArchived", (await Json(response)).GetProperty("title").GetString());
        Assert.Equal(0, api.Persistence.Saves);
        Assert.Single(api.Documents.Items);
    }

    [Theory]
    [InlineData("Manual", null)]
    [InlineData("Manual", WorkspaceDocument.GoogleDocsMimeType)]
    [InlineData("Uploaded", WorkspaceDocument.DocxMimeType)]
    public async Task Any_member_can_register_supported_metadata_with_trusted_audit_fields(string source, string? mime)
    {
        using var api = new TestApi();
        api.Authenticate(api.OtherMember.ToString());
        var response = await api.Client.PostAsJsonAsync(api.Collection, new
        {
            title = "  Shared notes  ", googleFileId = "ZyXwVuTsRqPoNmLkJiHgFeDcBa0987654321_-",
            sourceType = source, mimeType = mime,
            createdByUserId = Guid.NewGuid(), createdAtUtc = "1900-01-01T00:00:00Z"
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.Equal(api.Collection, location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString);
        var row = await Json(response);
        Assert.Equal("Shared notes", row.GetProperty("title").GetString());
        Assert.Equal(api.OtherMember, row.GetProperty("createdByUserId").GetGuid());
        Assert.Equal(TestClock.Now.UtcDateTime, row.GetProperty("createdAtUtc").GetDateTime());
        Assert.Equal(TestClock.Now.UtcDateTime, row.GetProperty("updatedAtUtc").GetDateTime());
        Assert.Equal(JsonValueKind.Null, row.GetProperty("updatedByUserId").ValueKind);
        Assert.Equal(1, api.Persistence.Saves);
    }

    [Theory]
    [InlineData("", "Manual", null)]
    [InlineData("short", "Manual", null)]
    [InlineData("https://docs.google.com/document/d/SomeSyntheticId/edit", "Manual", null)]
    [InlineData("AbCdEfGhIjKl?token=x", "Manual", null)]
    [InlineData(FileId, "Uploaded", null)]
    [InlineData(FileId, "Uploaded", "application/pdf")]
    [InlineData(FileId, "Manual", WorkspaceDocument.DocxMimeType)]
    [InlineData(FileId, "InvalidSource", null)]
    public async Task Invalid_metadata_is_rejected(string id, string source, string? mime)
    {
        using var api = new TestApi();
        api.Authenticate(api.Creator.ToString());
        var response = await api.Client.PostAsJsonAsync(api.Collection,
            new { title = "Notes", googleFileId = id, sourceType = source, mimeType = mime }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, api.Persistence.Saves);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Empty_titles_are_rejected_on_registration_and_rename(string? title)
    {
        using var api = new TestApi();
        api.Authenticate(api.Creator.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.PostAsJsonAsync(api.Collection,
            new { title, googleFileId = FileId, sourceType = "Manual" }, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.PatchAsJsonAsync(api.Item, new { title }, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(0, api.Persistence.Saves);
    }

    [Fact]
    public async Task Overlength_title_and_file_id_are_rejected()
    {
        using var api = new TestApi();
        api.Authenticate(api.Creator.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.PostAsJsonAsync(api.Collection,
            new { title = new string('x', 151), googleFileId = FileId, sourceType = "Manual" }, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.PostAsJsonAsync(api.Collection,
            new { title = "Notes", googleFileId = new string('x', 257), sourceType = "Manual" }, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Duplicate_is_conflict_but_case_different_ids_remain_distinct()
    {
        using var api = new TestApi();
        api.Authenticate(api.Creator.ToString());
        var duplicate = await api.Client.PostAsJsonAsync(api.Collection,
            new { title = "Duplicate", googleFileId = FileId, sourceType = "Manual" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("WorkspaceDocument.Duplicate", (await Json(duplicate)).GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.Created, (await api.Client.PostAsJsonAsync(api.Collection,
            new { title = "Different", googleFileId = FileId.ToLowerInvariant(), sourceType = "Manual" }, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Theory]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Wrong_workspace_cannot_access_a_document_even_for_a_member_of_both(string method)
    {
        using var api = new TestApi();
        api.Authenticate(api.Creator.ToString());
        var other = Workspace.Create("Other workspace", api.Creator).value;
        api.Workspaces.Items.Add(other);
        var request = api.Request(method);
        request.RequestUri = new Uri($"/api/workspaces/{other.Id}/documents/{api.Document.Id}", UriKind.Relative);
        var response = await api.Client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("WorkspaceDocument.NotFound", (await Json(response)).GetProperty("title").GetString());
        Assert.Equal(0, api.Persistence.Saves);
    }

    [Theory]
    [InlineData("creator", "PATCH", HttpStatusCode.NoContent)]
    [InlineData("owner", "PATCH", HttpStatusCode.NoContent)]
    [InlineData("admin", "PATCH", HttpStatusCode.NoContent)]
    [InlineData("other", "PATCH", HttpStatusCode.Forbidden)]
    [InlineData("creator", "DELETE", HttpStatusCode.NoContent)]
    [InlineData("owner", "DELETE", HttpStatusCode.NoContent)]
    [InlineData("admin", "DELETE", HttpStatusCode.NoContent)]
    [InlineData("other", "DELETE", HttpStatusCode.Forbidden)]
    public async Task Only_creator_or_workspace_managers_can_mutate(string actor, string method, HttpStatusCode status)
    {
        using var api = new TestApi();
        var actorId = actor switch { "creator" => api.Creator, "owner" => api.Owner, "admin" => api.Admin, _ => api.OtherMember };
        api.Authenticate(actorId.ToString(), "Admin");
        var response = await api.Client.SendAsync(api.Request(method), TestContext.Current.CancellationToken);
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(status == HttpStatusCode.NoContent ? 1 : 0, api.Persistence.Saves);
        if (status == HttpStatusCode.NoContent && method == "PATCH")
        {
            Assert.Equal("Renamed", api.Document.Title);
            Assert.Equal(actorId, api.Document.UpdatedByUserId);
            Assert.Equal(TestClock.Now.UtcDateTime, api.Document.UpdatedAtUtc);
            Assert.Equal(FileId, api.Document.GoogleFileId);
        }
        if (status == HttpStatusCode.NoContent && method == "DELETE") Assert.Empty(api.Documents.Items);
        // No Google service/HttpClient is registered: deletion is exclusively repository removal.
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Empty_workspace_ids_are_validation_errors(string method)
    {
        using var api = new TestApi();
        api.Authenticate(api.Creator.ToString());
        var request = api.Request(method);
        request.RequestUri = new Uri(request.RequestUri!.OriginalString.Replace(api.Workspace.Id.ToString(), Guid.Empty.ToString()), UriKind.Relative);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.SendAsync(request, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Theory]
    [InlineData("PATCH", false, HttpStatusCode.NotFound)]
    [InlineData("DELETE", false, HttpStatusCode.NotFound)]
    [InlineData("PATCH", true, HttpStatusCode.BadRequest)]
    [InlineData("DELETE", true, HttpStatusCode.BadRequest)]
    public async Task Missing_and_empty_document_ids_do_not_mutate_records(string method, bool empty, HttpStatusCode status)
    {
        using var api = new TestApi();
        api.Authenticate(api.Creator.ToString());
        using var request = api.Request(method);
        request.RequestUri = new Uri($"{api.Collection}/{(empty ? Guid.Empty : Guid.NewGuid())}", UriKind.Relative);
        Assert.Equal(status, (await api.Client.SendAsync(request, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(0, api.Persistence.Saves);
        Assert.Single(api.Documents.Items);
    }

    [Fact]
    public async Task Rename_ignores_protected_fields_in_body()
    {
        using var api = new TestApi();
        api.Authenticate(api.Creator.ToString());
        var createdAt = api.Document.CreatedAtUtc;
        var response = await api.Client.PatchAsJsonAsync(api.Item, new
        {
            title = "  Safe rename  ", id = Guid.NewGuid(), workspaceId = Guid.NewGuid(),
            googleFileId = "ZyXwVuTsRqPoNmLkJiHgFeDcBa", sourceType = "Uploaded", mimeType = WorkspaceDocument.DocxMimeType,
            createdByUserId = api.Owner, updatedByUserId = api.Owner, updatedAtUtc = "1900-01-01T00:00:00Z"
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("Safe rename", api.Document.Title);
        Assert.Equal(api.Workspace.Id, api.Document.WorkspaceId);
        Assert.Equal(FileId, api.Document.GoogleFileId);
        Assert.Equal(WorkspaceDocumentSource.Manual, api.Document.SourceType);
        Assert.Null(api.Document.MimeType);
        Assert.Equal(api.Creator, api.Document.CreatedByUserId);
        Assert.Equal(api.Creator, api.Document.UpdatedByUserId);
        Assert.Equal(createdAt, api.Document.CreatedAtUtc);
        Assert.Equal(TestClock.Now.UtcDateTime, api.Document.UpdatedAtUtc);
    }

    [Fact]
    public async Task Same_google_file_can_be_registered_in_a_different_workspace()
    {
        using var api = new TestApi();
        api.Authenticate(api.Creator.ToString());
        var other = Workspace.Create("Other workspace", api.Creator).value;
        api.Workspaces.Items.Add(other);
        var response = await api.Client.PostAsJsonAsync($"/api/workspaces/{other.Id}/documents",
            new { title = "Other library", googleFileId = FileId, sourceType = "Manual" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(other.Id, (await Json(response)).GetProperty("workspaceId").GetGuid());
    }

    [Theory]
    [InlineData("POST", "WorkspaceDocument.Duplicate")]
    [InlineData("PATCH", "WorkspaceDocument.Changed")]
    [InlineData("DELETE", "WorkspaceDocument.Changed")]
    public async Task Save_conflicts_propagate_to_http_instead_of_returning_success(string method, string errorCode)
    {
        using var api = new TestApi();
        api.Authenticate(api.Creator.ToString());
        api.Persistence.SaveResult = method == "POST" ? WorkspaceDocumentErrors.Duplicate : WorkspaceDocumentErrors.Changed;
        using var request = api.Request(method);
        if (method == "POST") request.Content = JsonContent.Create(new
        {
            title = "Race loser", googleFileId = "ZyXwVuTsRqPoNmLkJiHgFeDcBa", sourceType = "Manual"
        });
        var response = await api.Client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(errorCode, (await Json(response)).GetProperty("title").GetString());
        Assert.Equal(1, api.Persistence.Saves);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
    private static WorkspaceDocument CreateDocument(Guid workspaceId, Guid creator) => WorkspaceDocument.Create(
        workspaceId, "Original", FileId, WorkspaceDocumentSource.Manual, null, creator, TestClock.Now.UtcDateTime.AddDays(-1)).value;

    private sealed class TestClock : TimeProvider
    {
        public static readonly DateTimeOffset Now = new(2026, 11, 21, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class TestApi : IDisposable
    {
        private readonly string secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        private readonly IHost host;
        public Guid Owner { get; } = Guid.NewGuid();
        public Guid Admin { get; } = Guid.NewGuid();
        public Guid Creator { get; } = Guid.NewGuid();
        public Guid OtherMember { get; } = Guid.NewGuid();
        public Workspace Workspace { get; }
        public WorkspaceDocument Document { get; }
        public WorkspaceFake Workspaces { get; }
        public DocumentsFake Documents { get; } = new();
        public UnitOfWorkFake Persistence { get; } = new();
        public HttpClient Client { get; }
        public string Collection => $"/api/workspaces/{Workspace.Id}/documents";
        public string Item => $"{Collection}/{Document.Id}";

        public TestApi()
        {
            Workspace = global::Domain.Aggregates.Collaboration.Workspaces.Workspace.Workspace.Create("Test workspace", Owner).value;
            Join(Admin, WorkspaceRole.Admin);
            Join(Creator, WorkspaceRole.Member);
            Join(OtherMember, WorkspaceRole.Member);
            Workspaces = new WorkspaceFake(Workspace);
            Document = CreateDocument(Workspace.Id, Creator);
            Documents.Items.Add(Document);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = secret, ["Jwt:Issuer"] = "document-tests", ["Jwt:Audience"] = "document-tests"
            }).Build();
            host = new HostBuilder().ConfigureWebHost(web => web.UseTestServer().ConfigureServices(services =>
            {
                services.AddApplication();
                services.AddPresentation(configuration);
                services.AddSingleton<IWorkspaceRepository>(Workspaces);
                services.AddSingleton<IWorkspaceDocumentRepository>(Documents);
                services.AddSingleton<IUnitOfWork>(Persistence);
                services.AddSingleton<TimeProvider>(new TestClock());
                services.AddExceptionHandler<GlobalExceptionHandler>();
                services.AddProblemDetails();
                services.AddControllers().AddApplicationPart(typeof(WorkspaceDocumentsController).Assembly)
                    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
            }).Configure(app =>
            {
                app.UseExceptionHandler();
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            })).Start();
            Client = host.GetTestClient();
        }

        private void Join(Guid id, WorkspaceRole role)
        {
            Assert.True(Workspace.InviteMember(id, Owner, role, TestClock.Now.UtcDateTime).IsSuccess);
            Assert.True(Workspace.AcceptInvitation(Workspace.Invitations.Last().Id, id, TestClock.Now.UtcDateTime).IsSuccess);
        }

        public void Archive() => Assert.True(Workspace.Archive(Owner, TestClock.Now.UtcDateTime).IsSuccess);

        public HttpRequestMessage Request(string method) => new(new HttpMethod(method), method is "PATCH" or "DELETE" ? Item : Collection)
        {
            Content = method switch
            {
                "POST" => JsonContent.Create(new { title = "Notes", googleFileId = FileId, sourceType = "Manual" }),
                "PATCH" => JsonContent.Create(new { title = "Renamed" }),
                _ => null
            }
        };

        public void Authenticate(string? subject, string globalRole = "Employee", bool invalidSignature = false)
        {
            var claims = new List<Claim> { new("role", globalRole) };
            if (subject is not null) claims.Add(new Claim("sub", subject));
            var key = invalidSignature ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)) : secret;
            var token = new JwtSecurityToken("document-tests", "document-tests", claims, expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        }

        public void Dispose() { Client.Dispose(); host.Dispose(); }
    }

    private sealed class UnitOfWorkFake : IUnitOfWork
    {
        public int Saves { get; private set; }
        public Result SaveResult { get; set; } = Result.Success();
        public Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Saves++;
            return Task.FromResult(SaveResult);
        }
    }

    private sealed class DocumentsFake : IWorkspaceDocumentRepository
    {
        public List<WorkspaceDocument> Items { get; } = [];
        public Task<List<WorkspaceDocument>> ListForWorkspaceAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Items.Where(x => x.WorkspaceId == id).ToList());
        public Task<WorkspaceDocument?> GetInWorkspaceAsync(Guid workspaceId, Guid id, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(x => x.WorkspaceId == workspaceId && x.Id == id));
        public Task<bool> ExistsAsync(Guid id, string fileId, CancellationToken ct = default) => Task.FromResult(Items.Any(x => x.WorkspaceId == id && x.GoogleFileId == fileId));
        public Task AddAsync(WorkspaceDocument document, CancellationToken ct = default) { Items.Add(document); return Task.CompletedTask; }
        public void Remove(WorkspaceDocument document) => Items.Remove(document);
        public void Update(WorkspaceDocument document) => throw new NotSupportedException();
        public Task<WorkspaceDocument?> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<WorkspaceDocument>> GetAllAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class WorkspaceFake(Workspace workspace) : IWorkspaceRepository
    {
        public List<Workspace> Items { get; } = [workspace];
        public Task<Workspace?> GetByIdWithMembersAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<Workspace>> GetAllAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(Workspace value, CancellationToken ct = default) => throw new NotSupportedException();
        public void Update(Workspace value) => throw new NotSupportedException();
        public void Remove(Workspace value) => throw new NotSupportedException();
        public Task<WorkspaceSearchResult> GetAllWithMembersAsync(bool includeArchived, int skip, int take, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<UserWorkspaceMembership>> GetMembershipsByUserIdsAsync(IReadOnlyCollection<Guid> ids, bool includeArchived, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<Workspace>> GetByMemberIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<Workspace>> GetByMemberIdWithMembersAsync(Guid id, bool includeArchived, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Workspace?> GetByInvitationIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Workspace?> GetByIdWithInvitationsAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Workspace?> GetByIdWithMembersAndInvitationsAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
