---
name: adlc
description: Take a user story from raw ask through a written spec with acceptance criteria, architecture boundary guardrails, then clean-architecture layers built inside-out (domain, use case, adapters, presentation) each test-first, then integration tests, then docs — one phase per invocation, resumable via a progress table in the spec file. Use when the user gives a feature/story to implement in this repo where clean architecture, hexagonal architecture, or strict layer boundaries matter (e.g. "implement this story", "next phase", "continue the spec for X"), especially for take-home assignments or POCs being evaluated on architecture quality.
---

# ADLC — user story to shipped feature

Turn one user story into working, layered, test-first code through a fixed
sequence of phases. Run **exactly one phase per invocation**, then stop.
Progress lives in a spec file so the pipeline can be resumed at any time,
in any session.

This skill is stack-agnostic. Never assume a language, framework, test
runner, or package layout. Discover it from the repo, or ask, then record
the answer once.

## Non-negotiable rules

1. **One phase per invocation. Always.** Even if a phase takes 30 seconds
   and the next phase seems obvious, stop, update the progress table, and
   report what's next. Do not chain phases in one turn unless the user
   explicitly asks for a multi-phase run (see below). This is the whole
   point of the pipeline — it gives a review checkpoint at every layer
   boundary.
2. **Tests before implementation, every layer.** For domain, use case,
   adapters, and presentation: write the failing test(s) first, show them
   failing for missing behavior (not a syntax or import error), then write
   the minimum code to pass. Never write production code ahead of its test.
3. **Inside-out dependencies only.**
   - Domain depends on nothing.
   - Use case depends on domain, and on ports it declares itself.
   - Adapters and presentation depend inward; nothing depends on them.

   If a phase's constraints can't be met — a port doesn't fit the real
   infrastructure, a layer needs something it shouldn't — **stop and
   report it**. Do not resolve a layering conflict by widening an
   interface, reshaping a use case around a database, or relocating logic
   outward. Surfacing the conflict is the correct outcome, not a failure.
4. **The spec file is the source of truth for state.** Don't rely on
   conversation memory to know what phase you're on. Read the progress
   table at the start of every invocation.
5. **Don't invent scope.** Acceptance criteria drawn up in Phase 1 bound
   what gets built. If implementation reveals a gap, surface it and update
   the spec's criteria explicitly rather than quietly expanding scope. If
   a later phase reveals the *spec itself* was wrong (not just an
   infrastructure mismatch), stop and say so — update Phase 1's content
   and log it in Notes/deviations rather than silently designing around it
   in a later layer.

## Picking the phase

- Argument names a phase (`spec`, `arch`, `domain`, `usecase`, `adapters`,
  `ui`, `integration`, `docs`) → run that phase.
- Argument is `next`, or empty → read the Progress table in the story's
  spec file and run the first phase not marked `done` or `n/a`.
- Argument is a user story (prose, ticket ID, or URL) → this is a new
  story: run **Phase 1 — Spec**.
- If several specs exist and the phase or story is ambiguous, ask which
  feature — don't guess.

## Where state lives

One markdown file per story: `docs/specs/<feature-slug>.md` (create
`docs/specs/` if it doesn't exist; if the project already has a
convention for spec location, use that instead). Derive the slug from the
story title, kebab-case. This file contains, in order:

- Story title and raw ask (verbatim from the user)
- **Stack & conventions** — language, test framework, project layout,
  architecture-guardrail tool (once chosen in Phase 2). Filled in as it's
  discovered; never re-asked or re-derived once recorded.
- **Restatement** — the ask, in your own words
- **Acceptance criteria** — numbered, each independently testable
- **States** — every state the feature can be in: loading, empty, error,
  disabled, etc. (drives Phase 6's tests/stories)
- **Out of scope** — what is deliberately not being built
- **Open questions** — anything ambiguous, listed and never resolved by
  guessing
- **Layer map**:
  - Domain: entities/value objects touched or introduced, and their
    invariants
  - Use case: the operation(s), inputs/outputs as plain data, no
    framework types
  - Ports: every interface the use case needs outward — repositories,
    clocks, ID generators, external services — with method signatures
  - Adapters: the concrete implementation for each port, and the
    infrastructure it talks to
  - Presentation: what the UI needs from the use case, as a view model
- **Architecture boundary guardrails** — the specific dependency rules
  for this story, tying back to the layer map's ports
- **Progress table** (see below)
- **Notes / deviations** — a running log of anything that changed after
  Phase 1 and why, updated as it happens (not just at the end in Phase 8)

### Progress table format

```markdown
## Progress

| Phase | Status |
|---|---|
| 1 spec | done |
| 2 arch | not started |
| 3 domain | not started |
| 4 usecase | not started |
| 5 adapters | not started |
| 6 ui | not started |
| 7 integration | not started |
| 8 docs | not started |
```

Status values: `not started`, `in-progress`, `done`, `blocked`, `n/a`.
Use `n/a` for phases that don't apply — e.g. Phase 2 when boundary
enforcement already exists in the repo, or Phase 6 for a story with no
UI — and say why in the same row or in Notes.

Update this table as the last step of every phase, before reporting to
the user. Default to stopping for review between phases unless the user
has said to keep going.

## Startup routine (every invocation)

1. Identify the story from the argument (see "Picking the phase" above).
2. If `docs/specs/<feature-slug>.md` doesn't exist, this is a fresh
   story: start at Phase 1.
3. If it exists, read it fully — stack/conventions, spec, criteria,
   guardrails, progress table, notes. Determine the phase to run per
   "Picking the phase."
4. Run only that phase. Follow its instructions below.
5. Update the progress table and notes log.
6. Stop. Report using the format below. Do not start the next phase.

If the user explicitly asks to run multiple phases in one go ("do phases
3 through 5 now, I'll review at the end"), honor that, but still update
the progress table after each individual phase completes so the file
stays an accurate resume point if interrupted — and say clearly which
phases were covered.

## The dependency rule

Every code phase respects this. Dependencies point inward only:

- **Domain** depends on nothing: no framework, no I/O, no HTTP, no
  database, no clock (time arrives as a parameter).
- **Use case** depends on domain, and on ports it declares itself —
  nothing else.
- **Adapters** and **presentation** depend inward, and nothing depends
  on them.

## Phase 1 — Spec

Goal: turn the raw story into a written spec a reviewer could read
without you in the room.

- Read the surrounding code first so the spec matches existing
  conventions (naming, layering already in place, test runner, etc.).
- Restate the story in your own words; confirm scope boundaries — what's
  explicitly in and explicitly out.
- If the language/framework/test tooling isn't already evident from the
  repo (check for lockfiles, config, existing source before asking), ask
  the user. Record the answer in "Stack & conventions." Don't ask again
  in later phases.
- Write numbered, testable acceptance criteria.
- Enumerate every state the feature can be in (loading, empty, error,
  disabled, etc.).
- Fill in the layer map: domain entities/invariants, the use case's
  inputs/outputs as plain data, every port with method signatures,
  adapters per port, and the presentation view model.
- Note open questions. List them; never resolve by guessing. If the
  story can't be expressed without infrastructure that doesn't exist,
  say so here.
- Write no implementation, tests, or stories in this phase.
- Write the spec file; create the progress table with Phase 1 `done` and
  Phase 2 `not started`.
- End by showing the Open questions and asking the user to resolve them
  before Phase 2.

## Phase 2 — Architecture guardrails (once per repo)

Goal: make the layer boundaries mechanically enforced *before* more code
exists, so later phases have a contract to check themselves against —
and a build that fails if they don't.

- Check whether boundary enforcement already exists in the repo. If it
  does, mark this phase `n/a`, say so, and record which tool/config in
  "Stack & conventions."
- Otherwise pick the tool for the stack — e.g. `dependency-cruiser` or
  `eslint-plugin-boundaries` for TypeScript, `NetArchTest` for .NET,
  ArchUnit for Java (or the closest equivalent for the chosen stack).
  Record the choice in "Stack & conventions."
- Write one rule per forbidden direction, each with an error message
  naming the violated boundary.
- Wire it into the existing test or CI command so a violation fails the
  build.
- Prove it: add an illegal import, show the failure, then revert it.
- No feature code in this phase.
- Update the spec's guardrails section and progress table.

## Phases 3–6 — The layers, inside-out

Same rhythm every time:

1. Write the tests. Reference acceptance-criterion numbers in test names.
2. Show them failing — for missing behavior, not a syntax or import
   error.
3. Implement until green.
4. Run the full suite **and** the architecture checks (Phase 2 onward).
   Both must pass.

### Phase 3 — Domain

Entities, value objects, invariants. Imports nothing outward. No mocking
framework — if a test here needs one, the design is wrong; stop and say
so.

- Confirm zero outward dependencies (no imports from adapters,
  presentation, or infrastructure/framework packages).
- Update progress table; note any acceptance criteria this phase fully
  or partially satisfies.

### Phase 4 — Use case

Application logic that orchestrates domain objects and declares the
ports adapters will implement. Imports domain and the ports declared in
the spec's layer map, nothing else. Test with hand-written in-memory
fakes, not a mocking library.

- Declare ports exactly as the layer map specifies.
- Confirm it depends only on domain + its own ports, never on a concrete
  adapter.
- Update progress table.

### Phase 5 — Adapters

Concrete implementations of the ports defined in Phase 4.

- Write failing tests for each adapter (against a real or realistic
  target where practical — e.g. an in-memory or test instance — per the
  stack's conventions). Tests verify each adapter satisfies its port's
  contract.
- The port interfaces are **fixed**. If a port doesn't fit the real
  infrastructure, stop and report which one and why. Do not widen the
  interface, and do not reshape the use case around the database — that
  is the failure mode this whole pipeline exists to prevent.
- Confirm adapters depend inward and never leak infrastructure types
  back into domain or use case signatures.
- Update progress table.

### Phase 6 — Presentation (`ui`)

The boundary the outside world actually talks to — HTTP handlers, CLI
commands, UI components, whatever the story calls for. Skip this phase
entirely (`n/a`) for a story with no presentation surface.

- Write failing tests, one per state listed in the spec (request/response
  shape, status codes, error mapping, loading/empty/error/disabled — as
  applicable).
- Implement it, wiring use cases in via their interfaces, with adapters
  injected/composed at the outer edge (or the project's composition
  root). Talks to the use case only — never an adapter, repository, or
  entity directly.
- No business logic here; if a rule needs a home, it belongs inward —
  stop and say so.
- If the project uses Storybook (or an equivalent), add one story per
  state: matching the format and conventions of an existing story file,
  realistic props rather than placeholders, error and empty states
  included, fed by fakes and never real infrastructure.
- Confirm presentation depends on use cases, not the other way around.
- Update progress table.

## Phase 7 — Integration tests

Goal: verify the layers actually work together end-to-end, using real
(or near-real) adapters instead of test doubles or in-memory fakes.

- Map each acceptance criterion from Phase 1 to at least one integration
  test; call out any criterion that still isn't covered.
- Cover data crossing each boundary intact in both directions, and
  failure paths: network error, infrastructure unavailable, rejected
  input.
- Don't duplicate unit tests. Don't mock the thing under test.
- Update progress table; if gaps are found, log them in Notes/deviations
  rather than silently patching scope.

## Phase 8 — Docs

Goal: leave the spec file and repo in a state a reviewer can follow
without the conversation history.

- Document what was actually built, not what the spec proposed. Where
  they differ, the code wins — list every divergence under a "Deviations
  from spec" heading (fold in anything already logged in
  Notes/deviations).
- Add/update a README or architecture doc covering: what it does and
  when to use it, a usage example, the public API (use case signatures,
  port interfaces, component props), how to run tests, how to run the
  system, and any gotchas a caller would hit.
- Change no code in this phase.
- Mark Phase 8 `done` in the progress table. The pipeline is complete.

## Verification (before reporting any phase complete)

- The full test suite passes, and so do the architecture checks (Phase 2
  onward). Don't assume the linter caught a boundary violation if it
  isn't wired in yet — check the actual imports.
- Every acceptance criterion this phase covers maps to a named test.
- No import crosses a boundary outward.
- The Progress table in the spec is updated.

## Reporting format (every phase)

End each invocation with:

- **Phase completed:** name + number
- **What was done:** files added/changed, test results — concrete, not
  narrated
- **Progress table:** current state (or point at the spec file)
- **Next phase:** name, one sentence on what it involves, and the exact
  next command to run
- Nothing about phases beyond that — don't preview the whole remaining
  pipeline unless asked.
