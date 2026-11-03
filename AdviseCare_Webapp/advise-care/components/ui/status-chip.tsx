import { cn } from "@/lib/cn";
import type { StatusTone } from "@/lib/types";
import { Icon } from "./icon";

/**
 * A status is never colour alone: every chip has a tone (text / surface /
 * border) and a mandatory text label. Chips never shrink or wrap inside a
 * table cell.
 */
const toneClasses: Record<StatusTone, string> = {
  ontrack:
    "bg-status-ontrack-surface text-status-ontrack-text border-status-ontrack-border",
  attention:
    "bg-status-attention-surface text-status-attention-text border-status-attention-border",
  urgent:
    "bg-status-urgent-surface text-status-urgent-text border-status-urgent-border",
  referral:
    "bg-status-referral-surface text-status-referral-text border-status-referral-border",
  sealed:
    "bg-status-sealed-surface text-status-sealed-text border-status-sealed-border",
  info: "bg-status-info-surface text-status-info-text border-status-info-border",
};

const dotClasses: Record<StatusTone, string> = {
  ontrack: "bg-status-ontrack-text",
  attention: "bg-status-attention-text",
  urgent: "bg-status-urgent-text",
  referral: "bg-status-referral-text",
  sealed: "bg-status-sealed-text",
  info: "bg-status-info-text",
};

interface StatusChipProps {
  tone: StatusTone;
  label: string;
  icon?: string;
  /** A 6px dot before the label; used for workflow stages. */
  dot?: boolean;
  className?: string;
  size?: "sm" | "md";
}

export function StatusChip({
  tone,
  label,
  icon,
  dot,
  className,
  size = "md",
}: StatusChipProps) {
  return (
    <span
      className={cn(
        "inline-flex flex-none items-center gap-1.5 whitespace-nowrap rounded-full border font-semibold",
        size === "sm"
          ? "h-5 px-2 text-[0.75rem] leading-none"
          : "h-6 px-2.5 text-[0.8125rem] leading-none",
        toneClasses[tone],
        className,
      )}
    >
      {dot ? (
        <span aria-hidden className={cn("size-1.5 rounded-full", dotClasses[tone])} />
      ) : null}
      {icon ? <Icon name={icon} className="text-[14px]" /> : null}
      {label}
    </span>
  );
}
