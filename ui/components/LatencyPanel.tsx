import type { LatencySummary } from "@/lib/types";
import { formatMs } from "@/lib/format";

export function LatencyPanel({ latency }: { latency: LatencySummary }) {
  const stats = [
    { label: "Average", value: latency.avg },
    { label: "p50", value: latency.p50 },
    { label: "p95", value: latency.p95 },
    { label: "Max", value: latency.max },
  ];

  return (
    <div className="latency-grid">
      {stats.map((stat) => (
        <div className="latency-cell" key={stat.label}>
          <span className="latency-value">{formatMs(stat.value)}</span>
          <span className="latency-label">{stat.label}</span>
        </div>
      ))}
    </div>
  );
}
