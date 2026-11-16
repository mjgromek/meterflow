<!-- Cap: 120 lines. Over cap: move settled facts into DECISIONS.md and delete them here. State describes now. -->

# STATE

Pipeline: easydev, upstream 9bfa13b. History was rewritten on 2026-11-16 (squash and author
email), so pre-2026-11-16 hashes from conversations no longer resolve.

## Current slice

None. Patch v1.2.1 done (117 .NET + 11 frontend tests). Backlog empty apart from non-goals
recorded in PROJECT.md. All roadmap milestones released.

## Acceptance criteria (patch)

- Simulator: when `--api` is absent, `MeterFlow__ApiBaseUrl` (env var) or a `--api` default
  from configuration selects HTTP mode; otherwise CSV as now. Test on the argument/config
  parsing function.
- API: queue capacity from configuration `Pipeline:QueueCapacity` (default 10,000). A test
  with capacity 1 and a blocked processor/store shows a POST waits while the queue is full
  and completes once it drains, and that a cancelled request returns without enqueuing.
- Dashboard: missing readings and rejected readings get different colours and legend
  entries (both at least 3:1 against the background in light and dark); a quarter with both
  shows as rejected. Frontend test.
- Non-goals recorded in PROJECT.md: time zones other than Europe/Amsterdam; streaming
  exports for days above ~1M rows.

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
