# AI Usage

This document is a factual account of how Claude Code was used to build
this repository, drawn directly from the session transcript. Quotes are
verbatim. Where the transcript shows an outcome but not my reasoning for
it, I've left a placeholder for myself to fill in rather than let the
assistant guess at my intent.

## Tools and models

- **Claude Code** (CLI), model Sonnet 5, running in a VS Code-integrated
  terminal session on Windows (PowerShell/Git Bash tool access).
- A custom Claude Code **skill**, `adlc` (`.claude/skills/adlc/SKILL.md`),
  authored earlier in the session specifically for this project. It
  encodes a fixed 8-phase pipeline — spec → architecture guardrails →
  domain → use case → adapters → presentation → integration tests →
  docs — run one phase per invocation, test-first, inside-out, with a
  progress table in the spec file so it's resumable across sessions.
- No other AI tools, plugins, or code-generation assistants were used in
  this session.

## Process overview

1. Drafted the `adlc` skill itself (merging my own draft with Claude's
   proposed version — see below) before touching the assignment.
2. Wrote `CLAUDE.md` with fixed project facts (client, scope, no-auth
   constraint, multi-tenant requirement, sports-as-data requirement).
3. Provided the brief (`docs/specs/brief.md`) and asked Claude to pick a
   stack, with open questions raised explicitly rather than assumed.
4. Ran the `adlc` pipeline phase-by-phase (spec → arch → domain →
   usecase → adapters → ui → integration → docs), reviewing and
   resolving open questions between phases.
5. After the pipeline completed, made several direct, explicit changes
   outside the pipeline (branch list/create endpoints, Swagger UI fix,
   seed data tooling, removing the create-sport endpoint) — each done
   without re-invoking `/adlc`, at my explicit instruction each time.
6. Closed the session with an advisory-only discussion (no code
   changed): confirmed the codebase is clean architecture and that this
   was the original ask, then asked Claude to weigh clean architecture
   against the factory pattern, then posed a "worldwide release, load
   issues" scenario, then specifically asked about CQRS as an
   alternative. See "Architecture discussion" below.

## Significant prompts

Quoted verbatim, in order:

> "I'm about to start a take-home backend assignment — a membership system POC where clean architecture matters to the client. Before I touch any code, I want a reusable process: a Claude Code skill I can invoke by name that takes a user story through a written spec with acceptance criteria, architecture boundary guardrails, then clean-architecture layers built inside-out (domain, use case, adapters, presentation) — each written tests-first — then integration tests, then docs. It should run one phase at a time, never jump ahead even if a phase finishes fast, and be resumable mid-pipeline (e.g. via a progress table in the spec file)."

> "Yes — project-level, not user-level: this should only apply to this repo, not leak into other projects I work on. Before you finalize it, I also have my own draft of this same skill that I put together separately — here's what I have: [ADLC draft pasted in full]"

> "Now draft CLAUDE.md for this repo's root. It should capture the fixed project facts below as terse bullets, not prose..."

> "We're doing a take-home backend assignment for a job application — the brief is in docs/specs/brief.md, read it in full before responding. Fixed by the client, not up for debate: no login/auth, backend only, multi-tenant (multiple branches), sports are data not a fixed enum. Everything else is my call and genuinely undecided: language/framework, database, ORM or data-access approach, how member photos get stored. Ask me whatever you need to make a real recommendation on those — don't assume defaults. Give me options with tradeoffs for a POC of this size, not just one answer."

> "collect every open question we resolved with an assumption" (asked twice, at different points in the session)

> "did we use sql lite ?"

> "run the application"

> "is swagger configured?"

> "configure swagger UI so I can test the apis myself, or do you have another way to test?"

> "add a new api endpoint to get all the branches"

> "create seed files for initial data load, seed files for 2 branches, seed file for few sports across the 2 branches, some member and etc" (sent mid-turn, while a prior tool call was still running)

> "- remove create sports endpoint\n- in get member api, include the photo too"

> "which is better clean architecture or factory pattern as we may have multiple branches tomorrow"

> "what if the app is released worldwide tomorrow  and faces load issue?"

> "I wasnt talking about sql lite or local disk storage, I was wondering if CQRS would be a better option."

## What you generated before I wrote any code

Before any project code existed, Claude produced:

- A first draft of `.claude/skills/adlc/SKILL.md` in response to the
  initial skill request, then merged it with my own separately-drafted
  version (see "Where I corrected drift" below) into the final file
  actually used for the rest of the session.
- `CLAUDE.md` — project facts as terse bullets (client, scope, auth
  exclusion, multi-tenancy, sports-as-data, spec location, process
  pointer to `adlc`).
- A stack recommendation (unprompted specifics, prompted by my request
  for "options with tradeoffs"): ASP.NET Core Web API on .NET, EF Core,
  SQLite file-based with no Docker, local-disk photo storage — presented
  with tradeoffs rather than a single answer, per my instruction.
- The full Phase 1 spec (`docs/specs/membership-system.md`): restated
  ask, 14 numbered acceptance criteria, states table, out-of-scope list,
  a full layer map (domain entities/invariants, use-case operations,
  four ports with method signatures, adapters, presentation view
  models), and 7 open questions — before any implementation code was
  written, per the `adlc` skill's own Phase 1 rule ("Write no
  implementation, tests, or stories in this phase").

## What I kept, changed, or threw away

- **Kept**: the `adlc` skill's phase structure, its one-phase-per-run
  and tests-first rules, the overall stack recommendation (ASP.NET
  Core / EF Core / SQLite), the layer map and port designs from Phase 1,
  the `UseCaseResult<T>` outcome-type pattern introduced in Phase 4.
- **Changed via direct instruction, not through the pipeline**:
  - Added `GET /branches` and `POST /branches` after the 8-phase
    pipeline had already completed — explicitly instructed to add
    directly rather than reopen `/adlc` as a new story, reversing part
    of Phase 1's Decision 1 ("no branch list/management endpoints").
  - Added a Swagger UI (`Swashbuckle.AspNetCore.SwaggerUI`) for manual
    testing — not part of the original spec.
  - Added `tools/MembershipSystem.Seed`, a standalone console tool
    seeding 2 branches, 5 sports, and 4 members — created after I asked
    for "seed files for initial data load" with specific content
    (2 branches, sports across them, some members).
  - Removed the create-sport endpoint (`POST
    /branches/{branchId}/sports`) entirely — controller action, DTO,
    use case method, and the two port methods that existed only to
    support it — after instructing "remove create sports endpoint."
    This had been added during the original Phase 1–8 pipeline (Decision
    4) and was then fully reversed afterward.
  - [MY REASONING: why branch list/create was added directly rather
    than as a new `/adlc` story — ]
  - [MY REASONING: why create-sport was removed after having been
    explicitly built into scope during Phase 1 — ]
- **Explicitly declined**: when Claude asked which endpoint needed a
  photo field added ("GET /branches/{branchId}/members (the list)" vs.
  "something else"), I answered "never mind no change" — the existing
  `GET .../members/{memberId}` detail endpoint already returned
  `photoPath`, so no change was made.
- **Never written**: `AI-USAGE.md` itself was flagged by Claude at the
  end of the Phase 8 docs run as required by the brief but out of the
  spec's scope to author, since "its content is an honest account only
  the developer can give." It remained unwritten until this document.

## How I kept context

- A single markdown spec file (`docs/specs/membership-system.md`) with
  a progress table, resumed and updated at the start/end of every
  `/adlc` phase — this was the entire point of the `adlc` skill design.
- `CLAUDE.md` at the repo root, holding fixed project facts so they
  didn't need re-explaining each session.
- Explicit "Notes / deviations" log inside the spec file, updated at
  every phase and every post-pipeline change, recording what changed
  and (where stated) why — this is the running record most of this
  document's factual content is drawn from.
- Between-session continuity relied on re-invoking `/adlc next` (or a
  named phase) and Claude re-reading the spec's progress table each
  time, rather than on conversation memory.

## Where I corrected drift

- **The `SKILL.md` location**: after Claude drafted and saved the
  `adlc` skill, I corrected it to be **project-level**
  (`.claude/skills/`), not user-level: "this should only apply to this
  repo, not leak into other projects I work on." Claude had saved it at
  user level (`~/.claude/skills/`) initially and removed that copy after
  the correction.
- **Merging two independent `adlc` drafts**: I had written my own
  version of the skill separately from Claude's first draft and asked
  Claude to compare the two and merge them, rather than accept either
  draft as-is.
- **Photo-on-member-detail**: Claude initially interpreted "include the
  photo too" as ambiguous between the list endpoint and the detail
  endpoint, asked a clarifying question, and I clarified no change was
  needed — the field already existed on the detail response.
- **Seed tool not run / empty `GET /branches`**: reported twice as a bug
  ("no sample data for get branches" and later "seed did not run, get
  branches returning empty array"). Root cause both times was that the
  local `data/` directory had been deleted or the database left
  unseeded during unrelated testing (e.g. clearing state to test the
  Swagger fix), not a defect in the seed tool itself. Fixed both times
  by re-running `tools/MembershipSystem.Seed` and restarting the API,
  then verifying via direct HTTP calls to every affected GET endpoint.
- Following the second occurrence, I set a standing instruction: "when
  ever there is an update in api make sure the seed is also run" —
  Claude confirmed this as an ongoing practice (stop the running
  instance → rebuild → run the seed tool, idempotent → restart → verify
  GET endpoints return real data) rather than a one-time fix.

## Where I didn't use AI

[MY REASONING/FACTS: fill in anything done outside this Claude Code
session — e.g. any manual edits, research, or decisions made without
asking Claude, that wouldn't show up in this transcript. Nothing in the
session transcript itself indicates work done elsewhere, since Claude
cannot see outside its own tool calls.]

## Where you got it wrong

- **Saved the `adlc` skill at user level instead of project level** on
  first creation — corrected immediately when I pointed it out (see
  "Where I corrected drift").
- **Swagger UI showed a schema editor instead of a file-picker** for the
  photo-upload endpoint on the first attempt at wiring it in. The
  built-in `AddOpenApi()` generator has no native handling for
  `IFormFile`; Claude's first fix attempt (an `IOpenApiOperationTransformer`
  matching on parameter type) silently failed to fire at all — the
  generated spec was unchanged after the "fix." This was only caught
  because I reported "Im not seeing choose a file" with a screenshot;
  Claude then re-diagnosed, switched the transformer to match by route
  + HTTP method instead of by parameter type, and verified by inspecting
  the raw generated OpenAPI JSON directly before declaring it fixed.
- **Running the seed tool against a live, already-running API instance
  crashed the API process** (a SQLite file-lock conflict between the two
  processes) the first time it was tried this way. The app recovered
  cleanly on restart with no data loss, but this is what led to the
  "stop the app before seeding" sequencing that's now standing practice.
- **Empty `GET /branches` after the Swagger fix work** — the local
  database had been deleted during testing and not reseeded afterward;
  reported by me as "no sample data for get branches," root-caused and
  fixed in the same turn.
- **Empty `GET /branches` a second time** — reported again later
  ("seed did not run"); this time the seed tool's own idempotency check
  confirmed no branch had existed before that run (it did not print its
  "already exists, skipping" message), consistent with the database
  having been in an unseeded state rather than the seed tool failing to
  execute correctly.
- A `curl` multipart syntax error on my end (an unsupported
  `;type=image/jpeg` suffix on the `-F` flag for this curl build) was
  initially reported by Claude as a possible photo-upload endpoint bug
  before being correctly re-diagnosed as a client-side curl invocation
  issue, once retried without the `;type=` suffix succeeded.

## Client-facing questions

Assumptions made during the build that a real client conversation should
settle — collected from the spec's Decisions section and later
additions (some listed twice in-session at my request: "collect every
open question we resolved with an assumption"):

1. **Branch context delivery** — assumed route-parameter-based
   (`/branches/{branchId}/...`).
2. **Member↔branch cardinality** — assumed exactly one branch per
   member; the brief's phrasing doesn't rule out multiple.
3. **Sport scoping** — assumed per-branch, not global; the brief
   describes sports club-wide.
4. **Sport management surface** — a create-sport endpoint was added
   during the pipeline (Decision 4), then fully removed later at my
   direct instruction (see "What I kept, changed, or threw away").
   Sports are seed/admin data only in the delivered state.
5. **Photo constraints** — assumed JPEG/PNG only, 5 MB cap; not
   specified in the brief.
6. **Delete semantics** — assumed hard delete, no history retained.
7. **Error/response conventions** — assumed standard REST conventions
   (404/400/200/201/204, `ProblemDetails`); this one was **never
   actually confirmed even as a working assumption** — it remains an
   open question in the spec.
8. **Branch management surface** (post-pipeline) — `GET /branches` and
   `POST /branches` were added directly at my instruction, reversing
   Decision 1's original "no branch list/management endpoints" call.
9. **Branch name uniqueness** — `POST /branches` validates only that a
   name is present, with no uniqueness check, decided when Claude asked
   and I chose "name required only."

## Architecture discussion

A closing exchange, advisory only — no code, spec, or test changes came
from it. Recorded here because it reflects real questions raised about
the delivered architecture, not because anything in the repo changed as
a result.

1. **"this is clean architecture right? did i say to build clean
   architecture?"** — Claude confirmed the codebase follows clean
   architecture (four inward-only-dependency layers, mechanically
   enforced by NetArchTest and proven via the Phase 2 guardrail tests),
   and quoted the opening prompt of this session back as the origin of
   that requirement.
2. **"which is better clean architecture or factory pattern as we may
   have multiple branches tomorrow"** — Claude's position: these solve
   different problems (clean architecture = codebase layering; factory
   pattern = a creational pattern for picking an implementation at
   runtime) and aren't a real either/or. Claude noted multi-branch
   scoping in this codebase is already handled by passing `BranchId` as
   a parameter into shared repository methods, not by swapping
   implementations per branch, and that a factory would only become
   relevant if different branches needed genuinely different behavior
   (e.g. different photo storage backends per region) rather than just
   more rows of the same data. [MY REASONING: whether this framing
   matched what I was actually concerned about — ]
3. **"what if the app is released worldwide tomorrow and faces load
   issues?"** — Claude's position: the architecture itself isn't what
   would fail — the POC-specific infrastructure choices would (SQLite's
   single-writer limitation, already observed firsthand in-session when
   the seed tool crashed the live API; local-disk photo storage, which
   breaks under multiple server instances; no pagination/caching).
   Claude argued clean architecture is precisely what makes those fixes
   adapter-only changes (e.g. swap `EfMemberRepository`'s backing store,
   swap `LocalDiskPhotoStore` for an S3-backed implementation) without
   touching `Domain` or `UseCases`.
4. **"I wasnt talking about sql lite or local disk storage, I was
   wondering if CQRS would be a better option"** — a direct correction:
   the prior answer had focused on infrastructure, not the actual
   question asked. Claude's revised position: CQRS organizes the
   use-case layer, it isn't an alternative to clean architecture; the
   codebase already separates read/write *shapes* (`MemberSummary` vs.
   `MemberDetail` from the same `Member` aggregate) without full CQRS
   ceremony; full CQRS (separate stores/pipelines for reads vs. writes)
   would only pay off with a measured, significant read/write imbalance,
   and adopting it now would add complexity without addressing the
   actual bottleneck (SQLite). Claude recommended not adopting full
   CQRS at this stage, and described an incremental "CQRS-lite" path
   (an additional read-optimized repository behind the existing ports,
   only if read pressure becomes real) instead.
   [MY REASONING: what prompted the CQRS question specifically, and
   whether Claude's answer addressed the actual concern — ]

## Appendix

**Test suite size over the session** (from reported `dotnet test` runs):

| Point in session | Total tests | Passing |
|---|---|---|
| After Phase 5 (adapters) | 84 | 84 |
| After Phase 6 (presentation) | 108 | 108 |
| After Phase 7 (integration) | 126 | 126 |
| After Phase 8 (docs) | 126 | 126 (no code change) |
| After adding `GET /branches` | 133 | 133 |
| After adding `POST /branches` | 141 | 141 |
| After removing create-sport | 127 | 127 |

**Architecture guardrail proof** (Phase 2): the layer-boundary rules
were proven twice — an attempted `Domain → UseCases` project reference
was refused by MSBuild itself as a circular dependency before any test
ran; a separate illegal `Adapters` reference from a non-`Program` type
in the Api project was caught by the
`Api_Should_Not_DependOn_Adapters_Concrete_Types_Outside_Composition_Root`
NetArchTest rule, naming the violating type. Both violations were
reverted immediately after being observed.

**Files/projects Claude created directly** (non-exhaustive, by area):

- `.claude/skills/adlc/SKILL.md`
- `CLAUDE.md`
- `docs/specs/membership-system.md`
- `src/MembershipSystem.{Domain,UseCases,Adapters,Api}` (all layers)
- `tests/MembershipSystem.{Domain,UseCases,Adapters,Api}.Tests`,
  `tests/MembershipSystem.IntegrationTests`,
  `tests/MembershipSystem.ArchTests`
- `tools/MembershipSystem.Seed`
- `README.md`
- This file, `AI-USAGE.md`

[MY REASONING: any other context about how AI was used or not used that
isn't visible from the transcript itself — add here.]
