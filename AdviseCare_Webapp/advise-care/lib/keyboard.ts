import type { KeyboardEvent } from "react";

/** j / k or ↓ / ↑ move between rows of a list; Enter opens (native button). */
export function moveRowFocus(event: KeyboardEvent<HTMLElement>) {
  const keys: Record<string, number> = { j: 1, ArrowDown: 1, k: -1, ArrowUp: -1 };
  const step = keys[event.key];
  if (!step || event.altKey || event.ctrlKey || event.metaKey) return;
  const target = event.target as HTMLElement;
  if (target.closest("input, textarea, select")) return;
  const rows = Array.from(event.currentTarget.querySelectorAll<HTMLElement>("[data-row-open]"));
  const index = rows.indexOf(target.closest<HTMLElement>("[data-row-open]") ?? target);
  const next = rows[Math.min(rows.length - 1, Math.max(0, index + step))];
  if (next) {
    event.preventDefault();
    next.focus();
  }
}

