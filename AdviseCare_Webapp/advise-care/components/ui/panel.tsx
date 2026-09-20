import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import { Icon } from "./icon";

/**
 * Level 1 elevation: nền trắng + viền 1px, không đổ bóng.
 * Toàn bộ chiều sâu trong hệ thống này do viền tạo ra, không do shadow.
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
        "rounded-md border border-outline-variant/40 bg-surface-container-lowest",
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
        "flex flex-wrap items-start justify-between gap-space-md border-b border-outline-variant/30 px-space-lg py-space-base",
        className,
      )}
    >
      <div className="flex min-w-0 items-start gap-space-sm">
        {icon ? (
          <Icon name={icon} className="mt-0.5 text-[20px] text-primary" />
        ) : null}
        <div className="min-w-0">
          <h2 className="font-headline-sm text-headline-sm text-on-surface">
            {title}
          </h2>
          {subtitle ? (
            <p className="mt-0.5 font-body-sm text-body-sm text-on-surface-variant">
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
        "flex flex-wrap items-center justify-between gap-space-sm border-t border-outline-variant/30 bg-surface-container-low/50 px-space-lg py-space-md font-body-sm text-body-sm text-on-surface-variant",
        className,
      )}
    >
      {children}
    </footer>
  );
}
