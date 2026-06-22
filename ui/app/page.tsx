"use client";

import { useCallback, useEffect, useState } from "react";
import { Card } from "@/components/Card";
import {
  ToolResultBreakdownChart,
  UserBreakdownChart,
} from "@/components/BreakdownBarChart";
import { CallsOverTimeChart } from "@/components/CallsOverTimeChart";
import { DateRangeControl } from "@/components/DateRangeControl";
import { KpiCards } from "@/components/KpiCards";
import { RecentCallsTable } from "@/components/RecentCallsTable";
import { StatusDistribution } from "@/components/StatusDistribution";
import { ToolBreakdownTable } from "@/components/ToolBreakdownTable";
import { apiClient } from "@/lib/client";
import type {
  AnalyticsQuery,
  AnalyticsSummary,
  AnalyticsTimeSeries,
  RecentResponse,
  ToolResultsResponse,
  ToolsResponse,
  UsersResponse,
} from "@/lib/types";

function defaultQuery(): AnalyticsQuery {
  const now = new Date();
  const from = new Date(now.getTime() - 12 * 60 * 60 * 1000);
  return { from: from.toISOString(), to: now.toISOString(), bucket: "hour" };
}

export default function Page() {
  const [query, setQuery] = useState<AnalyticsQuery | null>(null);
  const [summary, setSummary] = useState<AnalyticsSummary>();
  const [tools, setTools] = useState<ToolsResponse>();
  const [users, setUsers] = useState<UsersResponse>();
  const [toolResults, setToolResults] = useState<ToolResultsResponse>();
  const [series, setSeries] = useState<AnalyticsTimeSeries>();
  const [recent, setRecent] = useState<RecentResponse>();
  const [error, setError] = useState<string>();
  const [loading, setLoading] = useState(false);
  const [showLoading, setShowLoading] = useState(false);

  // Set the initial range on the client to avoid SSR/CSR time mismatches.
  useEffect(() => {
    setQuery(defaultQuery());
  }, []);

  const load = useCallback(async (current: AnalyticsQuery) => {
    setLoading(true);
    setError(undefined);
    try {
      const window = { from: current.from, to: current.to };
      const [
        summaryResult,
        toolsResult,
        usersResult,
        toolResultsResult,
        seriesResult,
        recentResult,
      ] =
        await Promise.all([
          apiClient.GET("/api/analytics/summary", {
            params: { query: window },
          }),
          apiClient.GET("/api/analytics/tools", {
            params: { query: window },
          }),
          apiClient.GET("/api/analytics/users", {
            params: { query: window },
          }),
          apiClient.GET("/api/analytics/tool-results", {
            params: { query: window },
          }),
          apiClient.GET("/api/analytics/timeseries", {
            params: { query: { ...window, bucket: current.bucket } },
          }),
          apiClient.GET("/api/analytics/recent", {
            params: { query: { ...window, limit: 50 } },
          }),
        ]);

      const failed = [
        summaryResult,
        toolsResult,
        usersResult,
        toolResultsResult,
        seriesResult,
        recentResult,
      ].find((result) => result.error !== undefined);
      if (failed) {
        throw new Error(
          failed.error?.error ??
            `Analytics request failed (${failed.response.status}).`,
        );
      }

      setSummary(summaryResult.data);
      setTools(toolsResult.data);
      setUsers(usersResult.data);
      setToolResults(toolResultsResult.data);
      setSeries(seriesResult.data);
      setRecent(recentResult.data);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : String(caught));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (query) {
      void load(query);
    }
  }, [query, load]);

  useEffect(() => {
    if (!loading) {
      setShowLoading(false);
      return;
    }

    const timer = window.setTimeout(() => setShowLoading(true), 250);
    return () => window.clearTimeout(timer);
  }, [loading]);

  return (
    <main className="page">
      <header className="page-header">
        <div>
          <h1>MCP Server Analytics</h1>
          <p>Tool-call usage, latency, and recent activity.</p>
        </div>
        {query && <DateRangeControl query={query} onApply={setQuery} />}
      </header>

      {error && <div className="banner banner-error">{error}</div>}
      {showLoading && !error && (
        <div className="banner banner-loading">Loading analytics…</div>
      )}

      {summary && <KpiCards summary={summary} />}

      <div className="dashboard-grid">
        <Card title="Calls over time" span={12}>
          {series ? (
            <CallsOverTimeChart series={series} />
          ) : (
            <p className="empty">—</p>
          )}
        </Card>

        <Card title="Calls by tool result" span={6}>
          {toolResults ? (
            <ToolResultBreakdownChart results={toolResults.results} />
          ) : (
            <p className="empty">—</p>
          )}
        </Card>

        <Card title="Calls by user" span={6}>
          {users ? (
            <UserBreakdownChart users={users.users} />
          ) : (
            <p className="empty">—</p>
          )}
        </Card>

        <Card title="Per-tool breakdown" span={12}>
          {tools ? (
            <ToolBreakdownTable tools={tools.tools} />
          ) : (
            <p className="empty">—</p>
          )}
        </Card>

        <Card title="Recent calls" span={12}>
          {recent ? (
            <RecentCallsTable calls={recent.calls} />
          ) : (
            <p className="empty">—</p>
          )}
        </Card>
      </div>
    </main>
  );
}
