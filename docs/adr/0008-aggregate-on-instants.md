# 8. Aggregate on instants; a local day is computed from the time zone

Status: Accepted · Milestone: M3

## Context
In Europe/Amsterdam a day has 92, 96 or 100 quarter-hours. Grouping by wall-clock time
merges two real hours in October and invents one in March. See
[time-zones.md](../time-zones.md).

## Decision
`QuarterHours.Aggregate` floors each moment's UTC ticks to 15 minutes and returns quarter
starts in UTC. `QuarterHours.ForLocalDay(day, zone)` steps 15 minutes from local midnight
to the next local midnight using `TimeZoneInfo`.

## Consequences
- DST days come out right with no special cases.
- Only valid for zones whose offset is a multiple of 15 minutes and where local midnight
  exists; Europe/Amsterdam is the only zone in scope.
- Rejected: grouping by local `DateTime`.
