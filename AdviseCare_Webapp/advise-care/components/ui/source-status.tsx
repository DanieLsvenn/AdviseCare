import { cn } from "@/lib/cn";
import type { DataSourceStatus, IndicatorProvenance } from "@/lib/types";
import { Icon } from "./icon";
import { StatusChip } from "./status-chip";

/**
 * Design Rule §4.2 — mỗi nhóm dữ liệu phải công bố độ đầy đủ của nguồn.
 * Thanh này là nơi câu "attendance: HIGH risk" trở thành "insufficient data".
 */
export function SourceStatusBar({
  sources,
  className,
}: {
  sources: DataSourceStatus[];
  className?: string;
}) {
  return (
    <div className={cn("grid gap-space-md sm:grid-cols-2 xl:grid-cols-4", className)}>
      {sources.map((source) => (
        <div
          key={source.Code}
          className="flex items-center justify-between gap-space-md rounded-md border border-outline-variant/40 bg-surface-container-lowest px-space-base py-space-md"
        >
          <div className="flex min-w-0 items-center gap-space-sm">
            <Icon name={source.Icon} className="text-[18px] text-on-surface-variant" />
            <div className="min-w-0">
              <p className="truncate font-body-sm text-body-sm text-on-surface-variant">
                {source.Label}
              </p>
              <p className="truncate font-label-md text-label-md font-semibold text-on-surface">
                {source.StateLabel}
              </p>
            </div>
          </div>
          <span
            aria-hidden
            className={cn(
              "size-2 shrink-0 rounded-full",
              source.Tone === "ontrack" ? "bg-secondary" : "bg-outline-variant",
            )}
          />
        </div>
      ))}
    </div>
  );
}

const contributionTone = {
  USED: { tone: "ontrack", icon: "check_circle", label: "Used" },
  MISSING: { tone: "urgent", icon: "cancel", label: "Missing" },
  STALE: { tone: "attention", icon: "schedule", label: "Stale" },
  DENIED_BY_CONSENT: { tone: "sealed", icon: "lock", label: "Denied by consent" },
} as const;

export function ProvenanceList({
  items,
  className,
}: {
  items: IndicatorProvenance[];
  className?: string;
}) {
  return (
    <ul className={cn("divide-y divide-outline-variant/25", className)}>
      {items.map((item) => {
        const meta = contributionTone[item.ContributionKind];
        return (
          <li key={item.SourceCode} className="flex flex-wrap items-start gap-space-md py-space-md">
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-space-sm">
                <p className="font-label-md text-label-md font-semibold text-on-surface">
                  {item.SourceLabel}
                </p>
                <StatusChip tone={meta.tone} icon={meta.icon} label={meta.label} size="sm" />
              </div>
              {item.Note ? (
                <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                  {item.Note}
                </p>
              ) : null}
            </div>
            <div className="text-right">
              <p className="font-code-audit text-code-audit text-on-surface-variant">
                {item.SourceCode}
              </p>
              <p className="font-label-md text-label-md font-semibold text-on-surface">
                {Math.round(item.CoverageRatio * 100)}% · {item.RecordCount} rec
              </p>
            </div>
          </li>
        );
      })}
    </ul>
  );
}
