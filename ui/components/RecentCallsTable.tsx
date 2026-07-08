"use client";

import { useCallback, useMemo, useState } from "react";
import { RecentCallDetailPanel } from "@/components/RecentCallDetailPanel";
import type { AnalyticsQuery, RecentCall, RecentCallDetail } from "@/lib/types";
import { statusColor } from "@/lib/colors";
import { formatCount, formatDateTime, formatMs } from "@/lib/format";

const RECENT_CALL_LIMITS = [10, 25, 50, 100, 500];

type Direction = "asc" | "desc";
type RecentCallSortKey =
  | "startedAt"
  | "toolName"
  | "userName"
  | "durationMs"
  | "toolResultStatus";

interface SortState {
  key: RecentCallSortKey;
  direction: Direction;
}

export function RecentCallsTable({
  calls,
  totalCalls,
  limit,
  query,
  onLimitChange,
}: {
  calls: RecentCall[];
  totalCalls: number;
  limit: number;
  query: AnalyticsQuery;
  onLimitChange: (limit: number) => void;
}) {
  const [sort, setSort] = useState<SortState>({
    key: "startedAt",
    direction: "desc",
  });
  const [selectedCall, setSelectedCall] = useState<RecentCall>();
  const [detail, setDetail] = useState<RecentCallDetail>();
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState<string>();

  const rows = useMemo(
    () =>
      [...calls].sort((left, right) =>
        compareValues(sortValue(left, sort.key), sortValue(right, sort.key), sort.direction),
      ),
    [calls, sort],
  );

  const closeDetail = useCallback(() => {
    setSelectedCall(undefined);
    setDetail(undefined);
    setDetailError(undefined);
    setDetailLoading(false);
  }, []);

  const openDetail = useCallback(
    async (call: RecentCall) => {
      if (!call.hasDetail) {
        return;
      }

      setSelectedCall(call);
      setDetail(undefined);
      setDetailError(undefined);
      setDetailLoading(true);

      try {
        const search = new URLSearchParams({
          from: query.from,
          to: query.to,
        });
        const response = await fetch(
          `/api/analytics/recent/${encodeURIComponent(call.id)}?${search.toString()}`,
          { cache: "no-store" },
        );
        if (!response.ok) {
          throw new Error(
            response.status === 404
              ? "Call detail was not found for the selected time range."
              : `Failed to load call detail (${response.status}).`,
          );
        }

        setDetail((await response.json()) as RecentCallDetail);
      } catch (caught) {
        setDetailError(caught instanceof Error ? caught.message : String(caught));
      } finally {
        setDetailLoading(false);
      }
    },
    [query.from, query.to],
  );

  return (
    <div>
      <div className="table-toolbar">
        <span className="table-toolbar-label">
          Total calls: {formatCount(totalCalls)}
        </span>
        <label className="table-toolbar-control">
          <span>Show</span>
          <select
            value={limit}
            onChange={(event) => onLimitChange(Number(event.target.value))}
          >
            {RECENT_CALL_LIMITS.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>
      </div>
      {calls.length === 0 ? (
        <p className="empty">No calls in this range.</p>
      ) : (
        <div className="table-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <SortableHeader label="Time" sortKey="startedAt" sort={sort} onSort={setSortKey} />
                <SortableHeader label="Tool" sortKey="toolName" sort={sort} onSort={setSortKey} />
                <SortableHeader label="User" sortKey="userName" sort={sort} onSort={setSortKey} />
                <SortableHeader
                  label="Duration"
                  sortKey="durationMs"
                  sort={sort}
                  onSort={setSortKey}
                  align="right"
                />
                <SortableHeader
                  label="Tool result"
                  sortKey="toolResultStatus"
                  sort={sort}
                  onSort={setSortKey}
                />
              </tr>
            </thead>
            <tbody>
              {rows.map((call) => (
                <tr
                  key={call.id}
                  className={call.hasDetail ? "data-table-row-clickable" : undefined}
                  tabIndex={call.hasDetail ? 0 : undefined}
                  onClick={() => void openDetail(call)}
                  onKeyDown={(event) => {
                    if (call.hasDetail && (event.key === "Enter" || event.key === " ")) {
                      event.preventDefault();
                      void openDetail(call);
                    }
                  }}
                >
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
        </div>
      )}

      {selectedCall && (
        <RecentCallDetailPanel
          detail={detail}
          loading={detailLoading}
          error={detailError}
          onClose={closeDetail}
        />
      )}
    </div>
  );

  function setSortKey(key: RecentCallSortKey) {
    setSort((current) =>
      current.key === key
        ? { key, direction: current.direction === "asc" ? "desc" : "asc" }
        : { key, direction: key === "startedAt" || key === "durationMs" ? "desc" : "asc" },
    );
  }
}

function SortableHeader({
  label,
  sortKey,
  sort,
  onSort,
  align,
}: {
  label: string;
  sortKey: RecentCallSortKey;
  sort: SortState;
  onSort: (key: RecentCallSortKey) => void;
  align?: "right";
}) {
  const active = sort.key === sortKey;
  return (
    <th className={align === "right" ? "num" : undefined}>
      <button
        className={align === "right" ? "sort-button sort-button-right" : "sort-button"}
        type="button"
        onClick={() => onSort(sortKey)}
      >
        <span>{label}</span>
        <span className="sort-marker">
          {active ? (sort.direction === "asc" ? "^" : "v") : ""}
        </span>
      </button>
    </th>
  );
}

function sortValue(call: RecentCall, key: RecentCallSortKey): string | number {
  if (key === "startedAt") {
    return new Date(call.startedAt).getTime();
  }

  return call[key];
}

function compareValues(
  left: string | number,
  right: string | number,
  direction: Direction,
): number {
  const result =
    typeof left === "number" && typeof right === "number"
      ? left - right
      : String(left).localeCompare(String(right));
  return direction === "asc" ? result : -result;
}
