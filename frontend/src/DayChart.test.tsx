import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { DayChart } from "./DayChart";
import { toBars } from "./day";
import { longDay, shortDay } from "./test/fixtures";

/** Axis labels as [text, bar index]; bars are 8 units apart. */
function axisLabels(day: ReturnType<typeof longDay>) {
  const { container } = render(<DayChart bars={toBars(day)} rejected={0} />);
  return Array.from(container.querySelectorAll("text.axis")).map((t) => [t.textContent, Number(t.getAttribute("x")) / 8]);
}

describe("DayChart axis", () => {
  it("labels local 00:00, 06:00, 12:00 and 18:00 on the 25-hour day", () => {
    // Summer time until 03:00 local, so 06:00 local is 05:00Z, 7 hours after 22:00Z.
    expect(axisLabels(longDay())).toEqual([["00:00", 0], ["06:00", 28], ["12:00", 52], ["18:00", 76]]);
  });

  it("labels local 00:00, 06:00, 12:00 and 18:00 on the 23-hour day", () => {
    // Summer time from 02:00 local, so 06:00 local is 04:00Z, 5 hours after 23:00Z.
    expect(axisLabels(shortDay())).toEqual([["00:00", 0], ["06:00", 20], ["12:00", 44], ["18:00", 68]]);
  });
});
