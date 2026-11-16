# 18. CI on GitHub Actions; the API ships as a non-root container

Status: Accepted · Milestone: M9

## Context
Every push should prove the solution builds and its tests pass on a clean Linux machine,
and the API should run somewhere other than a developer's laptop.

## Decision
- `.github/workflows/ci.yml` runs on every push and pull request: restore the local tools,
  build (warnings are errors), run all tests. A second job builds the Docker image, starts
  it with a named volume, and checks `/health`, a POST, and that the database file is
  created under `/data`.
- `Dockerfile`: multi-stage. The .NET SDK image publishes the API; the smaller ASP.NET
  runtime image runs it as the image's non-root `app` user on port 8080. The database and
  exports live under the `/data` volume, configured through environment variables.

## Consequences
- The README badge reflects the last run on `main`.
- Native dependencies (DuckDB in tests, SQLite) are exercised on Linux in CI.
- Rejected: publishing the image to a registry (no deployment target in scope); a single-
  stage image (ships the SDK, several times larger); running as root.
