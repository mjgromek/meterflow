<!-- Cap: 120 lines. Over cap: move settled facts into DECISIONS.md and delete them here. State describes now. -->

# STATE

Pipeline: easydev, upstream 9bfa13b. History was rewritten on 2026-11-16 (squash and author
email), so pre-2026-11-16 hashes from conversations no longer resolve.

## Current slice

None. M5 complete (checker PASS, 82/82), auto-approved under the human's autopilot grant.

## Acceptance criteria (M5)

- S1 (Infrastructure): `MeterFlowDbContext` (EF Core, SQLite) and `SqliteReadingStore :
  IReadingStore`. Ean stored as its 18-digit string; Moment stored as UTC ticks (long) so
  SQLite can compare and order it; read back as UTC. Unique index on (Ean, ticks): `Add`
  returns false on a duplicate, enforced by the database, not a check-then-insert (closes the
  backlog race item). `ForMeter` is an indexed query over [from, to), ordered. Initial
  migration committed; `dotnet-ef` as a local tool (dotnet-tools.json at the repo root, where .NET 11 puts it).
  Store tests against a throwaway SQLite file: add, duplicate across offsets, [from, to).
- S2 (Api): registration scoped (DbContext lifetime), connection string in appsettings.json
  (`Data Source=meterflow.db`), migrations applied at startup, `InMemoryReadingStore`
  deleted, `*.db` gitignored. API tests use a throwaway database per factory. A restart test:
  POST with one factory, dispose, a new factory on the same file returns the data. All
  existing API tests still pass.

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
