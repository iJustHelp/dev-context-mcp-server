import type { RecentCall } from "@/lib/types";
import { statusColor } from "@/lib/colors";
import { formatDateTime, formatMs } from "@/lib/format";

export function RecentCallsTable({ calls }: { calls: RecentCall[] }) {
  if (calls.length === 0) {
    return <p className="empty">No calls in this range.</p>;
  }

  return (
    <table className="data-table">
      <thead>
        <tr>
          <th>Time</th>
          <th>Tool</th>
          <th>User</th>
          <th className="num">Duration</th>
          <th>Tool result</th>
        </tr>
      </thead>
      <tbody>
        {calls.map((call) => (
          <tr key={call.id}>
            <td>{formatDateTime(call.startedAt)}</td>
            <td>{call.toolName}</td>
            <td>{call.userName}</td>
            <td className="num">{formatMs(call.durationMs)}</td>
            <td>
              <span
                className="status-pill"
                style={{ backgroundColor: statusColor(call.toolResultStatus) }}
              >
                {call.toolResultStatus}
              </span>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
