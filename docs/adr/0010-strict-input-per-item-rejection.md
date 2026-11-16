# 10. Strict API input, rejected per item

Status: Duplicate reporting superseded by [14](0014-background-processing-with-channel.md) · Milestone: M4

## Context
`POST /readings` receives batches from clients that may send partial or wrong data. A
reading stored with a default moment (year 0001) or a guessed time zone is silent
corruption.

## Decision
- The response is 200 with `{accepted, rejected: [{index, reason}]}`. Valid items are
  stored even when others in the batch are rejected.
- A missing field, a null item, or a moment without an explicit offset or `Z` is
  `BadFormat` for that item. A duplicate (same EAN and instant, already stored or earlier in
  the batch) is `Duplicate`.
- An unreadable body, a non-array body, or `kwh` sent as a string is 400 for the request.
- The consumption endpoint uses the same moment parsing for `from` and `to`.

## Consequences
- A client learns exactly which items failed and why.
- Rejected: assuming server-local time for offset-less moments (the M3 DST bug again);
  rejecting a whole batch for one bad item.
