using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api;
using Api.Controllers;
using Api.Middleware;
using Application;
using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Tasks;
using Domain.Aggregates.Tasks.Task;
using Domain.Aggregates.Tasks.Task.ValueObjects;
using Domain.Aggregates.User;
using Domain.Shared.Base;
using Domain.Shared.Primitives;
using Domain.Shared.ResultPattern;
using Domain.Shared.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace TaskDocuments.Tests;

// Real routing/JWT/controllers/MediatR/validation/JSON. No Program startup or database/Google calls.
internal sealed class TestApi : IDisposable
{
    internal const string Url = "https://docs.google.com/document/d/AbCdEfGhIjKlMn_-1234/edit";
    internal const string OtherUrl = "https://docs.google.com/document/d/ZyXwVuTsRqPoNm_-5678/edit";
    internal static CancellationToken Ct => TestContext.Current.CancellationToken;
    internal static readonly DateTime Now = new(2026, 11, 21, 10, 0, 0, DateTimeKind.Utc);
    private readonly string secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    private readonly IHost host;
    internal Guid Owner { get; } = Guid.NewGuid();
    internal Guid Admin { get; } = Guid.NewGuid();
    internal Guid Creator { get; } = Guid.NewGuid();
    internal Guid Assignee { get; } = Guid.NewGuid();
    internal Guid Other { get; } = Guid.NewGuid();
    internal Workspace Workspace { get; }
    internal TaskItem Task { get; }
    internal TasksFake Tasks { get; } = new();
    internal WorkspacesFake Workspaces { get; } = new();
    internal SaveFake Persistence { get; } = new();
    internal HttpClient Client { get; }
    internal string Collection => $"/api/tasks/{Task.Id}/documents";
    internal string Legacy => $"/api/tasks/{Task.Id}/document";

    internal TestApi()
    {
        Workspace = Workspace.Create("Tests", Owner).value;
        Join(Admin, WorkspaceRole.Admin); Join(Creator, WorkspaceRole.Member);
        Join(Assignee, WorkspaceRole.Member); Join(Other, WorkspaceRole.Member);
        Workspaces.Items.Add(Workspace);
        Task = MakeTask(Workspace.Id, Creator);
        Task.AssignTo(Assignee, Owner);
        Tasks.Items.Add(Task);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Jwt:Secret"] = secret, ["Jwt:Issuer"] = "task-doc-tests", ["Jwt:Audience"] = "task-doc-tests" }).Build();
        host = new HostBuilder().ConfigureWebHost(web => web.UseTestServer().ConfigureServices(services =>
        {
            services.AddApplication(); services.AddPresentation(configuration);
            services.AddSingleton<ITaskItemRepository>(Tasks);
            services.AddSingleton<IWorkspaceRepository>(Workspaces);
            services.AddSingleton<IUserRepository>(new UsersFake());
            services.AddSingleton<IUnitOfWork>(Persistence);
            services.AddSingleton<TimeProvider>(new FixedClock());
            services.AddExceptionHandler<GlobalExceptionHandler>(); services.AddProblemDetails();
            services.AddControllers().AddApplicationPart(typeof(TaskDocumentsController).Assembly)
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
        }).Configure(app =>
        {
            app.UseExceptionHandler(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        })).Start();
        Client = host.GetTestClient();
        Authenticate(Creator.ToString());
    }

    internal static TaskItem MakeTask(Guid workspaceId, Guid creator) => TaskItem.Create(workspaceId,
        Name.Create("Task").value, DateTime.UtcNow.AddDays(30), creator).value;

    internal HttpRequestMessage Request(string method, Guid? documentId = null) => new(new HttpMethod(method),
        method is "PATCH" or "DELETE" or "GET-ITEM" ? Collection + "/" + documentId : Collection)
    { Content = method is "POST" or "PATCH" ? JsonContent.Create(new { url = Url, label = "Notes" }) : null };

    internal void Authenticate(string? subject, string globalRole = "Admin", bool invalidSignature = false)
    {
        var claims = new List<Claim> { new("role", globalRole) };
        if (subject is not null) claims.Add(new Claim("sub", subject));
        var key = invalidSignature ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)) : secret;
        var token = new JwtSecurityToken("task-doc-tests", "task-doc-tests", claims, expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }
    internal void Archive() => Assert.True(Workspace.Archive(Owner, Now).IsSuccess);
    private void Join(Guid member, WorkspaceRole role)
    {
        Assert.True(Workspace.InviteMember(member, Owner, role, Now).IsSuccess);
        Assert.True(Workspace.AcceptInvitation(Workspace.Invitations.Last().Id, member, Now).IsSuccess);
    }
    public void Dispose() { Client.Dispose(); host.Dispose(); }
    private sealed class FixedClock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(Now); }
}

internal abstract class RepositoryFake<T> : IRepository<T> where T : AggregateRoot
{
    public List<T> Items { get; } = [];
    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) => System.Threading.Tasks.Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
    public Task<List<T>> GetAllAsync(CancellationToken ct = default) => System.Threading.Tasks.Task.FromResult(Items.ToList());
    public Task AddAsync(T item, CancellationToken ct = default) { Items.Add(item); return System.Threading.Tasks.Task.CompletedTask; }
    public void Update(T item) { }
    public void Remove(T item) => Items.Remove(item);
}
internal sealed class TasksFake : RepositoryFake<TaskItem>, ITaskItemRepository
{
    public Task<TaskItem?> GetByIdWithDocumentsAsync(Guid id, CancellationToken ct = default) => System.Threading.Tasks.Task.FromResult(Items.FirstOrDefault(x => x.Id == id && !x.IsDeleted));
    public Task<TaskItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default) => GetByIdWithDocumentsAsync(id, ct);
    public Task<TaskItem?> GetByIdWithCommentsAsync(Guid id, CancellationToken ct = default) => GetByIdWithDocumentsAsync(id, ct);
    public Task<List<TaskItem>> GetByWorkspaceIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<List<TaskItem>> GetByAssigneeIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<List<TaskItem>> GetByCreatorIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<TaskSearchResult> SearchAsync(TaskSearchCriteria criteria, CancellationToken ct = default) => throw new NotSupportedException();
}
internal sealed class WorkspacesFake : RepositoryFake<Workspace>, IWorkspaceRepository
{
    public Task<Workspace?> GetByIdWithMembersAsync(Guid id, CancellationToken ct = default) => GetByIdAsync(id, ct);
    public Task<Workspace?> GetByIdWithInvitationsAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<Workspace?> GetByIdWithMembersAndInvitationsAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<Workspace?> GetByInvitationIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<List<Workspace>> GetByMemberIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<List<Workspace>> GetByMemberIdWithMembersAsync(Guid id, bool includeArchived, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<WorkspaceSearchResult> GetAllWithMembersAsync(bool includeArchived, int skip, int take, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<List<UserWorkspaceMembership>> GetMembershipsByUserIdsAsync(IReadOnlyCollection<Guid> ids, bool includeArchived, CancellationToken ct = default) => throw new NotSupportedException();
}
internal sealed class UsersFake : RepositoryFake<User>, IUserRepository
{
    public Task<List<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) => System.Threading.Tasks.Task.FromResult(new List<User>());
    public Task<UserSearchResult> GetPagedAsync(int skip, int take, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<User?> GetByVerificationTokenAsync(string token, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) => throw new NotSupportedException();
}
internal sealed class SaveFake : IUnitOfWork
{
    public int Saves { get; private set; }
    public Result Result { get; set; } = Result.Success();
    public Task<Result> SaveChangesAsync(CancellationToken ct = default) { Saves++; return System.Threading.Tasks.Task.FromResult(Result); }
}
