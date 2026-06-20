import type { Bucket } from "./types";

export function formatMs(value: number): string {
  if (value >= 1000) {
    return `${(value / 1000).toFixed(2)} s`;
  }
  return `${value.toFixed(1)} ms`;
}

export function formatPercent(value: number): string {
  return `${(value * 100).toFixed(1)}%`;
}

export function formatCount(value: number): string {
  return value.toLocaleString();
}

export function successRate(success: number, total: number): number {
  return total === 0 ? 0 : success / total;
}

// Formats an ISO timestamp for a time-series axis according to the bucket size.
export function formatBucket(iso: string, bucket: Bucket): string {
  const date = new Date(iso);
  if (bucket === "day") {
    return date.toLocaleDateString([], { month: "short", day: "numeric" });
  }
  return date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

export function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString([], {
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

// Converts a Date to the value format expected by <input type="datetime-local">,
// expressed in the browser's local time zone.
export function toLocalInputValue(date: Date): string {
  const offsetMs = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16);
}

// Interprets a datetime-local value as local time and returns an ISO-8601 UTC string.
export function localInputToIso(value: string): string {
  return new Date(value).toISOString();
}
