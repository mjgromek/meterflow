# 5. The simulator produces realistic faults deliberately

Status: Accepted · Milestone: M2

## Context
The validation code needs input with the problems real meter feeds have, and tests need the
same input on every run.

## Decision
The simulator generates a daily consumption profile per meter on a 5-minute schedule, then
`Faults.Inject` drops and duplicates readings at configurable rates. All randomness comes
from a `Random` passed in by the caller and seeded. Meters get synthetic EANs with the
unissued prefix 999.

## Consequences
- Tests can build exact expected sets of gaps and duplicates from a seed.
- No real connection identifiers ever appear in the repository.
- Rejected: hand-written fixture files. They are small, so they miss the cases a full day
  of data hits (DST hours, faults at batch boundaries).
