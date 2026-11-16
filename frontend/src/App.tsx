import { useEffect, useState, type KeyboardEvent } from "react";
import { api } from "./api/client";
import type { components } from "./api/schema";
import { DayChart } from "./DayChart";
import { clock, shiftDay, toBars } from "./day";

type Schemas = components["schemas"];

const failed = "Could not load data from the API.";

async function load<T>(call: Promise<{ data?: T; response: Response }>): Promise<T> {
  const { data, response } = await call;
  if (!response.ok || data === undefined) throw new Error(`HTTP ${response.status}`);
  return data;
}

const issues = (q?: Schemas["MeterQuality"]) => (q ? q.gaps + q.rejected + q.duplicates : 0);

const Chevron = ({ d }: { d: string }) => (
  <svg viewBox="0 0 16 16" width="16" height="16" aria-hidden="true">
    <path d={d} fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
  </svg>
);

/** Arrow keys move focus between the buttons of a list. */
function moveFocus(e: KeyboardEvent<HTMLElement>) {
  const step = { ArrowUp: -1, ArrowDown: 1 }[e.key];
  if (!step) return;
  const buttons = Array.from(e.currentTarget.querySelectorAll("button"));
  const next = buttons[buttons.indexOf(document.activeElement as HTMLButtonElement) + step];
  if (next) {
    e.preventDefault();
    next.focus();
  }
}

export default function App() {
  const [meters, setMeters] = useState<Schemas["MeterRow"][]>();
  const [ean, setEan] = useState("");
  const [date, setDate] = useState("2026-10-25");
  const [day, setDay] = useState<Schemas["MeterDay"]>();
  const [dayKey, setDayKey] = useState("");
  const [quality, setQuality] = useState<Schemas["MeterQuality"][]>();
  const [qualityDate, setQualityDate] = useState("");
  const [error, setError] = useState<string>();
  const [search, setSearch] = useState("");
  const [listOpen, setListOpen] = useState(false);

  useEffect(() => {
    load(api.GET("/meters")).then(
      (rows) => {
        setMeters(rows);
        setEan(rows[0]?.ean ?? "");
      },
      () => setError(failed),
    );
  }, []);

  // Previous data stays on screen (dimmed) until the new response arrives.
  useEffect(() => {
    if (!ean || !date) return;
    let current = true;
    load(api.GET("/meters/{ean}/days/{date}", { params: { path: { ean, date } } })).then(
      (d) => {
        if (!current) return;
        setDay(d);
        setDayKey(`${ean}/${date}`);
      },
      () => current && setError(failed),
    );
    return () => {
      current = false;
    };
  }, [ean, date]);

  useEffect(() => {
    if (!date) return;
    let current = true;
    load(api.GET("/quality/{date}", { params: { path: { date } } })).then(
      (q) => {
        if (!current) return;
        setQuality(q);
        setQualityDate(date);
      },
      () => current && setError(failed),
    );
    return () => {
      current = false;
    };
  }, [date]);

  const select = (next: string) => {
    setError(undefined);
    setEan(next);
    setListOpen(false);
  };
  const changeDate = (next: string) => {
    setError(undefined);
    setDate(next);
  };

  const byEan = new Map(quality?.map((q) => [q.ean, q]));
  const filtered = meters?.filter((m) => m.ean.includes(search.trim()));
  const bars = day && toBars(day);
  const empty = day && day.rejected.length === 0 && day.quarters.every((q) => q.readings === 0);
  const peak = bars?.reduce((best, b) => ((b.kwh ?? 0) > (best.kwh ?? 0) ? b : best), bars[0]);
  const dayStale = day !== undefined && dayKey !== `${ean}/${date}`;
  const qualityStale = quality !== undefined && qualityDate !== date;

  return (
    <div className="app">
      <aside className={listOpen ? "sidebar open" : "sidebar"}>
        <div className="brand">
          <span>MeterFlow</span>
          <button type="button" className="toggle" aria-expanded={listOpen} aria-controls="meter-panel" onClick={() => setListOpen(!listOpen)}>
            {listOpen ? "Hide meters" : "Meters"}
          </button>
        </div>
        <div id="meter-panel" className="panel">
          <div className="search">
            <label htmlFor="search" className="sr-only">Search meters</label>
            <input id="search" type="search" placeholder="Search" value={search} onChange={(e) => setSearch(e.target.value)} autoComplete="off" />
            {search && (
              <button type="button" className="clear" aria-label="Clear search" onClick={() => setSearch("")}>
                ×
              </button>
            )}
          </div>
          <ul className="meters" aria-label="Meters" onKeyDown={moveFocus}>
            {filtered?.map((m) => {
              const n = issues(byEan.get(m.ean));
              return (
                <li key={m.ean}>
                  <button type="button" aria-current={m.ean === ean} aria-label={n > 0 ? `${m.ean} ${n} issues` : undefined} onClick={() => select(m.ean)}>
                    <span className="ean">{m.ean}</span>
                    {n > 0 && <span className="badge">{n}</span>}
                  </button>
                </li>
              );
            })}
          </ul>
          {search && filtered?.length === 0 &&<p className="muted pad">No meters match.</p>}
        </div>
      </aside>

      <div className="content">
        <header className="topbar">
          <h1>{ean || "MeterFlow"}</h1>
          <div className="daynav">
            <button type="button" className="icon" aria-label="Previous day" onClick={() => changeDate(shiftDay(date, -1))} disabled={!date}>
              <Chevron d="M10 3 5 8l5 5" />
            </button>
            <label htmlFor="day" className="sr-only">Day</label>
            <input id="day" type="date" value={date} onChange={(e) => changeDate(e.target.value)} />
            <button type="button" className="icon" aria-label="Next day" onClick={() => changeDate(shiftDay(date, 1))} disabled={!date}>
              <Chevron d="M6 3l5 5-5 5" />
            </button>
          </div>
        </header>

        <main>
          {error && <p role="alert" className="error">{error}</p>}
          {meters?.length === 0 && <p className="card">No meters yet. Send readings with the simulator.</p>}

          {!day && !error && meters?.length !== 0 && (
            <div className="skeleton" aria-hidden="true">
              <div className="tiles">{[0, 1, 2, 3].map((i) => <div key={i} className="tile block" />)}</div>
              <div className="card block tall" />
            </div>
          )}

          {day && bars && (
            <div className={dayStale ? "stale" : undefined} aria-busy={dayStale}>
              <ul className="tiles fade" key={dayKey} aria-label="Day summary">
                <li className="tile"><span className="label">Total</span><span className="value">{bars.reduce((s, b) => s + (b.kwh ?? 0), 0).toFixed(2)} kWh</span></li>
                <li className="tile">
                  <span className="label">Peak quarter</span>
                  <span className="value">{peak && peak.kwh !== null ? `${clock.format(new Date(peak.start))} · ${peak.kwh} kWh` : "–"}</span>
                </li>
                <li className="tile"><span className="label">Quarters</span><span className="value">{bars.length}</span></li>
                <li className="tile"><span className="label">Issues</span><span className="value">{issues(byEan.get(dayKey.split("/")[0]))}</span></li>
              </ul>

              <section className="card">
                <h2>Consumption per quarter hour</h2>
                {empty ? <p className="muted">No readings for this meter on {dayKey.split("/")[1]}.</p> : <DayChart bars={bars} rejected={day.rejected.length} />}
              </section>
            </div>
          )}

          <section className={qualityStale ? "card stale" : "card"} aria-busy={qualityStale}>
            <h2>Data quality</h2>
            {!quality && !error && <div className="block" aria-hidden="true" />}
            {quality?.length === 0 && <p className="muted">No readings on {qualityDate}.</p>}
            {quality && quality.length > 0 && (
              <table className="fade" key={qualityDate}>
                <caption>Meters with most issues on {qualityDate}</caption>
                <thead>
                  <tr><th scope="col">EAN</th><th scope="col">Gaps</th><th scope="col">Rejected</th><th scope="col">Duplicates</th></tr>
                </thead>
                <tbody>
                  {quality.slice(0, 10).map((q) => (
                    // The row is a click target; the button inside gives keyboard access and bubbles its click here.
                    <tr key={q.ean} className={q.ean === ean ? "selected" : undefined} onClick={() => select(q.ean)}>
                      <td><button type="button" className="link">{q.ean}</button></td>
                      <td>{q.gaps}</td><td>{q.rejected}</td><td>{q.duplicates}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </section>
        </main>
      </div>
    </div>
  );
}
