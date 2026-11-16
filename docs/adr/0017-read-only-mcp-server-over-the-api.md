# 17. A read-only MCP server that goes through the REST API

Status: Accepted · Milestone: M8

## Context
An AI agent should answer questions such as "which meter had the most gaps yesterday?"
without access to the database and without being able to change data.

## Decision
- `MeterFlow.Mcp` is a stdio MCP server built on the official C# SDK (ModelContextProtocol).
  It exposes three read-only tools: `list_meters`, `get_consumption(ean, from, to)` and
  `find_gaps(from, to)`.
- Every tool calls the API through a typed `HttpClient` (`AddHttpClient<MeterFlowApi>`),
  base URL `MeterFlow:ApiBaseUrl`. The server has no reference to Infrastructure.
- The API gained the reads the tools need: `GET /meters` and `GET /gaps?from=&to=` (gap
  counts per meter, most first), so the agent does not compute aggregates itself.
- Tool descriptions state units, the offset requirement, ordering and what an empty
  result means. A 400 from the API becomes a short "Error: ..." text the agent can act on.
- Logs go to stderr; stdout carries the protocol.

## Consequences
- One validation and query path for every client: dashboard, MCP and scripts get the same
  answers.
- The MCP server needs the API running; an unreachable API surfaces as a generic tool error.
- Rejected: MCP tools reading SQLite directly (a second data path that can drift); write
  tools (an agent should not create or delete readings); one generic "query" tool (agents
  choose better between specific, well-described tools).
