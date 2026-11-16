import { useEffect, useState } from "react";
import { api } from "./api/client";
import type { components } from "./api/schema";
import { DayChart } from "./DayChart";
import { toBars } from "./day";

type Schemas = components["schemas"];

const failed = "Could not load data from the API.";

async function load<T>(call: Promise<{ data?: T; response: Response }>): Promise<T> {
  const { data, response } = await call;
  if (!response.ok || data === undefined) throw new Error(`HTTP ${response.status}`);
  return data;
}

export default function App() {
  const [meters, setMeters] = useState<Schemas["MeterRow"][]>();
  const [ean, setEan] = useState("");
  const [date, setDate] = useState("2026-10-25");
  const [day, setDay] = useState<Schemas["MeterDay"]>();
  const [quality, setQuality] = useState<Schemas["MeterQuality"][]>();
  const [error, setError] = useState<string>();

  useEffect(() => {
    load(api.GET("/meters")).then(
      (rows) => {
        setMeters(rows);
        setEan(rows[0]?.ean ?? "");
      },
      () => setError(failed),
    );
  }, []);

  useEffect(() => {
    if (!ean || !date) return;
    let current = true;
    setDay(undefined);
    load(api.GET("/meters/{ean}/days/{date}", { params: { path: { ean, date } } })).then(
      (d) => current && setDay(d),
      () => current && setError(failed),
    );
    return () => {
      current = false;
    };
  }, [ean, date]);

  useEffect(() => {
    if (!date) return;
    let current = true;
    setQuality(undefined);
    load(api.GET("/quality/{date}", { params: { path: { date } } })).then(
      (q) => current && setQuality(q),
      () => current && setError(failed),
    );
    return () => {
      current = false;
    };
  }, [date]);

  const empty = day && day.rejected.length === 0 && day.quarters.every((q) => q.readings === 0);

  return (
    <main>
      <h1>MeterFlow</h1>
      <div className="filters">
        <label htmlFor="meter">Meter</label>
        <select id="meter" value={ean} onChange={(e) => { setError(undefined); setEan(e.target.value); }}>
          {meters?.map((m) => (
            <option key={m.ean} value={m.ean}>{m.ean}</option>
          ))}
        </select>
        <label htmlFor="day">Day</label>
        <input id="day" type="date" value={date} onChange={(e) => { setError(undefined); setDate(e.target.value); }} />
      </div>
      {error && <p role="alert" className="error">{error}</p>}
      {meters?.length === 0 && <p>No meters yet. Send readings with the simulator.</p>}

      <section>
        <h2>Consumption per quarter hour</h2>
        {empty ? <p>No readings for this meter on {date}.</p> : day && <DayChart bars={toBars(day)} rejected={day.rejected.length} />}
      </section>

      <section>
        <h2>Data quality</h2>
        {quality?.length === 0 && <p>No readings on {date}.</p>}
        {quality && quality.length > 0 && (
          <table>
            <caption>Meters with most issues on {date}</caption>
            <thead>
              <tr><th scope="col">EAN</th><th scope="col">Gaps</th><th scope="col">Rejected</th><th scope="col">Duplicates</th></tr>
            </thead>
            <tbody>
              {quality.slice(0, 10).map((q) => (
                <tr key={q.ean}><td>{q.ean}</td><td>{q.gaps}</td><td>{q.rejected}</td><td>{q.duplicates}</td></tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </main>
  );
}
