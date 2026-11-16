# Architecture decision records

One record per architecture decision, numbered in order. A decision that changes later gets
a new record that supersedes the old one; old records are not rewritten.

| # | Decision | Milestone |
|---|---|---|
| [1](0001-inward-dependencies.md) | Projects depend inward on a dependency-free Domain | M0 |
| [2](0002-shared-strict-build-settings.md) | Strict build settings shared through Directory.Build.props | M0 |
| [3](0003-ean-always-valid.md) | An `Ean` instance is always valid | M1 |
| [4](0004-time-and-energy-types.md) | `DateTimeOffset` for moments, `decimal` for energy | M1 |
| [5](0005-simulator-faults-at-source.md) | The simulator produces realistic faults deliberately | M2 |
| [6](0006-csv-format.md) | A fixed, culture-invariant CSV format without a CSV library | M2 |
| [7](0007-report-dont-throw.md) | Validation reports problems instead of throwing or filtering | M3 |
| [8](0008-aggregate-on-instants.md) | Aggregate on instants; a local day is computed from the time zone | M3 |
| [9](0009-minimal-api-in-memory-store.md) | Minimal API with an in-memory store behind `IReadingStore` | M4 |
| [10](0010-strict-input-per-item-rejection.md) | Strict API input, rejected per item | M4 |
| [11](0011-in-process-api-tests.md) | API tests run in-process | M4 |
