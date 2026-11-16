import type { components } from "./api/schema";

type MeterDay = components["schemas"]["MeterDay"];
export type Bar = MeterDay["quarters"][number] & { flagged: boolean };

const quarterMs = 15 * 60 * 1000;

/** One bar per quarter; flagged when it has fewer than 3 readings (every gap does) or a rejection falls in it. */
export function toBars(day: MeterDay): Bar[] {
  const rejected = day.rejected.flatMap((r) => (r.moment ? [Date.parse(r.moment)] : []));
  return day.quarters.map((q) => {
    const start = Date.parse(q.start);
    return { ...q, flagged: q.readings < 3 || rejected.some((m) => m >= start && m < start + quarterMs) };
  });
}
