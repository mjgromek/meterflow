# 6. A fixed, culture-invariant CSV format without a CSV library

Status: Accepted · Milestone: M2

## Context
Readings move between the simulator and the pipeline as files before the API exists.

## Decision
Header `ean,moment,kwh`. Moments use the round-trip format `"O"` with offset; numbers use
`CultureInfo.InvariantCulture`. Writing and parsing are done with `TextWriter` and
`string.Split`.

## Consequences
- The same file parses identically on a machine with a Dutch locale (decimal comma).
- The parser accepts only what `CsvExport` writes; quoting and other date formats are out
  of scope.
- Rejected: a CSV NuGet package. Three columns with no quoting do not need one.
