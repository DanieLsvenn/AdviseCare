import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/icon";
import type { CaseStateCode } from "@/lib/types";

const order: CaseStateCode[] = ["TRIAGE", "CONTACT", "REFERRAL", "FOLLOW_UP", "CLOSED"];

const stageMeta: Record<CaseStateCode, { label: string; caption: string }> = {
  TRIAGE: { label: "Triage", caption: "Automated rule trigger" },
  CONTACT: { label: "Contact", caption: "Consultation complete" },
  REFERRAL: { label: "Referral", caption: "Pending referral filing" },
  FOLLOW_UP: { label: "Follow-up", caption: "Check-in target" },
  CLOSED: { label: "Closure", caption: "Supervisory approval required" },
};

/**
 * Tiến trình 5 giai đoạn hiển thị cả giai đoạn đã qua lẫn giai đoạn chưa tới,
 * kèm lý do — để người đọc thấy hồ sơ đang ở đâu mà không phải suy đoán.
 */
export function CasePipeline({ current }: { current: CaseStateCode }) {
  const currentIndex = order.indexOf(current);

  return (
    <ol className="grid gap-space-sm sm:grid-cols-2 xl:grid-cols-5">
      {order.map((stage, index) => {
        const done = index < currentIndex;
        const active = index === currentIndex;
        const meta = stageMeta[stage];

        return (
          <li
            key={stage}
            className={cn(
              "rounded-md border px-space-base py-space-md",
              active
                ? "border-secondary bg-secondary-container/20"
                : done
                  ? "border-outline-variant/40 bg-surface-container-low/50"
                  : "border-outline-variant/30 bg-surface-container-lowest",
            )}
          >
            <p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
              Stage {String(index + 1).padStart(2, "0")}
              {active ? " • Active" : null}
            </p>
            <p className="mt-1 flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
              <Icon
                name={
                  done
                    ? "check_circle"
                    : active
                      ? "radio_button_checked"
                      : stage === "CLOSED"
                        ? "lock"
                        : "radio_button_unchecked"
                }
                className={cn(
                  "text-[16px]",
                  done
                    ? "text-status-ontrack-text"
                    : active
                      ? "text-primary"
                      : "text-on-surface-variant",
                )}
              />
              {meta.label}
            </p>
            <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
              {meta.caption}
            </p>
          </li>
        );
      })}
    </ol>
  );
}
