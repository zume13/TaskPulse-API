# Multiple document links per task

## Baseline and scope

Baseline `origin/master`: `31dbb85ec813278654b1155ebc024ec9f9bb8833`.
Worktree: `TaskPulse-API-TaskDocuments`, branch `feature/task-multiple-documents`.
Remote inspection found no equivalent multi-link implementation. Open PR #6 concerns workspace
reads; PR #9 is the separate workspace-document feature. Neither PR nor previous worktree was changed.
Unpublished teammate work cannot be ruled out; coordinate before adopting the new migration.

This is a task-owned collection, not the superseded workspace library. No WorkspaceDocument entity,
migration or persistence is reused. No frontend, file content, Google token storage, editor proxy,
OAuth configuration or Google sharing API is introduced.

## Existing single-link contract, verified before changes

`TaskItem.LinkedDocument` is an optional `DocumentLink` value object with `Url` and `Label`.
EF stores it in `TaskItems.LinkedDocumentUrl` / `LinkedDocumentLabel`. Existing authenticated routes:

- `PUT /api/tasks/{taskId}/document` with `{ "url": "...", "label": "..." }`, returns 204.
- `DELETE /api/tasks/{taskId}/document`, returns 204.
- `GET /api/tasks/{taskId}` exposes `linkedDocument: { url, label } | null`, plus task data/comments.

Task document writes require current workspace membership and task creator, task assignee or
workspace Owner/Admin status. A global Admin role alone does not grant access. Archives reject
writes but permit member reads. Supported providers already included Google Docs/Sheets/Slides,
Microsoft 365/Office, OneDrive and GitHub; they were not limited to native Google Docs.

## New model and API

`TaskDocument` is a child entity of `TaskItem`, like its comments. Changes go through the aggregate
and existing task repository/UnitOfWork; there is no independent document repository. Required fields:
`Id`, `TaskId`, `Url`, `Label`, `CreatedAtUtc`, `CreatedByUserId`; nullable `UpdatedAtUtc`.
Internal fields `UrlHash`, `Version`, `IsLegacy` are not serialized. New IDs and version tokens are
server-generated GUIDs. Creation identity comes from the validated TaskPulse subject; timestamps
come from TimeProvider. PATCH cannot overwrite identity/audit fields.

| Method | Route | Success |
| --- | --- | --- |
| GET | `/api/tasks/{taskId}/documents` | 200 array, possibly empty |
| POST | same collection | 201 document DTO, Location points to item GET |
| GET | `/api/tasks/{taskId}/documents/{documentId}` | 200 document DTO |
| PATCH | same item | 204 |
| DELETE | same item | 204 |

POST and PATCH accept the full writable metadata shape `{ "url": "...", "label": "..." }`.
PATCH is not JSON Patch or a label-only merge: a rename sends the existing URL along with the new
label. Listing order is CreatedAtUtc ascending, then ID. Item lookup only searches the authorized
task's loaded collection. Deleted tasks, missing tasks and nonmember tasks return 404 `Task.NotFound`.
Missing/cross-task item IDs return 404 `TaskDocument.NotFound` for an otherwise accessible task.
Empty GUIDs/invalid metadata return 400; invalid/missing subject or JWT returns 401; disallowed task
mutations return 403 `Task.NotAllowed`; archives retain 400 `Workspace.DeletedOrArchived`.
Duplicate URL 409 is `TaskDocument.Duplicate`; overlapping update/delete or legacy-slot races use
409 `TaskDocument.Changed`. Existing Result/ProblemDetails/ValidationProblemDetails conventions apply.

All current workspace members can list/get documents. Attach/update/remove reuse the task's existing
creator/assignee/Owner/Admin permission policy, not document-creator ownership. Completed tasks keep
the existing document-link rules; soft-deleted tasks are inaccessible.

## URL validation and duplicate policy

New inputs retain the existing 2000-character request URL bound and trimmed required label of at most
200 characters. Storage is text, so URI escaping does not introduce a new truncation limit for old
links or Unicode paths. HTTPS, exact allowed hosts, no userinfo/nondefault ports and existing provider
path restrictions remain. Drive support is narrowly added for exact `drive.google.com/file/d/{id}`
links with optional view/edit/preview suffix, not arbitrary folders or new providers.

Google `/document|spreadsheets|presentation/u/{index}/d/...` routes are stored without `/u/{index}`.
Google `authuser` query/fragment parameters are removed; case-sensitive file IDs and meaningful remaining
path/query/fragment values are preserved. Google OAuth credential fields `access_token`,
`refresh_token` and `id_token` are rejected in both query strings and fragments, including encoded/case-varied
parameter names. This is not a general secret detector: arbitrary text in URLs/labels cannot be certified
credential-free. Browser session selection remains a client-side concern.
The API never contacts Google, verifies file type/access, grants permissions or deletes a Drive file.

Duplicates mean exact canonical URL strings within one task, independent of label, enforced by a
unique `(TaskId, UrlHash)` index. SHA-256 bounds the index key even for long escaped historical URLs.
Google ID case is significant; query/fragment differences other than removed session selectors may
still represent separate links. This is not provider-wide file-identity deduplication. Matching URLs
on different tasks are allowed. A hash collision would conservatively report a duplicate.

## Legacy compatibility and data preservation

Migration `20261121105737_AddTaskDocuments` is incremental after `AddLoginAudits`. It creates only
TaskDocuments and its three indexes/two foreign keys. Task deletion cascades child rows; creator
deletion is restricted. Original TaskItems columns and records remain unchanged.

The backfill inserts one `IsLegacy=true` child for every nonnull legacy URL, including soft-deleted
tasks. Its stable document ID is the task GUID; URLs are copied verbatim, not re-parsed or truncated.
A missing label gets `Legacy document` in the new row while the old null column is retained. Empty,
unusual or historically invalid URLs remain data for owner review, not silently discarded records.
When the URL is null, no child is created, even if the old label is nonnull; the old columns still remain.
Task creator/time are used as legacy audit approximations because original link author/time were
never stored. This is not evidence of the actual historic attachment author/date.
The hash uses PostgreSQL core `sha256(convert_to(..., 'UTF8'))`, with uppercase hex matching .NET;
no extension or pgcrypto install is needed. See [PostgreSQL 17 binary functions](https://www.postgresql.org/docs/17/functions-binarystring.html).

The collection reads only TaskDocuments, never unioning in the old columns, so the migrated link
appears once. A filtered unique index permits only one legacy compatibility slot per task.
New writes use fresh GUIDs, including creation of a new compatibility slot after deletion.

Legacy PUT updates/inserts only this slot, keeping its ID stable during updates; legacy DELETE
removes only this slot. Additional documents survive both operations. New PATCH/DELETE of the legacy
row also synchronize the old columns in the same SaveChanges transaction. `linkedDocument` stays
the compatibility-slot projection; **it is not an alias for the first new collection entry**.
An old Angular client cannot display additional entries until it adopts `documents`/the new routes.
A legacy PUT duplicating an additional entry returns 409 rather than destroying or moving it.

Historical backfilled URLs may retain old session selectors or parameters. They are intentionally
not rewritten by the preservation migration; new/updated links use the new canonicalization policy.
Review problematic historical URLs privately before deployment; no database contents were inspected.

The Down migration refuses to drop data if there are additional documents, changed/new legacy rows,
audit/version changes or mismatched old columns. Only unchanged initial backfill rows can be safely
removed while their original columns remain (including the documented null-label fallback). Export/reconcile data under separate approval before
any other downgrade; rollback is not a silent destructive escape hatch.

Backfill runs once through EF migration history, not on API startup or each request. The normal
transactional migration records its history entry together with schema/backfill changes; after a failure,
inspect the database/history before retrying. The bare Up SQL is not independently replay-safe: do not
delete history entries, rerun INSERT manually, or suppress duplicate errors with ON CONFLICT. An existing
unexpected table/history mismatch requires owner investigation, not automatic repair. Idempotent EF
scripts depend on intact migration history; they do not reconcile manual schema/data changes.

**Deployment coordination is required:** stop old backend binaries before applying the migration
and start the new binary afterward. Old Angular clients may continue using the updated compatibility
routes, but old API binaries/direct SQL writers do not synchronize the new table. Mixed old/new API
rolling deployment is not supported by this application-level synchronization strategy. No trigger
or automatic runtime backfill is installed. Review/reconcile snapshot ordering if another migration
(including PR #9) is merged first; do not combine workspace-document schema accidentally.

## Concurrency and limits

Each child Version is an EF concurrency token changed on update. It detects overlapping tracked
updates/deletes; the exact unique indexes handle insert/legacy-slot races. Only new constraint names
and SQLSTATE 23505 map to document conflicts. Document-entry database failures suppress URL-bearing
diagnostics. Other existing database error behavior remains unchanged.

There is no client If-Match/version contract: sequential stale payloads can overwrite newer data.
Workspace archive, membership, task deletion and assignee/role changes can race an operation that
already passed authorization, consistent with the existing task handlers. SaveChanges atomicity does
not serialize these checks with other requests. A coordinated cross-feature transaction/locking or
conditional-write policy would be needed for strict revocation ordering. No such infrastructure is
introduced here. These limitations must not be described as strong authorization-at-commit guarantees.

## Angular integration contract (no frontend changes made)

The existing detail DTO gains an additive `documents` array; all prior properties, including
`linkedDocument` and `comments`, remain. Existing create/edit task endpoints retain their shapes.

```typescript
interface TaskDocumentResponse {
  id: string; // opaque TaskPulse document GUID, not a Google file ID
  taskId: string;
  label: string;
  url: string;
  createdAtUtc: string;
  createdByUserId: string;
  updatedAtUtc: string | null;
}
interface TaskDocumentRequest { url: string; label: string; }
```

Use the existing TaskPulse authenticated HttpClient. Task details should render each `documents`
entry with a View document action navigating to a proposed Angular route such as
`/workspaces/:workspaceId/tasks/:taskId/documents/:documentId`. Re-fetch using item GET on a deep link;
do not trust a document passed only in router state or persist browser account indexes in URL metadata.
For label changes, PATCH both the new label and existing URL, then reload the item/detail after 204.
DELETE removes only metadata. Handle 409 explicitly without silently retrying writes.

The frontend must validate/provider-dispatch URLs: native Docs and Drive DOCX IDs can feed the Google
editor/viewer with browser session selection applied locally; Sheets/Slides/Office/OneDrive/GitHub
must not blindly be treated as Google Docs. Preserve supported non-Google links with an appropriate
view/open action. The current Google Docs-only utility does not accept Drive links and needs a scoped
adapter for this new task route. Phone editing restrictions, archive read-only views, TaskPulse
membership and Google access are separate controls. No current workspace-library mapper should be
silently reused as the task-document DTO: this API returns Url/Label, not GoogleFileId/SourceType.

## Verification and remaining database work

The scoped xUnit v3 suite uses TestHost with production JWT validation, routes, MediatR, validation
and serialization, with fake repositories/UnitOfWork. Separate offline tests inspect EF change
tracking, schema/model, generated SQL, backfill/rollback operations and simulated provider failures.
These do not prove an applied backfill, live constraint enforcement or true concurrent PostgreSQL writes.

Executed verification with .NET SDK 10.0.403: restore passed; build passed with 0 warnings/0 errors;
96 tests passed (57 HTTP/application cases and 39 domain/model/migration/error-mapping cases), none
failed or skipped. `git diff --check`, all-new-file whitespace checks and a scoped sensitive-pattern
scan passed. Generated migration designer/snapshot model bodies match. Remote master remained at
the baseline on the implementation's final fetch; this subsequent review did not fetch remote changes.
No database-backed migration/backfill test was executed.

Final review corrected two confirmed defects: Google credential/session parameters in fragments
were not handled, and the downgrade guard rejected an unchanged backfilled null-label row because
it did not compare against the same fallback as Up. Regression tests reproduced these gaps first.
Additional tests cover PATCH's required fields, task editing with 26 links, repeated singular deletion,
and EF tracking of loaded/unloaded collections during unrelated task edits. The final DO block syntax
is valid PL/pgSQL (the outermost END may omit its semicolon); it was not changed.

The design-time factory creates a connectionless Npgsql context without Program/startup/seeding/jobs
or secret reads. EF database commands require an explicit approved connection override. An isolated
dotnet-ef 10.0.15 tool under ignored `src/Api/bin/ef-tools` generated the migration, matching resolved
EF runtime 10.0.15. No production dependencies were changed. Existing floating health-check dependency
version resolution remains a separate reproducibility concern.

```powershell
dotnet restore TaskPulse.slnx
dotnet build TaskPulse.slnx --no-restore
dotnet test TaskPulse.slnx --no-restore
git diff --check
```

Before runtime readiness, obtain separate approval for an isolated PostgreSQL database and:

1. Confirm history through AddLoginAudits, inspect legacy null/partial/long/unusual values privately,
   and back up TaskItems. Confirm there is no conflicting TaskDocuments table or migration.
2. Apply the incremental SQL with old API writers stopped; compare row counts and original URLs,
   labels, creator/time approximations and deterministic backfill IDs. Verify no duplicate display.
3. Exercise old PUT/DELETE and new CRUD against the same task and confirm compatibility-slot mirroring,
   additional-row preservation, FK behavior, restart persistence, unique indexes and rollback refusal.
4. Test real concurrent duplicate inserts, legacy upserts, update/delete conflicts and revoked/archive
   races. Verify all expected HTTP codes without private URLs in diagnostics.
5. Run task create/edit/detail/comment regressions and browser integration for each supported provider,
   actual Google editing/preview, permissions, phone restrictions and dedicated-route navigation.
6. Review startup migration/admin seeding/outbox job effects before starting the full API or Compose.

No migration was applied, database contacted, API/Compose started, frontend changed, commit created,
push performed or PR modified/created for this implementation.

## Changed-file inventory

15 modified tracked files:

- `TaskPulse.slnx`
- `src/Api/Controllers/TaskDocumentController.cs`
- `src/Application/Dto/TaskDetailsDto.cs`
- `src/Application/Features/TaskItem/Commands/RemoveDocumentLink/RemoveDocumentLinkCommandHandler.cs`
- `src/Application/Features/TaskItem/Commands/SetDocumentLink/SetDocumentLinkCommandHandler.cs`
- `src/Application/Features/TaskItem/Queries/GetTaskById/GetTaskByIdQueryHandler.cs`
- `src/Domain/Aggregates/Tasks/TaskItem/Errors/TaskErrors.cs`
- `src/Domain/Aggregates/Tasks/TaskItem/ITaskItemRepository.cs`
- `src/Domain/Aggregates/Tasks/TaskItem/TaskItem.cs`
- `src/Domain/Aggregates/Tasks/TaskItem/ValueObjects/DocumentLink.cs`
- `src/Infrastructure/Persistence/Database/TaskPulseDbContext.cs`
- `src/Infrastructure/Persistence/Database/UnitOfWork.cs`
- `src/Infrastructure/Persistence/EntityConfiguration/TaskItemConfig.cs`
- `src/Infrastructure/Persistence/Migrations/TaskPulseDbContextModelSnapshot.cs`
- `src/Infrastructure/Persistence/Repositories/TaskItemRepository.cs`

18 new files:

- `docs/task-multiple-documents-api.md`
- `src/Api/Controllers/TaskDocumentsController.cs`
- `src/Api/DesignTime/TaskPulseDbContextFactory.cs`
- `src/Application/Dto/TaskDocumentDto.cs`
- `src/Application/Features/TaskItem/Documents/TaskDocumentAccess.cs`
- `src/Application/Features/TaskItem/Documents/TaskDocumentHandlers.cs`
- `src/Application/Features/TaskItem/Documents/TaskDocumentRequests.cs`
- `src/Application/Features/TaskItem/Documents/TaskDocumentValidators.cs`
- `src/Domain/Aggregates/Tasks/TaskDocument/TaskDocument.cs`
- `src/Domain/Aggregates/Tasks/TaskDocument/TaskDocumentCollectionErrors.cs`
- `src/Domain/Aggregates/Tasks/TaskItem/TaskItem.Documents.cs`
- `src/Infrastructure/Persistence/EntityConfiguration/TaskDocumentConfig.cs`
- `src/Infrastructure/Persistence/Migrations/20261121105737_AddTaskDocuments.Designer.cs`
- `src/Infrastructure/Persistence/Migrations/20261121105737_AddTaskDocuments.cs`
- `tests/TaskDocuments.Tests/TaskDocumentPersistenceTests.cs`
- `tests/TaskDocuments.Tests/TaskDocuments.Tests.csproj`
- `tests/TaskDocuments.Tests/TaskDocumentsApiTests.cs`
- `tests/TaskDocuments.Tests/TestApi.cs`
