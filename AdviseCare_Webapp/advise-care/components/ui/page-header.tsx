import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import { Icon } from "./icon";

export function Eyebrow({
  items,
  className,
}: {
  items: string[];
  className?: string;
}) {
  return (
    <p
      className={cn(
        "flex flex-wrap items-center gap-space-sm font-caption text-caption text-on-surface-variant",
        className,
      )}
    >
      {items.map((item, index) => (
        <span key={item} className="flex items-center gap-space-sm">
          {index > 0 ? <span className="text-outline-variant">/</span> : null}
          {item}
        </span>
      ))}
    </p>
  );
}

export function PageHeader({
  title,
  badge,
  description,
  meta,
  actions,
  className,
}: {
  eyebrow?: string[];
  title: ReactNode;
  badge?: ReactNode;
  description?: ReactNode;
  meta?: ReactNode;
  actions?: ReactNode;
  className?: string;
}) {
  return (
    <header
      className={cn(
        "flex flex-wrap items-start justify-between gap-space-lg",
        className,
      )}
    >
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-space-md">
          <h1 className="font-display-sm text-display-sm text-on-surface">
            {title}
          </h1>
          {badge}
        </div>
        {description ? (
          <p className="mt-space-sm max-w-3xl font-body-sm text-body-sm text-on-surface-variant">
            {description}
          </p>
        ) : null}
      </div>
      {meta || actions ? (
        <div className="flex flex-col items-start gap-space-sm lg:items-end">
          {meta}
          {actions}
        </div>
      ) : null}
    </header>
  );
}

export function MetaPill({
  icon,
  label,
  value,
  tone = "neutral",
}: {
  icon: string;
  label: string;
  value?: string;
  tone?: "neutral" | "primary";
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded border px-space-md py-1.5 font-label-md text-label-md",
        tone === "primary"
          ? "border-transparent bg-primary text-on-primary"
          : "border-outline-variant/50 bg-surface-container-lowest text-on-surface-variant",
      )}
    >
      <Icon name={icon} className="text-[16px]" />
      <span>
        {label}
        {value ? (
          <strong
            className={cn(
              "ml-1 font-semibold",
              tone === "primary" ? "text-on-primary" : "text-on-surface",
            )}
          >
            {value}
          </strong>
        ) : null}
      </span>
    </span>
  );
}
