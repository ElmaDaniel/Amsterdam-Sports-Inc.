# Membership System POC

Backend API for a sports club membership system (Amsterdam Sports Inc.,
via Yellowtail). Lets an administrator list, view, create, edit, and
remove members; assign members to sports; and list the sports a branch
offers (sports themselves are seed/admin data, not created via the API).
Multi-tenant across branches; no authentication (per the client brief —
see [docs/specs/brief.md](docs/specs/brief.md)). Backend only: a
separate team builds the frontend against this API later.

Built story-by-story via the `adlc` skill
([.claude/skills/adlc/SKILL.md](.claude/skills/adlc/SKILL.md)), test-first,
inside-out (domain → use cases → adapters → presentation → integration
tests). Full spec, acceptance criteria, decisions, and a phase-by-phase
build log: [docs/specs/membership-system.md](docs/specs/membership-system.md).

## Stack

C# / .NET 10, ASP.NET Core Web API (controllers), EF Core + SQLite
(file-based, no Docker), xUnit, `NetArchTest.Rules` for architecture
boundary enforcement, Swagger UI (`Swashbuckle.AspNetCore.SwaggerUI`)
for manual API exploration in Development.

## Running the system

```
dotnet run --project src/MembershipSystem.Api
```

Creates `src/MembershipSystem.Api/data/app.db` (migrations run
automatically on startup) and serves on the URL printed in the console
— `http://localhost:5081` by default (see `Properties/launchSettings.json`;
pass `--urls http://localhost:<port>` to override). Uploaded photos are
written to `src/MembershipSystem.Api/data/photos/`.

**Swagger UI**: `http://localhost:5081/swagger` (Development only) —
browse and call every endpoint interactively without a separate HTTP
client. Backed by the OpenAPI spec at `/openapi/v1.json`.

**Branches can now be created via `POST /branches`**, or seeded with
demo data:

```
dotnet run --project src/MembershipSystem.Api   # once, to create the db via migrations
dotnet run --project tools/MembershipSystem.Seed
```

The seed tool (`tools/MembershipSystem.Seed`) loads two branches (North
Amsterdam, South Amsterdam), five sports split across them, and four
members with sport assignments — idempotent, safe to re-run (it skips
seeding if any branch already exists). See
[docs/specs/membership-system.md](docs/specs/membership-system.md)'s
Notes for the exact seeded data.

## Running the tests

```
dotnet test
```

Runs all 127 tests across every layer:

| Project | Tests | What it exercises |
|---|---|---|
| `MembershipSystem.Domain.Tests` | 26 | Entity invariants, no I/O |
| `MembershipSystem.UseCases.Tests` | 30 | Use cases against hand-written in-memory fakes |
| `MembershipSystem.Adapters.Tests` | 20 | EF Core repositories + photo store against a real SQLite database and real disk |
| `MembershipSystem.Api.Tests` | 24 | Controllers directly, against real use cases + in-memory fakes (no HTTP) |
| `MembershipSystem.ArchTests` | 9 | Layer boundary rules (NetArchTest) |
| `MembershipSystem.IntegrationTests` | 18 | Full ASP.NET Core pipeline, real SQLite file + real disk, real HTTP incl. multipart photo upload |

Every acceptance criterion in the spec except AC13/AC14 (sport
creation, removed — see Gotchas) is referenced by name in at least one
test.

## Public API

All routes are scoped under a branch (the tenant boundary):

| Method | Route | Purpose |
|---|---|---|
| GET | `/branches` | List all branches |
| POST | `/branches` | Create a branch |
| GET | `/branches/{branchId}/members` | List members (+ sport names) |
| GET | `/branches/{branchId}/members/{memberId}` | Member detail (+ photo path, sports) |
| POST | `/branches/{branchId}/members` | Create a member |
| PUT | `/branches/{branchId}/members/{memberId}` | Update name + sport associations |
| DELETE | `/branches/{branchId}/members/{memberId}` | Remove a member (hard delete) |
| PUT | `/branches/{branchId}/members/{memberId}/photo` | Upload a photo (multipart, field name `file`) |
| GET | `/branches/{branchId}/sports` | List sports offered by the branch |

`GET /branches` and `POST /branches` are the only endpoints not scoped
under a specific branch — together they let a caller discover and
create branches without a separate seeding step. There is no `POST
.../sports` — sports are seed/admin data only (see Gotchas).

### Example: create a branch, then a member playing a seeded sport

```bash
curl -X POST http://localhost:5081/branches \
  -H "Content-Type: application/json" \
  -d '{"name":"North Amsterdam"}'
# => 201 {"id":"...","name":"North Amsterdam"}

# Sports aren't created via the API — seed them first:
#   dotnet run --project tools/MembershipSystem.Seed
# then look up a sport id:
curl http://localhost:5081/branches/{branchId}/sports

curl -X POST http://localhost:5081/branches/{branchId}/members \
  -H "Content-Type: application/json" \
  -d '{"firstName":"Ada","lastName":"Lovelace","sportIds":["<sport id>"]}'
# => 201 {"id":"...","firstName":"Ada","lastName":"Lovelace","photoPath":null,"sports":[{"id":"...","name":"Tennis"}]}

curl -X PUT http://localhost:5081/branches/{branchId}/members/{memberId}/photo \
  -F "file=@photo.jpg"
# => 200 {"id":"...", ..., "photoPath":"data/photos/{memberId}.jpg"}
```

Errors: 404 for a not-found branch/member (no body), 400 with
`ValidationProblemDetails` (an `errors` array) for validation failures.

### Layer-by-layer public surface

- **Use cases** (`src/MembershipSystem.UseCases/`): `MemberUseCases`,
  `SportUseCases` — see the spec's Layer Map for full method signatures.
  Every operation returns `UseCaseResult<T>` / `UseCaseResult`
  (`Success` / `NotFound` / `ValidationFailed`).
- **Ports** (`src/MembershipSystem.UseCases/Ports/`): `IMemberRepository`,
  `ISportRepository`, `IBranchRepository`, `IPhotoStore` — the
  interfaces a new adapter (a different database, cloud photo storage,
  etc.) would need to implement.
- **Domain** (`src/MembershipSystem.Domain/`): `Member`, `Sport`,
  `Branch`, and the `MemberId`/`SportId`/`BranchId` value types.

## Architecture

Clean/hexagonal, enforced by `NetArchTest.Rules`
(`tests/MembershipSystem.ArchTests/LayerBoundaryTests.cs`) and by the
project reference graph itself:

```
Domain  <—  UseCases  <—  Adapters
              ^              |
              |              v
           Api (controllers) + Program.cs (composition root)
```

- **Domain** depends on nothing (no EF Core, no ASP.NET Core, no I/O).
- **UseCases** depends on Domain and declares its own ports; never
  references a concrete adapter.
- **Adapters** implement the ports; never referenced by Domain or
  UseCases.
- **Api** controllers depend on UseCases only. `Program.cs` is the only
  place allowed to reference `Adapters` concrete types (wiring them
  behind their ports via DI).

## Gotchas

- **No sport-creation API.** Sports are seed/admin data only — added
  via `tools/MembershipSystem.Seed` or a direct DB insert, same as
  branches originally were. The create-sport endpoint existed briefly
  then was removed at developer request.
- **Branch names aren't required to be unique.** `POST /branches` only
  validates that a name is present, with no duplicate-name check — a
  developer decision, not something the brief specified either way.
- **Photos**: local disk only (`data/photos/`), JPEG/PNG, 5 MB cap. Not
  production-shaped — flagged for the client as needing object storage
  (S3-compatible or similar) before going live.
- **Hard delete**: removing a member permanently deletes the row. No
  membership history is retained — flagged for the client since a real
  club may want historical records.
- **Sports are per-branch**, not global: the same sport name (e.g.
  "Tennis") can exist as a distinct row in each branch. There is no
  cross-branch sport catalog.
- **`Program` is `public partial`** in `Program.cs` — required so
  `WebApplicationFactory<Program>` (Phase 7's integration tests) can
  reference it; harmless in production.

## Deviations from the Phase 1 spec

The spec ([docs/specs/membership-system.md](docs/specs/membership-system.md))
was kept up to date at every phase, so it already reflects what was
actually built rather than what was originally proposed. In summary,
where the built system differs from the initial Phase 1 sketch:

- **Stack**: recorded as .NET 8 in Phase 1; switched to .NET 10 (only
  SDKs available on the build machine). No behavioral effect.
- **`IPhotoStore.Save`**: returns `Task<PhotoSaveResult>` (a
  success/failure record) instead of the originally sketched bare
  `Task<string>`, so format/size rejection (AC12) is a return value, not
  a thrown exception.
- **`UseCaseResult<T>`/`UseCaseResult`**: introduced in Phase 4 as the
  concrete shape for every use case's outcome — Phase 1 only described
  outcomes in prose ("or not-found result").
- **`PhotoReference` value object** (sketched in Phase 1's domain
  layer map): never introduced. `Member.PhotoPath` stayed a plain
  nullable string; nothing needed more.
- **`IClock`** (sketched as a possible port): never introduced. No
  acceptance criterion needed a timestamp.
- **`Member.SportIds` storage**: a comma-joined string column with a
  custom EF `ValueComparer`, not EF Core's built-in primitive-collection
  mapping — that support forces its own per-element conversion which
  conflicted with converting the whole set at once. Pure storage detail;
  `IMemberRepository`'s signature is unchanged.
- **Photo upload endpoint**: the use case and controller's testable
  `SetPhoto(Guid, Guid, Stream, string)` method has no direct HTTP
  binding — the actual routed action is `SetPhotoFromForm(Guid, Guid,
  IFormFile)`, which adapts a real multipart upload into the same call.
  Found only by exercising the endpoint over real HTTP, not by unit
  tests alone.
- **Branch management**: added after the 8-phase pipeline completed —
  `GET /branches`, then `POST /branches` — reversing Decision 1's "no
  branch list/management endpoints." A `tools/MembershipSystem.Seed`
  console tool was also added to seed demo data (2 branches, 5 sports,
  4 members) directly into the database.
- **Sport creation removed**: `POST /branches/{branchId}/sports` (added
  during the original pipeline per Decision 4) was later removed at
  developer request, all the way down through the use case
  (`SportUseCases.CreateSport`) and port
  (`ISportRepository.ExistsByName`/`Add`). AC13 and AC14 no longer have
  an implementation. Sports are back to seed/admin-only data.

Full detail, plus every other point-in-time decision and why, is in the
spec's **Decisions** and **Notes / deviations** sections.

## AI usage

See [AI-USAGE.md](AI-USAGE.md) at the repo root for how AI was used
while building this (per the brief's request), delivered as a separate
document from this README.
