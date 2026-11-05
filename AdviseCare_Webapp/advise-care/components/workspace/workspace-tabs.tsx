"use client";

import { useId, useState, type KeyboardEvent } from "react";
import { cn } from "@/lib/cn";
import { PageHeader } from "@/components/ui/page-header";
import {
  ApiWorkspace,
  type ApiWorkspaceDefinition,
} from "@/components/workspace/api-workspace";

/**
 * Several lists of one screen behind tabs at the top, so the second list is
 * one click away instead of below the whole first list. Each tab keeps its
 * own filters and page while the other is shown.
 */
export function WorkspaceTabs({
  title,
  description,
  tabs,
}: {
  title: string;
  description?: string;
  tabs: Array<{ label: string; definition: ApiWorkspaceDefinition }>;
}) {
  const baseId = useId();
  const [active, setActive] = useState(0);

  function onKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
    event.preventDefault();
    const next =
      (active + (event.key === "ArrowRight" ? 1 : tabs.length - 1)) % tabs.length;
    setActive(next);
    document.getElementById(`${baseId}-tab-${next}`)?.focus();
  }

  return (
    <div className="flex flex-col gap-space-lg">
      <PageHeader title={title} description={description} />
      <div
        role="tablist"
        aria-label={tabs.map((tab) => tab.label).join(" / ")}
        className="flex gap-space-xs border-b border-rule"
        onKeyDown={onKeyDown}
      >
        {tabs.map((tab, index) => {
          const selected = index === active;
          return (
            <button
              key={tab.label}
              id={`${baseId}-tab-${index}`}
              type="button"
              role="tab"
              aria-selected={selected}
              aria-controls={`${baseId}-panel-${index}`}
              tabIndex={selected ? 0 : -1}
              onClick={() => setActive(index)}
              className={cn(
                "-mb-px border-b-2 px-space-md py-2 text-sm font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ink",
                selected
                  ? "border-ink text-ink"
                  : "border-transparent text-text-muted hover:text-text",
              )}
            >
              {tab.label}
            </button>
          );
        })}
      </div>
      {tabs.map((tab, index) => (
        <div
          key={tab.label}
          id={`${baseId}-panel-${index}`}
          role="tabpanel"
          aria-labelledby={`${baseId}-tab-${index}`}
          hidden={index !== active}
        >
          <ApiWorkspace
            definition={{ ...tab.definition, embedded: true, hideToolbarTitle: true }}
          />
        </div>
      ))}
    </div>
  );
}
