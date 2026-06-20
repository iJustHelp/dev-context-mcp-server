import type {
  AnalyticsQuery,
  AnalyticsSummary,
  AnalyticsTimeSeries,
  RecentResponse,
  ToolsResponse,
} from "./types";

async function getJson<T>(
  endpoint: string,
  params: Record<string, string>,
): Promise<T> {
  const query = new URLSearchParams(params).toString();
  const response = await fetch(`/api/analytics/${endpoint}?${query}`, {
    cache: "no-store",
  });
  if (!response.ok) {
    const detail = await response.text();
    throw new Error(`Request to ${endpoint} failed (${response.status}): ${detail}`);
  }
  return (await response.json()) as T;
}

export function fetchSummary(query: AnalyticsQuery): Promise<AnalyticsSummary> {
  return getJson<AnalyticsSummary>("summary", { from: query.from, to: query.to });
}

export function fetchTools(query: AnalyticsQuery): Promise<ToolsResponse> {
  return getJson<ToolsResponse>("tools", { from: query.from, to: query.to });
}

export function fetchTimeSeries(
  query: AnalyticsQuery,
  tool?: string,
): Promise<AnalyticsTimeSeries> {
  const params: Record<string, string> = {
    from: query.from,
    to: query.to,
    bucket: query.bucket,
  };
  if (tool) {
    params.tool = tool;
  }
  return getJson<AnalyticsTimeSeries>("timeseries", params);
}

export function fetchRecent(
  query: AnalyticsQuery,
  limit = 50,
): Promise<RecentResponse> {
  return getJson<RecentResponse>("recent", {
    from: query.from,
    to: query.to,
    limit: String(limit),
  });
}
