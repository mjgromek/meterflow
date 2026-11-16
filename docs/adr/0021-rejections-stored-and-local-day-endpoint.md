# 21. Attributable rejections are stored; the API serves whole local days

Status: Accepted, amended by [22](0022-hardening.md) · Milestone: M10

## Context
The dashboard highlights gaps and invalid readings per meter and ranks meters by
data-quality issues. Until now invalid readings were only reported in the POST response,
and a browser would have to compute Amsterdam day bounds across DST.

## Decision
- A rejection that names a valid EAN (negative kWh, bad moment or missing field) is stored
  with its moment if readable, reason and receive time; duplicates found by the background
  processor are stored the same way. Rejections with an invalid EAN cannot be attributed and
  stay response-only.
- `GET /meters/{ean}/days/{date}` returns every quarter of the Europe/Amsterdam day (92, 96
  or 100) with kWh or null and the reading count, plus gaps and rejections.
  `GET /quality/{date}` returns per-meter gap, rejection and duplicate counts, worst first.

## Consequences
- The frontend draws what it gets; DST logic stays in one place (`QuarterHours`).
- Gaps at the very start or end of a day are not detected (backlog), but such quarters still
  show fewer than 3 readings and are highlighted.
- Rejected: computing local days in the browser; a separate quality database.
