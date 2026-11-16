# MeterFlow roadmap

An energy-metering pipeline in C#/.NET 11. Simulated meters emit a reading every
5 minutes; the system validates, stores, aggregates and exports those readings, and
exposes them over a REST API and an MCP server. All data is synthetic.

## Architecture

```
Simulator ──POST /readings──▶ Api ──▶ Channel<T> queue ──▶ BackgroundService
  (console)                   │                              │ validate, store
                              │                              ▼
  MCP server ──GET──▶ Api ◀── SQLite (EF Core) ──▶ 15-min aggregation ──▶ Parquet export
```

## Solution layout

```
MeterFlow.slnx
Directory.Build.props          shared settings: net11.0, nullable, warnings as errors
src/
  MeterFlow.Domain/            pure C#, no packages: Ean, Reading, aggregation rules
  MeterFlow.Simulator/         console app that simulates meters
  MeterFlow.Api/               ASP.NET Core minimal API
  MeterFlow.Infrastructure/    EF Core, SQLite, Parquet
  MeterFlow.Mcp/               MCP server over the API
tests/
  MeterFlow.Domain.Tests/
  MeterFlow.Api.Tests/
```

Dependencies point inward: Domain references nothing, everything else may reference
Domain. This keeps the domain rules testable without a database, web server or file
system.

---

## [x] M0. Skeleton
Repo, solution, projects, references, `.gitignore` and `Directory.Build.props`.

Done when: `dotnet build` and `dotnet test` pass on an empty solution with warnings as
errors.

## [x] M1. Domain types
`Ean` as a validated value type with a check-digit rule. `Reading` as a record: EAN,
timestamp, kWh. `MeterKind` enum (electricity, gas).

Scope: records, value semantics, constructors that refuse invalid input,
`DateTimeOffset`, `decimal` for energy values.

Done when: an invalid `Ean` cannot be constructed, and tests prove it.

## [x] M2. Simulator
Console app generating realistic 5-minute readings for N meters (day/night pattern,
noise, deliberate missing and duplicate readings) and writing them to CSV.

Scope: `IEnumerable<T>` and `yield return`, `Random`, `CultureInfo.InvariantCulture`
formatting, file I/O, command-line arguments.

Done when: it writes a CSV that is indistinguishable in shape from real meter output.

## [x] M3. Validation and aggregation
Read the CSV, detect duplicates, gaps, negative values and invalid EANs. Aggregate
5-minute readings into 15-minute intervals.

Scope: LINQ, pattern matching, result types versus exceptions, time zones. DST days are
covered: in Europe/Amsterdam a day has 92 or 100 quarter-hours, not 96.

Done when: a DST-day test passes, and the failure mode of naive local times is documented.

## [ ] M4. Web API
Minimal API: `POST /readings`, `GET /meters/{ean}/consumption?from=&to=`, `GET /health`.
The simulator sends over HTTP instead of writing CSV.

Scope: ASP.NET Core, dependency injection and service lifetimes, `appsettings.json`,
`ILogger`, `HttpClient`, `async`/`await`.

Done when: simulator to API works end to end.

## [ ] M5. Persistence
Store readings in SQLite with EF Core. Repository interface in Domain, implementation in
Infrastructure.

Scope: interfaces, EF Core mapping, migrations, `DbContext` lifetime.

Done when: data survives a restart and an integration test runs against a throwaway
database.

## [ ] M6. Background pipeline
The API puts readings on a `Channel<T>`; a `BackgroundService` validates, stores and
aggregates them.

Scope: hosted services, `Channel<T>`, `CancellationToken`, graceful shutdown.

Done when: the API returns immediately, processing happens behind it, and shutdown does
not drop queued readings silently.

## [ ] M7. Parquet export
Nightly-style export of 15-minute values per meter to Parquet, in a blob-style folder
layout (`year=/month=/day=`).

Scope: Parquet.Net, streams, generics.

Done when: DuckDB reads the files and the totals match the API.

## [ ] M8. MCP server
A read-only MCP server exposing `list_meters`, `get_consumption` and `find_gaps` over the
API.

Scope: the C# MCP SDK, attributes, `HttpClient` factories, tool design for agents.

Done when: an MCP client answers "which meter had the most gaps yesterday?" through these
tools.

## [ ] M9. Ship it
GitHub Actions running build and tests, a Dockerfile for the API, API integration tests
with `WebApplicationFactory`, and a README covering the architecture.

Scope: CI for .NET, multi-stage Docker builds, in-process testing of a whole app.

Done when: CI is green and the README documents the design decisions.

## [ ] M10. Dashboard
A React + TypeScript single-page dashboard (Vite) in `frontend/`, reading only through the
API. Select a meter, see its 15-minute consumption for a chosen day with gaps and invalid
readings highlighted, and a table of the meters with the most data-quality issues.

Scope: CORS limited to the dev frontend origin (no wildcard), the API's OpenAPI document as
the contract, a TypeScript client generated from it so frontend types follow the C# records,
the frontend build in GitHub Actions, the README architecture diagram updated.

Done when: the dashboard shows a DST day correctly from a running API, and CI builds both.
