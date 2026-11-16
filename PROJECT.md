# PROJECT.md

<!-- Cap: 60 lines. -->

## Objective

An energy-metering pipeline in C#/.NET 11: simulated meters emit 5-minute readings that are
validated, stored, aggregated to 15-minute values, exported, and served over a REST API and
an MCP server. The milestone plan is in ROADMAP.md.

## Users

Developers reading the code as a reference for a small, well-tested .NET pipeline, and an
MCP client answering consumption questions through the API.

## Stakeholder language

**Stakeholder language:** English

## Constraints

- .NET 11, nullable enabled, warnings as errors (Directory.Build.props). No suppressions.
- Domain references nothing; every other project may reference Domain.
- New NuGet packages are justified in DECISIONS.md before they are added.
- All data is synthetic. No real company names, EAN codes or customer data.
- Time zone for local-day logic: Europe/Amsterdam.

## Non-goals

- Real meter protocols, authentication, multi-tenant use, production hosting.
- Time zones other than Europe/Amsterdam (QuarterHours assumes 15-minute offsets).
- Streaming exports for days above ~1M rows; a day is built in memory.

## Invariants

- An invalid `Ean` cannot be constructed.
- Energy values are `decimal`; moments are `DateTimeOffset` and compare as instants.
- A local day in Europe/Amsterdam has 92, 96 or 100 quarter-hours.
- Every behaviour has a test; `dotnet build` and `dotnet test` pass on every commit.

## Definition of done

Every milestone in ROADMAP.md is checked off: CI is green, the simulator feeds the API end
to end, and DuckDB reads the Parquet export with totals that match the API.
