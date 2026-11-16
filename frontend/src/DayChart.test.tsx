import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { DayChart } from "./DayChart";
import { toBars } from "./day";
import { longDay, shortDay } from "./test/fixtures";

/** Axis labels as [text, bar index]; bars are 8 units apart. */
function axisLabels(day: ReturnType<typeof longDay>) {
  const { container } = render(<DayChart bars={toBars(day)} rejected={0} />);
  return Array.from(container.querySelectorAll("text.axis")).map((t) => [t.textContent, Number(t.getAttribute("x")) / 8]);
}

describe("DayChart flags", () => {
  it("draws missing and rejected quarters apart, each with a legend entry", () => {
    const day = longDay();
    day.quarters[9] = { ...day.quarters[9], readings: 2, kwh: 0.1 };
    day.rejected = [{ moment: "2026-10-25T02:35:00+01:00", reason: "NegativeKwh" }];

    const { container } = render(<DayChart bars={toBars(day)} rejected={1} />);

    expect(Array.from(container.querySelectorAll("rect.bar.missing, rect.bar.rejected")).map((r) => r.getAttribute("class"))).toEqual([
      "bar flagged missing",
      "bar flagged rejected",
    ]);
    expect(Array.from(container.querySelectorAll(".legend .swatch")).map((s) => s.getAttribute("class"))).toEqual([
      "swatch",
      "swatch missing",
      "swatch rejected",
    ]);
  });
});

describe("DayChart tooltip", () => {
  it("shows local time, kWh, readings and flags for the hovered or focused bar", () => {
    const day = longDay();
    day.quarters[9] = { ...day.quarters[9], readings: 2, kwh: 0.1 };
    day.rejected = [{ moment: "2026-10-25T02:35:00+01:00", reason: "NegativeKwh" }];
    const { container } = render(<DayChart bars={toBars(day)} rejected={1} />);
    const bars = container.querySelectorAll("[data-bar]");

    fireEvent.mouseEnter(bars[9]);
    expect(screen.getByRole("tooltip")).toHaveTextContent("02:15 CEST · 0.1 kWh · 2 readings · missing readings");
    expect(bars[9]).toHaveAttribute("aria-label", screen.getByRole("tooltip").textContent);

    fireEvent.focus(bars[14]);
    expect(screen.getByRole("tooltip")).toHaveTextContent("02:30 CET · 0.25 kWh · 3 readings · rejected reading");

    fireEvent.blur(bars[14]);
    expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();
  });
});

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
