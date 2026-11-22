using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Aggregates.Tasks.Task.ValueObjects;
using Domain.Aggregates.Tasks.TaskDocument;
using Xunit;

namespace TaskDocuments.Tests;

public class TaskDocumentsApiTests
{
    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestApi.Ct);

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Every_collection_endpoint_requires_authentication(string method)
    {
        using var api = new TestApi(); api.Client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.SendAsync(api.Request(method, Guid.NewGuid()), TestApi.Ct)).StatusCode);
        Assert.Equal(0, api.Persistence.Saves);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Missing_malformed_and_empty_subjects_are_rejected(string? subject)
    {
        using var api = new TestApi(); api.Authenticate(subject);
        foreach (var method in new[] { "GET", "POST", "PATCH", "DELETE" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.SendAsync(api.Request(method, Guid.NewGuid()), TestApi.Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync(api.Collection + "/" + Guid.NewGuid(), TestApi.Ct)).StatusCode);
    }

    [Fact]
    public async Task Invalid_signature_is_not_accepted()
    {
        using var api = new TestApi(); api.Authenticate(api.Creator.ToString(), invalidSignature: true);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync(api.Collection, TestApi.Ct)).StatusCode);
    }

    [Fact]
    public async Task Empty_collection_is_an_array_and_multiple_documents_have_stable_ids()
    {
        using var api = new TestApi();
        Assert.Empty((await Json(await api.Client.GetAsync(api.Collection, TestApi.Ct))).EnumerateArray());
        var ids = new HashSet<Guid>();
        foreach (var url in new[] { TestApi.Url, TestApi.OtherUrl })
        {
            var added = await api.Client.PostAsJsonAsync(api.Collection, new { url, label = "  Notes  " }, TestApi.Ct);
            Assert.Equal(HttpStatusCode.Created, added.StatusCode);
            var row = await Json(added); var id = row.GetProperty("id").GetGuid(); Assert.True(ids.Add(id));
            Assert.Equal(api.Task.Id, row.GetProperty("taskId").GetGuid());
            Assert.Equal("Notes", row.GetProperty("label").GetString());
            Assert.Equal(url, row.GetProperty("url").GetString());
            Assert.Equal(api.Creator, row.GetProperty("createdByUserId").GetGuid());
            Assert.Equal(TestApi.Now, row.GetProperty("createdAtUtc").GetDateTime());
            var location = added.Headers.Location!;
            var get = await api.Client.GetAsync(location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString, TestApi.Ct);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            Assert.Equal(id, (await Json(get)).GetProperty("id").GetGuid());
        }
        var listed = (await Json(await api.Client.GetAsync(api.Collection, TestApi.Ct))).EnumerateArray();
        Assert.Equal(ids.Order(), listed.Select(x => x.GetProperty("id").GetGuid()).Order());
    }

    [Theory]
    [InlineData("creator", true)] [InlineData("assignee", true)] [InlineData("owner", true)]
    [InlineData("admin", true)] [InlineData("other", false)]
    public async Task Existing_task_document_write_permissions_are_reused(string actor, bool allowed)
    {
        using var api = new TestApi();
        var user = actor switch { "creator" => api.Creator, "assignee" => api.Assignee, "owner" => api.Owner, "admin" => api.Admin, _ => api.Other };
        api.Authenticate(user.ToString(), "Admin"); // Global role must not elevate an ordinary workspace member.
        var added = await api.Client.PostAsJsonAsync(api.Collection, new { url = TestApi.Url, label = "Notes" }, TestApi.Ct);
        Assert.Equal(allowed ? HttpStatusCode.Created : HttpStatusCode.Forbidden, added.StatusCode);
        var seeded = allowed ? api.Task.Documents.Single() : api.Task.AddDocument(DocumentLink.Create(TestApi.Url, "Seeded").value, api.Creator, TestApi.Now).value;
        var item = api.Collection + "/" + seeded.Id;
        var version = seeded.Version;
        var changed = await api.Client.PatchAsJsonAsync(item, new { url = TestApi.OtherUrl, label = "Changed" }, TestApi.Ct);
        Assert.Equal(allowed ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden, changed.StatusCode);
        if (allowed) { Assert.NotEqual(version, seeded.Version); Assert.Equal(TestApi.Now, seeded.UpdatedAtUtc); }
        Assert.Equal(allowed ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden, (await api.Client.DeleteAsync(item, TestApi.Ct)).StatusCode);
        Assert.Equal(allowed ? 0 : 1, api.Task.Documents.Count);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Every_current_workspace_member_can_read_active_and_archived_documents(bool archived)
    {
        using var api = new TestApi();
        var document = api.Task.AddDocument(DocumentLink.Create(TestApi.Url, "Notes").value, api.Creator, TestApi.Now).value;
        if (archived) api.Archive();
        foreach (var user in new[] { api.Creator, api.Assignee, api.Owner, api.Admin, api.Other })
        {
            api.Authenticate(user.ToString());
            Assert.Equal(HttpStatusCode.OK, (await api.Client.GetAsync(api.Collection, TestApi.Ct)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await api.Client.GetAsync(api.Collection + "/" + document.Id, TestApi.Ct)).StatusCode);
        }
    }

    [Theory]
    [InlineData("GET")] [InlineData("POST")] [InlineData("PATCH")] [InlineData("DELETE")]
    public async Task Nonmembers_including_global_admin_see_only_task_not_found(string method)
    {
        using var api = new TestApi(); api.Authenticate(Guid.NewGuid().ToString());
        var response = await api.Client.SendAsync(api.Request(method, Guid.NewGuid()), TestApi.Ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Task.NotFound", (await Json(response)).GetProperty("title").GetString());
        Assert.Equal(0, api.Persistence.Saves);
    }

    [Theory]
    [InlineData("POST")] [InlineData("PATCH")] [InlineData("DELETE")]
    public async Task Archived_workspaces_reject_new_and_legacy_mutations(string method)
    {
        using var api = new TestApi(); api.Archive();
        var document = api.Task.AddDocument(DocumentLink.Create(TestApi.Url, "Notes").value, api.Creator, TestApi.Now).value;
        var response = await api.Client.SendAsync(api.Request(method, document.Id), TestApi.Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Workspace.DeletedOrArchived", (await Json(response)).GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.PutAsJsonAsync(api.Legacy, new { url = TestApi.Url, label = "Old client" }, TestApi.Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.DeleteAsync(api.Legacy, TestApi.Ct)).StatusCode);
        Assert.Equal(0, api.Persistence.Saves);
    }

    [Theory]
    [InlineData("GET")] [InlineData("PATCH")] [InlineData("DELETE")]
    public async Task Document_ids_cannot_be_used_under_another_task_even_in_the_same_workspace(string method)
    {
        using var api = new TestApi();
        var document = api.Task.AddDocument(DocumentLink.Create(TestApi.Url, "Notes").value, api.Creator, TestApi.Now).value;
        var other = TestApi.MakeTask(api.Workspace.Id, api.Creator); api.Tasks.Items.Add(other);
        using var request = new HttpRequestMessage(new HttpMethod(method), $"/api/tasks/{other.Id}/documents/{document.Id}");
        if (method == "PATCH") request.Content = JsonContent.Create(new { url = TestApi.OtherUrl, label = "Cross task" });
        var response = await api.Client.SendAsync(request, TestApi.Ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("TaskDocument.NotFound", (await Json(response)).GetProperty("title").GetString());
        Assert.Single(api.Task.Documents); Assert.Equal(0, api.Persistence.Saves);
    }

    [Fact]
    public async Task Deleted_tasks_and_inaccessible_workspaces_do_not_expose_documents()
    {
        using var api = new TestApi(); api.Task.Delete(api.Creator, TestApi.Now);
        Assert.Equal(HttpStatusCode.NotFound, (await api.Client.GetAsync(api.Collection, TestApi.Ct)).StatusCode);
        var foreign = TestApi.MakeTask(Guid.NewGuid(), api.Creator); api.Tasks.Items.Add(foreign);
        Assert.Equal(HttpStatusCode.NotFound, (await api.Client.GetAsync($"/api/tasks/{foreign.Id}/documents", TestApi.Ct)).StatusCode);
    }

    [Theory]
    [InlineData("http://docs.google.com/document/d/AbCdEfGhIjKl/edit", "Notes")]
    [InlineData("https://evil.example/document/d/AbCdEfGhIjKl/edit", "Notes")]
    [InlineData("https://docs.google.com.evil.example/document/d/AbCdEfGhIjKl/edit", "Notes")]
    [InlineData("https://docs.google.com/", "Notes")]
    [InlineData("https://user:password@docs.google.com/document/d/AbCdEfGhIjKl/edit", "Notes")]
    [InlineData("https://docs.google.com/document/d/AbCdEfGhIjKl/edit?access_token=synthetic", "Notes")]
    [InlineData("https://drive.google.com/file/d/AbCdEfGhIjKl/view?refresh_token=synthetic", "Notes")]
    [InlineData("https://docs.google.com/document/d/AbCdEfGhIjKl/edit#access_token=synthetic", "Notes")]
    [InlineData(TestApi.Url, "")]
    [InlineData(TestApi.Url, "   ")]
    [InlineData(TestApi.Url, null)]
    public async Task Invalid_metadata_never_reaches_save(string url, string? label)
    {
        using var api = new TestApi();
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.PostAsJsonAsync(api.Collection, new { url, label }, TestApi.Ct)).StatusCode);
        Assert.Equal(0, api.Persistence.Saves);
    }

    [Fact]
    public async Task Overlength_fields_and_empty_route_guids_are_validation_errors()
    {
        using var api = new TestApi();
        foreach (var body in new[] { new { url = TestApi.Url, label = new string('x', 201) }, new { url = "https://office.com/" + new string('x', 2001), label = "Notes" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.PostAsJsonAsync(api.Collection, body, TestApi.Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.GetAsync($"/api/tasks/{Guid.Empty}/documents", TestApi.Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.DeleteAsync(api.Collection + "/" + Guid.Empty, TestApi.Ct)).StatusCode);
    }

    [Fact]
    public async Task Server_identity_and_clock_cannot_be_overridden_by_body_metadata()
    {
        using var api = new TestApi();
        var response = await api.Client.PostAsJsonAsync(api.Collection, new
        { url = TestApi.Url, label = "Notes", id = Guid.NewGuid(), taskId = Guid.NewGuid(), createdByUserId = Guid.NewGuid(), createdAtUtc = "1900-01-01T00:00:00Z", version = Guid.NewGuid() }, TestApi.Ct);
        var row = await Json(response);
        Assert.Equal(api.Creator, row.GetProperty("createdByUserId").GetGuid());
        Assert.Equal(TestApi.Now, row.GetProperty("createdAtUtc").GetDateTime());
        Assert.False(row.TryGetProperty("version", out _)); Assert.False(row.TryGetProperty("urlHash", out _));
        Assert.False(row.TryGetProperty("isLegacy", out _));
    }

    [Fact]
    public async Task Duplicate_urls_return_409_without_destroying_existing_records()
    {
        using var api = new TestApi();
        await api.Client.PostAsJsonAsync(api.Collection, new { url = TestApi.Url, label = "First" }, TestApi.Ct);
        var response = await api.Client.PostAsJsonAsync(api.Collection, new { url = TestApi.Url, label = "Duplicate" }, TestApi.Ct);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("TaskDocument.Duplicate", (await Json(response)).GetProperty("title").GetString());
        Assert.Single(api.Task.Documents);
        Assert.Equal(HttpStatusCode.Created, (await api.Client.PostAsJsonAsync(api.Collection,
            new { url = TestApi.Url.Replace("AbCd", "abcd"), label = "Case distinct ID" }, TestApi.Ct)).StatusCode);
        var other = TestApi.MakeTask(api.Workspace.Id, api.Creator); api.Tasks.Items.Add(other);
        Assert.Equal(HttpStatusCode.Created, (await api.Client.PostAsJsonAsync($"/api/tasks/{other.Id}/documents", new { url = TestApi.Url, label = "Other task" }, TestApi.Ct)).StatusCode);
    }

    [Fact]
    public async Task Legacy_routes_and_task_details_keep_the_single_slot_without_duplicating_it()
    {
        using var api = new TestApi();
        Assert.Equal(HttpStatusCode.NoContent, (await api.Client.PutAsJsonAsync(api.Legacy, new { url = TestApi.Url, label = "Legacy" }, TestApi.Ct)).StatusCode);
        var legacy = Assert.Single(api.Task.Documents); Assert.NotEqual(Guid.Empty, legacy.Id);
        await api.Client.PutAsJsonAsync(api.Legacy, new { url = TestApi.OtherUrl, label = "Replaced" }, TestApi.Ct);
        Assert.Equal(legacy.Id, Assert.Single(api.Task.Documents).Id);
        await api.Client.PostAsJsonAsync(api.Collection, new { url = TestApi.Url, label = "Additional" }, TestApi.Ct);
        var detail = await Json(await api.Client.GetAsync($"/api/tasks/{api.Task.Id}", TestApi.Ct));
        Assert.Equal("Replaced", detail.GetProperty("linkedDocument").GetProperty("label").GetString());
        Assert.Equal(2, detail.GetProperty("documents").GetArrayLength());
        Assert.True(detail.TryGetProperty("comments", out _)); Assert.True(detail.TryGetProperty("createdByName", out _));
        Assert.Equal(HttpStatusCode.NoContent, (await api.Client.DeleteAsync(api.Legacy, TestApi.Ct)).StatusCode);
        Assert.Null(api.Task.LinkedDocument); Assert.Single(api.Task.Documents);
        Assert.False(api.Task.Documents.Single().IsLegacy);
    }

    [Fact]
    public async Task New_routes_mirror_updates_and_removal_of_the_legacy_slot()
    {
        using var api = new TestApi();
        api.Task.UpdateDocumentLink(DocumentLink.Create(TestApi.Url, "Legacy").value, api.Creator, TestApi.Now);
        var legacy = api.Task.Documents.Single(); var item = api.Collection + "/" + legacy.Id;
        await api.Client.PatchAsJsonAsync(item, new { url = TestApi.OtherUrl, label = "New label" }, TestApi.Ct);
        Assert.Equal("New label", api.Task.LinkedDocument!.Label);
        await api.Client.DeleteAsync(item, TestApi.Ct); Assert.Null(api.Task.LinkedDocument);
    }

    [Fact]
    public async Task Existing_task_create_edit_and_detail_contracts_still_work()
    {
        using var api = new TestApi();
        var due = DateTime.UtcNow.AddDays(10);
        var created = await api.Client.PostAsJsonAsync($"/api/workspaces/{api.Workspace.Id}/tasks",
            new { title = "New task", dueDate = due, priority = "High", description = "Original" }, TestApi.Ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var taskId = (await Json(created)).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await api.Client.PutAsJsonAsync($"/api/tasks/{taskId}",
            new { title = "Edited task", dueDate = due, priority = "Low", description = "Changed" }, TestApi.Ct)).StatusCode);
        var detail = await Json(await api.Client.GetAsync($"/api/tasks/{taskId}", TestApi.Ct));
        Assert.Equal("Edited task", detail.GetProperty("title").GetString());
        Assert.Equal("Changed", detail.GetProperty("description").GetString());
        Assert.Equal("Low", detail.GetProperty("priority").GetString());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("linkedDocument").ValueKind);
        Assert.Empty(detail.GetProperty("documents").EnumerateArray());
    }

    [Fact]
    public async Task Editing_unrelated_task_fields_preserves_many_documents_and_the_compatibility_slot()
    {
        using var api = new TestApi();
        api.Task.UpdateDocumentLink(DocumentLink.Create(TestApi.Url, "Legacy").value, api.Creator, TestApi.Now);
        for (var i = 0; i < 25; i++)
            api.Task.AddDocument(DocumentLink.Create($"https://docs.google.com/document/d/SyntheticFile{i}/edit", $"Notes {i}").value,
                api.Creator, TestApi.Now);
        var before = api.Task.Documents.OrderBy(x => x.Id)
            .Select(x => (x.Id, x.Url, x.Label, x.Version, x.CreatedByUserId, x.CreatedAtUtc, x.UpdatedAtUtc)).ToArray();
        var edited = await api.Client.PutAsJsonAsync($"/api/tasks/{api.Task.Id}", new
        {
            title = "Unrelated edit", dueDate = DateTime.UtcNow.AddDays(10), priority = "Low", description = "Changed",
            documents = Array.Empty<object>(), linkedDocument = (object?)null
        }, TestApi.Ct); // Unknown body fields must not bind to document mutations.
        Assert.Equal(HttpStatusCode.NoContent, edited.StatusCode);
        Assert.Equal(before, api.Task.Documents.OrderBy(x => x.Id)
            .Select(x => (x.Id, x.Url, x.Label, x.Version, x.CreatedByUserId, x.CreatedAtUtc, x.UpdatedAtUtc)).ToArray());
        var detail = await Json(await api.Client.GetAsync($"/api/tasks/{api.Task.Id}", TestApi.Ct));
        Assert.Equal(26, detail.GetProperty("documents").GetArrayLength());
        Assert.Equal("Legacy", detail.GetProperty("linkedDocument").GetProperty("label").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await api.Client.DeleteAsync(api.Legacy, TestApi.Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await api.Client.DeleteAsync(api.Legacy, TestApi.Ct)).StatusCode);
        Assert.Equal(25, api.Task.Documents.Count);
    }

    [Theory]
    [InlineData("{\"label\":\"Rename\"}")]
    [InlineData("{\"url\":\"https://docs.google.com/document/d/SyntheticFile/edit\"}")]
    [InlineData("{\"url\":null,\"label\":\"Rename\"}")]
    [InlineData("{\"url\":\"https://docs.google.com/document/d/SyntheticFile/edit\",\"label\":null}")]
    public async Task Patch_requires_both_url_and_label_without_altering_the_document(string body)
    {
        using var api = new TestApi();
        var document = api.Task.AddDocument(DocumentLink.Create(TestApi.Url, "Existing").value, api.Creator, TestApi.Now).value;
        var version = document.Version;
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await api.Client.PatchAsync(api.Collection + "/" + document.Id, content, TestApi.Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(version, document.Version); Assert.Equal("Existing", document.Label);
        Assert.Equal(0, api.Persistence.Saves);
    }

    [Fact]
    public async Task Legacy_put_also_rejects_google_credentials_in_fragments()
    {
        using var api = new TestApi();
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.PutAsJsonAsync(api.Legacy,
            new { url = TestApi.Url + "#id_token=synthetic", label = "Notes" }, TestApi.Ct)).StatusCode);
        Assert.Null(api.Task.LinkedDocument); Assert.Empty(api.Task.Documents);
        Assert.Equal(0, api.Persistence.Saves);
    }

    [Theory]
    [InlineData("POST")] [InlineData("PATCH")] [InlineData("DELETE")]
    public async Task Database_conflicts_propagate_as_409_not_success(string method)
    {
        using var api = new TestApi();
        var document = api.Task.AddDocument(DocumentLink.Create(TestApi.Url, "Seeded").value, api.Creator, TestApi.Now).value;
        api.Persistence.Result = method == "POST" ? TaskDocumentCollectionErrors.Duplicate : TaskDocumentCollectionErrors.Changed;
        using var request = api.Request(method, document.Id);
        if (method == "POST") request.Content = JsonContent.Create(new { url = TestApi.OtherUrl, label = "Race" });
        Assert.Equal(HttpStatusCode.Conflict, (await api.Client.SendAsync(request, TestApi.Ct)).StatusCode);
    }

    [Fact]
    public async Task Duplicate_update_and_legacy_put_do_not_overwrite_a_different_document()
    {
        using var api = new TestApi();
        var first = api.Task.AddDocument(DocumentLink.Create(TestApi.Url, "First").value, api.Creator, TestApi.Now).value;
        var second = api.Task.AddDocument(DocumentLink.Create(TestApi.OtherUrl, "Second").value, api.Creator, TestApi.Now).value;
        var before = second.Version;
        Assert.Equal(HttpStatusCode.Conflict, (await api.Client.PatchAsJsonAsync(api.Collection + "/" + second.Id,
            new { url = first.Url, label = "Overwrite" }, TestApi.Ct)).StatusCode);
        Assert.Equal("Second", second.Label); Assert.Equal(before, second.Version);
        Assert.Equal(HttpStatusCode.Conflict, (await api.Client.PutAsJsonAsync(api.Legacy,
            new { url = first.Url, label = "Old client overwrite" }, TestApi.Ct)).StatusCode);
        Assert.Null(api.Task.LinkedDocument); Assert.Equal(2, api.Task.Documents.Count);
    }

    [Theory]
    [InlineData("creator")] [InlineData("assignee")]
    public async Task Revoked_task_owners_and_assignees_cannot_bypass_membership(string actor)
    {
        using var api = new TestApi();
        var user = actor == "creator" ? api.Creator : api.Assignee;
        Assert.True(api.Workspace.RemoveMember(api.Owner, user, TestApi.Now).IsSuccess);
        api.Authenticate(user.ToString());
        foreach (var method in new[] { "GET", "POST", "PATCH", "DELETE" })
            Assert.Equal(HttpStatusCode.NotFound, (await api.Client.SendAsync(api.Request(method, Guid.NewGuid()), TestApi.Ct)).StatusCode);
        Assert.Equal(0, api.Persistence.Saves);
    }

    [Fact]
    public async Task Item_get_requires_authentication_and_hides_inaccessible_tasks()
    {
        using var api = new TestApi();
        api.Client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync(api.Collection + "/" + Guid.NewGuid(), TestApi.Ct)).StatusCode);
        api.Authenticate(Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.NotFound, (await api.Client.GetAsync(api.Collection + "/" + Guid.NewGuid(), TestApi.Ct)).StatusCode);
    }
}
