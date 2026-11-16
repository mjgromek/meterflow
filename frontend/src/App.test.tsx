import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import App from "./App";
import type { components } from "./api/schema";
import { longDay, meters, quarters } from "./test/fixtures";

type Routes = Record<string, unknown>;

/** Stubs fetch with path -> JSON body; unknown paths answer 500. Returns the requested paths. */
function stubApi(routes: Routes) {
  const fetchMock = vi.fn(async (request: Request) => {
    const path = new URL(request.url).pathname;
    return path in routes
      ? new Response(JSON.stringify(routes[path]), { headers: { "Content-Type": "application/json" } })
      : new Response("", { status: 500 });
  });
  vi.stubGlobal("fetch", fetchMock);
  return () => fetchMock.mock.calls.map(([request]) => new URL(request.url).pathname);
}

const quality: components["schemas"]["MeterQuality"][] = Array.from({ length: 12 }, (_, i) => ({
  ean: `99900000000000${String(1000 + i)}`,
  gaps: 12 - i,
  rejected: i % 2,
  duplicates: 0,
}));

const flaggedDay = () => {
  const day = longDay();
  day.quarters[9] = { ...day.quarters[9], readings: 1, kwh: 0.05 };
  day.rejected = [{ moment: "2026-10-25T02:35:00+01:00", reason: "NegativeKwh" }];
  return day;
};

describe("App", () => {
  it("shows meters, the day chart with a summary and highlights, and the quality table in API order", async () => {
    stubApi({
      "/meters": meters,
      "/meters/999000000000000007/days/2026-10-25": flaggedDay(),
      "/quality/2026-10-25": quality,
    });
    render(<App />);

    const select = await screen.findByLabelText("Meter");
    expect(within(select).getAllByRole("option").map((o) => o.textContent)).toEqual(meters.map((m) => m.ean));
    expect(screen.getByLabelText("Day")).toHaveValue("2026-10-25");

    const chart = await screen.findByRole("img", { name: /100 quarters/ });
    expect(chart.querySelectorAll("rect.bar")).toHaveLength(100);
    expect(chart.querySelectorAll("rect.bar.flagged")).toHaveLength(2);
    expect(screen.getByText(/2 quarters with missing or rejected readings/)).toBeInTheDocument();

    const table = await screen.findByRole("table", { name: /most issues/ });
    const eans = within(table).getAllByRole("row").slice(1).map((row) => within(row).getAllByRole("cell")[0].textContent);
    expect(eans).toEqual(quality.slice(0, 10).map((q) => q.ean));
  });

  it("refetches the day when the meter changes", async () => {
    const requested = stubApi({
      "/meters": meters,
      "/meters/999000000000000007/days/2026-10-25": longDay(),
      "/meters/999000000000000014/days/2026-10-25": flaggedDay(),
      "/quality/2026-10-25": quality,
    });
    render(<App />);
    await screen.findByRole("img", { name: /100 quarters/ });

    await userEvent.selectOptions(screen.getByLabelText("Meter"), "999000000000000014");

    expect(await screen.findByText(/2 quarters with missing or rejected readings/)).toBeInTheDocument();
    expect(requested()).toContain("/meters/999000000000000014/days/2026-10-25");
  });

  it("refetches the day and the quality table when the date changes", async () => {
    const requested = stubApi({
      "/meters": meters,
      "/meters/999000000000000007/days/2026-10-25": longDay(),
      "/meters/999000000000000007/days/2026-03-29": { quarters: quarters("2026-03-28T23:00:00Z", 92), gaps: [], rejected: [] },
      "/quality/2026-10-25": quality,
      "/quality/2026-03-29": [],
    });
    render(<App />);
    await screen.findByRole("img", { name: /100 quarters/ });

    fireEvent.change(screen.getByLabelText("Day"), { target: { value: "2026-03-29" } });

    const chart = await screen.findByRole("img", { name: /92 quarters/ });
    expect(chart.querySelectorAll("rect.bar")).toHaveLength(92);
    expect(requested()).toContain("/quality/2026-03-29");
  });

  it("shows an error message when the API fails", async () => {
    stubApi({ "/meters": meters });
    render(<App />);

    expect(await screen.findByRole("alert")).toHaveTextContent(/could not load/i);
  });

  it("shows an empty state for a day without readings", async () => {
    stubApi({
      "/meters": meters,
      "/meters/999000000000000007/days/2026-10-25": { quarters: quarters("2026-10-24T22:00:00Z", 100, 0), gaps: [], rejected: [] },
      "/quality/2026-10-25": [],
    });
    render(<App />);

    expect(await screen.findByText(/no readings for this meter on 2026-10-25/i)).toBeInTheDocument();
    expect(screen.queryByRole("img")).not.toBeInTheDocument();
  });
});
