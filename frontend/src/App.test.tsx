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

    const list = await screen.findByRole("list", { name: "Meters" });
    await within(list).findAllByRole("button");
    expect(within(list).getAllByRole("button").map((o) => o.textContent)).toEqual(meters.map((m) => m.ean));
    expect(within(list).getByRole("button", { name: meters[0].ean })).toHaveAttribute("aria-current", "true");
    expect(screen.getByLabelText("Day")).toHaveValue("2026-10-25");

    const chart = await screen.findByRole("group", { name: /100 quarters/ });
    expect(chart.querySelectorAll("rect.bar")).toHaveLength(100);
    expect(chart.querySelectorAll("rect.bar.flagged")).toHaveLength(2);
    expect(screen.getByText(/2 quarters with missing or rejected readings/)).toBeInTheDocument();

    const tiles = screen.getByRole("list", { name: "Day summary" });
    // 99 quarters at 0.25 kWh plus one at 0.05.
    expect(within(tiles).getByText("Total").nextSibling).toHaveTextContent("24.80 kWh");
    expect(within(tiles).getByText("Quarters").nextSibling).toHaveTextContent("100");
    expect(within(tiles).getByText("Peak quarter").nextSibling).toHaveTextContent("00:00 · 0.25 kWh");

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
    await screen.findByRole("group", { name: /100 quarters/ });

    await userEvent.click(screen.getByRole("button", { name: "999000000000000014" }));

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
    await screen.findByRole("group", { name: /100 quarters/ });

    fireEvent.change(screen.getByLabelText("Day"), { target: { value: "2026-03-29" } });

    const chart = await screen.findByRole("group", { name: /92 quarters/ });
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
    expect(screen.queryByRole("group", { name: /quarters/ })).not.toBeInTheDocument();
  });

  it("filters the meter list by search text and clears it", async () => {
    stubApi({ "/meters": meters, "/meters/999000000000000007/days/2026-10-25": longDay(), "/quality/2026-10-25": [] });
    render(<App />);
    const list = await screen.findByRole("list", { name: "Meters" });
    await within(list).findAllByRole("button");

    await userEvent.type(screen.getByLabelText("Search meters"), "014");
    expect(within(list).getAllByRole("button").map((b) => b.textContent)).toEqual(["999000000000000014"]);

    await userEvent.click(screen.getByRole("button", { name: "Clear search" }));
    expect(within(list).getAllByRole("button")).toHaveLength(2);
    expect(screen.getByLabelText("Search meters")).toHaveValue("");
  });

  it("moves one day back and forward with the day buttons", async () => {
    const requested = stubApi({
      "/meters": meters,
      "/meters/999000000000000007/days/2026-10-25": longDay(),
      "/meters/999000000000000007/days/2026-10-26": longDay(),
      "/meters/999000000000000007/days/2026-10-24": longDay(),
      "/quality/2026-10-25": [],
      "/quality/2026-10-26": [],
      "/quality/2026-10-24": [],
    });
    render(<App />);
    await screen.findByRole("group", { name: /100 quarters/ });

    await userEvent.click(screen.getByRole("button", { name: "Next day" }));
    expect(screen.getByLabelText("Day")).toHaveValue("2026-10-26");
    await userEvent.click(screen.getByRole("button", { name: "Previous day" }));
    await userEvent.click(screen.getByRole("button", { name: "Previous day" }));
    expect(screen.getByLabelText("Day")).toHaveValue("2026-10-24");

    await vi.waitFor(() => expect(requested()).toContain("/meters/999000000000000007/days/2026-10-24"));
    expect(requested()).toEqual(expect.arrayContaining(["/quality/2026-10-26", "/quality/2026-10-24"]));
  });

  it("selects a meter from the quality table by click or Enter", async () => {
    const requested = stubApi({
      "/meters": meters,
      "/meters/999000000000000007/days/2026-10-25": longDay(),
      "/meters/999000000000000014/days/2026-10-25": flaggedDay(),
      "/quality/2026-10-25": [
        { ean: "999000000000000014", gaps: 3, rejected: 1, duplicates: 0 },
        { ean: "999000000000000007", gaps: 1, rejected: 0, duplicates: 0 },
      ],
    });
    render(<App />);
    const table = await screen.findByRole("table", { name: /most issues/ });
    const [, first, second] = within(table).getAllByRole("row");

    await userEvent.click(within(first).getAllByRole("cell")[1]);
    expect(await screen.findByText(/2 quarters with missing or rejected readings/)).toBeInTheDocument();
    expect(requested()).toContain("/meters/999000000000000014/days/2026-10-25");
    expect(screen.getByRole("heading", { level: 1 })).toHaveTextContent("999000000000000014");
    expect(within(screen.getByRole("list", { name: "Day summary" })).getByText("Issues").nextSibling).toHaveTextContent("4");

    within(second).getByRole("button").focus();
    await userEvent.keyboard("{Enter}");
    expect(screen.getByRole("heading", { level: 1 })).toHaveTextContent("999000000000000007");
  });

  it("takes the sidebar issue badges from /quality without fetching other meters' days", async () => {
    const requested = stubApi({
      "/meters": meters,
      "/meters/999000000000000007/days/2026-10-25": longDay(),
      "/quality/2026-10-25": [{ ean: "999000000000000014", gaps: 2, rejected: 1, duplicates: 4 }],
    });
    render(<App />);

    const list = await screen.findByRole("list", { name: "Meters" });
    expect(await within(list).findByRole("button", { name: "999000000000000014 7 issues" })).toBeInTheDocument();
    expect(within(list).getByRole("button", { name: "999000000000000007" })).toBeInTheDocument();
    expect(requested().filter((p) => p.includes("/days/"))).toEqual(["/meters/999000000000000007/days/2026-10-25"]);
  });
});
