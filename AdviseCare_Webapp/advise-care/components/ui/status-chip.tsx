import { cn } from "@/lib/cn";
import type { StatusTone } from "@/lib/types";
import { Icon } from "./icon";

/**
 * Design Rule §4.1 — trạng thái không bao giờ chỉ là một chấm màu.
 * Mỗi chip gồm bộ 3 token (chữ / nền / viền) và bắt buộc có nhãn chữ.
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
};

interface StatusChipProps {
  tone: StatusTone;
  label: string;
  icon?: string;
  className?: string;
  size?: "sm" | "md";
}

export function StatusChip({
  tone,
  label,
  icon,
  className,
  size = "md",
}: StatusChipProps) {
  return (
    <span
      className={cn(
        "inline-flex max-w-full items-center gap-1 rounded border font-semibold",
        size === "sm"
          ? "px-1.5 py-0.5 text-[11px] leading-[14px]"
          : "px-2 py-0.5 text-xs leading-[18px]",
        toneClasses[tone],
        className,
      )}
    >
      {icon ? <Icon name={icon} className="text-[14px]" /> : null}
      <span className="truncate">{label}</span>
    </span>
  );
}
