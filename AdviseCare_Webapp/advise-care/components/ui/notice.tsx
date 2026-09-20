import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import type { StatusTone } from "@/lib/types";
import { Icon } from "./icon";

const toneClasses: Record<StatusTone, string> = {
  ontrack: "border-status-ontrack-border bg-status-ontrack-surface/60 text-status-ontrack-text",
  attention:
    "border-status-attention-border bg-status-attention-surface/60 text-status-attention-text",
  urgent: "border-status-urgent-border bg-status-urgent-surface/60 text-status-urgent-text",
  referral:
    "border-status-referral-border bg-status-referral-surface/50 text-status-referral-text",
  sealed: "border-outline-variant/50 bg-surface-container-low text-on-surface-variant",
};

export function Notice({
  tone = "sealed",
  icon = "info",
  title,
  children,
  actions,
  className,
}: {
  tone?: StatusTone;
  icon?: string;
  title?: ReactNode;
  children?: ReactNode;
  actions?: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex flex-wrap items-start gap-space-md rounded-md border px-space-base py-space-md",
        toneClasses[tone],
        className,
      )}
    >
      <Icon name={icon} className="mt-0.5 text-[18px] shrink-0" />
      <div className="min-w-0 flex-1 font-body-sm text-body-sm">
        {title ? (
          <p className="font-label-md text-label-md font-semibold">{title}</p>
        ) : null}
        {children ? (
          <div className={cn("text-on-surface-variant", title && "mt-1")}>{children}</div>
        ) : null}
      </div>
      {actions ? (
        <div className="flex shrink-0 items-center gap-space-sm">{actions}</div>
      ) : null}
    </div>
  );
}
