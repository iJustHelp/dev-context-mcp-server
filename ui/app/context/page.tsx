"use client";

import type { ReactNode } from "react";
import { useEffect, useState } from "react";
import { Card } from "@/components/Card";
import {
  LastRunTable,
  NuGetInventoryTable,
} from "@/components/ContextInventoryTables";
import { apiClient } from "@/lib/client";
import { formatCount, formatDateTime } from "@/lib/format";
import type { IndexedContextResponse, IndexSnapshot } from "@/lib/types";

export default function ContextPage() {
  const [context, setContext] = useState<IndexedContextResponse>();
  const [lastRun, setLastRun] = useState<IndexSnapshot>();
  const [error, setError] = useState<string>();
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;

    async function load() {
      setLoading(true);
      setError(undefined);

      try {
        const result = await apiClient.GET("/api/context");
        if (result.error) {
          throw new Error(
            result.error.error ??
              `Context request failed (${result.response.status}).`,
          );
        }

        const lastRunResult = await apiClient.GET("/api/context/last-run");

        if (active) {
          setContext(result.data);
          setLastRun(lastRunResult.data);
        }
      } catch (caught) {
        if (active) {
          setError(caught instanceof Error ? caught.message : String(caught));
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    void load();

    return () => {
      active = false;
    };
  }, []);

  return (
    <main className="page">
      <header className="page-header">
        <div>
          <h1>Indexed Context</h1>
          <p>Read-only inventory of the current documentation index.</p>
        </div>
        {context && (
          <p className="generated-at">
            Generated {formatDateTime(context.generatedAt)}
          </p>
        )}
      </header>

      {error && <div className="banner banner-error">{error}</div>}
      {loading && !error && (
        <div className="banner banner-loading">Loading context...</div>
      )}

      {context && (
        <>
          <div className="kpi-row context-kpi-row">
            <Kpi value={context.totals.environmentCount} label="Environments">
              <EnvironmentNames nugets={context.nugets} />
            </Kpi>
            <Kpi value={context.totals.nuGetLibraryCount} label="NuGets">
              <EnvironmentBreakdown nugets={context.nugets} />
            </Kpi>
          </div>

          <div className="dashboard-grid">
            <Card title="NuGet packages" span={12}>
              <NuGetInventoryTable nugets={context.nugets} />
            </Card>            
            {lastRun && lastRun.packages.length > 0 && (
              <Card title="Last indexing run" span={12}>
                <LastRunTable packages={lastRun.packages} />
              </Card>
            )}
          </div>
        </>
      )}
    </main>
  );
}

function Kpi({
  value,
  label,
  children,
}: {
  value: number;
  label: string;
  children?: ReactNode;
}) {
  return (
    <div className="kpi">
      <div className="kpi-value">{formatCount(value)}</div>
      <div className="kpi-label">{label}</div>
      {children}
    </div>
  );
}

function EnvironmentNames({
  nugets,
}: {
  nugets: IndexedContextResponse["nugets"];
}) {
  const environments = Array.from(
    new Set(nugets.map((nuget) => nuget.environment ?? "Unspecified")),
  ).sort(compareEnvironment);

  return (
    <div className="kpi-breakdown">
      {environments.map((environment) => (
        <span className="kpi-breakdown-item" key={environment}>
          <span>{environment}</span>
        </span>
      ))}
    </div>
  );
}

function EnvironmentBreakdown({
  nugets,
}: {
  nugets: IndexedContextResponse["nugets"];
}) {
  const counts = nugets.reduce<Map<string, number>>((totals, nuget) => {
    const environment = nuget.environment ?? "Unspecified";
    totals.set(environment, (totals.get(environment) ?? 0) + 1);
    return totals;
  }, new Map<string, number>());

  const rows = Array.from(counts.entries()).sort(([left], [right]) =>
    compareEnvironment(left, right),
  );

  return (
    <div className="kpi-breakdown">
      {rows.map(([environment, count]) => (
        <span className="kpi-breakdown-item" key={environment}>
          <span>{environment}</span>
          <strong>{formatCount(count)}</strong>
        </span>
      ))}
    </div>
  );
}

function compareEnvironment(left: string, right: string): number {
  const order = ["qa", "prod", "public"];
  const leftIndex = order.indexOf(left.toLowerCase());
  const rightIndex = order.indexOf(right.toLowerCase());

  if (leftIndex !== -1 || rightIndex !== -1) {
    return (leftIndex === -1 ? order.length : leftIndex)
      - (rightIndex === -1 ? order.length : rightIndex);
  }

  return left.localeCompare(right, undefined, { sensitivity: "base" });
}
