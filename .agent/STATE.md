<!-- Cap: 120 lines. Over cap: move settled facts into DECISIONS.md and delete them here. State describes now. -->

# STATE

Pipeline: easydev, upstream 9bfa13b. History was rewritten on 2026-11-16 (squash and author
email), so pre-2026-11-16 hashes from conversations no longer resolve.

## Current slice

None. M10 complete (CI green: build, docker, frontend; 99 .NET + 7 frontend tests),
auto-approved. All roadmap milestones done.

## Acceptance criteria (M10 S1, API)

- Rejections that name a valid EAN (NegativeKwh, BadFormat with a valid ean) are stored
  with ean, moment if parseable, reason, received time. Duplicates found by the processor are
  stored the same way with reason Duplicate. Invalid-EAN items cannot be attributed and are
  only counted in the POST response, as now.
- `GET /meters/{ean}/days/{yyyy-MM-dd}`: the Europe/Amsterdam local day as
  {quarters: [{start, kwh|null, readings}] for every QuarterHours.ForLocalDay start (92/96/
  100), gaps: [moment], rejected: [{moment|null, reason}]}. Bad ean/date: 400.
- `GET /quality/{yyyy-MM-dd}`: per meter {ean, gaps, rejected, duplicates} for that local
  day, ordered by gaps+rejected+duplicates desc.
- OpenAPI: Microsoft.AspNetCore.OpenApi (`AddOpenApi`, `MapOpenApi` at /openapi/v1.json).
  Endpoints return typed results so every response schema comes from the C# records. The
  document is also generated at build time (Microsoft.Extensions.ApiDescription.Server) into
  `frontend/openapi/MeterFlow.Api.json`, committed.
- CORS: one named policy allowing only the origin in `Cors:FrontendOrigin`
  (appsettings.Development.json: http://localhost:5173), GET only; no wildcard. Absent
  setting: no CORS. Tests: preflight from 5173 gets the header; another origin does not.

## Acceptance criteria (M10 S2, frontend)

- `frontend/`: Vite + React + TypeScript. TS types generated from
  frontend/openapi/MeterFlow.Api.json with openapi-typescript; calls via openapi-fetch.
  API base URL from VITE_API_URL (default http://localhost:5265).
- One page: meter select (GET /meters), date input (default 2026-10-25), an SVG bar chart of
  the day's quarters (all 92/96/100), quarters with gaps or rejections highlighted, a
  legend, and a table of the 10 meters with most issues (GET /quality/{date}). Accessible:
  labels, table semantics, chart has a text summary.
- Frontend tests with Vitest + React Testing Library (jsdom), API calls mocked at the fetch
  level, covering: the data-mapping function (DST day gives 100 bars, 92 on 2026-03-29;
  gaps and rejections mark the right quarters); the page renders a meter list, the chart
  with the right number of bars and an accessible text summary, highlighted quarters, and
  the quality table ordered as returned; changing meter or date refetches; an API error
  shows a message instead of crashing; an empty day shows an empty state.
  `npm test` runs them; `npm run build` type-checks.
- CI: a frontend job: dotnet build regenerates the OpenAPI document and fails if it differs
  from the committed one; then npm ci, generate types, test, build.
- README architecture diagram and run instructions updated.

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
