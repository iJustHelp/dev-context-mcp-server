"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const links = [
  { href: "/", label: "Dashboard" },
  { href: "/context", label: "Context" },
];

export function AppNav() {
  const pathname = usePathname();

  return (
    <nav className="app-nav" aria-label="Primary navigation">
      <div className="app-nav-inner">
        <div className="app-title">Dev Context MCP</div>
        <div className="app-links">
          {links.map((link) => {
            const active =
              link.href === "/" ? pathname === "/" : pathname.startsWith(link.href);
            return (
              <Link
                key={link.href}
                href={link.href}
                className={active ? "app-link app-link-active" : "app-link"}
              >
                {link.label}
              </Link>
            );
          })}
        </div>
      </div>
    </nav>
  );
}
