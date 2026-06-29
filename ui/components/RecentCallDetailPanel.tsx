"use client";

import type { ReactNode } from "react";
import type { RecentCallDetail } from "@/lib/types";
import { statusColor } from "@/lib/colors";
import { formatDateTime, formatMs } from "@/lib/format";

export function RecentCallDetailPanel({
  detail,
  loading,
  error,
  onClose,
}: {
  detail?: RecentCallDetail;
  loading: boolean;
  error?: string;
  onClose: () => void;
}) {
  return (
    <div className="detail-overlay" role="presentation" onClick={onClose}>
      <div
        className="detail-panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="recent-call-detail-title"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="detail-panel-header">
          <h2 id="recent-call-detail-title">Call detail</h2>
          <button className="detail-close" type="button" onClick={onClose}>
            Close
          </button>
        </div>

        {loading && <p className="empty">Loading detail…</p>}
        {error && <div className="banner banner-error">{error}</div>}

        {!loading && !error && detail && (
          <div className="detail-panel-body">
            <dl className="detail-list">
              <DetailItem label="Time" value={formatDateTime(detail.startedAt)} />
              <DetailItem label="Tool" value={detail.toolName} />
              <DetailItem label="User" value={detail.userName} />
              <DetailItem label="Duration" value={formatMs(detail.durationMs)} />
              <DetailItem label="Transport status" value={detail.status} />
              <DetailItem
                label="Tool result"
                value={
                  <span
                    className="status-pill"
                    style={{ backgroundColor: statusColor(detail.toolResultStatus) }}
                  >
                    {detail.toolResultStatus}
                  </span>
                }
              />
              {detail.errorType && (
                <DetailItem label="Exception type" value={detail.errorType} />
              )}
            </dl>

            {detail.detail?.resolvedContext && (
              <section className="detail-section">
                <h3>Resolved context</h3>
                <dl className="detail-list">
                  <ContextItem
                    label="Library"
                    value={detail.detail.resolvedContext.libraryId}
                  />
                  <ContextItem
                    label="Environment"
                    value={detail.detail.resolvedContext.environment}
                  />
                  <ContextItem
                    label="Version"
                    value={detail.detail.resolvedContext.version}
                  />
                  <ContextItem
                    label="Version reason"
                    value={detail.detail.resolvedContext.versionSelectionReason}
                  />
                  <ContextItem
                    label="Source"
                    value={detail.detail.resolvedContext.sourceId}
                  />
                </dl>
              </section>
            )}

            {detail.detail?.errors && detail.detail.errors.length > 0 && (
              <section className="detail-section">
                <h3>Errors</h3>
                <ul className="detail-errors">
                  {detail.detail.errors.map((item) => (
                    <li key={`${item.code}:${item.message}`}>
                      <strong>{item.code}</strong>
                      <span>{item.message}</span>
                    </li>
                  ))}
                </ul>
              </section>
            )}

            {!detail.errorType &&
              !detail.detail?.errors?.length &&
              !detail.detail?.resolvedContext && (
                <p className="empty">No additional detail was captured for this call.</p>
              )}
          </div>
        )}
      </div>
    </div>
  );
}

function DetailItem({
  label,
  value,
}: {
  label: string;
  value: ReactNode;
}) {
  return (
    <>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </>
  );
}

function ContextItem({
  label,
  value,
}: {
  label: string;
  value?: string | null;
}) {
  if (!value) {
    return null;
  }

  return (
    <>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </>
  );
}
