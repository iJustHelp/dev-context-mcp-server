import type { ReactNode } from "react";

export function Card({
  title,
  children,
  span,
}: {
  title: string;
  children: ReactNode;
  span?: number;
}) {
  return (
    <section className="card" style={span ? { gridColumn: `span ${span}` } : undefined}>
      <h2 className="card-title">{title}</h2>
      {children}
    </section>
  );
}
