"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/cn";
import { Button } from "./button";

/** Pages a list held in memory; the page snaps back when the list shrinks below it. */
export function usePaged<T>(items: T[], pageSize = 10) {
  const [page, setPage] = useState(1);
  const pageCount = Math.max(1, Math.ceil(items.length / pageSize));
  const current = Math.min(page, pageCount);
  return {
    page: current,
    pageCount,
    pageSize,
    total: items.length,
    items: items.slice((current - 1) * pageSize, current * pageSize),
    setPage,
  };
}

/** Range and previous / next under a list; renders nothing for a single page. */
export function Pager({
  page,
  pageCount,
  pageSize,
  total,
  setPage,
  className,
}: {
  page: number;
  pageCount: number;
  pageSize: number;
  total: number;
  setPage: (page: number) => void;
  className?: string;
}) {
  const t = useTranslations("workspace");
  const tu = useTranslations("ui");
  if (pageCount <= 1) return null;
  const from = (page - 1) * pageSize + 1;
  const to = Math.min(total, page * pageSize);
  return (
    <div className={cn("flex items-center justify-between gap-space-sm", className)}>
      <span className="font-mono text-[0.8125rem] text-text-faint">{tu("workspace.range", { from, to, total })}</span>
      <div className="flex gap-space-sm">
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
