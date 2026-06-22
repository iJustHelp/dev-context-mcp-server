"use client";

import {
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
} from "recharts";
import type {
  ToolResultBreakdownItem,
  UserBreakdownItem,
} from "@/lib/types";
import { ACCENT, statusColor } from "@/lib/colors";

const USER_COLORS = [
  ACCENT,
  "#16a34a",
  "#d97706",
  "#9333ea",
  "#0891b2",
  "#dc2626",
  "#4f46e5",
  "#65a30d",
];

export function UserBreakdownChart({
  users,
}: {
  users: UserBreakdownItem[];
}) {
  const data = users.slice(0, 8).map((user) => ({
    name: user.userName,
    count: user.count,
    color: userColor(user.userName),
  }));

  return <BreakdownChart data={data} />;
}

export function ToolResultBreakdownChart({
  results,
}: {
  results: ToolResultBreakdownItem[];
}) {
  const data = results.map((result) => ({
    name: result.toolResultStatus,
    count: result.count,
    color: statusColor(result.toolResultStatus),
  }));

  return <BreakdownChart data={data} />;
}

function BreakdownChart({
  data,
}: {
  data: Array<{ name: string; count: number; color?: string }>;
}) {
  if (data.length === 0) {
    return <p className="empty">No calls in this range.</p>;
  }

  return (
    <ResponsiveContainer width="100%" height={240}>
      <PieChart>
        <Pie
          data={data}
          dataKey="count"
          nameKey="name"
          innerRadius={50}
          outerRadius={82}
          paddingAngle={2}
        >
          {data.map((item) => (
            <Cell key={item.name} fill={item.color ?? ACCENT} />
          ))}
        </Pie>
        <Tooltip />
      </PieChart>
    </ResponsiveContainer>
  );
}

function userColor(userName: string): string {
  let hash = 0;
  for (const character of userName) {
    hash = (hash * 31 + character.charCodeAt(0)) >>> 0;
  }

  return USER_COLORS[hash % USER_COLORS.length];
}
