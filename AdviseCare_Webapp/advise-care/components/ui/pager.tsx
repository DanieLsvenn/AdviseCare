"use client";

import { useId, useState } from "react";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/cn";
import { Button } from "./button";
import { controlClass } from "./field";

const ROWS_PER_PAGE = [10, 20, 50, 100];

/** Pages a list held in memory; the page snaps back when the list shrinks below it. The page size can be changed under the list. */
export function usePaged<T>(items: T[], initialPageSize = 10) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSizeState] = useState(initialPageSize);
  const pageCount = Math.max(1, Math.ceil(items.length / pageSize));
  const current = Math.min(page, pageCount);
  return {
    page: current,
    pageCount,
    pageSize,
    total: items.length,
    items: items.slice((current - 1) * pageSize, current * pageSize),
    setPage,
    setPageSize: (size: number) => {
      setPageSizeState(size);
      setPage(1);
    },
  };
}

/** "Rows per page" choice under a list. */
export function RowsPerPage({ value, onChange }: { value: number; onChange: (size: number) => void }) {
  const tu = useTranslations("ui");
  const id = useId();
  const options = ROWS_PER_PAGE.includes(value) ? ROWS_PER_PAGE : [...ROWS_PER_PAGE, value].sort((a, b) => a - b);
  return (
    <label htmlFor={id} className="flex items-center gap-1.5 text-[0.8125rem] text-text-faint">
      {tu("workspace.rowsPerPage")}
      <select
        id={id}
        className={cn(controlClass, "h-8 w-auto py-0 pr-7 text-[0.8125rem] text-text")}
        value={value}
        onChange={(event) => onChange(Number(event.target.value))}
      >
        {options.map((size) => (
          <option key={size} value={size}>
            {size}
          </option>
        ))}
      </select>
    </label>
  );
}

/** Range, rows per page and previous / next under a list; renders nothing for an empty list. */
export function Pager({
  page,
  pageCount,
  pageSize,
  total,
  setPage,
  setPageSize,
  className,
}: {
  page: number;
  pageCount: number;
  pageSize: number;
  total: number;
  setPage: (page: number) => void;
  setPageSize?: (size: number) => void;
  className?: string;
}) {
  const t = useTranslations("workspace");
  const tu = useTranslations("ui");
  if (total === 0 || (pageCount <= 1 && !setPageSize)) return null;
  const from = (page - 1) * pageSize + 1;
  const to = Math.min(total, page * pageSize);
  return (
    <div className={cn("flex flex-wrap items-center justify-between gap-space-sm", className)}>
      <span className="font-mono text-[0.8125rem] text-text-faint">{tu("workspace.range", { from, to, total })}</span>
      <div className="flex flex-wrap items-center gap-space-sm">
        {setPageSize ? <RowsPerPage value={pageSize} onChange={setPageSize} /> : null}
        <Button size="sm" variant="secondary" icon="chevron_left" disabled={page <= 1} onClick={() => setPage(page - 1)}>
          {t("previous")}
        </Button>
        <Button size="sm" variant="secondary" iconAfter="chevron_right" disabled={page >= pageCount} onClick={() => setPage(page + 1)}>
          {t("next")}
        </Button>
      </div>
    </div>
  );
}
