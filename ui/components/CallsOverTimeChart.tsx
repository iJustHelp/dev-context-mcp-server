"use client";

import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { AnalyticsTimeSeries } from "@/lib/types";
import { ACCENT } from "@/lib/colors";
import { formatBucket } from "@/lib/format";

export function CallsOverTimeChart({ series }: { series: AnalyticsTimeSeries }) {
  const bucket = series.bucket === "day" ? "day" : "hour";
  const data = series.points.map((point) => ({
    label: formatBucket(point.bucketStart, bucket),
    count: point.count,
  }));

  if (data.length === 0) {
    return <p className="empty">No calls in this range.</p>;
  }

  return (
    <ResponsiveContainer width="100%" height={260}>
      <AreaChart data={data} margin={{ top: 8, right: 16, bottom: 0, left: 0 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
        <XAxis dataKey="label" tick={{ fontSize: 12 }} />
        <YAxis allowDecimals={false} tick={{ fontSize: 12 }} width={40} />
        <Tooltip />
        <Area
          type="monotone"
          dataKey="count"
          name="Calls"
          stroke={ACCENT}
          fill={ACCENT}
          fillOpacity={0.15}
          strokeWidth={2}
        />
      </AreaChart>
    </ResponsiveContainer>
  );
}
