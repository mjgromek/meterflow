<!-- Cap: 120 lines. Over cap: move settled facts into DECISIONS.md and delete them here. State describes now. -->

# STATE

Pipeline: easydev, upstream 9bfa13b. History was rewritten on 2026-11-16 (squash and author
email), so pre-2026-11-16 hashes from conversations no longer resolve.

## Current slice

None. M12 complete (16 frontend tests; checked by the orchestrator in Chrome with 200 meters:
sidebar search, badges, tiles, tooltip, quality-row selection; width and tile wrap fixed).

## Acceptance criteria (M12)

- Layout: sidebar with a search field filtering the meter list (200+ meters, scrolls on its
  own); each row shows the EAN and an issue badge (gaps+rejected+duplicates for the chosen
  day from /quality). On narrow screens the list collapses above the content.
- Header: selected meter, date with previous/next-day buttons and a date input; a
  translucent sticky header (backdrop blur) in light and dark mode.
- Summary tiles: total kWh, peak quarter (local time and kWh), quarters in the day
  (92/96/100), issues.
- Chart: bars grow from the baseline with a CSS transform transition (~300 ms ease-out) when
  data changes; hover or keyboard focus on a bar shows a tooltip with local time, kWh,
  reading count and its flags. Existing colours, legend and text summary stay.
- Quality card: top 10 as now; clicking or pressing Enter on a row selects that meter.
- While refetching, the previous data stays visible (dimmed); first load shows a skeleton.
  Content changes fade in. `prefers-reduced-motion: reduce` disables all transitions.
- Style: system font stack (-apple-system, BlinkMacSystemFont, "SF Pro Text", system-ui),
  rounded cards, soft shadows, generous spacing, visible focus rings. Plain CSS, no new
  runtime dependencies.
- Tests: existing behaviour kept; new tests for search filtering, previous/next day,
  clicking a quality row, the tooltip content, and that the badge counts come from /quality.

## Shipped in M3

- Duplicate and gap detection (`ReadingChecks`).
- `ReadingCsv.Parse` with line-numbered rejections, including zero kWh accepted.
- `QuarterHours.Aggregate` and `ForLocalDay` (92/96/100), CSV round trip for two meters.
- docs/time-zones.md, every number re-measured by the checker.

## In flight

Nothing.

## Blocked

Nothing.

## Last verification result

2026-11-16: dotnet test 56/56, build 0 warnings, on m3 before the squash. Checker PASS on all
five slices.
