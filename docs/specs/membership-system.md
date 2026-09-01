# Membership System

## Raw ask (verbatim)

> You are working for Yellowtail and have been approached by a client that
> would like a Proof of Concept put together showing how they could
> implement a membership system for their sports club called Amsterdam
> Sports Inc. The club currently offers Tennis, Squash and Football, but
> they are in discussions to extend the sports offered. They require a
> system that will allow a system administrator to see a list of members
> and the sports they play. A member could play more than one sport. The
> chairman of the sports club needs the system to be reachable by people
> using a web browser, both on a mobile and a desktop. The system also
> needs to be multi tenant to support multiple of their branches.
>
> For the POC the client would like us to deliver:
> - A page which shows all members (removable, link to individual page,
>   ability to create a new member)
> - A page which shows a single member (first name, last name, photo,
>   list of all sports they play, ability to edit)
> - A page which shows all the sports
> - Login is not required
>
> Backend only — frontend will be built later, driven by this API.

Full brief: `docs/specs/brief.md`.

## Stack & conventions

- Language/runtime: C#, .NET 10
- Web framework: ASP.NET Core Web API
- Data access: EF Core
- Database: SQLite, file-based (default `data/app.db`, path configurable
  via `Database:Path` in `appsettings.json`), no Docker — `dotnet run`
  applies migrations and creates the file on first run
- Test framework: xUnit
- Photo storage: local disk under `data/photos/`, relative path persisted
  on the member record (see Open Questions — not production-shaped)
- Project layout: `MembershipSystem.slnx` at repo root;
  `src/MembershipSystem.{Domain,UseCases,Adapters,Api}` and
  `tests/MembershipSystem.{Domain,UseCases,Adapters,Api}.Tests`,
  `tests/MembershipSystem.IntegrationTests`,
  `tests/MembershipSystem.ArchTests`. Api project uses controllers
  (`--use-controllers`). `tools/MembershipSystem.Seed` (added
  post-Phase-8) is a standalone console tool referencing Domain and
  Adapters directly — outside the layered application, same category
  as the test projects, not subject to the architecture guardrails.
- Architecture-guardrail tool: `NetArchTest.Rules` 1.3.2, in
  `tests/MembershipSystem.ArchTests`, wired into `dotnet test`

## Restatement

Backend API for a sports club membership system, used internally by a
system administrator (no end-user login). The API must let the admin:
list, view, create, edit, and remove members; see which sport(s) each
member plays (many-to-many — a member can play multiple sports); and
list the sports on offer. The club has multiple branches, and the system
must keep each branch's data isolated (multi-tenant). Sports are today
fixed (Tennis, Squash, Football) but the client is discussing adding
more, so sports must be stored as data, not hardcoded as an enum or
constant set. This repo delivers the backend only; a separate team
builds the frontend against this API later, so response shapes need to
be complete and stable enough to drive a UI without backend involvement.

## Acceptance criteria

1. Given at least one branch and members in it, an admin can retrieve a
   list of all members for that branch, each entry showing at minimum
   first name, last name, and the sport(s) they play.
2. Given a branch with no members yet, the members list returns
   successfully with an empty collection (not an error).
3. An admin can retrieve a single member's detail by ID, showing first
   name, last name, photo (URL/path), and the full list of sports that
   member plays.
4. Requesting a member ID that doesn't exist, or that belongs to a
   different branch than the one specified, returns a not-found result
   — never another branch's data.
5. An admin can create a new member by supplying at minimum first name,
   last name, and zero or more sports to associate at creation time.
   Photo may be omitted at creation.
6. Creating a member with missing required fields (first or last name)
   is rejected with a validation error identifying which field(s) failed.
7. An admin can update an existing member's first name, last name, photo,
   and sport associations (add/remove sports played).
8. An admin can remove an existing member; after removal, that member no
   longer appears in the members list or is retrievable by ID.
9. Removing a member ID that doesn't exist returns a not-found result,
   not a success.
10. An admin can retrieve a list of all sports on offer for a given
    branch, including at least Tennis, Squash, and Football, without any
    code change required to add a new sport (sports are rows in a store,
    not an enum).
11. All member and sport operations are scoped to a branch (tenant); data
    from one branch is never visible or mutable through another branch's
    context.
12. Uploading/attaching a photo whose content is not JPEG or PNG, or that
    exceeds the size cap (5 MB), is rejected with a validation error, not
    silently accepted or crashing the request.
13. ~~An admin can create a new sport within a branch by supplying a
    name...~~ **Removed post-Phase-8** at developer request — the
    create-sport endpoint no longer exists. Sports are seed/admin data
    only now (same as branches originally were), added via
    `tools/MembershipSystem.Seed` or a direct DB insert, not via the
    API. See Notes.
14. ~~Creating a sport with a missing name, or a name that already
    exists within that branch, is rejected with a validation error.~~
    **Removed post-Phase-8** for the same reason — with no create
    endpoint, this validation has nothing to attach to.

## States

- **Members list**: populated; empty (branch has zero members); branch
  not found; unhandled error (500-class)
- **Single member**: found with photo; found without photo (no photo
  uploaded yet); not found; found but belongs to a different branch
  (treated as not found); unhandled error
- **Create member**: success; validation failure (missing name field(s),
  invalid sport reference, invalid photo format/size); unhandled error
- **Edit member**: success; member not found; validation failure (same
  shapes as create); unhandled error
- **Remove member**: success (hard delete); member not found; unhandled
  error
- **Sports list**: populated (Tennis/Squash/Football at minimum, per
  branch); empty (a branch with no sports yet — must not crash);
  unhandled error
- ~~**Create sport**~~: removed post-Phase-8 — no longer a state this
  system exposes over HTTP.

## Out of scope

- Any authentication, authorization, login, or session handling
  (explicitly excluded by the client)
- Any frontend/UI code — this repo is backend-only
- Creating, editing, or deleting sports via the API (as of post-Phase-8:
  the create-sport endpoint was removed at developer request — see
  Notes and Decision 4's revision below; sports are seed/admin data
  only, same status as branches originally had)
- Branch (tenant) management endpoints — **superseded**: `GET
  /branches` and `POST /branches` were added post-Phase-8 (see Notes).
  Branch *editing* remains out of scope.
- Pagination, search, filtering, or sorting of the members list (not
  mentioned in the brief; add only if the client asks)
- Any notification, audit log, or history of changes to a member
- Soft-delete/deactivation of members (hard delete only, Decision 6) and
  history of past membership
- Production-grade photo storage (cloud/object storage), authentication
  on the photo file endpoint, or image resizing/thumbnailing
- A member belonging to more than one branch (Decision 2 — one branch
  per member)
- Sports shared/global across branches (Decision 3 — sports are
  per-branch; the same name, e.g. "Tennis", may exist as a distinct row
  in more than one branch)

## Decisions (resolved 2026-09-01, prior to Phase 2)

These were open questions raised during Phase 1 and resolved by the
developer before proceeding. Recorded as decisions, not left open, so
later phases build against a fixed model. Each is also a candidate
"question for the client" per the brief's ask, since these are exactly
the judgment calls a client conversation would settle for the real
product.

1. **Branch context**: supplied via route parameter
   (`/branches/{branchId}/...`), not a header. `GET /branches` (list)
   and `POST /branches` (create) were both added after Phase 8 (see
   Notes) — a branch-management surface now exists after all, reversing
   the original "no branch list/management endpoints" call. A
   `tools/MembershipSystem.Seed` console tool also seeds two demo
   branches (with sports and members) directly into the database for
   local development/demoing without going through the API.
2. **Member↔branch cardinality**: a member belongs to exactly one
   branch. `Member.BranchId` is a single required reference, not a join
   table.
3. **Sport scoping**: sports are per-branch, not global. Each branch has
   its own set of sport rows; the same sport name (e.g. "Tennis") may
   exist as a distinct row per branch. `Sport.BranchId` is required.
4. **Sport management**: ~~a create-sport endpoint is in scope~~
   **revised post-Phase-8**: the create-sport endpoint was removed at
   developer request. Sports are once again listing-only over HTTP —
   added via `tools/MembershipSystem.Seed` or a direct DB insert.
   "Sports are data, not an enum" is now demonstrated only by the
   storage model (`Sport` is a DB row keyed by `BranchId`+`Name`, not a
   C# enum), not by a create endpoint. Editing/deleting a sport remains
   out of scope, as before.
5. **Photo constraints**: accept `image/jpeg` and `image/png` only, cap
   at 5 MB. Anything else (wrong content type or over the cap) is a
   validation failure (AC 12). Storage remains local disk — flagged in
   Phase 8 docs as a POC shortcut; production would need object storage
   and this constraint set revisited with the client.
6. **Delete semantics**: hard delete. Removing a member actually deletes
   the row; no soft-delete/status field. Flagged in Phase 8 docs as a
   choice the client should confirm for the production system, since
   membership history may matter there.

## Open questions

7. No specific error-handling or response-shape conventions were given.
   This spec assumes standard REST conventions (404 for not-found, 400
   for validation, 200/201/204 for success) unless the client has an
   existing API style guide to follow.

## Layer map

### Domain

- **Member** (entity, implemented): Id (`MemberId`), BranchId
  (`BranchId`, required, single), FirstName, LastName, PhotoPath
  (nullable string), SportIds (`IReadOnlySet<SportId>`). Invariants
  enforced in the constructor and `Rename`: FirstName and LastName
  required and non-empty (`ArgumentException`). `AssignSport(Sport)`
  throws `InvalidOperationException` if the sport's BranchId doesn't
  match the member's own — the only way this invariant can be checked
  without Member reaching outward, since the caller must hand it an
  already-loaded `Sport` from the correct branch. `AssignSport` is
  idempotent (a `HashSet<SportId>` backing field); `RemoveSport` is a
  no-op if the sport wasn't assigned. `SetPhotoPath` has no format/size
  validation at the domain level — that's `IPhotoStore`'s contract
  (Phase 5), since it requires inspecting actual bytes.
- **Sport** (entity, implemented): Id (`SportId`), BranchId
  (`BranchId`, required), Name. Invariant: Name required and non-empty
  (`ArgumentException`). Cross-branch name uniqueness is deliberately
  *not* a domain invariant — two Sport instances with the same Name in
  different branches are both valid (`SportTests
  .Two_Sports_In_Different_Branches_Can_Share_The_Same_Name`).
  Within-branch uniqueness (AC 14) requires querying existing rows, so
  it lives at the use-case/repository boundary (`ISportRepository
  .ExistsByName`), not here.
- **Branch** (entity, implemented): Id (`BranchId`), Name. Invariant:
  Name required and non-empty (`ArgumentException`). Used as the tenant
  boundary; referenced by Id from Member and Sport.
- `BranchId`, `SportId`, `MemberId` (implemented): `readonly record
  struct` wrapping a `Guid`, each with a static `New()` factory. Kept
  distinct types (not raw `Guid` or a single generic `Id<T>`) so a
  `SportId` can never be passed where a `MemberId` is expected.
- **PhotoReference**: not introduced. `PhotoPath` stayed a plain
  nullable `string` on `Member` — nothing in Phase 3 needed more than
  that, and adding a wrapper type with no behavior would be premature
  structure. Revisit only if Phase 5 needs more than a path.

### Use case

Implemented as two classes, `MemberUseCases` and `SportUseCases`
(`src/MembershipSystem.UseCases/`), grouped by aggregate rather than one
per operation. Each operation returns `UseCaseResult<T>` (or
`UseCaseResult` for the void case) — a single outcome type with
`Success`/`NotFound`/`ValidationFailed` variants — instead of throwing,
so Phase 6 controllers can map outcomes to HTTP status without
exception-based control flow for expected results.

- `MemberUseCases.ListMembers(branchId)` →
  `UseCaseResult<IReadOnlyList<MemberSummary>>` — `NotFound` if the
  branch doesn't exist (AC1/AC2)
- `MemberUseCases.GetMember(branchId, memberId)` →
  `UseCaseResult<MemberDetail>` — `NotFound` for unknown member or one
  in a different branch (AC3/AC4)
- `MemberUseCases.CreateMember(branchId, firstName, lastName, sportIds)`
  → `UseCaseResult<MemberDetail>` — `NotFound` if branch doesn't exist,
  `ValidationFailed` for missing names (AC6) or a `sportId` not
  belonging to the branch
- `MemberUseCases.UpdateMember(branchId, memberId, firstName, lastName,
  sportIds)` → `UseCaseResult<MemberDetail>` — replaces the member's
  sport set wholesale with the given `sportIds` (AC7)
- `MemberUseCases.SetMemberPhoto(branchId, memberId, content,
  contentType)` → `UseCaseResult<MemberDetail>` — kept as its own
  operation (not folded into `UpdateMember`) since it's the only
  operation that takes binary content rather than plain data;
  `ValidationFailed` surfaces `IPhotoStore`'s format/size rejection
  (AC12) verbatim
- `MemberUseCases.RemoveMember(branchId, memberId)` → `UseCaseResult` —
  `NotFound` for unknown member (AC8/AC9), hard delete via
  `IMemberRepository.Remove`
- `SportUseCases.ListSports(branchId)` →
  `UseCaseResult<IReadOnlyList<SportSummary>>` — `NotFound` if branch
  doesn't exist, empty list if branch has no sports yet (AC10)
- ~~`SportUseCases.CreateSport(branchId, name)`~~ — **removed
  post-Phase-8** at developer request. See Notes.
- `BranchUseCases.ListBranches()` → `UseCaseResult<IReadOnlyList
  <BranchSummary>>` — added post-Phase-8; always `Success` (branches
  aren't tenant-scoped by anything themselves), empty list if none
  exist. Not tied to a numbered acceptance criterion — see Notes.
- `BranchUseCases.CreateBranch(name)` → `UseCaseResult<BranchSummary>`
  — added post-Phase-8; `ValidationFailed` for a missing/empty name
  (same pattern as `CreateSport`), no uniqueness check (branch names
  aren't required to be unique — developer decision, not a client ask).
  Not tied to a numbered acceptance criterion.

### Ports

Interfaces the use case layer declares, implemented by adapters:

- `IMemberRepository`
  - `Task<IReadOnlyList<Member>> ListByBranch(BranchId branchId)`
  - `Task<Member?> GetById(BranchId branchId, MemberId memberId)`
  - `Task Add(Member member)`
  - `Task Update(Member member)`
  - `Task Remove(BranchId branchId, MemberId memberId)`
- `ISportRepository` (reduced post-Phase-8, see Notes):
  - `Task<IReadOnlyList<Sport>> ListByBranch(BranchId branchId)`
  - `Task<Sport?> GetById(BranchId branchId, SportId sportId)`
  - ~~`Task<bool> ExistsByName(...)`~~ and ~~`Task Add(Sport sport)`~~
    removed — both existed only for `CreateSport`. Seeding a sport now
    goes directly through `MembershipDbContext.Sports.Add` (the seed
    tool, or integration tests' `MembershipApiFactory.SeedSport`), not
    through this port.
- `IBranchRepository`
  - `Task<Branch?> GetById(BranchId branchId)` — used to validate a
    branch exists/is valid tenant context before other operations
  - `Task<IReadOnlyList<Branch>> ListAll()` — added post-Phase-8, backs
    `BranchUseCases.ListBranches()` and `GET /branches`
  - `Task Add(Branch branch)` — added post-Phase-8, backs
    `BranchUseCases.CreateBranch()` and `POST /branches`
- `IPhotoStore` (implemented as specced, with one addition): `Save`
  returns `Task<PhotoSaveResult>` rather than a bare `Task<string>` —
  `PhotoSaveResult` is a small success/failure record (`IsSuccess`,
  `PhotoPath`, `Error`) so an implementation rejects bad content type or
  over-cap size (AC12) by returning a failure value, not by throwing.
  This is a mechanical refinement of the Phase 1 signature, not a scope
  change — the port's contract (reject `image/jpeg`/`image/png`-only,
  5 MB cap) is unchanged, only how failure is communicated.
  - `Task<PhotoSaveResult> Save(MemberId memberId, Stream content,
    string contentType)`
  - `Task<Stream?> Get(string photoPath)`
  - `Task Delete(string photoPath)`
- `IClock`: not introduced. No timestamp fields were needed by any
  acceptance criterion in Phase 3 or 4.

### Adapters

- `EfMemberRepository`, `EfSportRepository`, `EfBranchRepository`
  (implemented) — EF Core over SQLite, implementing the repository
  ports above with no signature changes. `MembershipDbContext`
  (`src/MembershipSystem.Adapters/MembershipDbContext.cs`) maps `Member`,
  `Sport`, `Branch` via private-field access (`UsePropertyAccessMode
  .Field`), so the domain layer's constructor-enforced invariants and
  lack of public setters/parameterless constructors needed zero changes
  — EF reads/writes through the private `_sportIds` field and the
  existing constructors, never through new EF-only members.
  `Member.SportIds` (a `HashSet<SportId>`, not a navigation to `Sport`)
  is stored as a single comma-joined string column with a custom
  `ValueComparer`, not EF's built-in primitive-collection support —
  that support insists on its own per-element (`SportId → Guid`)
  conversion and conflicted with converting the whole collection at
  once. `BranchId`/`SportId`/`MemberId` each get a `HasConversion`
  round-tripping through `Guid`.
- `LocalDiskPhotoStore` (implemented) — implements `IPhotoStore` against
  a configurable root directory (`data/photos/` at runtime, a temp
  directory in tests). Enforces AC 12 (JPEG/PNG only, 5 MB cap) by
  returning `PhotoSaveResult.Failure(...)`, matching the port's
  contract from Phase 4. Stored filename is `{memberId}{.jpg|.png}`; the
  path returned to the use case is `data/photos/{filename}` regardless
  of the actual root used, so a caller's stored `PhotoPath` string is
  stable across environments.

### Presentation (implemented)

Two ASP.NET Core controllers in `src/MembershipSystem.Api/Controllers/`,
each depending only on its use case class (constructor-injected):
`MembersController(MemberUseCases)`, `SportsController(SportUseCases)`.
Response/request contracts in `src/MembershipSystem.Api/Contracts/`:

- `MemberListItemResponse`: id, firstName, lastName, sports: string[]
- `MemberDetailResponse`: id, firstName, lastName, photoPath (nullable),
  sports: `SportRefResponse[]` ({ id, name })
- `SportResponse`: id, name
- `CreateMemberRequest` / `UpdateMemberRequest`: firstName, lastName,
  sportIds: Guid[]? (nullable, treated as empty when omitted)
- ~~`CreateSportRequest`~~ — removed post-Phase-8 along with the
  create-sport endpoint.
- Errors: ASP.NET Core's built-in `ProblemDetails` (404s) and
  `ValidationProblemDetails` (400s, via the internal `ApiProblemDetails`
  helper) — matches the Decision 7 REST-convention assumption exactly.

`UseCaseResult`/`UseCaseResult<T>` outcomes are mapped once per action
via a `switch` on `Outcome`: `NotFound` → 404, `ValidationFailed` → 400
with `ValidationProblemDetails`, `Success` → 200 (or 201 via
`CreatedAtAction` for creates, 204 for delete). No business logic lives
in the controllers — every branch is a direct status-code mapping.

Endpoints (current, post-Phase-8 additions/removals noted):
`GET /branches/{branchId}/members`,
`GET /branches/{branchId}/members/{memberId}`,
`POST /branches/{branchId}/members`,
`PUT /branches/{branchId}/members/{memberId}`,
`DELETE /branches/{branchId}/members/{memberId}`,
`PUT /branches/{branchId}/members/{memberId}/photo` (multipart
`IFormFile`, field name `file`),
`GET /branches/{branchId}/sports`,
~~`POST /branches/{branchId}/sports`~~ (removed post-Phase-8),
`GET /branches` and `POST /branches` (both added post-Phase-8; see
Notes).

Composition root: `src/MembershipSystem.Api/Program.cs` registers
`MembershipDbContext` (SQLite, path from config — see below) and
`LocalDiskPhotoStore` (path from config), the three `Ef*Repository`
adapters behind their ports, and the two use case classes — this is the
one place in Api allowed to reference `MembershipSystem.Adapters`
concrete types, per the guardrails. Runs `db.Database.Migrate()` on
startup so `dotnet run` needs no separate migration step.

Both paths (`Database:Path`, `PhotoStorage:Path`) are read from
`appsettings.json` (default `data/app.db` / `data/photos`, relative to
content root) rather than hardcoded — added post-Phase-8, see Notes.

## Architecture boundary guardrails

Enforced by `NetArchTest.Rules` 1.3.2 in the
`MembershipSystem.ArchTests` project
(`tests/MembershipSystem.ArchTests/LayerBoundaryTests.cs`), which runs
as part of `dotnet test`:

- Domain (`Member`, `Sport`, `Branch`, invariants) has zero dependencies
  on UseCases, Adapters, Api, EF Core, or ASP.NET Core.
- Use case layer depends on Domain and on the ports it declares
  (`IMemberRepository`, `ISportRepository`, `IBranchRepository`,
  `IPhotoStore`); it must not reference Adapters, Api, EF Core types,
  `DbContext`, or ASP.NET Core request/response types.
- Adapters (`Ef*Repository`, `LocalDiskPhotoStore`) implement the ports
  and depend inward; they must not reference Api.
- Presentation (API controllers/endpoints) depends on Use Case
  operations only; only the composition root (`Program.cs`) may
  reference Adapters concrete types directly, to wire them behind the
  ports UseCases declares — every other type in Api is checked and must
  not reference Adapters.

Also enforced structurally by project references (not just NetArchTest):
Domain has no project reference to UseCases, Adapters, or Api; UseCases
has no reference to Adapters or Api; Adapters has no reference to Api.
A reference in the wrong direction here would be caught even before
NetArchTest runs — proven during Phase 2 by attempting a
`Domain → UseCases` project reference, which MSBuild itself refused
(circular dependency in the restore graph), and separately by adding an
illegal `Adapters` reference from a non-`Program` type in Api, which the
`Api_Should_Not_DependOn_Adapters_Concrete_Types_Outside_Composition_Root`
NetArchTest rule failed on as expected, naming the violating type. Both
violations were reverted immediately after being observed.

## Integration tests

`tests/MembershipSystem.IntegrationTests/` — 18 tests, real ASP.NET Core
pipeline via `WebApplicationFactory<Program>` (`MembershipApiFactory`),
real SQLite file database per test class, real `LocalDiskPhotoStore`
against a real temp directory, real HTTP requests including real
multipart photo uploads. No fakes, no mocks, nothing in-memory-only.

- `MembersEndpointsTests`: create→list, create→get, cross-branch 404
  (AC4), missing-field validation (AC6), update persisting across
  separate requests (AC7), delete then re-delete 404 (AC8/AC9), photo
  upload persisting a real file on disk and both AC12 rejection paths
  over real multipart HTTP, empty-branch list (AC2), unknown-branch 404.
- `SportsEndpointsTests`: empty list (AC10), list returns seeded sports,
  cross-branch isolation (AC11, via `MembershipApiFactory.SeedSport`
  rather than the now-removed create endpoint), unknown-branch 404.

AC13 and AC14 (sport creation, validation) are no longer implemented or
tested — the create-sport endpoint was removed post-Phase-8. Every
other acceptance criterion (AC1–AC12) is still referenced by name in at
least one test somewhere in the solution.

## Progress

| Phase | Status |
|---|---|
| 1 spec | done |
| 2 arch | done |
| 3 domain | done |
| 4 usecase | done |
| 5 adapters | done |
| 6 ui | done |
| 7 integration | done |
| 8 docs | done |

## Notes / deviations

- 2026-09-01: Resolved Phase 1's open questions 1–6 before starting
  Phase 2 (see Decisions section above). Updated acceptance criteria
  (added AC 13/14 for sport creation, tightened AC 10/12), states,
  out-of-scope, and layer map (Member has single BranchId, Sport gained
  BranchId, ISportRepository gained ExistsByName/Add, IPhotoStore's
  Save contract states the format/size limits explicitly) to match.
  Question 7 (error/response convention) remains open — proceeding on
  the stated REST-convention assumption.
- 2026-09-01: Stack originally recorded .NET 8, but this machine only
  has .NET 9/10 SDKs installed. Switched to .NET 10 (developer's choice
  over installing .NET 8 or falling back to 9). No effect on the layer
  map or guardrails — pure runtime-version substitution.
- 2026-09-01: Phase 2 added one throwaway `public sealed class
  ArchTestMarker` per empty layer project (Domain, UseCases, Adapters)
  so NetArchTest had a real type to check before Phase 3+ adds actual
  code. Each is marked for deletion once its layer's real Phase adds
  real types — Phase 3 must delete Domain's, Phase 4 UseCases', Phase 5
  Adapters'. Not a deviation in scope, just phase-ordering scaffolding.
- 2026-09-01: No git repository exists in this folder yet. Not
  addressed in Phase 2 (out of this phase's scope) — flagged here so
  it isn't forgotten; initialize before work accumulates further.
- 2026-09-01: Phase 3 deleted Domain's throwaway `ArchTestMarker` (per
  the Phase 2 note) now that real Domain types exist, and updated
  `LayerBoundaryTests.cs`'s four `Domain_Should_Not_DependOn_*` tests to
  anchor on `typeof(Domain.Member)` instead — mechanical fix, no rule
  changed. UseCases' and Adapters' markers remain until Phases 4 and 5.
- 2026-09-01: `PhotoReference` value object from the Phase 1 layer map
  was not introduced in Phase 3 — `Member.PhotoPath` stayed a plain
  nullable `string`. No behavior in this phase needed the wrapper;
  revisit in Phase 5 only if `IPhotoStore`/adapter needs richer photo
  metadata than a path.
- 2026-09-01: Phase 4 deleted UseCases' throwaway `ArchTestMarker` and
  updated `LayerBoundaryTests.cs`'s three `UseCases_Should_Not_DependOn_*`
  tests to anchor on `typeof(UseCases.MemberUseCases)` — mechanical,
  same pattern as Phase 3's Domain fix. Adapters' marker remains until
  Phase 5.
- 2026-09-01: `IPhotoStore.Save` returns `Task<PhotoSaveResult>` (a
  success/failure record) instead of the Phase 1 layer map's bare
  `Task<string>`, so a failed save (bad content type, over size cap) is
  a return value the use case can turn into `ValidationFailed`, not an
  exception. Same contract, different signature — noted as a deviation
  since it changes the port's method signature from what Phase 1 wrote
  down.
- 2026-09-01: `IClock` was never introduced — no acceptance criterion
  through Phase 4 needed a timestamp. Left out rather than speculatively
  added.
- 2026-09-01: `UseCaseResult<T>`/`UseCaseResult` (Success/NotFound/
  ValidationFailed outcome types) were added in Phase 4 as the return
  shape for every use case operation. Not in the original Phase 1 layer
  map, which only sketched "or not-found result" / "or validation
  failure" in prose — Phase 4 made that concrete as an actual type all
  operations share consistently.
- 2026-09-01: Phase 5 deleted Adapters' throwaway `ArchTestMarker` and
  updated `LayerBoundaryTests.cs`'s `Adapters_Should_Not_DependOn_Api`
  test to anchor on `typeof(Adapters.EfMemberRepository)` — mechanical,
  same pattern as Phases 3 and 4. All three throwaway markers are now
  gone; every arch test anchors on a real type.
- 2026-09-01: `Member.SportIds` mapping required a design decision not
  anticipated in Phase 1: EF Core's built-in primitive-collection
  support forces its own element-level conversion for non-primitive
  element types (here, `SportId`), which cannot compose with a
  whole-collection `HasConversion`. Resolved by storing the set as one
  comma-joined string column (with a custom `ValueComparer` for EF's
  change tracking) instead of using primitive-collection mapping. Pure
  storage-representation choice — `IMemberRepository`'s signatures and
  `Member`'s public API are unchanged.
- 2026-09-01: Per the earlier resolved open question (EF private
  field/collection mapping vs. adding EF-only members to Domain), EF
  reads/writes `Member`/`Sport`/`Branch` entirely through private field
  access and existing constructors. Domain's public API from Phase 3 is
  untouched — no parameterless constructor, no public setters were
  added for EF's benefit.
- 2026-09-01: Phase 6 controllers are unit-tested directly (constructing
  `MembersController`/`SportsController` with a real use-case instance
  wired to Phase-4-style in-memory fakes, no HTTP server, no mocking
  library) rather than via `WebApplicationFactory`, per developer
  decision — full end-to-end HTTP behavior against real adapters is
  Phase 7's job; testing it twice would duplicate coverage the skill
  warns against.
- 2026-09-01: The photo endpoint required a decision not visible from
  unit tests alone: a bare `Stream content, string contentType` action
  signature has no ASP.NET Core model binder for multipart form data,
  so real HTTP requests to it returned no response at all. Fixed by
  adding `SetPhotoFromForm(Guid, Guid, IFormFile)` as the actual
  `[HttpPut]`-routed action, which reads the file's stream/content type
  and delegates to the original `SetPhoto(Guid, Guid, Stream, string)`
  — kept as a plain method so Phase 6's existing unit tests (which call
  it directly) still pass unchanged. Caught only by manually exercising
  every endpoint over real HTTP with `curl`, including a real multipart
  upload — unit tests alone would have missed this, since they call the
  method directly and never go through ASP.NET Core's model binding.
- 2026-09-01: Every endpoint (list/get/create/update/delete/photo for
  members; list/create for sports) was exercised live over real HTTP
  against the running app with a SQLite-backed database and a seeded
  branch — not just via the 24 controller unit tests — covering the
  populated/empty/not-found/validation-failure paths for each. This
  included the two AC12 rejection paths (unsupported content type,
  over-5MB) via real multipart uploads. `UseHttpsRedirection()` prints a
  harmless "Failed to determine the https port" warning under plain
  HTTP in this POC (no HTTPS configured) — not an error, left as-is.
  `UseAuthorization()` was removed from `Program.cs`: no auth is
  configured (per the client brief) and the call was dead middleware.
- 2026-09-01: Phase 7 used `WebApplicationFactory<Program>`
  (`MembershipApiFactory`) rather than duplicating either Phase 5's
  adapter-only tests or Phase 6's controller-direct unit tests — it
  boots the real DI container, real routing, real model binding
  (including real multipart parsing), against a real SQLite file
  database and a real temp photo directory, swapped in only via
  `ConfigureWebHost`/`RemoveAll` for isolation. `Program.cs`'s `public
  partial class Program;` marker (added in Phase 6 to satisfy top-level
  statement visibility) is what makes `Program` referenceable as the
  factory's generic argument from the test assembly.
- 2026-09-01: SQLite/EF Core pools connections even after a `DbContext`
  is disposed, so deleting the per-test-run SQLite file in teardown
  raced an still-open file handle (`IOException`) until
  `SqliteConnection.ClearAllPools()` was added before the delete in
  `MembershipApiFactory`'s `DisposeAsync`. Caught only because the
  factory uses a real SQLite file (not `:memory:`) per test class, to
  mirror how `dotnet run` actually persists data.
- 2026-09-01: All 14 acceptance criteria (AC1–AC14) are referenced by
  name in at least one test across the solution — confirmed by grepping
  every test project for `AC\d+`. No criterion from Phase 1 shipped
  without a test naming it explicitly.
- 2026-09-01: Phase 8 wrote `README.md` at the repo root (architecture,
  how to run/test, public API with a curl example, gotchas, and a
  "Deviations from the Phase 1 spec" summary) and changed no code. Full
  test suite re-run to confirm: 126/126 passing, unchanged from Phase 7.
  The brief separately asks for `AI-USAGE.md` at the repo root — that
  file does not exist yet. It's explicitly out of this spec's scope
  (the brief says it's "delivered separately," and its content is an
  honest account only the developer can give) — not authored here.
  README.md points to it; create it before calling the assignment
  complete.
- 2026-09-01: Added `GET /branches` (list all branches) after the
  8-phase pipeline had already completed, at the developer's explicit
  request to add it directly rather than reopen the pipeline as a new
  story. This **reverses part of Decision 1** ("no branch list/
  management endpoints in this POC"): a list endpoint now exists,
  though branch *creation* still does not — branches remain
  seeded/pre-existing. Built following the same test-first,
  layer-by-layer shape as the rest of the system even though it wasn't
  run through `/adlc`: `IBranchRepository.ListAll()` added to the port,
  `EfBranchRepository.ListAll()` implemented and tested against real
  SQLite (2 new adapter tests), `BranchUseCases.ListBranches()` added
  and unit-tested against a fake (2 new use-case tests), `BranchSummary`
  DTO added, `BranchesController` (`GET /branches`) added and
  unit-tested (2 new API tests), one new integration test over real
  HTTP, and wired into `Program.cs`. All 14 numbered acceptance criteria
  are unaffected — this endpoint isn't tied to a client-requested AC, so
  it isn't numbered among them. Full suite after: 133/133 passing (up
  from 126). Confirmed `BranchesController` has no reference to
  `Adapters`, consistent with the existing guardrails.
- 2026-09-01: Added `POST /branches` (create a branch), again at the
  developer's explicit request to add directly rather than reopen
  `/adlc`. This **fully reverses Decision 1**'s "no branch list/
  management endpoints in this POC" — branches can now be listed and
  created via the API, though a real client conversation should still
  confirm whether the production system wants this exposed the same
  way. Same test-first, layer-by-layer shape as every other addition:
  `IBranchRepository.Add(Branch)` added to the port,
  `EfBranchRepository.Add()` implemented and tested against real SQLite
  (1 new adapter test), `BranchUseCases.CreateBranch(name)` added and
  unit-tested (2 new use-case tests: success, missing-name rejection),
  `CreateBranchRequest` DTO added, `BranchesController.Create` (`POST
  /branches`) added and unit-tested (2 new API tests), 2 new integration
  tests over real HTTP (create-then-list, missing-name 400). Validation
  is name-required only, matching `CreateSport`'s pattern — no
  uniqueness check, since nothing suggests branch names must be unique
  (a developer decision, not derived from the brief). Full suite after:
  141/141 passing (up from 133).
- 2026-09-01: Added `tools/MembershipSystem.Seed`, a standalone console
  tool (referencing Domain and Adapters directly, outside the layered
  application and its guardrails) that seeds two demo branches (North
  Amsterdam, South Amsterdam), five sports split across them
  (Tennis/Squash/Football for North, Tennis/Squash for South —
  deliberately distinct rows per branch per Decision 3), and four
  members with sport assignments (including one member, Margaret
  Hamilton, with no sport yet, to demonstrate that's valid). Run via
  `dotnet run --project tools/MembershipSystem.Seed` against an
  already-migrated `data/app.db` (the API must be run once first so
  migrations create the file). Idempotent: exits without changes if any
  branch already exists, rather than duplicating data on a second run.
  Verified live over real HTTP after seeding: `GET /branches` and both
  branches' `/members` and `/sports` endpoints returned the expected
  seeded data.
- 2026-09-01: Removed `POST /branches/{branchId}/sports` (create a
  sport) at developer request, all the way down the stack — this is
  the mirror image of the `POST /branches` addition earlier the same
  day, and **reverses Decision 4** ("a create-sport endpoint is in
  scope"). Removed: `SportsController.Create` action,
  `CreateSportRequest` DTO, `SportUseCases.CreateSport`, and
  `ISportRepository.ExistsByName`/`Add` (both existed only to support
  `CreateSport`; nothing else called them — confirmed by checking every
  caller before removing). `EfSportRepository`'s matching
  implementations removed too. AC13 and AC14 (sport creation, its
  validation) are no longer implemented — see the struck-through
  acceptance criteria above. Sports are back to being seed/admin-only
  data, same status as branches had before this session's earlier
  change reversed that for branches. The seed tool
  (`tools/MembershipSystem.Seed`) is unaffected — it always seeded
  sports via `MembershipDbContext.Sports.AddRange` directly, not
  through the port or use case. Test fallout: 14 tests removed (7 unit
  tests for `CreateSport`/`Create` across `SportUseCasesTests` and
  `SportsControllerTests`, 5 integration tests, plus 2 adapter tests
  for `ExistsByName`); `EfSportRepositoryTests`,
  `SportsEndpointsTests`, and `MembersEndpointsTests` (which had used
  the create-sport endpoint purely as test setup) were rewritten to
  seed sports directly via EF Core instead — a new
  `MembershipApiFactory.SeedSport` helper was added for the integration
  tests, mirroring the existing `SeedBranch`. Full suite after:
  127/127 passing (down from 141, as expected — no functionality lost
  elsewhere, only removed). Verified live: `GET
  /branches/{id}/sports` still returns seeded sports; `POST` to the
  same route now returns 405 (Method Not Allowed) rather than routing
  to a handler, confirming the action is genuinely gone rather than
  just erroring.
- 2026-09-02: Moved the SQLite database path and photo storage path out
  of hardcoded strings in `Program.cs` into `appsettings.json`
  (`Database:Path`, `PhotoStorage:Path`; both default to `data/app.db`
  / `data/photos`, resolved relative to content root). Prompted by the
  seed tool's fragile hardcoded relative path
  (`../../../../..` up from its own build output to guess the Api
  project's `data/app.db`). The seed tool
  (`tools/MembershipSystem.Seed`) now reads the same `Database:Path`
  setting directly from the Api project's `appsettings.json`
  (`Microsoft.Extensions.Configuration.Json`, added as a package
  reference) instead of guessing — one source of truth for the path,
  both projects agree on it. `--url`-style override still supported: a
  positional argument to the seed tool still takes precedence if given
  explicitly. Full suite unaffected (127/127 passing) since integration
  tests resolve their own isolated SQLite path via
  `MembershipApiFactory`, never through this config. Verified live:
  cleared `data/`, ran the Api once (created the db at the
  config-resolved path), stopped it, ran the seed tool (resolved the
  identical path via the Api's `appsettings.json` and seeded
  successfully), restarted the Api, confirmed `GET /branches` and a
  branch's `/sports`/`/members` all returned the seeded data.
