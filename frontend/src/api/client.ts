import createClient from "openapi-fetch";
import type { paths } from "./schema";

export const api = createClient<paths>({
  baseUrl: import.meta.env.VITE_API_URL ?? "http://localhost:5265",
  // Looked up per call, so a stubbed global fetch is used.
  fetch: (request) => fetch(request),
});
