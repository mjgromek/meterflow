# 4. `DateTimeOffset` for moments, `decimal` for energy

Status: Accepted · Milestone: M1

## Context
Readings are local to Europe/Amsterdam, where one hour per year repeats and one is skipped.
Energy values are summed into totals that must match exactly.

## Decision
`Reading(Ean, DateTimeOffset Moment, decimal Kwh)`. A moment always carries its UTC offset,
and equality compares instants. kWh is `decimal`.

## Consequences
- 02:30+02:00 and 02:30+01:00 on 2026-10-25 are different readings.
- Totals compare with `==`; no floating-point tolerance is needed.
- Rejected: `DateTime` (its `Kind` is easy to lose, and local values are ambiguous at DST);
  `double` (0.1 is not exact, and sums drift).
