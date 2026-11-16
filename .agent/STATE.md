<!-- Cap: 120 lines. Over cap: move settled facts into DECISIONS.md and delete them here. State describes now. -->

# STATE

Pipeline: easydev, upstream 9bfa13b. History was rewritten on 2026-11-16 (squash and author
email), so pre-2026-11-16 hashes from conversations no longer resolve.

## Current slice

None. M4 complete (S1-S5, checker PASS, 79/79, real run simulator to API); awaiting
milestone review. No merge or push before "approved".

## M4 slices (Web API, in-memory storage; persistence is M5)

- S1: `GET /health` returns 200; API test harness via WebApplicationFactory; the template
  "Hello World" route and placeholder UnitTest1 removed.
- S2: `POST /readings` takes a JSON array of {ean, moment, kwh}; valid readings go to a
  singleton in-memory store behind an interface; response lists rejected items by index and
  reason (same reasons as ReadingCsv). Malformed JSON: 400.
- S3: `GET /meters/{ean}/consumption?from=&to=` returns 15-minute quarters from
  QuarterHours.Aggregate in [from, to). Invalid EAN or from >= to: 400. Unknown meter: empty.
- S4: simulator sends readings over HTTP (`--api <url>`) with HttpClient and async/await;
  CSV output stays the default. API base URL from appsettings.json.
- S5: end to end: API started, simulator run against it, consumption totals equal the
  generated totals. ILogger logs accepted and rejected counts per POST.

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
