export const STATUS_COLORS: Record<string, string> = {
  success: "#16a34a",
  error: "#dc2626",
  canceled: "#d97706",
};

export const ACCENT = "#2563eb";

export function statusColor(status: string): string {
  return STATUS_COLORS[status] ?? "#64748b";
}
