// Shapes mirror the .NET analytics API (spec section 7). Kept camelCase to match
// the server's JSON responses.

export interface StatusCounts {
  success: number;
  error: number;
  canceled: number;
}

export interface LatencySummary {
  avg: number;
  p50: number;
  p95: number;
  max: number;
}

export interface AnalyticsSummary {
  from: string;
  to: string;
  totalCalls: number;
  statusCounts: StatusCounts;
  latencyMs: LatencySummary;
}

export interface ToolUsage {
  toolName: string;
  count: number;
  share: number;
  statusCounts: StatusCounts;
  latencyMs: LatencySummary;
}

export interface ToolsResponse {
  tools: ToolUsage[];
}

export interface TimeBucketPoint {
  bucketStart: string;
  count: number;
}

export interface AnalyticsTimeSeries {
  bucket: string;
  tool: string | null;
  points: TimeBucketPoint[];
}

export interface RecentCall {
  id: string;
  toolName: string;
  userName: string;
  startedAt: string;
  durationMs: number;
  status: string;
}

export interface RecentResponse {
  calls: RecentCall[];
}

export type Bucket = "hour" | "day";

export interface AnalyticsQuery {
  from: string; // ISO-8601 UTC
  to: string; // ISO-8601 UTC
  bucket: Bucket;
}
