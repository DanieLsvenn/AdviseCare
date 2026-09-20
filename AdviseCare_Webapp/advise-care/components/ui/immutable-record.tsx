import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import { Icon } from "./icon";
import { StatusChip } from "./status-chip";

/**
 * Design Rule §4.4 — bản ghi đã commit không có nút sửa/xoá. Dải viền trái 3px
 * và ổ khoá là dấu hiệu thị giác của tính bất biến; mọi đính chính phải đi qua
 * một addendum có audit marker riêng.
 */
export function ImmutableRecord({
  title,
  typeLabel,
  author,
  timestamp,
  hash,
  children,
  footer,
  supersedes,
  className,
}: {
  title: ReactNode;
  typeLabel?: string;
  author: string;
  timestamp: string;
  hash: string;
  children: ReactNode;
  footer?: ReactNode;
  supersedes?: string | null;
  className?: string;
}) {
  return (
    <article
      className={cn(
        "border-l-[3px] border-l-outline-variant border-y border-r border-outline-variant/40 bg-surface-container-lowest",
        className,
      )}
    >
      <header className="flex flex-wrap items-start justify-between gap-space-sm border-b border-outline-variant/25 px-space-base py-space-md">
        <div className="flex min-w-0 flex-wrap items-center gap-space-sm">
          <h3 className="font-label-md text-label-md font-semibold text-on-surface">
            {title}
          </h3>
          <StatusChip
            tone="sealed"
            icon="lock"
            size="sm"
            label="Committed Record — No Edit / No Delete"
          />
          {supersedes ? (
            <StatusChip
              tone="referral"
              icon="history"
              size="sm"
              label={`Supersedes ${supersedes}`}
            />
          ) : null}
        </div>
        <time className="font-code-audit text-code-audit text-on-surface-variant">
          {timestamp}
        </time>
      </header>

      <div className="px-space-base py-space-md font-body-base text-body-base text-on-surface">
        {typeLabel ? (
          <p className="mb-space-sm font-caption text-caption uppercase tracking-wider text-on-surface-variant">
            {typeLabel}
          </p>
        ) : null}
        {children}
      </div>

      <footer className="flex flex-wrap items-center justify-between gap-space-sm border-t border-outline-variant/25 bg-surface-container-low/40 px-space-base py-space-sm">
        <p className="flex items-center gap-1.5 font-body-sm text-body-sm text-on-surface-variant">
          <Icon name="badge" className="text-[14px]" />
          <span>
            Author: <strong className="font-semibold text-on-surface">{author}</strong>
          </span>
        </p>
        <div className="flex flex-wrap items-center gap-space-md">
          {footer}
          <span className="font-code-audit text-code-audit text-on-surface-variant">
            Hash: {hash.slice(0, 6)}…{hash.slice(-4)}
          </span>
        </div>
      </footer>
    </article>
  );
}
