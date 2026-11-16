# 11. API tests run in-process

Status: Accepted · Milestone: M4

## Context
Endpoint tests need the real routing, JSON settings and dependency injection, without
starting a server process per test.

## Decision
`Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) runs the API inside
the test process. The simulator's `ApiSender` takes an `HttpClient`, so the end-to-end
test passes the factory's client to the real sender.

## Consequences
- Tests are fast and need no free port.
- One package added, to the test project only.
- Rejected: launching `dotnet run` from tests (slow, port conflicts, cleanup).
