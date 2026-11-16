import { useRef, useState, type CSSProperties, type KeyboardEvent } from "react";
import { clock, time, type Bar } from "./day";

const axisTimes = ["00:00", "06:00", "12:00", "18:00"];
const [barWidth, gap, height] = [6, 2, 160];

const describe = (b: Bar) =>
  [time.format(new Date(b.start)), `${b.kwh ?? "no"} kWh`, `${b.readings} readings`, b.rejected ? "rejected reading" : b.flagged ? "missing readings" : ""]
    .filter(Boolean)
    .join(" · ");

export function DayChart({ bars, rejected }: { bars: Bar[]; rejected: number }) {
  const [active, setActive] = useState<number>();
  const svg = useRef<SVGSVGElement>(null);
  const max = Math.max(...bars.map((b) => b.kwh ?? 0), 0.001);
  const total = bars.reduce((sum, b) => sum + (b.kwh ?? 0), 0);
  const flagged = bars.filter((b) => b.flagged);
  const times = flagged.length > 0 && flagged.length <= 10 ? `: ${flagged.map((b) => time.format(new Date(b.start))).join(", ")}` : "";
  const summary =
    `${bars.length} quarters, ${total.toFixed(2)} kWh in total, ${rejected} rejected readings, ` +
    `${flagged.length} quarters with missing or rejected readings${times}.`;
  const width = bars.length * (barWidth + gap);
  const tip = active !== undefined && active < bars.length ? active : undefined;

  // Roving tabindex: one tab stop for the chart, arrow keys move between bars.
  const onKeyDown = (e: KeyboardEvent) => {
    const step = { ArrowLeft: -1, ArrowRight: 1 }[e.key];
    if (!step || tip === undefined) return;
    e.preventDefault();
    svg.current?.querySelectorAll<SVGElement>("[data-bar]")[Math.min(Math.max(tip + step, 0), bars.length - 1)]?.focus();
  };

  return (
    <figure>
      <div className="plot">
        <svg ref={svg} role="group" aria-labelledby="day-summary" viewBox={`0 0 ${width} ${height + 16}`} className="chart" onKeyDown={onKeyDown}>
          {bars.map((b, i) => {
            // Flagged quarters stay visible even when nothing was measured.
            const h = Math.max(((b.kwh ?? 0) / max) * height, b.flagged ? 4 : 0);
            const x = i * (barWidth + gap);
            const local = clock.format(new Date(b.start));
            return (
              <g key={b.start}>
                <rect className={b.rejected ? "bar flagged rejected" : b.flagged ? "bar flagged missing" : "bar"} x={x} y={0} width={barWidth} height={height} style={{ "--scale": h / height } as CSSProperties} />
                <rect
                  data-bar
                  className="hit"
                  x={x - gap / 2}
                  y={0}
                  width={barWidth + gap}
                  height={height}
                  tabIndex={i === (tip ?? 0) ? 0 : -1}
                  aria-label={describe(b)}
                  onMouseEnter={() => setActive(i)}
                  onMouseLeave={() => setActive(undefined)}
                  onFocus={() => setActive(i)}
                  onBlur={() => setActive(undefined)}
                />
                {axisTimes.includes(local) && (
                  <text className="axis" x={x} y={height + 13}>
                    {local}
                  </text>
                )}
              </g>
            );
          })}
          <line className="baseline" x1={0} x2={width} y1={height} y2={height} />
        </svg>
        {tip !== undefined && (
          <div role="tooltip" className="tooltip" style={{ left: `${Math.min(Math.max(((tip * (barWidth + gap) + barWidth / 2) / width) * 100, 12), 88)}%` }}>
            {describe(bars[tip])}
          </div>
        )}
      </div>
      <figcaption id="day-summary" aria-live="polite">{summary}</figcaption>
      <ul className="legend">
        <li><span className="swatch" /> Complete quarter (3 readings)</li>
        <li><span className="swatch missing" /> Missing readings</li>
        <li><span className="swatch rejected" /> Rejected reading</li>
      </ul>
    </figure>
  );
}
