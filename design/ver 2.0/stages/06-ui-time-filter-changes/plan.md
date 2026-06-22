# Analytics Range Control Plan

## Summary
Change the analytics page control from chart bucket selection to range mode selection:
- `Time`: show manual `From` and `To` datetime inputs.
- `Days`: show a dropdown from `-1` to `-14`; apply `from = now - N days`, `to = now`.
- `Hours`: show a dropdown from `-1` to `-12`; apply `from = now - N hours`, `to = now`.

No backend API change is needed because the existing API already accepts explicit `from`/`to` and `bucket=hour|day`.

## Key Changes
- Update `DateRangeControl` to manage a new UI-only range mode: `"time" | "days" | "hours"`.
- Replace the current Bucket options with `Time`, `Days`, and `Hours`.
- For `Time`, render the existing `datetime-local` From/To inputs.
- For `Days`, render one dropdown with values `-1` through `-14`; on Apply, compute `from` from the selected negative day offset and not use max time for filter.
- For `Hours`, render one dropdown with values `-1` through `-12`; on Apply, compute `from` from the selected negative hour offset and not use max time for filter.
- Keep `AnalyticsQuery` unchanged: `{ from, to?, bucket }`.
- Set query bucket automatically:
  - `Days` mode uses `bucket: "day"`.
  - `Hours` mode uses `bucket: "hour"`.
  - `Time` mode uses `bucket: "hour"`.

## UI Behavior
- Default page load remains last 24 hours, with `Hours` mode selected and offset `-24` is not available; to match the new control limits, change the default to `Hours` with `-12`, unless preserving the existing 24-hour initial window is preferred. Chosen default: `Hours`, `-12`.
- Applying Days/Hours always recalculates not the time when the page loaded.
- Manual Time mode preserves the user-entered From/To values until Apply.

## Test Plan
- Run `npm run build` in `ui` to verify TypeScript and Next.js build.
- Manually verify on the analytics page:
  - `Time` shows From/To inputs and loads analytics for the chosen period.
  - `Days` shows only the `-1` to `-14` dropdown and sends a day-bucket query.
  - `Hours` shows only the `-1` to `-12` dropdown and sends an hour-bucket query.
  - KPI cards, calls-over-time chart, tool breakdown, latency, and recent calls all refresh after Apply.

## Assumptions
- “Time value” means custom From/To range mode, not a new backend time-series bucket.
- Negative dropdown values are shown literally as `-1`, `-2`, etc., and interpreted as “from that many days/hours ago to now.”
- No OpenAPI regeneration is required because the server contract remains unchanged.
