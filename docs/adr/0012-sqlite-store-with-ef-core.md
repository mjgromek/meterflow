# 12. Readings stored in SQLite through EF Core

Status: Accepted · Milestone: M5 · Supersedes [9](0009-minimal-api-in-memory-store.md) (storage part)

## Context
Readings must survive an API restart. `IReadingStore` from M4 stays the boundary, so the
endpoints do not change.

## Decision
`SqliteReadingStore` in `Infrastructure` implements `IReadingStore` with EF Core 11 and
SQLite. `Reading` is mapped without attributes, so `Domain` stays free of EF Core:
- `Ean` is stored as its 18-digit string.
- `Moment` is stored as UTC ticks (`MomentUtcTicks`, an integer), because SQLite cannot
  compare or order `DateTimeOffset` in queries. It is read back with offset +00:00.
- The primary key (Ean, MomentUtcTicks) is the duplicate rule. `Add` inserts and returns
  false on SQLite's constraint error, so two concurrent requests cannot both store the same
  reading. The same key serves `ForMeter`'s range query.
- `MeterFlowDbContext` is registered scoped (one per request), the default for a
  `DbContext`; the store is scoped with it.

## Consequences
- The original offset of a reading is not kept; the instant is. Equality and aggregation
  only use the instant.
- Each `Add` is its own transaction: about 0.4 s per 500 readings, enough for now.
- `IReadingStore` stays synchronous. EF Core's async methods are the norm for web apps and
  are worth adopting when the background pipeline (M6) moves writes off the request.
- Rejected: hand-written SQL with `Microsoft.Data.Sqlite` (smaller, but no migrations or
  mapping); check-then-insert duplicate detection (racy under concurrent requests).
