import { cn } from "@/lib/cn";
import { Icon } from "./icon";

/** Dải chân trang xác nhận mọi truy vấn trên màn hình đã được ghi vào access log. */
export function AuditFooter({
  message,
  sessionHash,
  timestamp,
  className,
}: {
  message: string;
  sessionHash: string;
  timestamp: string;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex flex-wrap items-center justify-between gap-space-md rounded-md border border-outline-variant/40 bg-surface-container-lowest px-space-lg py-space-base",
        className,
      )}
    >
      <div className="flex min-w-0 items-start gap-space-md">
        <Icon name="verified_user" className="mt-0.5 text-[20px] text-primary" />
        <div className="min-w-0">
          <p className="font-label-md text-label-md font-semibold text-on-surface">
            Cryptographic Read-Access Ledger Active
          </p>
          <p className="mt-0.5 font-body-sm text-body-sm text-on-surface-variant">
            {message}
          </p>
        </div>
      </div>
      <p className="rounded border border-outline-variant/40 bg-surface-container-low px-space-md py-space-sm font-code-audit text-code-audit text-on-surface-variant">
        SESSION HASH: {sessionHash} • {timestamp}
      </p>
    </div>
  );
}
