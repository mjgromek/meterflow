# 19. CORS allows only the configured frontend origin, GET only

Status: Accepted · Milestone: M10

## Context
The dashboard runs on the Vite dev server (`http://localhost:5173`) and calls the API on
another origin, so the browser requires CORS headers from the API.

## Decision
One named policy, `frontend`, with `WithOrigins(<Cors:FrontendOrigin>)` and
`WithMethods("GET")`. It is registered and applied (`UseCors("frontend")`) only when
`Cors:FrontendOrigin` is set; `dotnet run` sets it through `launchSettings.json`. Without
the setting, as in the Docker image, the API sends no CORS headers.

## Consequences
- A page on any other origin cannot read API responses in a browser; tests check a
  preflight from 5173 is allowed and one from another origin is not.
- The dashboard is read-only, so no POST from browsers is needed.
- Rejected: `AllowAnyOrigin()` (any website could read the data from a user's browser);
  serving the dashboard from the API to avoid CORS (couples the two builds); allowing all
  methods "for later".
