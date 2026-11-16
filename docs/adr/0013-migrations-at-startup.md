# 13. EF Core migrations, applied at startup

Status: Accepted · Milestone: M5

## Context
The schema must be created on a fresh machine and evolve with the code.

## Decision
Schema changes are EF Core migrations in `Infrastructure/Migrations`, generated with the
`dotnet-ef` local tool (`dotnet-tools.json` at the repository root). The API calls
`Database.Migrate()` at startup. The connection string `Data Source=meterflow.db` is in
`appsettings.json`; database files are gitignored. Tests point the same app at a throwaway
file per test factory.

## Consequences
- `dotnet run` on a clean clone creates the database; no manual step.
- Fine for a single API instance. Several instances starting at once, or a production
  database, would need migrations applied as a separate deployment step.
- Rejected: `EnsureCreated()` (cannot evolve a schema); SQL scripts by hand.
