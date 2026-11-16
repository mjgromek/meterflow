<!-- No cap. Entries are deleted when done or when they turn out to be false. Every entry carries the condition that makes it urgent. -->

# BACKLOG

Deferred items. P2 findings land here. An item with no urgency condition is not a backlog
item, it is a wish — write the condition or drop it.

Format:

```
- <item> — Urgent when: <the observable condition that promotes it into a slice>
```

## Deferred

- `QuarterHours.ForLocalDay` assumes local midnight exists and the zone offset is a multiple
  of 15 minutes. Urgent when: a zone other than Europe/Amsterdam is supported.
- API base URL comes only from `--api`, not appsettings.json. Urgent when: the simulator
  runs without arguments against an API.
- Channel backpressure (full queue waits, honours cancellation) has no test; capacity is a
  constant. Urgent when: clients rely on the full-queue behaviour or capacity is tuned.
- Export builds the whole day in memory inside the request. Urgent when: a day exceeds ~1M
  rows or the request times out.
- Dashboard flags missing and rejected readings with one colour. Urgent when: users need to
  tell the two apart from the chart.
