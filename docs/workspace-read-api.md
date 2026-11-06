# Workspace read API (Milestone 3.0A)

## Contract

Both endpoints require the existing TaskPulse bearer JWT. The actor comes only from its verified
`sub` claim, never a query/body parameter. Missing, malformed and empty GUID subjects return 401.

- `GET /api/workspaces`: 200 with an array of the current member's workspaces, including archived
  ones. No memberships returns `[]`. Ordered by creation time then workspace ID.
- `GET /api/workspaces/{workspaceId:guid}`: 200 for a current member, including archived workspaces.
  Nonmembers and missing workspaces both return 404 `Workspace.NotFound`. Global roles do not
  bypass membership. A malformed GUID does not match the route (404); an empty GUID is rejected
  by FluentValidation (400).

Each item has `id`, `name`, `myRole`, `memberCount`, `isArchived`. `myRole` is `Owner`, `Admin` or
`Member`, following the existing backend string-enum converter. The Angular mock's lowercase roles
will need an explicit adapter when frontend integration is implemented. Membership rosters,
invitations and creator information are not returned.

The existing workspace-list repository method now loads members eagerly and uses no tracking so
counts and roles are available without N+1 queries. Detail uses the existing member-loading method.
Existing mutation routes, authentication configuration and create-response DTO are unchanged.

## Automated verification

Prerequisite: a .NET 10 SDK (a runtime alone cannot build or test), plus access to the configured
NuGet feed on first restore. From the repository root:

```powershell
dotnet restore TaskPulse.slnx
dotnet build TaskPulse.slnx --no-restore
dotnet test tests/WorkspaceReadApi.Tests/WorkspaceReadApi.Tests.csproj --no-build --no-restore
git diff --check
```

There was no existing test project. The new, isolated xUnit v3 project uses ASP.NET Core TestHost
and the production presentation/application registrations. It exercises real JWT signature
validation, authorization middleware, routing, controllers, MediatR, validators, and JSON mapping.
Signing keys are random, in-memory and test-only. Persistence alone is replaced by a repository
fake containing domain-created workspaces and memberships. The application startup/seeder is not run.

These are HTTP/application tests, not PostgreSQL integration tests. They do not prove EF SQL
translation, query counts, database connectivity, deployment configuration or full startup behavior.
No SDK was available in the implementation environment, so compilation, restore and execution must
be verified in an SDK-equipped environment before committing or merging.

## Manual API verification

1. Use an existing disposable development PostgreSQL database with the current schema. This
   repository has no tracked EF migration baseline; agree on schema provisioning with the backend
   owner. Read endpoints require no new tables or migrations. Do not reset a database to test them.
2. Configure the existing database/JWT/email/admin settings through your approved local mechanism.
   Do not paste configuration, tokens, or passwords into tickets or shared logs. Full application
   startup runs the existing admin seeder, so use development infrastructure only.
3. Once the SDK, schema and configuration are ready, start the API:

   ```powershell
   dotnet run --project src/Api/Api.csproj --launch-profile https
   ```

   If that profile is not configured locally, use the existing development launch configuration.
   Use the HTTPS address reported by the running app; do not disable certificate validation.
4. In a local API client, log in through `POST /api/auth/login` using a verified disposable TaskPulse
   account. Keep the returned access token private. Use it as Bearer authorization, or use Swagger's
   Authorize control in Development. Never use a Google token for these endpoints.
5. Using the existing create/invite/accept routes or approved fixtures, prepare one owned workspace,
   another membership, an archived workspace, and a private workspace belonging to a second account.
   Repeat with Owner/Admin/Member memberships. Confirm persisted membership counts.
6. `GET /api/workspaces`: expect 200 with only that user's memberships, correct role/count and both
   archive states. A second verified account with no memberships must receive `[]`. Adding a
   `?userId=` parameter for another account must not change the actor.
7. `GET /api/workspaces/{id}`: expect the same five fields for an accessible workspace. Request a
   private workspace and a random nonexistent GUID: both must return 404 without workspace data.
   A global Admin who is not a workspace member must also be denied.
8. Remove the bearer header, use a malformed token, or use an expired token: expect 401 on both
   endpoints. Use the automated tests for validly signed missing/malformed/empty `sub` claims;
   do not mint or change production credentials to exercise those cases.
9. Request `/api/workspaces/not-a-guid`: expect 404. Request the all-zero GUID: expect 400. Confirm
   an archived workspace remains readable to members. Remove a disposable membership using the
   existing authorized route, then confirm subsequent reads no longer expose it.

No manual HTTP or database checks were performed during implementation. Angular authentication,
workspace synchronization, CORS, document metadata, Google OAuth and Drive permissions are outside
this milestone. No commits or pushes are part of this task.
