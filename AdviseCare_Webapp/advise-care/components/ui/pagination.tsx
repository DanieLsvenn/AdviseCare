import { cn } from "@/lib/cn";
import { Icon } from "./icon";

export function Pagination({
  page,
  totalPages,
  summary,
  perPage,
  className,
}: {
  page: number;
  totalPages: number;
  summary: string;
  perPage?: number;
  className?: string;
}) {
  const pages = [1, 2, 3].filter((item) => item <= totalPages);

  return (
    <div
      className={cn(
        "flex flex-wrap items-center justify-between gap-space-md px-space-lg py-space-md font-body-sm text-body-sm text-on-surface-variant",
        className,
      )}
    >
      <p className="flex items-center gap-space-sm">
        <span>{summary}</span>
        {perPage ? (
          <span className="flex items-center gap-1.5">
            <span>Per page:</span>
            <span className="rounded border border-outline-variant/60 bg-surface-container-lowest px-space-sm py-0.5 font-label-md text-label-md text-on-surface">
              {perPage}
            </span>
          </span>
        ) : null}
      </p>
      <nav className="flex items-center gap-space-xs" aria-label="Pagination">
        <span className="flex size-7 items-center justify-center rounded text-on-surface-variant">
          <Icon name="chevron_left" className="text-[18px]" />
        </span>
        {pages.map((item) => (
          <span
            key={item}
            aria-current={item === page ? "page" : undefined}
            className={cn(
              "flex size-7 items-center justify-center rounded font-label-md text-label-md",
              item === page
                ? "bg-primary font-semibold text-on-primary"
                : "text-on-surface-variant",
            )}
          >
            {item}
          </span>
        ))}
        {totalPages > 4 ? <span className="px-1">…</span> : null}
        {totalPages > 3 ? (
          <span className="flex size-7 items-center justify-center rounded font-label-md text-label-md text-on-surface-variant">
            {totalPages}
          </span>
        ) : null}
        <span className="flex size-7 items-center justify-center rounded text-on-surface-variant">
          <Icon name="chevron_right" className="text-[18px]" />
        </span>
      </nav>
    </div>
  );
}
