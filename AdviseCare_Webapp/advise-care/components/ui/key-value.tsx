import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

/** Cặp nhãn/giá trị dùng trong inspector pane: nhãn 12px muted, giá trị 14px đậm. */
export function KeyValue({
  label,
  value,
  className,
  mono,
}: {
  label: ReactNode;
  value: ReactNode;
  className?: string;
  mono?: boolean;
}) {
  return (
    <div className={cn("min-w-0", className)}>
      <dt className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
        {label}
      </dt>
      <dd
        className={cn(
          "mt-0.5 break-words text-on-surface",
          mono
            ? "font-code-audit text-code-audit"
            : "font-label-md text-label-md font-semibold",
        )}
      >
        {value}
      </dd>
    </div>
  );
}

export function KeyValueGrid({
  children,
  columns = 2,
  className,
}: {
  children: ReactNode;
  columns?: 1 | 2 | 3 | 4;
  className?: string;
}) {
  const columnClass = {
    1: "grid-cols-1",
    2: "grid-cols-1 sm:grid-cols-2",
    3: "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3",
    4: "grid-cols-2 lg:grid-cols-4",
  }[columns];

  return (
    <dl className={cn("grid gap-space-base", columnClass, className)}>{children}</dl>
  );
}
