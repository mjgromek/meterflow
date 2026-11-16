# Time zones

## How the code represents time

- Every reading carries a `DateTimeOffset`: a local wall-clock time plus its UTC offset, so it
  names one instant. Two readings are equal only if they are the same instant.
- [`QuarterHours.Aggregate`](../src/MeterFlow.Domain/QuarterHours.cs) floors each reading to a
  15-minute boundary in UTC ticks, so a quarter is a span of real time, not a clock label.
- [`QuarterHours.ForLocalDay`](../src/MeterFlow.Domain/QuarterHours.cs) lists a local day's
  quarters from local midnight to the next local midnight, taking each midnight's offset from
  a `TimeZoneInfo`. The length of the day follows from the two offsets.
- [`ReadingChecks`](../src/MeterFlow.Domain/ReadingChecks.cs) compares moments as instants:
  `FindDuplicates` groups on `(Ean, Moment)`, `FindGaps` steps 5 minutes between instants.

Quarters per day in `Europe/Amsterdam`, covered by
`A_local_day_has_one_quarter_per_15_minutes_between_local_midnights` in
[`QuarterHoursTests`](../tests/MeterFlow.Domain.Tests/QuarterHoursTests.cs):

| Local day  | Clock change          | Hours | Quarters | 5-minute readings |
|------------|-----------------------|-------|----------|-------------------|
| 2026-03-29 | 02:00 CET -> 03:00 CEST | 23  | 92       | 276               |
| 2026-11-16 | none                  | 24    | 96       | 288               |
| 2026-10-25 | 03:00 CEST -> 02:00 CET | 25  | 100      | 300               |

`The_repeated_02_00_on_the_last_summer_time_day_is_two_quarters` checks that 02:00+02:00 and
02:00+01:00 stay separate quarters. `Each_meter_gets_the_days_100_quarters_and_keeps_its_own_total`
in [`RoundTripTests`](../tests/MeterFlow.Simulator.Tests/RoundTripTests.cs) runs the 25-hour
day through CSV export, parsing and aggregation for two meters.

## Failure mode: naive local times

Storing `DateTime` wall-clock values without the offset, and assuming every day has 96
quarters from 00:00 to 23:45, breaks on both change days.

**2026-03-29, spring forward.** Local 02:00 to 02:59 does not exist.

- A naive 96-slot grid includes 02:00, 02:15, 02:30 and 02:45: 4 quarters that never existed
  and stay empty. The correct count is 92.
- Naive moments jump from 01:55 to 03:00, so a gap check on them reports 12 missing readings
  that were never due.

**2026-10-25, fall back.** Local 02:00 to 02:59 happens twice, first at +02:00, then at +01:00.

- Without the offset, both hours map to the same labels. The quarters 02:00, 02:15, 02:30 and
  02:45 each receive 6 readings instead of 3, so their kWh is the sum of two real hours,
  roughly double.
- The day shows 96 quarters instead of 100, while the total kWh is unchanged, so the
  per-quarter values no longer match the real intervals.
- `FindDuplicates` on naive moments reports 12 duplicates per meter (02:00 to 02:55), all of
  them valid readings.

Keeping the offset on every reading and flooring in UTC avoids these effects; only the
day boundaries need a `TimeZoneInfo`.
