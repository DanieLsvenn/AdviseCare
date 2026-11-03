import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import { Icon } from "./icon";

/**
 * A page of the book: white paper, one 1px ruled edge, no shadow.
 * Depth in this system comes from rules, never from shadows.
 */
export function Panel({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <section
      className={cn(
        "rounded-lg border border-rule bg-paper",
        className,
      )}
    >
      {children}
    </section>
  );
}

export function PanelHeader({
  title,
  subtitle,
  icon,
  actions,
  className,
}: {
  title: ReactNode;
  subtitle?: ReactNode;
  icon?: string;
  actions?: ReactNode;
  className?: string;
}) {
  return (
    <header
      className={cn(
        "flex flex-wrap items-center justify-between gap-space-md border-b border-rule px-space-lg py-space-md",
        className,
      )}
    >
      <div className="flex min-w-0 items-start gap-space-sm">
        {icon ? (
          <Icon name={icon} className="mt-0.5 text-[20px] text-ink" />
        ) : null}
        <div className="min-w-0">
          <h2 className="text-base font-semibold text-text">
            {title}
          </h2>
          {subtitle ? (
            <p className="mt-0.5 text-sm text-text-muted">
              {subtitle}
            </p>
          ) : null}
        </div>
      </div>
      {actions ? (
        <div className="flex shrink-0 items-center gap-space-sm">{actions}</div>
      ) : null}
    </header>
  );
}

export function PanelBody({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return <div className={cn("px-space-lg py-space-base", className)}>{children}</div>;
}

export function PanelFooter({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <footer
      className={cn(
        "flex flex-wrap items-center justify-between gap-space-sm border-t border-rule px-space-lg py-space-md text-sm text-text-muted",
        className,
      )}
    >
      {children}
    </footer>
  );
}
