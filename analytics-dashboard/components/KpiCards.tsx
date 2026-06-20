import type { AnalyticsSummary } from "@/lib/types";
import { formatCount, formatMs, formatPercent, successRate } from "@/lib/format";

export function KpiCards({ summary }: { summary: AnalyticsSummary }) {
  const rate = successRate(summary.statusCounts.success, summary.totalCalls);
  const kpis = [
    { label: "Total calls", value: formatCount(summary.totalCalls) },
    { label: "Success rate", value: formatPercent(rate) },
    { label: "Avg latency", value: formatMs(summary.latencyMs.avg) },
    { label: "p95 latency", value: formatMs(summary.latencyMs.p95) },
  ];

  return (
    <div className="kpi-row">
      {kpis.map((kpi) => (
        <div className="kpi" key={kpi.label}>
          <span className="kpi-value">{kpi.value}</span>
          <span className="kpi-label">{kpi.label}</span>
        </div>
      ))}
    </div>
  );
}
