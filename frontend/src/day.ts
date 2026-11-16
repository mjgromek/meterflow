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

export const time = new Intl.DateTimeFormat("en-GB", { timeZone: "Europe/Amsterdam", hour: "2-digit", minute: "2-digit", timeZoneName: "short" });
export const clock = new Intl.DateTimeFormat("en-GB", { timeZone: "Europe/Amsterdam", hour: "2-digit", minute: "2-digit", hourCycle: "h23" });

/** Calendar arithmetic on a yyyy-mm-dd string, done in UTC so DST never shifts the date. */
export function shiftDay(date: string, days: number): string {
  const d = new Date(`${date}T00:00:00Z`);
  d.setUTCDate(d.getUTCDate() + days);
  return d.toISOString().slice(0, 10);
}
