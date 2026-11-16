<!-- No cap. Entries are deleted when done or when they turn out to be false. Every entry carries the condition that makes it urgent. -->

# BACKLOG

Deferred items. P2 findings land here. An item with no urgency condition is not a backlog
item, it is a wish — write the condition or drop it.

Format:

```
- <item> — Urgent when: <the observable condition that promotes it into a slice>
```

## Deferred

- `FindGaps` misses readings absent at the start or end of a day; it only looks between a
  meter's first and last reading. Urgent when: aggregation reports a day as complete while
  its first or last quarter-hour is missing.
- `ReadingCsv.Parse` expects lines without line breaks; a caller splitting a CRLF file on '\n'
  gets every row rejected as BadFormat. Urgent when: the first caller reads a file itself
  instead of via File.ReadLines or StringReader.
- `ReadingCsv.Parse` skips line 1 without checking it is the header; a headerless file loses
  its first reading unreported. Urgent when: input other than CsvExport output reaches Parse.
- `QuarterHours.ForLocalDay` assumes local midnight exists and the zone offset is a multiple
  of 15 minutes. Urgent when: a zone other than Europe/Amsterdam is supported.
