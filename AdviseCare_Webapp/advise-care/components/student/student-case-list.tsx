"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { clientApiGet } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { daysSince, formatDate } from "@/lib/format";
import { listOf, toCase, type CaseRecord } from "@/components/case/case-api";
import { CaseProgress } from "@/components/case/case-progress";
import { Button, ButtonLink } from "@/components/ui/button";
import { Icon } from "@/components/ui/icon";
import { EmptyState, ErrorState, SkeletonRows } from "@/components/ui/states";
import { StatusValue } from "@/components/ui/status-value";

/**
 * The student's own contact book: one page (card) per request, open ones
 * first. Each answers "where is it and who has it?" at a glance, on a phone.
 * No filter bar: a student has a handful of requests, not a dataset.
 */
export function StudentCaseList() {
  const t = useTranslations("ui.myRequests");
  const tu = useTranslations("ui");
  const tw = useTranslations("workspace");
  const [cases, setCases] = useState<CaseRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showClosed, setShowClosed] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const payload = await clientApiGet<unknown>("Cases/me");
      setCases(
        listOf(payload)
          .map(toCase)
          .sort((a, b) => b.openedAt.localeCompare(a.openedAt)),
      );
    } catch {
      setError(tw("apiErrorNetwork"));
    } finally {
      setLoading(false);
    }
  }, [tw]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  const open = cases.filter((item) => item.status !== "closed" && item.currentStage !== "closed");
  const closed = cases.filter((item) => !open.includes(item));

  return (
    <div className="flex flex-col gap-space-lg">
      <header className="flex flex-wrap items-end justify-between gap-space-md">
        <div className="min-w-0">
          <h1 className="text-2xl font-semibold leading-8 text-text">{t("title")}</h1>
          <p className="mt-1 max-w-[60ch] text-[0.9375rem] leading-6 text-text-muted">{t("description")}</p>
        </div>
        <ButtonLink href="/me/support-request" icon="add">
          {t("newRequest")}
        </ButtonLink>
      </header>

      {loading ? (
        <div className="rounded-lg border border-rule bg-paper">
          <SkeletonRows rows={3} columns={3} label={tw("loading")} />
        </div>
      ) : error ? (
        <div className="rounded-lg border border-rule bg-paper">
          <ErrorState
            title={error}
            action={
              <Button size="sm" variant="secondary" onClick={() => void load()}>
                {tu("retry")}
              </Button>
            }
          />
        </div>
      ) : cases.length === 0 ? (
        <div className="rounded-lg border border-rule bg-paper">
          <EmptyState
            icon="edit_note"
            title={t("emptyTitle")}
            action={
              <ButtonLink href="/me/support-request" icon="add">
                {t("newRequest")}
              </ButtonLink>
            }
          >
            {t("emptyBody")}
          </EmptyState>
        </div>
      ) : (
        <>
          <section aria-labelledby="open-requests">
            <h2 id="open-requests" className="mb-space-sm text-sm font-semibold text-text-muted">
              {t("openHeading", { count: open.length })}
            </h2>
            {open.length ? (
              <ul className="flex flex-col gap-space-sm">
                {open.map((item) => (
                  <RequestCard key={item.caseId} item={item} />
                ))}
              </ul>
            ) : (
              <p className="rounded-lg border border-dashed border-rule-strong px-space-base py-space-md text-sm text-text-muted">
                {t("noneOpen")}
              </p>
            )}
          </section>

          {closed.length ? (
            <section aria-labelledby="closed-requests">
              <button
                id="closed-requests"
                type="button"
                aria-expanded={showClosed}
                onClick={() => setShowClosed(!showClosed)}
                className="flex min-h-11 items-center gap-1.5 text-sm font-semibold text-ink hover:underline"
              >
                <Icon
                  name="expand_more"
                  className={cn("text-[20px] transition-transform", showClosed && "rotate-180")}
                />
                {showClosed ? t("hideClosed") : t("showClosed", { count: closed.length })}
              </button>
              {showClosed ? (
                <ul className="mt-space-sm flex flex-col gap-space-sm">
                  {closed.map((item) => (
                    <RequestCard key={item.caseId} item={item} />
                  ))}
                </ul>
              ) : null}
            </section>
          ) : null}
        </>
      )}
    </div>
  );
}

function RequestCard({ item }: { item: CaseRecord }) {
  const t = useTranslations("ui.myRequests");
  const tu = useTranslations("ui");
  const waited = daysSince(item.openedAt) ?? 0;
  const matched = Boolean(item.advisorName);
  const isClosed = item.currentStage === "closed";

  return (
    <li>
      <Link
        href={`/me/cases/${item.caseId}`}
        className="group block rounded-lg border border-rule bg-paper p-space-base transition-colors hover:border-ink focus-visible:border-ink"
      >
        <div className="flex items-start justify-between gap-space-md">
          <div className="min-w-0">
            <p className="font-semibold leading-6 text-text">{item.categoryName}</p>
            <p className="text-sm text-text-muted">
              {tu(`status.${item.supportType.toLowerCase()}`)}
              {item.subjectCode ? ` · ${item.subjectCode}` : ""}
            </p>
          </div>
          <StatusValue field="currentStage" value={item.currentStage} size="sm" />
        </div>

        <CaseProgress compact stage={item.currentStage} className="mt-space-md" />

        <div className="mt-space-md flex items-center justify-between gap-space-md text-sm">
          <p className={cn("min-w-0 truncate", matched ? "text-text" : "text-text-muted")}>
            {isClosed
              ? t("closedOn", { date: formatDate(item.closedAt ?? item.openedAt) })
              : matched
                ? t("withAdvisor", { name: item.advisorName })
                : waited === 0
                  ? t("waitingToday")
                  : t("waitingDays", { count: waited })}
          </p>
          <Icon
            name="chevron_right"
            className="shrink-0 text-[20px] text-rule-strong transition-colors group-hover:text-ink"
          />
        </div>
      </Link>
    </li>
  );
}
