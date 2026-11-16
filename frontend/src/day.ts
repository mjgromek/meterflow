import type { components } from "./api/schema";

type MeterDay = components["schemas"]["MeterDay"];
export type Bar = MeterDay["quarters"][number] & { flagged: boolean; rejected: boolean };

const quarterMs = 15 * 60 * 1000;

/** One bar per quarter; flagged when it has fewer than 3 readings (every gap does) or a rejection falls in it, which also marks it rejected. */
export function toBars(day: MeterDay): Bar[] {
  const rejections = day.rejected.flatMap((r) => (r.moment ? [Date.parse(r.moment)] : []));
  return day.quarters.map((q) => {
    const start = Date.parse(q.start);
    const rejected = rejections.some((m) => m >= start && m < start + quarterMs);
    return { ...q, flagged: q.readings < 3 || rejected, rejected };
  });
}
