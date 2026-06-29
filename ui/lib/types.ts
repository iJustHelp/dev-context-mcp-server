// API contract types, sourced from the generated OpenAPI schema so the rest of the app
// keeps importing stable names from "@/lib/types". Regenerate with `npm run gen:api`.
import type { components } from "./generated/schema";

type Schemas = components["schemas"];

export type AnalyticsSummary = Schemas["AnalyticsSummary"];
export type AnalyticsTimeSeries = Schemas["AnalyticsTimeSeries"];
export type LatencySummary = Schemas["LatencySummary"];
export type RecentCall = Schemas["RecentCall"];
export type RecentCallDetail = Schemas["RecentCallDetail"];
export type ToolInvocationPayloadDetail = Schemas["ToolInvocationPayloadDetail"];
export type TimeBucketPoint = Schemas["TimeBucketPoint"];
export type ToolResultBreakdownItem = Schemas["ToolResultBreakdownItem"];
export type ToolUsage = Schemas["ToolUsage"];
export type UserBreakdownItem = Schemas["UserBreakdownItem"];
export type IndexedContextResponse = Schemas["IndexedContextResponse"];
export type IndexedContextTotals = Schemas["IndexedContextTotals"];
export type IndexedNuGetInventoryItem = Schemas["IndexedNuGetInventoryItem"];

// The server schema names the status counters StatusBreakdown; the dashboard refers to
// the same shape as StatusCounts.
export type StatusCounts = Schemas["StatusBreakdown"];

// Response envelopes.
export type ToolsResponse = Schemas["ToolBreakdownResponse"];
export type ToolResultsResponse = Schemas["ToolResultBreakdownResponse"];
export type UsersResponse = Schemas["UserBreakdownResponse"];
export type RecentResponse = Schemas["RecentCallsResponse"];

// Request-shaping helpers (query inputs, not response schemas).
export type Bucket = "hour" | "day";

export interface AnalyticsQuery {
  from: string;
  to: string;
  bucket: Bucket;
}
