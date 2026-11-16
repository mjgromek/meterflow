# 1. Projects depend inward on a dependency-free Domain

Status: Accepted · Milestone: M0

## Context
The system has a simulator, an API, storage, export and an MCP server. The rules that matter
(EAN validity, time handling, aggregation) must be testable without a web server, database
or file system.

## Decision
One solution, one project per concern: `Domain`, `Simulator`, `Api`, `Infrastructure`, `Mcp`,
plus test projects. `Domain` references nothing and uses no NuGet packages. Every other
project may reference `Domain`; `Domain` never references them.

## Consequences
- Domain tests are plain unit tests with no setup.
- Interfaces that need storage (`IReadingStore`) are declared in `Domain` and implemented
  outside it.
- Rejected: one project with folders. Nothing would stop the domain from depending on
  ASP.NET Core or EF Core.
