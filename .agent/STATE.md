<!-- Cap: 120 lines. Over cap: move settled facts into DECISIONS.md and delete them here. State describes now. -->

# STATE

Pipeline: easydev, upstream 9bfa13b. History was rewritten on 2026-11-16 (squash and author
email), so pre-2026-11-16 hashes from conversations no longer resolve.

## Current slice

None. M11 complete: checker PASS over M6, M7, M10, M11 (real run incl. SIGINT drain,
CORS, export, MCP and simulator error paths); its three code P2s fixed; 115 .NET + 9
frontend tests. Auto-approved.

## Acceptance criteria (M11)

- Gaps over a window: `ReadingChecks.FindGaps(readings, from, to)` reports every missing
  5-minute moment in [from, to) per meter that has readings in the window, including at the
  window edges. `/gaps`, the day endpoint and `/quality` use it. Existing FindGaps behaviour
  stays for callers without a window.
- `ReadingCsv.Parse`: a trailing '\r' on any line is ignored; line 1 must equal the header
  `ean,moment,kwh`, otherwise it is parsed as a data row (so a headerless file loses nothing).
- Export: written to a temp file in the same folder, then `File.Move(overwrite: true)`; a
  per-day lock (or a single semaphore) stops two exports of the same day racing.
- Simulator: `--api` without a URL or with an invalid URL prints a usage message and exits
  with code 1; an unreachable API or a failed batch prints one clear error line and exits 1
  instead of a stack trace. Argument parsing is a testable function.
- MCP tools: API unreachable or 5xx returns an "Error: ..." text naming the base URL,
  instead of an unhandled exception.
- `GET /` returns 404 (test).
- Dashboard: x-axis labels at local 00:00, 06:00, 12:00 and 18:00 (by the quarter's local
  start), so a DST day still shows those four labels at the right bars. Frontend test for
  the 100- and 92-quarter days.

Deliberately not in scope (stay in backlog): other time zones, exports of more than ~1M
rows per day, a backpressure test, the simulator reading its URL from configuration.

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
