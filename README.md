# MeterFlow

[![CI](https://github.com/mjgromek/meterflow/actions/workflows/ci.yml/badge.svg)](https://github.com/mjgromek/meterflow/actions/workflows/ci.yml)

An energy-metering pipeline in C# on .NET 11. Simulated meters send a reading every 5 minutes;
the API validates and queues them, a background service stores them in SQLite, and the data
is served as 15-minute consumption over REST, exported to Parquet, and exposed to AI agents
through a read-only MCP server. All data is synthetic.

## Architecture

```mermaid
flowchart LR
    Sim[Simulator<br/>console] -- POST /readings --> Api[API<br/>ASP.NET Core minimal API]
    Api -- Channel&lt;Reading&gt; --> Proc[ReadingProcessor<br/>BackgroundService]
    Proc -- store --> Db[(SQLite<br/>EF Core)]
    Api -- queries, 15-min aggregation --> Db
    Api -- POST /exports/date --> Pq[Parquet files<br/>year=/month=/day=]
    Mcp[MCP server<br/>stdio] -- GET --> Api
    Agent[MCP client] --> Mcp
    Duck[DuckDB] -.-> Pq
```

| Project | Role |
|---|---|
| `src/MeterFlow.Domain` | `Ean`, `Reading`, validation, gap and duplicate checks, 15-minute aggregation. No packages. |
| `src/MeterFlow.Infrastructure` | EF Core SQLite store and migrations, Parquet export. |
| `src/MeterFlow.Api` | Minimal API, bounded channel, background processor. |
| `src/MeterFlow.Simulator` | Generates readings with a day/night profile and deliberate gaps and duplicates; writes CSV or posts to the API. |
| `src/MeterFlow.Mcp` | Read-only MCP server (`list_meters`, `get_consumption`, `find_gaps`) that calls the API. |
| `tests/` | xUnit tests; API tests run the whole app in-process with `WebApplicationFactory`. |

Dependencies point inward: Domain references nothing. Design decisions are recorded in
[docs/adr](docs/adr/README.md); time zone handling and the DST-day behaviour (92, 96 or 100
quarters per local day) are in [docs/time-zones.md](docs/time-zones.md).

## Build and test

Requires the .NET 11 SDK.

```sh
dotnet tool restore
dotnet build
dotnet test
```

## Run

**API** (listens on `http://localhost:5265`, creates `meterflow.db` and applies migrations on
startup):

```sh
dotnet run --project src/MeterFlow.Api
```

**Simulator**: arguments are meters, days, and either `--api <url>` or a CSV path.

```sh
dotnet run --project src/MeterFlow.Simulator -- 3 2 --api http://localhost:5265
dotnet run --project src/MeterFlow.Simulator -- 3 1 readings.csv
```

**Queries** (the simulator starts its series N days before today, so adjust the dates):

```sh
curl http://localhost:5265/meters
curl "http://localhost:5265/meters/999000000000000007/consumption?from=2026-11-15T00:00:00Z&to=2026-11-15T01:00:00Z"
curl "http://localhost:5265/gaps?from=2026-11-15T00:00:00Z&to=2026-11-16T00:00:00Z"
```

**Parquet export and DuckDB**: exports one Europe/Amsterdam local day to
`exports/year=YYYY/month=MM/day=DD/quarters.parquet`.

```sh
curl -X POST http://localhost:5265/exports/2026-11-15
duckdb -c "SELECT ean, day, count(*) AS quarters, sum(kwh) AS kwh
           FROM read_parquet('src/MeterFlow.Api/exports/**/*.parquet', hive_partitioning = true)
           GROUP BY ALL ORDER BY ean"
```

**MCP server** (needs the API running; base URL in `MeterFlow:ApiBaseUrl`, default
`http://localhost:5265`). Register it with Claude Code from the repository root:

```sh
claude mcp add meterflow -- dotnet run --project src/MeterFlow.Mcp
```

**Docker** (API only; database and exports go to the `/data` volume):

```sh
docker build -t meterflow-api .
docker run -d -p 8080:8080 -v meterflow-data:/data meterflow-api
curl http://localhost:8080/health
```

The image runs as a non-root user. `ConnectionStrings__MeterFlow` and `Export__Root` override
the database and export paths.

## API

| Method | Path | Result |
|---|---|---|
| GET | `/health` | 200 |
| POST | `/readings` | Body: array of `{ean, moment, kwh}`. 202 with accepted count and per-item rejections (`BadFormat`, `InvalidEan`, `NegativeKwh`). Stored asynchronously. |
| GET | `/meters` | Each meter with reading count, first and last moment. |
| GET | `/meters/{ean}/consumption?from=&to=` | 15-minute kWh totals in `[from, to)`. |
| GET | `/gaps?from=&to=` | Missing 5-minute readings per meter, most gaps first. |
| POST | `/exports/{yyyy-MM-dd}` | Writes the local day's quarters to Parquet; returns path and row count. |

Moments need an explicit offset or `Z`; anything else is a 400.

## Continuous integration

GitHub Actions ([ci.yml](.github/workflows/ci.yml)) restores tools, builds with warnings as
errors and runs all tests on every push and pull request.

## Next

A React + TypeScript dashboard reading through the API (milestone M10).
