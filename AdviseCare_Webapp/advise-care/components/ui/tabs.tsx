"use client";

import { useState, type ReactNode } from "react";
import { cn } from "@/lib/cn";

export interface TabItem {
  id: string;
  label: string;
  content: ReactNode;
  count?: number;
}

export function Tabs({
  items,
  className,
  initialId,
}: {
  items: TabItem[];
  className?: string;
  initialId?: string;
}) {
  const [active, setActive] = useState(initialId ?? items[0]?.id);
  const current = items.find((item) => item.id === active) ?? items[0];

  return (
    <div className={className}>
      <div
        role="tablist"
        className="flex flex-wrap gap-space-xs border-b border-outline-variant/40"
      >
        {items.map((item) => {
          const isActive = item.id === current?.id;
          return (
            <button
              key={item.id}
              type="button"
              role="tab"
              aria-selected={isActive}
              onClick={() => setActive(item.id)}
              className={cn(
                "-mb-px rounded-t border-b-2 px-space-md py-space-sm font-label-md text-label-md transition-colors",
                isActive
                  ? "border-secondary font-semibold text-primary"
                  : "border-transparent text-on-surface-variant hover:text-on-surface",
              )}
            >
              {item.label}
              {typeof item.count === "number" ? (
                <span className="ml-1.5 rounded bg-surface-container-high px-1.5 py-0.5 font-caption text-caption text-on-surface-variant">
                  {item.count}
                </span>
              ) : null}
            </button>
          );
        })}
      </div>
      <div role="tabpanel" className="pt-space-lg">
        {current?.content}
      </div>
    </div>
  );
}
