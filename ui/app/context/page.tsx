"use client";

import { useEffect, useState } from "react";
import { Card } from "@/components/Card";
import {
  DocumentInventoryTable,
  NuGetInventoryTable,
} from "@/components/ContextInventoryTables";
import { apiClient } from "@/lib/client";
import { formatCount, formatDateTime } from "@/lib/format";
import type { IndexedContextResponse } from "@/lib/types";

export default function ContextPage() {
  const [context, setContext] = useState<IndexedContextResponse>();
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

        if (active) {
          setContext(result.data);
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
          <div className="kpi-row">
            <Kpi value={context.totals.sourceCount} label="Sources" />
            <Kpi value={context.totals.environmentCount} label="Environments" />
            <Kpi value={context.totals.nuGetLibraryCount} label="NuGets" />
            <Kpi value={context.totals.documentCount} label="Documents" />
          </div>

          <div className="dashboard-grid">
            <Card title="Documents" span={12}>
              <DocumentInventoryTable documents={context.documents} />
            </Card>

            <Card title="NuGet packages" span={12}>
              <NuGetInventoryTable nugets={context.nugets} />
            </Card>
          </div>
        </>
      )}
    </main>
  );
}

function Kpi({ value, label }: { value: number; label: string }) {
  return (
    <div className="kpi">
      <div className="kpi-value">{formatCount(value)}</div>
      <div className="kpi-label">{label}</div>
    </div>
  );
}
