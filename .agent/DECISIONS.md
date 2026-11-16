<!-- Cap: 8 lines per entry. Append only — never edit or delete a past entry. Newest at the bottom. -->

# DECISIONS

One entry per material decision: what, why, what it rules out.

<!--
## 2026-01-01 — Sessions keyed by an opaque token, not a row id
What: Session subjects are a random opaque token issued at login.
Why: Row ids are recycled by the database and a recycled id was reachable as another
     user's session in three requests, under a passing test suite.
Rules out: Any lookup that treats a primary key as an identity claim.
Level: 2, approved by the human.
-->

## 2026-11-16 — Reading checks report, they do not filter or throw
What: `ReadingChecks.FindDuplicates` and `FindGaps` return (Ean, Moment) pairs; the input is untouched.
Why: The caller decides whether a duplicate is dropped, kept or flagged. Gaps step in instants
     (DateTimeOffset), so the repeated hour on 2026-10-25 is two distinct runs of moments, not a gap.
Rules out: Gap detection on local wall-clock times; silent de-duplication inside the checks.
Level: 1.

## 2026-11-16 — CSV parse returns readings and rejected rows, never throws per row
What: `ReadingCsv.Parse` returns `ReadingCsvResult(Readings, Rejected)`; a rejected row is (LineNumber, RejectReason).
Why: One bad line must not lose the rest of a file. Moments parse with the exact "O" format and
     invariant culture, so the UTC offset survives and only what `CsvExport` writes is accepted.
Rules out: Lenient CSV parsing (quoting, other date formats, culture decimals); a CSV NuGet package.
Level: 1.

## 2026-11-16 — Quarter-hours are floored on the instant and returned in UTC
What: `QuarterHours.Aggregate` floors UtcTicks to 15 minutes; quarter starts carry offset +00:00.
Why: Flooring the instant keeps the repeated 02:00 on 2026-10-25 as two quarters. UTC is the
     simplest offset to build; DateTimeOffset equality compares instants, so callers are unaffected.
     `ForLocalDay` steps 15 minutes from local midnight to the next one, using the zone's offset.
Rules out: Flooring local wall-clock times; zones with non-15-minute offsets (UTC floor misaligns).
Level: 1.

## 2026-11-16 — API tests run in-process with Microsoft.AspNetCore.Mvc.Testing
What: The API test project adds Microsoft.AspNetCore.Mvc.Testing (WebApplicationFactory).
Why: Tests call the real pipeline through an in-memory HttpClient, no port, no process start.
     Without it, each test would launch `dotnet run`, wait on a port and kill it: slower, flaky.
Rules out: Spawning the API process from unit tests. Level: 2, approved by the human.

## 2026-11-16 — Architecture decisions are published as ADRs in docs/adr
What: One numbered ADR per architecture decision, with its milestone; index in docs/adr/README.md.
Why: DECISIONS.md is pipeline state; the ADRs are the public, durable record per milestone.
Rules out: Rewriting an old ADR; a changed decision gets a new ADR that supersedes it.
Level: 1, requested by the human.
