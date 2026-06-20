import type { ToolUsage } from "@/lib/types";
import { formatCount, formatMs, formatPercent } from "@/lib/format";

export function ToolBreakdownTable({ tools }: { tools: ToolUsage[] }) {
  if (tools.length === 0) {
    return <p className="empty">No calls in this range.</p>;
  }

  return (
    <table className="data-table">
      <thead>
        <tr>
          <th>Tool</th>
          <th className="num">Calls</th>
          <th className="num">Share</th>
          <th className="num">Success</th>
          <th className="num">Errors</th>
          <th className="num">Avg</th>
          <th className="num">p95</th>
        </tr>
      </thead>
      <tbody>
        {tools.map((tool) => (
          <tr key={tool.toolName}>
            <td>{tool.toolName}</td>
            <td className="num">{formatCount(tool.count)}</td>
            <td className="num">{formatPercent(tool.share)}</td>
            <td className="num">{formatCount(tool.statusCounts.success)}</td>
            <td className="num">{formatCount(tool.statusCounts.error)}</td>
            <td className="num">{formatMs(tool.latencyMs.avg)}</td>
            <td className="num">{formatMs(tool.latencyMs.p95)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
