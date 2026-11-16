import { describe, expect, it } from "vitest";
import { toBars } from "./day";
import { longDay, shortDay } from "./test/fixtures";

describe("toBars", () => {
  it("gives one bar per quarter of the local day", () => {
    expect(toBars(longDay())).toHaveLength(100);
    expect(toBars(shortDay())).toHaveLength(92);
  });

  it("flags quarters with a gap or a rejection and nothing else", () => {
    const day = longDay();
    // 02:20 local in summer time is 00:20Z, the quarter starting 00:15Z (index 9).
    day.quarters[9] = { ...day.quarters[9], readings: 2, kwh: 0.1 };
    day.gaps = ["2026-10-25T00:20:00+00:00"];
    // 02:35 in winter time (+01:00) is 01:35Z, the quarter starting 01:30Z (index 14).
    day.rejected = [
      { moment: "2026-10-25T02:35:00+01:00", reason: "NegativeKwh" },
      { moment: null, reason: "BadFormat" },
    ];

    const flagged = toBars(day).flatMap((bar, i) => (bar.flagged ? [i] : []));

    expect(flagged).toEqual([9, 14]);
  });

  it("marks a quarter with a rejection as rejected even when readings are also missing", () => {
    const day = longDay();
    day.quarters[9] = { ...day.quarters[9], readings: 2, kwh: 0.1 };
    day.quarters[14] = { ...day.quarters[14], readings: 2, kwh: 0.1 };
    day.rejected = [{ moment: "2026-10-25T02:35:00+01:00", reason: "NegativeKwh" }];

    const bars = toBars(day);

    expect([bars[9].rejected, bars[14].rejected]).toEqual([false, true]);
  });
});
