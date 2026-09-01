# Project facts

- Client: Yellowtail, on behalf of Amsterdam Sports Inc.
- Scope: backend only. Frontend is a separate team, built later against
  this API. No UI work in this repo.
- Auth: explicitly not required per the client brief. Do not add login.
- Multi-tenant: must support multiple branches of the club.
- Sports offered today: Tennis, Squash, Football. More may be added
  later — model sports as data, not an enum.
- Full brief: `docs/specs/brief.md`. Once Phase 1 of a story runs, its
  spec at `docs/specs/<slug>.md` is the source of truth alongside the
  brief.
- Process: every user story goes through the `adlc` skill
  (`.claude/skills/adlc/SKILL.md`). Follow it phase by phase; never skip
  ahead.
