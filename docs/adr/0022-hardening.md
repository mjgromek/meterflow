# 22. Gaps over a whole window, crash-safe exports, clear errors at the edges

Status: Accepted · Milestone: M11 · Amends [16](0016-parquet-export-per-local-day.md), [17](0017-read-only-mcp-server-over-the-api.md), [21](0021-rejections-stored-and-local-day-endpoint.md)

## Context
A review of M6 to M10 found behaviour that was correct in the normal case but weak at the
edges: gaps before a meter's first or after its last reading in a day went unreported,
exports wrote their final file directly, and the simulator and MCP tools surfaced stack
traces or generic errors when the API was unreachable.

## Decision
- `ReadingChecks.FindGaps(readings, from, to)` reports every missing 5-minute moment in the
  window for each meter with readings in it, edges included. `/gaps`, the day endpoint and
  `/quality` use it. (Amends ADR 21's note on edge gaps.)
- Exports read and write under one lock, write to a temp file in the same folder and move it
  over `quarters.parquet`. A crash leaves at most a `.tmp-*` file; readers never see a
  partial export, and an older export cannot overwrite a newer one. (Amends ADR 16.)
- The MCP tools return an "Error: ..." text naming the API base URL when the API refuses,
  times out or answers 5xx. (Amends ADR 17.)
- The simulator validates its arguments and reports any send or file error in one line
  with exit code 1.
- `ReadingCsv.Parse` ignores a trailing carriage return and only skips line 1 when it is the
  exact header.

## Consequences
- Gap counts in `/quality` and `/gaps` are higher than before for days with missing data at
  the edges; that is the intended correction.
- One global export lock serializes all exports; exports are rare, so this is not a
  bottleneck.
- Rejected: a per-day lock table (needs cleanup for little gain); retrying failed sends in
  the simulator (it is a test tool; a clear failure is more useful).
