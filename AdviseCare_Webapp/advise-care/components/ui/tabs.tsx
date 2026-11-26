"use client";

import { useId, type KeyboardEvent, type ReactNode } from "react";
import { cn } from "@/lib/cn";

/** A tab bar for a custom page: the caller keeps the active tab and renders its panel. */
export function TabBar({
  tabs,
  active,
  onChange,
  label,
}: {
  tabs: Array<{ key: string; label: ReactNode; badge?: number }>;
  active: string;
  onChange: (key: string) => void;
  label: string;
}) {
  const baseId = useId();
  const index = Math.max(0, tabs.findIndex((tab) => tab.key === active));

  function onKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
    event.preventDefault();
    const next = (index + (event.key === "ArrowRight" ? 1 : tabs.length - 1)) % tabs.length;
    onChange(tabs[next]!.key);
    document.getElementById(`${baseId}-${next}`)?.focus();
  }

  return (
    <div role="tablist" aria-label={label} className="flex flex-wrap gap-space-xs border-b border-rule" onKeyDown={onKeyDown}>
      {tabs.map((tab, position) => {
        const selected = tab.key === active;
        return (
          <button
            key={tab.key}
            id={`${baseId}-${position}`}
            type="button"
            role="tab"
            aria-selected={selected}
            tabIndex={selected ? 0 : -1}
            onClick={() => onChange(tab.key)}
            className={cn(
              "-mb-px inline-flex items-center gap-1.5 border-b-2 px-space-md py-2 text-sm font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ink",
              selected ? "border-ink text-ink" : "border-transparent text-text-muted hover:text-text",
            )}
          >
            {tab.label}
            {tab.badge ? (
              <span className="rounded-full bg-ink-wash px-1.5 font-mono text-[0.75rem] leading-5 text-ink">{tab.badge}</span>
            ) : null}
          </button>
        );
      })}
    </div>
  );
}
