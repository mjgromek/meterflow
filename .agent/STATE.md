<!-- Cap: 120 lines. Over cap: move settled facts into DECISIONS.md and delete them here. State describes now. -->

# STATE

Pipeline: easydev, upstream 9bfa13b. History was rewritten on 2026-11-16 (squash and author
email), so pre-2026-11-16 hashes from conversations no longer resolve.

## Current slice

None. M7 complete (86/86, DuckDB test, builder real run; no separate checker pass under
autopilot), auto-approved under the human's autopilot grant. Next: M8 MCP server.

## Acceptance criteria (M7)

- `IReadingStore.Between(from, to)` returns all meters' readings in [from, to) (SQLite
  implementation, indexed if cheap).
- `QuarterExport` in Infrastructure writes one Europe/Amsterdam local day (bounds from
  `QuarterHours.ForLocalDay`) as Parquet with Parquet.Net to
  `<root>/year=YYYY/month=MM/day=DD/quarters.parquet`, columns ean (string), start
  (timestamp, UTC), kwh (decimal). Rows from QuarterHours.Aggregate, ordered by ean, start.
  Re-exporting a day overwrites the file. Writes through a stream; uses a generic helper only
  if it removes duplication.
- `POST /exports/{date}` (date as yyyy-MM-dd) exports that day and returns 200 with
  {path, rows}; bad date: 400. Export root from appsettings.json (`Export:Root`, default
  `exports`), gitignored.
- A test reads the exported file with DuckDB (DuckDB.NET.Data.Full in Api.Tests only):
  for 2 meters on 2026-10-25, DuckDB sees 200 rows (100 per meter) and per-meter sums equal
  the API's consumption totals for that day.

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
