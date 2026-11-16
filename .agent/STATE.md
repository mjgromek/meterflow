<!-- Cap: 120 lines. Over cap: move settled facts into DECISIONS.md and delete them here. State describes now. -->

# STATE

Pipeline: easydev, upstream 9bfa13b. History was rewritten on 2026-11-16 (squash and author
email), so pre-2026-11-16 hashes from conversations no longer resolve.

## Current slice

None. M8 complete (92/92, real stdio run answered the gaps question), auto-approved.
Next: M9.

## Acceptance criteria (M8)

- API: `GET /meters` returns [{ean, readings, first, last}] ordered by ean. `GET
  /gaps?from=&to=` (same strict moment parsing) returns [{ean, count, moments}] for every
  meter with readings in [from, to), using ReadingChecks.FindGaps, ordered by count desc.
  Bad bounds: 400. Store gets what it needs (e.g. `Meters()` summary query).
- `MeterFlow.Mcp`: a stdio MCP server with the official C# SDK (ModelContextProtocol
  package), read-only tools `list_meters`, `get_consumption(ean, from, to)`,
  `find_gaps(from, to)`, each with a description written for an agent (units, UTC/offset
  requirement, what an empty result means). Tools call the API through a typed HttpClient
  registered with `AddHttpClient`, base URL from configuration (`MeterFlow:ApiBaseUrl`,
  default http://localhost:5265, overridable by env var). Logging goes to stderr only
  (stdout is the protocol).
- Tests: the tool methods called with WebApplicationFactory's HttpClient answer "which
  meter had the most gaps on 2026-10-25" correctly for 2 meters with injected faults.
- Real run: an MCP client (the SDK's client over stdio, in a throwaway program or test)
  lists the three tools and calls find_gaps against a running API.

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
