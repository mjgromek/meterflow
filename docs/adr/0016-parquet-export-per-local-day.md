# 16. Quarter-hour export as Parquet per local day, Hive-style paths

Status: Accepted · Milestone: M7

## Context
Analytics tools need the 15-minute values outside the API, as files that a query engine
such as DuckDB can read directly and prune by date.

## Decision
- `QuarterExport.WriteAsync` exports one Europe/Amsterdam local day (bounds from
  `QuarterHours.ForLocalDay`, so 92, 96 or 100 quarters per meter) to
  `<root>/year=YYYY/month=MM/day=DD/quarters.parquet`, overwriting an earlier export.
- Columns: `ean` (string), `start` (UTC timestamp, microseconds), `kwh` (decimal, the
  library default precision 38, scale 18, so no rounding). Rows are ordered by EAN, start.
- Parquet.Net's class serializer writes a private record; `IReadingStore.Between` reads all
  meters for the range, backed by an index on the instant (migration `MomentIndex`).
- Trigger: `POST /exports/{yyyy-MM-dd}`, so a scheduler (cron, CI job) runs the nightly
  export with one HTTP call. The root folder is `Export:Root` in `appsettings.json`.

## Consequences
- DuckDB reads the files with `read_parquet('exports/**/*.parquet', hive_partitioning=true)`;
  a test checks DuckDB's per-meter totals equal the API's on 2026-10-25.
- The day is built in memory inside the request; fine for thousands of meters, not millions.
- A crash mid-write can leave a partial file; re-running the export replaces it.
- Rejected: CSV export (no types, no column pruning); a timer `BackgroundService` for the
  nightly run (scheduling belongs to the host's scheduler and is harder to test); UTC days
  (the business day is the local day).
