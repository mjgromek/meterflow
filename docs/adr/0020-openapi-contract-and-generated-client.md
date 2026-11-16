# 20. The OpenAPI document is the contract; frontend types are generated from it

Status: Accepted · Milestone: M10

## Context
The dashboard is TypeScript, the API is C#. Hand-written TypeScript types for the
responses would drift from the C# records without any compiler noticing.

## Decision
- The API uses `Microsoft.AspNetCore.OpenApi` (`AddOpenApi`, `MapOpenApi` at
  `/openapi/v1.json`). Endpoints return `TypedResults` with declared
  `Results<Ok<T>, BadRequest>` types, so every response schema is derived from the C#
  records (`MeterDay`, `DayQuarter`, `MeterQuality`, ...).
- `Microsoft.Extensions.ApiDescription.Server` writes the document at build time to
  `frontend/openapi/MeterFlow.Api.json`, which is committed. The generator runs the app's
  entry point, so database migration is skipped when the entry assembly is
  `GetDocument.Insider`.
- `openapi-typescript` turns the document into `frontend/src/api/schema.d.ts`; calls go
  through `openapi-fetch`, typed by those `paths`.
- CI rebuilds both and fails if either differs from the committed files.

## Consequences
- Renaming or changing a C# record changes the document, the generated types, and then
  breaks the TypeScript build where the frontend uses it.
- The document is OpenAPI 3.2 (the .NET 11 default), which openapi-typescript reads.
- Rejected: hand-written TypeScript types; NSwag or Kiota generated clients (larger output
  and runtime for three GET calls); generating at runtime only (the frontend build would
  need a running API).
