import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import { Icon } from "./icon";

/** An empty list keeps its frame and says what will appear here and how. */
export function EmptyState({
  icon = "inbox",
  title,
  children,
  action,
  className,
}: {
  icon?: string;
  title: ReactNode;
  children?: ReactNode;
  action?: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex flex-col items-center px-space-lg py-space-2xl text-center",
        className,
      )}
    >
      <span
        aria-hidden
        className="flex size-11 items-center justify-center rounded-full border border-rule bg-paper-tint text-text-faint"
      >
        <Icon name={icon} className="text-[22px]" />
      </span>
      <p className="mt-space-md text-[0.9375rem] font-semibold text-text">{title}</p>
      {children ? (
        <div className="mt-1 max-w-[52ch] text-sm leading-6 text-text-muted">{children}</div>
      ) : null}
      {action ? <div className="mt-space-base">{action}</div> : null}
    </div>
  );
}

/** Row placeholders while a list loads; same pitch as the rows they stand in for. */
export function SkeletonRows({
  rows = 5,
  columns = 5,
  label,
}: {
  rows?: number;
  columns?: number;
  label: string;
}) {
  return (
    <div role="status" aria-live="polite" className="divide-y divide-rule">
      <span className="sr-only">{label}</span>
      {Array.from({ length: rows }, (_, row) => (
        <div key={row} className="flex h-11 items-center gap-space-lg px-space-lg">
          {Array.from({ length: columns }, (_, column) => (
            <span
              key={column}
              aria-hidden
              className="h-2.5 animate-pulse-soft rounded-full bg-surface-container"
              style={{ width: `${column === 0 ? 18 : 8 + ((row + column) % 4) * 4}%` }}
            />
          ))}
        </div>
      ))}
    </div>
  );
}

/** A failed load names the problem and offers the retry in the same place. */
export function ErrorState({
  title,
  children,
  action,
}: {
  title: ReactNode;
  children?: ReactNode;
  action?: ReactNode;
}) {
  return (
    <div
      role="alert"
      className="m-space-lg flex flex-wrap items-start gap-space-md rounded-lg border border-status-urgent-border bg-status-urgent-surface/60 p-space-base"
    >
      <Icon name="error" className="mt-0.5 text-[20px] text-red-ink" />
      <div className="min-w-0 flex-1">
        <p className="text-sm font-semibold text-red-ink">{title}</p>
        {children ? <div className="mt-0.5 text-sm text-text-muted">{children}</div> : null}
      </div>
      {action}
    </div>
  );
}
