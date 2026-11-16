import type { components } from "../api/schema";

type Schemas = components["schemas"];

const quarterMs = 15 * 60 * 1000;

/** Quarters as the API returns them: UTC starts, three readings each unless overridden. */
export function quarters(firstUtc: string, count: number, readings = 3): Schemas["DayQuarter"][] {
  const first = Date.parse(firstUtc);
  return Array.from({ length: count }, (_, i) => ({
    start: new Date(first + i * quarterMs).toISOString().replace(".000Z", "+00:00"),
    kwh: readings > 0 ? 0.25 : null,
    readings,
  }));
}

// 2026-10-25 in Europe/Amsterdam runs 25 hours, 2026-03-29 runs 23.
export const longDay = (): Schemas["MeterDay"] => ({ quarters: quarters("2026-10-24T22:00:00Z", 100), gaps: [], rejected: [] });
export const shortDay = (): Schemas["MeterDay"] => ({ quarters: quarters("2026-03-28T23:00:00Z", 92), gaps: [], rejected: [] });

export const meters: Schemas["MeterRow"][] = [
  { ean: "999000000000000007", readings: 300, first: "2026-10-24T22:00:00+00:00", last: "2026-10-25T22:55:00+00:00" },
  { ean: "999000000000000014", readings: 290, first: "2026-10-24T22:00:00+00:00", last: "2026-10-25T22:55:00+00:00" },
];
