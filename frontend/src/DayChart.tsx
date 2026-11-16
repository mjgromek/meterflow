import type { Bar } from "./day";

const time = new Intl.DateTimeFormat("en-GB", { timeZone: "Europe/Amsterdam", hour: "2-digit", minute: "2-digit", timeZoneName: "short" });
const clock = new Intl.DateTimeFormat("en-GB", { timeZone: "Europe/Amsterdam", hour: "2-digit", minute: "2-digit", hourCycle: "h23" });
const axisTimes = ["00:00", "06:00", "12:00", "18:00"];
const [barWidth, gap, height] = [6, 2, 160];

export function DayChart({ bars, rejected }: { bars: Bar[]; rejected: number }) {
  const max = Math.max(...bars.map((b) => b.kwh ?? 0), 0.001);
  const total = bars.reduce((sum, b) => sum + (b.kwh ?? 0), 0);
  const flagged = bars.filter((b) => b.flagged);
  const times = flagged.length > 0 && flagged.length <= 10 ? `: ${flagged.map((b) => time.format(new Date(b.start))).join(", ")}` : "";
  const summary =
    `${bars.length} quarters, ${total.toFixed(2)} kWh in total, ${rejected} rejected readings, ` +
    `${flagged.length} quarters with missing or rejected readings${times}.`;
  const width = bars.length * (barWidth + gap);

  return (
    <figure>
      <svg role="img" aria-labelledby="day-summary" viewBox={`0 0 ${width} ${height + 16}`} className="chart">
        {bars.map((b, i) => {
          // Flagged quarters stay visible even when nothing was measured.
          const h = Math.max(((b.kwh ?? 0) / max) * height, b.flagged ? 4 : 0);
          const label = new Date(b.start);
          const local = clock.format(label);
          return (
            <g key={b.start}>
              <rect className={b.rejected ? "bar flagged rejected" : b.flagged ? "bar flagged missing" : "bar"} x={i * (barWidth + gap)} y={height - h} width={barWidth} height={h} rx={1}>
                <title>{`${time.format(label)}: ${b.kwh ?? "no"} kWh, ${b.readings} readings`}</title>
              </rect>
              {axisTimes.includes(local) && (
                <text className="axis" x={i * (barWidth + gap)} y={height + 13}>
                  {local}
                </text>
              )}
            </g>
          );
        })}
        <line className="baseline" x1={0} x2={width} y1={height} y2={height} />
      </svg>
      <figcaption id="day-summary">{summary}</figcaption>
      <ul className="legend">
        <li><span className="swatch" /> Complete quarter (3 readings)</li>
        <li><span className="swatch missing" /> Missing readings</li>
        <li><span className="swatch rejected" /> Rejected reading</li>
      </ul>
    </figure>
  );
}
