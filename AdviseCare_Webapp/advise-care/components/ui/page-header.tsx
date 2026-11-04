import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import { Icon } from "./icon";

/**
 * The title row of a screen. Product UI keeps it at 24px: the data below is
 * the hero, not the heading. No eyebrow/kicker above the title.
 */
export function PageHeader({
  title,
  badge,
  description,
  meta,
  actions,
  className,
}: {
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
        "flex flex-wrap items-end justify-between gap-x-space-lg gap-y-space-md",
        className,
      )}
    >
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-space-md">
          <h1 className="text-2xl font-semibold leading-8 tracking-[-0.01em] text-text">
            {title}
          </h1>
          {badge}
        </div>
        {description ? (
          <p className="mt-1 max-w-[68ch] text-[0.9375rem] leading-6 text-text-muted">
            {description}
          </p>
        ) : null}
        {meta ? <div className="mt-space-sm">{meta}</div> : null}
      </div>
      {actions ? (
        <div className="flex flex-wrap items-center gap-space-sm">{actions}</div>
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
        "inline-flex items-center gap-1.5 rounded-md border px-2.5 py-1 text-sm",
        tone === "primary"
          ? "border-transparent bg-ink text-white"
          : "border-rule bg-paper text-text-muted",
      )}
    >
      <Icon name={icon} className="text-[16px]" />
      <span>
        {label}
        {value ? (
          <strong
            className={cn(
              "ml-1 font-semibold",
              tone === "primary" ? "text-white" : "text-text",
            )}
          >
            {value}
          </strong>
        ) : null}
      </span>
    </span>
  );
}
