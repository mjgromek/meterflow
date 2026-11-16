# 9. Minimal API with an in-memory store behind `IReadingStore`

Status: Storage part superseded by [12](0012-sqlite-store-with-ef-core.md) · Milestone: M4

## Context
The API needs storage now; the database arrives in M5.

## Decision
ASP.NET Core minimal API (`MapGet`/`MapPost` in `Program.cs`). `IReadingStore` (`Add`
returns false for a duplicate, `ForMeter(ean, from, to)` over `[from, to)`) is declared in
`Domain`. `InMemoryReadingStore` is registered as a singleton and uses an atomic
`TryAdd` keyed by (EAN, instant).

## Consequences
- M5 swaps the registration for a database implementation; endpoints do not change.
- Data is lost when the API stops, which is acceptable until M5.
- `ForMeter` scans all readings; fine at this size and replaced in M5.
- Rejected: controllers (more files for three endpoints); EF Core's in-memory provider
  (M5 decides the storage technology).
