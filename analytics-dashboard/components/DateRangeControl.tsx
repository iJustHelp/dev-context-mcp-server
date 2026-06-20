"use client";

import { useState } from "react";
import type { AnalyticsQuery, Bucket } from "@/lib/types";
import { localInputToIso, toLocalInputValue } from "@/lib/format";

export function DateRangeControl({
  query,
  onApply,
}: {
  query: AnalyticsQuery;
  onApply: (next: AnalyticsQuery) => void;
}) {
  const [from, setFrom] = useState(() => toLocalInputValue(new Date(query.from)));
  const [to, setTo] = useState(() => toLocalInputValue(new Date(query.to)));
  const [bucket, setBucket] = useState<Bucket>(query.bucket);

  function apply() {
    onApply({
      from: localInputToIso(from),
      to: localInputToIso(to),
      bucket,
    });
  }

  return (
    <div className="controls">
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
      <label>
        <span>Bucket</span>
        <select
          value={bucket}
          onChange={(event) => setBucket(event.target.value as Bucket)}
        >
          <option value="hour">Hourly</option>
          <option value="day">Daily</option>
        </select>
      </label>
      <button type="button" onClick={apply}>
        Apply
      </button>
    </div>
  );
}
