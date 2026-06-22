"use client";

import { useMemo, useState } from "react";
import type {
  IndexedDocumentInventoryItem,
  IndexedNuGetInventoryItem,
} from "@/lib/types";
import { formatCount, formatDateTime } from "@/lib/format";

type Direction = "asc" | "desc";

type DocumentSortKey =
  | "name"
  | "length"
  | "chunkCount"
  | "lastIndexedAt";

type NuGetSortKey =
  | "displayName"
  | "environment"
  | "versions"
  | "documentCount"
  | "lastIndexedAt";

interface SortState<TKey extends string> {
  key: TKey;
  direction: Direction;
}

export function DocumentInventoryTable({
  documents,
}: {
  documents: IndexedDocumentInventoryItem[];
}) {
  const [search, setSearch] = useState("");
  const [sort, setSort] = useState<SortState<DocumentSortKey>>({
    key: "name",
    direction: "asc",
  });

  const rows = useMemo(() => {
    const query = search.trim().toLowerCase();
    return documents
      .filter((document) =>
        query.length === 0
          ? true
          : [
              document.name,
              document.environment ?? "",
              String(document.length),
            ]
              .join(" ")
              .toLowerCase()
              .includes(query),
      )
      .toSorted((left, right) =>
        compareValues(documentSortValue(left, sort.key), documentSortValue(right, sort.key), sort.direction),
      );
  }, [documents, search, sort]);

  return (
    <div>
      <TableSearch
        label="Search documents"
        value={search}
        onChange={setSearch}
      />
      <div className="table-scroll">
        <table className="data-table">
          <thead>
            <tr>
              <SortableHeader
                label="Name"
                sortKey="name"
                sort={sort}
                onSort={setDocumentSort}
              />
              <SortableHeader
                label="Length"
                sortKey="length"
                sort={sort}
                onSort={setDocumentSort}
                align="right"
              />
              <SortableHeader
                label="Chunks"
                sortKey="chunkCount"
                sort={sort}
                onSort={setDocumentSort}
                align="right"
              />
              <SortableHeader
                label="Last indexed"
                sortKey="lastIndexedAt"
                sort={sort}
                onSort={setDocumentSort}
              />
            </tr>
          </thead>
          <tbody>
            {rows.map((document) => (
              <tr key={`${document.sourceName}:${document.name}`}>
                <td>{document.name}</td>
                <td className="num">{formatCount(document.length)}</td>
                <td className="num">{formatCount(document.chunkCount)}</td>
                <td>{formatOptionalDate(document.lastIndexedAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {rows.length === 0 && <p className="empty">No documents match.</p>}
    </div>
  );

  function setDocumentSort(key: DocumentSortKey) {
    setSort((current) => nextSort(current, key));
  }
}

export function NuGetInventoryTable({
  nugets,
}: {
  nugets: IndexedNuGetInventoryItem[];
}) {
  const [search, setSearch] = useState("");
  const [sort, setSort] = useState<SortState<NuGetSortKey>>({
    key: "displayName",
    direction: "asc",
  });

  const rows = useMemo(() => {
    const query = search.trim().toLowerCase();
    return nugets
      .filter((nuget) =>
        query.length === 0
          ? true
          : [
              nuget.displayName,
              nuget.packageId,
              nuget.environment ?? "",
              nuget.versions.join(" "),
            ]
              .join(" ")
              .toLowerCase()
              .includes(query),
      )
      .toSorted((left, right) =>
        compareValues(nugetSortValue(left, sort.key), nugetSortValue(right, sort.key), sort.direction),
      );
  }, [nugets, search, sort]);

  return (
    <div>
      <TableSearch label="Search NuGets" value={search} onChange={setSearch} />
      <div className="table-scroll">
        <table className="data-table">
          <thead>
            <tr>
              <SortableHeader
                label="Package"
                sortKey="displayName"
                sort={sort}
                onSort={setNuGetSort}
              />
              <SortableHeader
                label="Environment"
                sortKey="environment"
                sort={sort}
                onSort={setNuGetSort}
              />
              <SortableHeader
                label="Versions"
                sortKey="versions"
                sort={sort}
                onSort={setNuGetSort}
              />            
              <SortableHeader
                label="Last indexed"
                sortKey="lastIndexedAt"
                sort={sort}
                onSort={setNuGetSort}
              />
            </tr>
          </thead>
          <tbody>
            {rows.map((nuget) => (
              <tr key={nuget.libraryId}>
                <td>{nuget.displayName}</td>
                <td>{nuget.environment ?? "-"}</td>
                <td className="version-list">{formatVersions(nuget.versions)}</td>
                <td>{formatOptionalDate(nuget.lastIndexedAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {rows.length === 0 && <p className="empty">No NuGets match.</p>}
    </div>
  );

  function setNuGetSort(key: NuGetSortKey) {
    setSort((current) => nextSort(current, key));
  }
}

function TableSearch({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="table-search">
      <span>{label}</span>
      <input
        value={value}
        onChange={(event) => onChange(event.target.value)}
        type="search"
      />
    </label>
  );
}

function SortableHeader<TKey extends string>({
  label,
  sortKey,
  sort,
  onSort,
  align,
}: {
  label: string;
  sortKey: TKey;
  sort: SortState<TKey>;
  onSort: (key: TKey) => void;
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

function nextSort<TKey extends string>(
  current: SortState<TKey>,
  key: TKey,
): SortState<TKey> {
  if (current.key !== key) {
    return { key, direction: "asc" };
  }

  return { key, direction: current.direction === "asc" ? "desc" : "asc" };
}

function documentSortValue(
  item: IndexedDocumentInventoryItem,
  key: DocumentSortKey,
): string | number {
  if (key === "lastIndexedAt") {
    return dateValue(item.lastIndexedAt);
  }

  return item[key] ?? "";
}

function nugetSortValue(
  item: IndexedNuGetInventoryItem,
  key: NuGetSortKey,
): string | number {
  if (key === "lastIndexedAt") {
    return dateValue(item.lastIndexedAt);
  }

  if (key === "environment") {
    return environmentSortValue(item.environment);
  }

  if (key === "versions") {
    return item.versions.join(" ");
  }

  return item[key] ?? "";
}

function compareValues(
  left: string | number,
  right: string | number,
  direction: Direction,
): number {
  const multiplier = direction === "asc" ? 1 : -1;
  if (typeof left === "number" && typeof right === "number") {
    return (left - right) * multiplier;
  }

  return String(left).localeCompare(String(right), undefined, {
    numeric: true,
    sensitivity: "base",
  }) * multiplier;
}

function dateValue(value: string | null): number {
  return value ? new Date(value).getTime() : 0;
}

function environmentSortValue(value: string | null): string {
  const environment = value?.toLowerCase() ?? "";
  const order = ["qa", "prod", "public"];
  const index = order.indexOf(environment);
  return `${index === -1 ? order.length : index}:${environment}`;
}

function formatOptionalDate(value: string | null): string {
  return value ? formatDateTime(value) : "-";
}

function formatVersions(versions: string[]): string {
  return versions.length === 0 ? "-" : versions.join(", ");
}
