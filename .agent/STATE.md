<!-- Cap: 120 lines. Over cap: move settled facts into DECISIONS.md and delete them here. State describes now. -->

# STATE

Pipeline: easydev, upstream 9bfa13b. History was rewritten on 2026-11-16 (squash and author
email), so pre-2026-11-16 hashes from conversations no longer resolve.

## Current slice

None. M9 complete (CI green on GitHub incl. container smoke test), auto-approved.
Next: M10.

## Acceptance criteria (M9)

- `.github/workflows/ci.yml`: on push and pull_request; ubuntu-latest; actions/setup-dotnet
  with the .NET 11 SDK (global.json pins the SDK if needed for CI to match); `dotnet tool
  restore`, `dotnet build`, `dotnet test`. No secrets.
- `src/MeterFlow.Api/Dockerfile` (or at repo root): multi-stage, sdk:11.0 build/publish, aspnet:11.0
  runtime, non-root user, database path under a volume (`/data`, connection string via env
  var), port 8080. `.dockerignore` excludes bin/obj/*.db/exports. Built and smoke-tested
  locally (container /health 200) if disk allows; images removed afterwards.
- `README.md`: what it is, architecture diagram (current: simulator, API, channel,
  processor, SQLite, Parquet export, MCP server), how to run each part (API, simulator,
  MCP with `claude mcp add`, Docker, export + DuckDB query), API endpoint list, link to
  docs/adr and docs/time-zones.md, CI badge. Neutral, factual, no learner framing.
- CI is green on GitHub (verified by the orchestrator after push).

## Shipped in M3

- Duplicate and gap detection (`ReadingChecks`).
- `ReadingCsv.Parse` with line-numbered rejections, including zero kWh accepted.
- `QuarterHours.Aggregate` and `ForLocalDay` (92/96/100), CSV round trip for two meters.
- docs/time-zones.md, every number re-measured by the checker.

## In flight

Nothing.

## Blocked

Nothing.

## Last verification result

2026-11-16: dotnet test 56/56, build 0 warnings, on m3 before the squash. Checker PASS on all
five slices.
