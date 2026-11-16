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
| [12](0012-sqlite-store-with-ef-core.md) | Readings stored in SQLite through EF Core | M5 |
| [13](0013-migrations-at-startup.md) | EF Core migrations, applied at startup | M5 |
| [14](0014-background-processing-with-channel.md) | Readings are stored by a background service fed by a bounded channel | M6 |
| [15](0015-drain-on-shutdown.md) | Queued readings are drained on shutdown | M6 |
| [16](0016-parquet-export-per-local-day.md) | Quarter-hour export as Parquet per local day, Hive-style paths | M7 |
| [17](0017-read-only-mcp-server-over-the-api.md) | A read-only MCP server that goes through the REST API | M8 |
| [18](0018-ci-and-container.md) | CI on GitHub Actions; the API ships as a non-root container | M9 |
| [19](0019-cors-for-the-dev-frontend-only.md) | CORS allows only the configured frontend origin, GET only | M10 |
| [20](0020-openapi-contract-and-generated-client.md) | The OpenAPI document is the contract; frontend types are generated from it | M10 |
| [21](0021-rejections-stored-and-local-day-endpoint.md) | Attributable rejections are stored; the API serves whole local days | M10 |
