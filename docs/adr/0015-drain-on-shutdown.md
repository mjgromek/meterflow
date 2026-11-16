# 15. Queued readings are drained on shutdown

Status: Accepted · Milestone: M6

## Context
Readings accepted with 202 sit in the in-memory channel. Stopping the API must not lose
them silently.

## Decision
The web server stops first (ASP.NET Core registers it after user hosted services, and
hosted services stop in reverse order), so no new readings arrive. `ReadingProcessor` then
stores everything still in the channel before returning, and logs "Queue drained on
shutdown". If the host's shutdown timeout cancels the drain, it logs how many readings were
dropped.

## Consequences
- A normal stop (SIGINT/SIGTERM) stores every accepted reading; a test proves it.
- A crash or kill -9 still loses what is queued. Acceptable here; a durable queue would be
  the next step if it were not.
- The shutdown timeout bounds how long a stop can take.
- Rejected: discarding the queue on stop; writing the queue to disk (a second storage
  system to keep consistent).
