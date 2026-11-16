# 14. Readings are stored by a background service fed by a bounded channel

Status: Accepted · Milestone: M6 · Supersedes [10](0010-strict-input-per-item-rejection.md) (duplicate reporting)

## Context
Storing readings inside the HTTP request ties the client's wait time to the database. The
API should accept a batch quickly and store it behind the request.

## Decision
- `POST /readings` checks format per item as before (`BadFormat`, `InvalidEan`,
  `NegativeKwh`), writes valid readings to a singleton bounded `Channel<Reading>` (capacity
  10,000, waits when full) and returns `202 Accepted` with the queued count and the
  rejections.
- `ReadingProcessor`, a `BackgroundService`, reads the channel, stores up to 1,000 readings
  per batch in its own DI scope (the store and `DbContext` are scoped), and logs stored and
  duplicate counts.
- Duplicates are detected when stored, so they are logged, no longer reported in the POST
  response.
- A failed batch is logged with its size and skipped; the processor continues instead of
  stopping the host. Readings in that batch after the failure are not retried.

## Consequences
- The response no longer says a reading is stored, only that it is queued. Clients that
  need confirmation read it back through the consumption endpoint.
- Queued readings live in memory until stored; see [15](0015-drain-on-shutdown.md).
- Rejected: storing synchronously (slow requests); an external queue such as RabbitMQ (an
  extra service for a single-process system); an unbounded channel (memory grows without
  limit when the database is slower than the clients).
