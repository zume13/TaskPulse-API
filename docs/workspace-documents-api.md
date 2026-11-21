# Shared Workspace Documents API — Milestone 3.1

Baseline: `31dbb85ec813278654b1155ebc024ec9f9bb8833`. This is an independent workspace metadata
library, not a replacement for the existing task-owned `LinkedDocument` URL/label value object.
Files and editing stay in Google Drive. No Google token, account identity, session number, file
content, or file bytes are accepted or persisted by this feature.

## Contract

All routes require a valid TaskPulse JWT with a nonempty GUID `sub`. Body/query user IDs and audit
timestamps are not trusted; creator/updater come from the principal and UTC timestamps from
`TimeProvider`. PascalCase enum strings follow the existing backend JSON convention.

| Method | Route | Success |
| --- | --- | --- |
| GET | `/api/workspaces/{workspaceId:guid}/documents` | 200, array (possibly empty) |
| POST | same collection route | 201, created record; Location points to the collection |
| PATCH | `/api/workspaces/{workspaceId:guid}/documents/{documentId:guid}` | 204 |
| DELETE | same item route | 204 |

GET returns newest records first, then ID as a stable tie-breaker. There is no per-document GET yet.
PATCH supports **title only**; it cannot replace the Google file or change creator/workspace/source.
DELETE hard-deletes only this TaskPulse metadata row, never the Google file or its permissions.

POST manual Google Docs record:

```json
{
  "title": "Sprint notes",
  "googleFileId": "AbCdEfGhIjKlMnOpQrStUvWxYz1234567890_-",
  "sourceType": "Manual"
}
```

That ID is a synthetic example. For an uploaded DOCX use `sourceType: "Uploaded"` and
`mimeType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document"`.
Manual links accept omitted/null MIME type or `application/vnd.google-apps.document` only.
MIME/source describe supplied metadata; the API does not contact Google to verify the actual file.

Record fields: `id`, `workspaceId`, `title`, `googleFileId`, `sourceType`, `mimeType`,
`createdByUserId`, `createdAtUtc`, `updatedAtUtc`, `updatedByUserId`. On creation updated time equals
creation time and updated-by is null. Rename sets updated time and authenticated updater.

PATCH body: `{ "title": "Renamed notes" }`.

Validation: title is trimmed, nonblank and at most 150 characters; canonical file IDs use exactly
the frontend's `[A-Za-z0-9_-]` alphabet and minimum length 10, with a server storage/input bound of
256 characters. IDs are not lowercased, trimmed into a different ID, or accepted as arbitrary URLs.
The existing frontend currently has no maximum ID length; its future API adapter should adopt this
bound. Numeric enum values must be defined; unsupported source/MIME combinations fail validation.

## Authorization and errors

- Any current member can list/register. Owner/Admin do not need to be the creator to rename/delete.
- An ordinary member can rename/delete only records they created. A global application Admin role
  never substitutes for workspace membership or a workspace management role.
- Archived workspaces remain readable; all writes fail using the existing 400
  `Workspace.DeletedOrArchived` convention.
- Missing and inaccessible workspaces both return 404 `Workspace.NotFound`.
- Item queries constrain **both** workspace and document IDs; missing/cross-workspace records return
  404 `WorkspaceDocument.NotFound`. Document lookup occurs before archive/record-role checks.
- Insufficient record mutation permission returns 403 `WorkspaceDocument.NotAllowed`.
- Duplicates return 409 `WorkspaceDocument.Duplicate`, including an insert race caught by the exact
  PostgreSQL unique-index constraint. Its key detail is not returned or logged by that catch.
- Concurrent tracked update/delete conflicts return 409 `WorkspaceDocument.Changed`.
- Invalid input/empty route GUIDs return 400; malformed GUID routes do not match (404).
- Anonymous, invalid JWT and missing/malformed/empty subject GUID requests return 401.

Responses retain the existing ProblemDetails conventions (`title` = stable error code, `detail` =
message); FluentValidation uses the existing ValidationProblemDetails handler. No schema changes
or behavior changes are made to tasks, task links, invitations or workspace/auth endpoints.

## Persistence and concurrency guarantees

`WorkspaceDocuments` is a standalone aggregate/table with required workspace and creator foreign
keys, optional updater foreign key, and Restrict delete behavior. The database unique index is
`IX_WorkspaceDocuments_WorkspaceId_GoogleFileId`. GoogleFileId uses PostgreSQL `C` collation so
case-distinct IDs are not merged even if database defaults differ. `UpdatedAtUtc` is an EF
optimistic concurrency token for overlapping tracked server operations. Audit timestamps are
normalized to PostgreSQL microsecond precision. Every rename advances the token by at least one
microsecond relative to the loaded row, even if the server clock repeats or moves backward. In that
case updated time is a monotonic logical timestamp and can be slightly ahead of wall-clock time.
This follows the precision difference documented by [Npgsql](https://www.npgsql.org/doc/types/datetime.html).

One SaveChanges is atomic, and the database unique index protects concurrent registration. The
precheck is only friendly early feedback. Existing generic database failures retain their current
behavior; only the new constraint and document-entry concurrency errors receive new mappings.

**Known concurrency limitation:** membership/archive are checked before saving, following the
existing task-command pattern. They are NOT locked or included as predicates in the document write.
A concurrently removed member, changed role, or archived workspace can therefore race a request
that already passed authorization. SaveChanges' transaction does not close this check/save window.
The timestamp token protects the document row, not membership or workspace status. A stale client
can also rename a record after another completed rename: this contract has no client If-Match token.
Strict serialization requires a coordinated lock/transaction convention or conditional database
writes shared with archive/membership handlers; that cross-feature change is not included here.
A second application-level membership check would only narrow the race, not eliminate it. This is
a real revocation/archive ordering limitation, not a guarantee that revoked requests cannot finish.
Sequential stale-client rename requests remain last-writer-wins; the EF token is not a client version.

No backend decision implies Google access. Google can refuse a file even when TaskPulse returns its
record. Registering or deleting metadata never grants access, publishes a file, or changes sharing.

## Migration and safe verification

Generated incrementally after `20261116142659_AddLoginAudits`:
`20261121080347_AddWorkspaceDocuments` (migration, designer and updated snapshot).
Only the new table, its foreign keys and indexes are added; no initial baseline is recreated.
The migration has **not** been applied and no database connection is needed for the tests below.

`Api/DesignTime/TaskPulseDbContextFactory` scaffolds with Npgsql options but **no connection string**.
It avoids executing Program, reading runtime secrets, running seeding/outbox jobs or connecting to
a database during scaffolding. Any future EF database operation requires an explicit, separately
approved connection (for example the EF command's connection override); this factory does not load
runtime appsettings or process database configuration. Normal runtime DI remains unchanged.

An isolated temporary dotnet-ef 10.0.13 tool generated the migration. The existing floating health
check dependency resolved EF runtime 10.0.15 and the tool emitted an older-tool warning; generation
succeeded. No production package versions were changed. The team should agree on pinning/tool
alignment separately instead of mixing dependency changes into this feature.

```powershell
dotnet restore TaskPulse.slnx
dotnet build TaskPulse.slnx --no-restore
dotnet test TaskPulse.slnx --no-restore
git diff --check
```

The scoped xUnit v3 tests use real HTTP routing, production JWT validation, controllers, MediatR,
FluentValidation and JSON serialization with substituted repositories, UnitOfWork and clock.
Separate tests inspect the Npgsql EF model, generated DDL, migration operations and snapshot, and
simulate provider failures for duplicate mapping. These are **not live PostgreSQL tests** and do
not demonstrate that an applied database enforces constraints or that migrations execute there.

Milestone 3.1B hardening reproduced three failing domain cases before the timestamp fix: repeated
clock, sub-microsecond advance and backward clock. Added coverage also checks creation precision,
removed-creator membership, protected rename fields, missing/empty document IDs, workspace-scoped
duplicates, save-conflict HTTP propagation, exact SQLSTATE filtering, and document-only concurrency
exception translation. Simulated exceptions and substituted repositories do not prove real races.

Implementation verification: .NET SDK 10.0.403; solution restore passed; build passed with 0 warnings
and 0 errors; all 80 tests passed (62 HTTP/application cases and 18 domain/model/migration/error-
mapping cases), with none failed or skipped. The snapshot consistency check passed without a
database. The earlier test-harness deprecation/cancellation warnings were fixed, not suppressed.

Before an owner-approved PostgreSQL run: confirm the exact disposable database and existing applied
migration history, review the SQL with the migration owner, and check startup effects. Compose
enables startup migrations; Program also runs admin seeding and scheduled outbox jobs can write.
Do not start it against an unknown/shared database or treat a GET-only smoke test as write-free.
No database update, API startup, Compose startup, commit, push or PR is part of this implementation.

## Frontend handoff (not implemented here)

Replace the persistence boundary behind `DocumentWorkspaceService` with authenticated HttpClient
calls to these routes, using the existing API URL/interceptor/error helpers. Map `googleFileId` to
the editor's `googleDocumentId`; map `Manual`/`Uploaded` to `manual-link`/`google-drive-upload`,
`mimeType` to `fileMimeType`, and `createdAtUtc` to `addedAt`. For DOCX, derive googleDriveFileId
from that same canonical ID and construct Google URLs locally through the existing validated tools.
Keep the editor and archived viewer unchanged.

After browser upload succeeds, register metadata. On registration failure retry registration, not
the upload; never automatically delete the Google file. Clear user-scoped caches on logout or user
change, handle late responses, and do not silently fall back to local-only persistence as if shared.
Legacy localStorage records need an explicit reviewed import and mock-workspace-to-real-ID mapping;
they must not be automatically uploaded into the team's shared library.
