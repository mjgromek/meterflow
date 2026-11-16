# 7. Validation reports problems instead of throwing or filtering

Status: Accepted · Milestone: M3

## Context
A file or batch with one bad row is the normal case, not an exceptional one.

## Decision
`ReadingCsv.Parse` returns valid readings plus rejected rows with line number and a
`RejectReason` (`BadFormat`, `InvalidEan`, `NegativeKwh`). `ReadingChecks.FindDuplicates`
and `FindGaps` return what they found and leave the input unchanged.

## Consequences
- One bad line never costs the rest of a file, and every problem is reported, not only
  the first.
- The caller decides what to do with duplicates.
- Rejected: exceptions per row (stops at the first error); silent de-duplication inside
  the checks (hides data-quality problems).
