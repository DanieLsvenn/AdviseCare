import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import { formatDate, formatTime } from "@/lib/format";

export interface LedgerEntry {
  id: string;
  /** ISO timestamp of when the entry was written. */
  at: string;
  title?: ReactNode;
  body?: ReactNode;
  /** Chips or small facts shown above the body. */
  tags?: ReactNode;
  /** Who signed the entry. Omit (or set signed=false) for drafts. */
  signer?: { name: string; role?: string };
  signed: boolean;
  /** Controls available on this entry (only ever on unsigned drafts). */
  actions?: ReactNode;
  /** Just signed in this session: the signature line is drawn in. */
  fresh?: boolean;
}

/**
 * The signature pattern of the "Sổ liên lạc" system: every append-only history
 * is set in the contact book's three columns — Date | Entry | Signature.
 * A signed entry has no edit affordance; a draft shows an empty, dashed
 * signature line until it is finalized.
 */
export function Ledger({
  entries,
  labels,
  className,
}: {
  entries: LedgerEntry[];
  labels: { date: string; entry: string; signature: string; unsigned: string; justSigned?: string };
  className?: string;
}) {
  return (
    <div className={cn("@container text-sm", className)}>
      <div
        aria-hidden
        className="hidden grid-cols-[6.5rem_minmax(0,1fr)_12rem] gap-space-lg border-b border-rule-strong pb-1.5 text-[0.75rem] font-semibold text-text-faint @lg:grid"
      >
        <span>{labels.date}</span>
        <span>{labels.entry}</span>
        <span>{labels.signature}</span>
      </div>
      <ol className="divide-y divide-rule">
        {entries.map((entry) => (
          <li
            key={entry.id}
            className={cn(
              "grid gap-x-space-lg gap-y-1.5 py-space-md @lg:grid-cols-[6.5rem_minmax(0,1fr)_12rem]",
              entry.fresh && "pb-space-lg",
            )}
          >
            <time
              dateTime={entry.at}
              className="font-mono text-[0.8125rem] leading-5 text-text-muted"
            >
              <span className="text-text">{formatDate(entry.at)}</span>
              <span className="ml-2 @lg:ml-0 @lg:block">{formatTime(entry.at)}</span>
            </time>

            <div className="min-w-0">
              {entry.title ? (
                <p className="font-semibold leading-5 text-text">{entry.title}</p>
              ) : null}
              {entry.tags ? (
                <div className="mt-1 flex flex-wrap items-center gap-1.5">{entry.tags}</div>
              ) : null}
              {entry.body ? (
                <div
                  className={cn(
                    "whitespace-pre-line break-words leading-6 text-text-muted",
                    (entry.title || entry.tags) && "mt-1",
                  )}
                >
                  {entry.body}
                </div>
              ) : null}
              {entry.actions ? (
                <div className="mt-space-sm flex flex-wrap gap-space-sm">{entry.actions}</div>
              ) : null}
            </div>

            <div className="@lg:pt-0.5">
              {entry.signed && entry.signer ? (
                <div className="relative inline-block min-w-40 pb-1 @lg:block">
                  <p className="truncate font-semibold leading-5 text-text">{entry.signer.name}</p>
                  {entry.signer.role ? (
                    <p className="truncate text-[0.75rem] leading-4 text-text-faint">
                      {entry.signer.role}
                    </p>
                  ) : null}
                  <span
                    aria-hidden
                    className={cn(
                      "absolute inset-x-0 bottom-0 h-[1.5px] origin-left rounded-full bg-ink",
                      entry.fresh && "animate-sign",
                    )}
                  />
                  {entry.fresh && labels.justSigned ? (
                    <p
                      role="status"
                      className="absolute left-0 top-full mt-1 flex animate-signed-note items-center gap-1 text-[0.75rem] font-semibold text-ink"
                    >
                      {labels.justSigned}
                    </p>
                  ) : null}
                </div>
              ) : (
                <div className="inline-block min-w-40 border-b border-dashed border-rule-strong pb-1 @lg:block">
                  <p className="text-[0.8125rem] italic leading-5 text-text-faint">
                    {labels.unsigned}
                  </p>
                  {entry.signer ? (
                    <p className="truncate text-[0.75rem] leading-4 text-text-faint">
                      {entry.signer.name}
                    </p>
                  ) : null}
                </div>
              )}
            </div>
          </li>
        ))}
      </ol>
    </div>
  );
}
