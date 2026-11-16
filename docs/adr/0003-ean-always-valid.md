# 3. An `Ean` instance is always valid

Status: Accepted · Milestone: M1

## Context
EANs arrive as text from CSV files and HTTP requests. If a raw string travels through the
system, every layer has to remember to validate it.

## Decision
`Ean` is a sealed record with a private constructor. The only ways in are `Ean.Parse`
(throws `FormatException`) and `Ean.TryParse` (returns `bool`), which check 18 ASCII digits
and the GS1 check digit.

## Consequences
- Any method that takes an `Ean` can trust it; validation happens once, at the boundary.
- Boundaries that must not throw (CSV rows, API items) use `TryParse`.
- Rejected: `string` plus a validator function, which can be skipped.
