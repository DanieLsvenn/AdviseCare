"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { clientApiGet } from "@/lib/client-api";
import { ApiError } from "@/lib/api-contract";
import { daysSince, formatDateTime } from "@/lib/format";
import {
  byDate,
  listOf,
  signerRole,
  type SignerRole,
  toCase,
  toHistory,
  unwrap,
  type CaseRecord,
  type StageHistoryRecord,
} from "@/components/case/case-api";
import { CaseProgress } from "@/components/case/case-progress";
import { Avatar } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import { Icon } from "@/components/ui/icon";
import { Ledger } from "@/components/ui/ledger";
import { EmptyState, ErrorState, SkeletonRows } from "@/components/ui/states";
import { StatusValue } from "@/components/ui/status-value";
import { getInitials } from "@/lib/initials";

/**
 * A student's own case: where it stands, who is looking after it and how to
 * reach them, what they asked, and the signed history of every step.
 * Case notes are staff-only (SRS) and never shown here.
 */
export function StudentCase({ caseId }: { caseId: string }) {
  const t = useTranslations("ui");
  const tw = useTranslations("workspace");
  const [record, setRecord] = useState<CaseRecord | null>(null);
  const [history, setHistory] = useState<StageHistoryRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [notFound, setNotFound] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const [casePayload, historyPayload] = await Promise.all([
        clientApiGet<unknown>(`Cases/${encodeURIComponent(caseId)}`),
        clientApiGet<unknown>("CaseStageHistories/search", { query: { caseId } }),
      ]);
      const data = unwrap(casePayload);
      setRecord(data ? toCase(data) : null);
      setHistory(byDate(listOf(historyPayload).map(toHistory), (item) => item.changedAt));
    } catch (cause) {
      if (cause instanceof ApiError && cause.status === 404) setNotFound(true);
      else setError(tw("apiErrorNetwork"));
    } finally {
      setLoading(false);
    }
  }, [caseId, tw]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  const back = (
    <Link href="/me/cases" className="inline-flex items-center gap-1 text-sm font-semibold text-ink hover:underline">
      <Icon name="arrow_back" className="text-[18px]" />
      {t("case.backToRequests")}
    </Link>
  );

  if (loading) {
    return (
      <div className="flex flex-col gap-space-lg">
        {back}
        <div className="rounded-lg border border-rule bg-paper">
          <SkeletonRows rows={4} columns={3} label={tw("loading")} />
        </div>
      </div>
    );
  }
  if (notFound || !record) {
    return (
      <div className="flex flex-col gap-space-lg">
        {back}
        <div className="rounded-lg border border-rule bg-paper">
          {error ? (
            <ErrorState title={error} action={<Button size="sm" variant="secondary" onClick={() => void load()}>{t("retry")}</Button>} />
          ) : (
            <EmptyState icon="search_off" title={t("case.notFoundTitle")}>
              {t("case.notFoundBody")}
            </EmptyState>
          )}
        </div>
      </div>
    );
  }

  const matched = Boolean(record.advisorName);

  return (
    <div className="flex flex-col gap-space-lg">
      {back}

      <header className="rounded-lg border border-rule bg-paper p-space-lg">
        <div className="flex flex-wrap items-start justify-between gap-space-md">
          <div className="min-w-0">
            <p className="text-sm text-text-muted">
              {t(`status.${record.supportType.toLowerCase()}`)}
              {record.subjectName ? ` · ${record.subjectCode ? `${record.subjectCode} — ` : ""}${record.subjectName}` : ""}
            </p>
            <h1 className="mt-0.5 text-2xl font-semibold leading-8 text-text">{record.categoryName}</h1>
            <p className="mt-1 text-[0.8125rem] text-text-faint">
              {t.rich("case.sentOn", {
                date: formatDateTime(record.openedAt),
                mono: (chunks) => <span className="font-mono">{chunks}</span>,
              })}
            </p>
          </div>
          <StatusValue field="currentStage" value={record.currentStage} />
        </div>
        <CaseProgress className="mt-space-lg" stage={record.currentStage} />
      </header>

      <div className="grid gap-space-lg md:grid-cols-[minmax(0,1fr)_18rem]">
        <section className="rounded-lg border border-rule bg-paper p-space-lg">
          <h2 className="text-base font-semibold text-text">{t("case.yourRequest")}</h2>
          <p className="mt-space-sm whitespace-pre-line text-[0.9375rem] leading-7 text-text">
            {record.requestReason}
          </p>
        </section>

        <aside className="rounded-lg border border-rule bg-paper p-space-lg">
          <h2 className="text-base font-semibold text-text">{t("case.yourAdvisor")}</h2>
          {matched ? (
            <div className="mt-space-md">
              <div className="flex items-center gap-space-md">
                <Avatar initials={getInitials(record.advisorName)} />
                <div className="min-w-0">
                  <p className="truncate font-semibold text-text">{record.advisorName}</p>
                  {record.advisorEmail ? (
                    <a
                      href={`mailto:${record.advisorEmail}`}
                      className="block truncate text-sm text-ink hover:underline"
                    >
                      {record.advisorEmail}
                    </a>
                  ) : null}
                </div>
              </div>
              <p className="mt-space-md text-[0.8125rem] leading-5 text-text-muted">{t("case.meetHint")}</p>
            </div>
          ) : (
            <div className="mt-space-sm">
              <p className="text-sm font-semibold text-text">
                {t("case.waitingFor", { count: daysSince(record.openedAt) ?? 0 })}
              </p>
              <p className="mt-1 text-sm leading-6 text-text-muted">{t("case.waitingMatch")}</p>
            </div>
          )}
        </aside>
      </div>

      <section className="rounded-lg border border-rule bg-paper p-space-lg">
        <h2 className="mb-space-md text-base font-semibold text-text">{t("case.history")}</h2>
        {history.length ? (
          <Ledger
            labels={{
              date: t("ledger.date"),
              entry: t("ledger.entry"),
              signature: t("ledger.signature"),
              unsigned: t("ledger.unsigned"),
            }}
            entries={history.map((item) => ({
              id: item.caseStageHistoryId || `${item.changedAt}-${item.toStage}`,
              at: item.changedAt,
              title:
                item.fromStage === item.toStage
                  ? t(`case.event.${item.toStage === "pending" ? "opened" : "updated"}`)
                  : t("case.event.moved", {
                      to: t(`status.${item.toStage.toLowerCase()}`),
                    }),
              body: item.reason,
              signed: true,
              signer: {
                name: item.changedByName,
                role: roleLabel(signerRole(item, record, "student"), t),
              },
            }))}
          />
        ) : (
          <p className="text-sm text-text-muted">{t("case.noHistory")}</p>
        )}
      </section>
    </div>
  );
}

function roleLabel(
  role: SignerRole,
  t: ReturnType<typeof useTranslations<"ui">>,
): string | undefined {
  return role ? t(`signer.${role}`) : undefined;
}
