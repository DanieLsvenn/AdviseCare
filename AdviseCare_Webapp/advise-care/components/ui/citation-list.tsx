import { cn } from "@/lib/cn";
import type { LedgerEntryCitation } from "@/lib/types";
import { Icon } from "./icon";
import { StatusChip } from "./status-chip";

const typeIcons: Record<LedgerEntryCitation["CitedObjectType"], string> = {
  INDICATOR_VALUE: "insights",
  ACADEMIC_RECORD: "school",
  ATTENDANCE_RECORD: "event_available",
  SENSITIVE_ATTRIBUTE: "shield_lock",
  CONSENT_GRANT: "handshake",
  LEDGER_ENTRY: "history_edu",
};

/**
 * Bảng trích dẫn là thứ khiến hồ sơ này bảo vệ được: nó lưu *đúng chuỗi đã hiện
 * trên màn hình* cùng valid-time và transaction-time tại thời điểm đọc.
 */
export function CitationList({
  citations,
  className,
}: {
  citations: LedgerEntryCitation[];
  className?: string;
}) {
  if (citations.length === 0) {
    return (
      <p className={cn("font-body-sm text-body-sm text-on-surface-variant", className)}>
        No evidence was cited by this entry.
      </p>
    );
  }

  return (
    <ul className={cn("space-y-space-sm", className)}>
      {citations.map((citation) => (
        <li
          key={citation.CitationId}
          className="rounded border border-outline-variant/40 bg-surface-container-low/50 px-space-md py-space-sm"
        >
          <div className="flex flex-wrap items-center gap-space-sm">
            <Icon
              name={typeIcons[citation.CitedObjectType]}
              className="text-[16px] text-on-surface-variant"
            />
            <p className="font-label-md text-label-md font-semibold text-on-surface">
              {citation.Label}
            </p>
            <span className="font-code-audit text-code-audit text-on-surface-variant">
              {citation.CitedObjectRef}
            </span>
            {citation.Redacted ? (
              <StatusChip tone="sealed" icon="lock" size="sm" label="Value erased" />
            ) : null}
          </div>
          <p className="mt-1 font-body-sm text-body-sm text-on-surface">
            Displayed as: <span className="font-semibold">{citation.DisplayedValue}</span>
          </p>
          <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
            valid {citation.AsOfValidTime} · recorded {citation.AsOfTransactionTime}
          </p>
        </li>
      ))}
    </ul>
  );
}
