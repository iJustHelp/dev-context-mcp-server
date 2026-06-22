"use client";

import { useState } from "react";
import type { AnalyticsQuery, Bucket } from "@/lib/types";
import { localInputToIso, toLocalInputValue } from "@/lib/format";

type RangeMode = "time" | "days" | "hours";

const DAY_OFFSETS = Array.from({ length: 14 }, (_, index) => -(index + 1));
const HOUR_OFFSETS = Array.from({ length: 12 }, (_, index) => -(index + 1));

export function DateRangeControl({
  query,
  onApply,
}: {
  query: AnalyticsQuery;
  onApply: (next: AnalyticsQuery) => void;
}) {
  const [from, setFrom] = useState(() => toLocalInputValue(new Date(query.from)));
  const [to, setTo] = useState(() => toLocalInputValue(new Date(query.to)));
  const [mode, setMode] = useState<RangeMode>("hours");
  const [dayOffset, setDayOffset] = useState(-1);
  const [hourOffset, setHourOffset] = useState(-12);

  function apply() {
    if (mode === "time") {
      onApply({
        from: localInputToIso(from),
        to: localInputToIso(to),
        bucket: "hour",
      });
      return;
    }

    const now = new Date();
    const amount = mode === "days" ? dayOffset : hourOffset;
    const unitMs = mode === "days" ? 24 * 60 * 60 * 1000 : 60 * 60 * 1000;
    const bucket: Bucket = mode === "days" ? "day" : "hour";

    onApply({
      from: new Date(now.getTime() + amount * unitMs).toISOString(),
      to: now.toISOString(),
      bucket,
    });
  }

  return (
    <div className="controls">      
      {mode === "time" && (
        <>
          <label>
            <span>From</span>
            <input
              type="datetime-local"
              value={from}
              onChange={(event) => setFrom(event.target.value)}
            />
          </label>
          <label>
            <span>To</span>
            <input
              type="datetime-local"
              value={to}
              onChange={(event) => setTo(event.target.value)}
            />
          </label>
        </>
      )}
      {mode === "days" && (
        <label>        
          <select
            value={dayOffset}
            onChange={(event) => setDayOffset(Number(event.target.value))}
          >
            {DAY_OFFSETS.map((offset) => (
              <option key={offset} value={offset}>
                {offset}
              </option>
            ))}
          </select>
        </label>
      )}
      {mode === "hours" && (
        <label>
          <select
            value={hourOffset}
            onChange={(event) => setHourOffset(Number(event.target.value))}
          >
            {HOUR_OFFSETS.map((offset) => (
              <option key={offset} value={offset}>
                {offset}
              </option>
            ))}
          </select>
        </label>
      )}
      <label>
        <select
          value={mode}
          onChange={(event) => setMode(event.target.value as RangeMode)}
        >
          <option value="time">Time</option>
          <option value="days">Days</option>
          <option value="hours">Hours</option>
        </select>
      </label>
      <button type="button" onClick={apply}>
        Apply
      </button>
    </div>
  );
}
