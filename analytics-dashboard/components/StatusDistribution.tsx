"use client";

import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts";
import type { StatusCounts } from "@/lib/types";
import { statusColor } from "@/lib/colors";

export function StatusDistribution({ counts }: { counts: StatusCounts }) {
  const data = [
    { name: "success", value: counts.success },
    { name: "error", value: counts.error },
    { name: "canceled", value: counts.canceled },
  ].filter((slice) => slice.value > 0);

  if (data.length === 0) {
    return <p className="empty">No calls in this range.</p>;
  }

  return (
    <ResponsiveContainer width="100%" height={220}>
      <PieChart>
        <Pie
          data={data}
          dataKey="value"
          nameKey="name"
          innerRadius={50}
          outerRadius={80}
          paddingAngle={2}
        >
          {data.map((slice) => (
            <Cell key={slice.name} fill={statusColor(slice.name)} />
          ))}
        </Pie>
        <Tooltip />
      </PieChart>
    </ResponsiveContainer>
  );
}
