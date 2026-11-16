<!-- Cap: 120 lines. Over cap: move settled facts into DECISIONS.md and delete them here. State describes now. -->

# STATE

Pipeline: easydev, upstream 9bfa13b. History was rewritten on 2026-11-16 (squash and author
email), so pre-2026-11-16 hashes from conversations no longer resolve.

## Current slice

None. M6 complete (83/83, builder real run with graceful drain; no separate checker pass
under autopilot time limits), auto-approved under the human's autopilot grant.

## Acceptance criteria (M6)

- `POST /readings` validates format synchronously (BadFormat, InvalidEan, NegativeKwh, per
  item as now) and returns 202 Accepted with {accepted: queued count, rejected}. Valid
  readings go onto a bounded `Channel<Reading>` (singleton); when full, the POST awaits
  (backpressure), honouring the request's CancellationToken.
- A `BackgroundService` (ReadingProcessor) reads the channel, stores readings through a
  scoped IReadingStore (one scope per drained batch), and logs stored and duplicate counts.
  Duplicates are no longer in the POST response; ADR 10 is superseded for that part.
- Graceful shutdown: on stop, the processor drains everything already queued before
  returning; if the host's shutdown timeout cuts it off, it logs how many readings were
  dropped. A test proves queued readings are stored after StopAsync.
- Tests that relied on synchronous storing wait for processing (e.g. poll consumption with a
  timeout, or a small "drained" signal) rather than sleeping fixed times. The concurrent
  identical POSTs still store once; the end-to-end test still matches totals.
- ApiSender and the simulator still work (202 handled; rejected counts are format only).

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
