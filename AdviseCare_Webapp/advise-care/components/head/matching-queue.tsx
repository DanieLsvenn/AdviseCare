"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import { ApiError, isRecord } from "@/lib/api-contract";
import { clientApiGet, clientApiRequest } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { daysSince, formatDateTime } from "@/lib/format";
import { listOf, str, toCase, type CaseRecord } from "@/components/case/case-api";
import { Avatar } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import { useConfirm } from "@/components/ui/confirm-dialog";
import { controlClass } from "@/components/ui/field";
import { Icon } from "@/components/ui/icon";
import { Notice } from "@/components/ui/notice";
import { PageHeader } from "@/components/ui/page-header";
import { EmptyState, ErrorState, SkeletonRows } from "@/components/ui/states";
import { StatusValue } from "@/components/ui/status-value";
import { getInitials } from "@/lib/initials";

interface Suggestion {
  advisorId: string;
  fullName: string;
  email: string;
  facultyName: string;
  specialisedSubjectCodes: string[];
  isSubjectSpecialist: boolean;
  isGeneralAdvisor: boolean;
  isSameFaculty: boolean;
  isFamiliarAdvisor: boolean;
  openCaseCount: number;
  matchReasons: string[];
}

const PRIORITY_ORDER: Record<string, number> = { urgent: 0, high: 1, medium: 2, low: 3 };

/**
 * Programme Head's main screen (SRS "Match cases to advisors"): the queue of
 * unassigned requests on the left, oldest and most urgent first; the chosen
 * request and the API's suggested advisors on the right, each with why they
 * fit and how many open cases they already hold. Assigning is one click and
 * a confirmation.
 */
export function MatchingQueue() {
  const t = useTranslations("ui.matching");
  const tu = useTranslations("ui");
  const tw = useTranslations("workspace");
  const { confirm, element: confirmDialog } = useConfirm();
  const [queue, setQueue] = useState<CaseRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [suggestions, setSuggestions] = useState<Suggestion[]>([]);
  const [suggestionsLoading, setSuggestionsLoading] = useState(false);
  const [suggestionsError, setSuggestionsError] = useState("");
  const [note, setNote] = useState("");
  const [assigning, setAssigning] = useState<string | null>(null);
  const [done, setDone] = useState<{ student: string; advisor: string } | null>(null);
  const [assignError, setAssignError] = useState("");

  const loadQueue = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const payload = await clientApiGet<unknown>("Cases/matching-queue");
      const items = listOf(payload)
        .map(toCase)
        .sort(
          (a, b) =>
            (PRIORITY_ORDER[a.priority] ?? 9) - (PRIORITY_ORDER[b.priority] ?? 9) ||
            a.openedAt.localeCompare(b.openedAt),
        );
      setQueue(items);
      setSelectedId((current) =>
        current && items.some((item) => item.caseId === current) ? current : (items[0]?.caseId ?? null),
      );
    } catch {
      setError(tw("apiErrorNetwork"));
    } finally {
      setLoading(false);
    }
  }, [tw]);

  useEffect(() => {
    const timer = window.setTimeout(() => void loadQueue(), 0);
    return () => window.clearTimeout(timer);
  }, [loadQueue]);

  useEffect(() => {
    if (!selectedId) return;
    let active = true;
    const timer = window.setTimeout(() => {
      setSuggestionsLoading(true);
      setSuggestionsError("");
      setAssignError("");
      clientApiGet<unknown>(`Cases/${encodeURIComponent(selectedId)}/suggested-advisors`)
        .then((payload) => {
          if (!active) return;
          setSuggestions(
            listOf(payload).map((record) => ({
              advisorId: str(record.advisorId),
              fullName: str(record.fullName),
              email: str(record.email),
              facultyName: str(record.facultyName),
              specialisedSubjectCodes: Array.isArray(record.specialisedSubjectCodes)
                ? record.specialisedSubjectCodes.map(str)
                : [],
              isSubjectSpecialist: record.isSubjectSpecialist === true,
              isGeneralAdvisor: record.isGeneralAdvisor === true,
              isSameFaculty: record.isSameFaculty === true,
              isFamiliarAdvisor: record.isFamiliarAdvisor === true,
              openCaseCount: typeof record.openCaseCount === "number" ? record.openCaseCount : 0,
              matchReasons: Array.isArray(record.matchReasons) ? record.matchReasons.map(str) : [],
            })),
          );
        })
        .catch(() => {
          if (active) setSuggestionsError(tw("apiErrorNetwork"));
        })
        .finally(() => {
          if (active) setSuggestionsLoading(false);
        });
    }, 0);
    return () => {
      active = false;
      window.clearTimeout(timer);
    };
  }, [selectedId, tw]);

  const selected = queue.find((item) => item.caseId === selectedId) ?? null;
  const oldestDays = useMemo(
    () => queue.reduce((max, item) => Math.max(max, daysSince(item.openedAt) ?? 0), 0),
    [queue],
  );
  const maxLoad = Math.max(1, ...suggestions.map((item) => item.openCaseCount));

  async function assign(suggestion: Suggestion) {
    if (!selected) return;
    const ok = await confirm({
      title: t("confirmTitle", { advisor: suggestion.fullName }),
      body: t("confirmBody", { student: selected.studentName }),
      confirmLabel: t("assign"),
      cancelLabel: tu("cancel"),
    });
    if (!ok) return;
    setAssigning(suggestion.advisorId);
    setAssignError("");
    try {
      const response = await clientApiRequest<unknown>(
        `Cases/${encodeURIComponent(selected.caseId)}/assign`,
        {
          method: "PUT",
          body: { advisorId: suggestion.advisorId, ...(note.trim() ? { reason: note.trim() } : {}) },
          redirectOnUnauthorized: true,
        },
      );
      if (isRecord(response) && response.isSuccess === false) {
        throw new ApiError({
          message: str(response.message) || tw("actionFailed"),
          status: 400,
          code: "assign_failed",
        });
      }
      setDone({ student: selected.studentName, advisor: suggestion.fullName });
      setNote("");
      setQueue((current) => current.filter((item) => item.caseId !== selected.caseId));
      const next = queue.find((item) => item.caseId !== selected.caseId);
      setSelectedId(next?.caseId ?? null);
    } catch (cause) {
      setAssignError(
        cause instanceof ApiError && cause.status < 500 && cause.message
          ? cause.message
          : tw("apiErrorNetwork"),
      );
    } finally {
      setAssigning(null);
    }
  }

  return (
    <div className="flex flex-col gap-space-lg">
      <PageHeader
        title={t("title")}
        description={
          loading || error
            ? t("description")
            : queue.length
              ? t("finding", { count: queue.length, days: oldestDays })
              : t("description")
        }
        actions={
          <Button icon="refresh" variant="secondary" onClick={() => void loadQueue()} disabled={loading}>
            {tw("refresh")}
          </Button>
        }
      />

      {done ? (
        <Notice
          tone="ontrack"
          icon="check_circle"
          title={t("assigned", done)}
          actions={
            <Button size="sm" variant="tertiary" onClick={() => setDone(null)} aria-label={tu("close")} icon="close" />
          }
        >
          {t("assignedBody")}
        </Notice>
      ) : null}

      <div className="grid gap-space-lg lg:grid-cols-[minmax(0,22rem)_minmax(0,1fr)] xl:grid-cols-[minmax(0,26rem)_minmax(0,1fr)] lg:items-start">
        {/* Queue */}
        <section aria-label={t("queue")} className="overflow-hidden rounded-lg border border-rule bg-paper lg:sticky lg:top-20">
          <header className="flex items-center justify-between border-b border-rule px-space-base py-space-md">
            <h2 className="text-base font-semibold text-text">{t("queue")}</h2>
            {!loading && !error ? (
              <span className="text-[0.8125rem] text-text-faint">{t("waiting", { count: queue.length })}</span>
            ) : null}
          </header>
          {error ? (
            <ErrorState title={error} action={<Button size="sm" variant="secondary" onClick={() => void loadQueue()}>{tu("retry")}</Button>} />
          ) : loading ? (
            <SkeletonRows rows={5} columns={2} label={tw("loading")} />
          ) : queue.length === 0 ? (
            <EmptyState icon="done_all" title={t("emptyTitle")}>
              {t("emptyBody")}
            </EmptyState>
          ) : (
            <ul className="max-h-[calc(100dvh-14rem)] divide-y divide-rule overflow-y-auto">
              {queue.map((item) => {
                const isSelected = item.caseId === selectedId;
                const waited = daysSince(item.openedAt) ?? 0;
                return (
                  <li key={item.caseId}>
                    <button
                      type="button"
                      aria-current={isSelected ? "true" : undefined}
                      onClick={() => setSelectedId(item.caseId)}
                      className={cn(
                        "flex w-full flex-col gap-1 px-space-base py-space-md text-left transition-colors",
                        isSelected ? "bg-ink-wash" : "hover:bg-paper-tint",
                      )}
                    >
                      <span className="flex items-center justify-between gap-space-sm">
                        <span className="truncate font-semibold text-text">{item.studentName}</span>
                        <StatusValue field="priority" value={item.priority} size="sm" />
                      </span>
                      <span className="truncate text-sm text-text-muted">
                        {item.categoryName}
                        {item.subjectCode ? ` · ${item.subjectCode}` : ""}
                      </span>
                      <span className="flex items-center justify-between text-[0.8125rem] text-text-faint">
                        <span className="font-mono">{item.studentCode}</span>
                        <span className={cn(waited >= 2 && "font-semibold text-status-attention-text")}>
                          {waited === 0 ? t("today") : t("waitedDays", { count: waited })}
                        </span>
                      </span>
                    </button>
                  </li>
                );
              })}
            </ul>
          )}
        </section>

        {/* Selected request */}
        {selected ? (
          <div className="flex min-w-0 flex-col gap-space-lg">
            <section className="rounded-lg border border-rule bg-paper p-space-lg">
              <div className="flex flex-wrap items-start justify-between gap-space-md">
                <div className="min-w-0">
                  <h2 className="text-xl font-semibold text-text">{selected.studentName}</h2>
                  <p className="mt-0.5 text-sm text-text-muted">
                    <span className="font-mono">{selected.studentCode}</span>
                    {selected.studentEmail ? ` · ${selected.studentEmail}` : ""}
                  </p>
                </div>
                <StatusValue field="priority" value={selected.priority} />
              </div>
              <dl className="mt-space-base grid gap-space-md border-t border-rule pt-space-base sm:grid-cols-3">
                <div>
                  <dt className="text-[0.8125rem] text-text-faint">{t("type")}</dt>
                  <dd className="mt-0.5 text-sm font-semibold text-text">
                    {tu(`status.${selected.supportType.toLowerCase()}`)}
                  </dd>
                </div>
                <div>
                  <dt className="text-[0.8125rem] text-text-faint">{t("category")}</dt>
                  <dd className="mt-0.5 text-sm font-semibold text-text">
                    {selected.categoryName}
                    {selected.subjectName ? (
                      <span className="block font-normal text-text-muted">
                        {selected.subjectCode} — {selected.subjectName}
                      </span>
                    ) : null}
                  </dd>
                </div>
                <div>
                  <dt className="text-[0.8125rem] text-text-faint">{t("sent")}</dt>
                  <dd className="mt-0.5 font-mono text-[0.8125rem] text-text">{formatDateTime(selected.openedAt)}</dd>
                </div>
              </dl>
              <div className="mt-space-base border-t border-rule pt-space-base">
                <p className="text-[0.8125rem] text-text-faint">{t("reason")}</p>
                <p className="mt-1 whitespace-pre-line text-[0.9375rem] leading-7 text-text">{selected.requestReason}</p>
              </div>
            </section>

            <section className="rounded-lg border border-rule bg-paper">
              <header className="flex flex-wrap items-end justify-between gap-space-md border-b border-rule px-space-lg py-space-md">
                <div>
                  <h2 className="text-base font-semibold text-text">{t("suggestions")}</h2>
                  <p className="text-[0.8125rem] text-text-faint">{t("suggestionsHint")}</p>
                </div>
              </header>
              <div className="border-b border-rule bg-paper-tint/60 px-space-lg py-space-md">
                <label htmlFor="assign-note" className="mb-1.5 block text-sm font-semibold text-text">
                  {t("note")}
                </label>
                <textarea
                  id="assign-note"
                  className={cn(controlClass, "min-h-12 w-full py-2")}
                  maxLength={2000}
                  placeholder={t("notePlaceholder")}
                  value={note}
                  onChange={(event) => setNote(event.target.value)}
                />
                {assignError ? <Notice className="mt-space-md" tone="urgent" icon="error" title={assignError} /> : null}
              </div>
              {suggestionsError ? (
                <ErrorState title={suggestionsError} />
              ) : suggestionsLoading ? (
                <SkeletonRows rows={3} columns={4} label={tw("loading")} />
              ) : suggestions.length === 0 ? (
                <EmptyState icon="person_search" title={t("noSuggestionsTitle")}>
                  {t("noSuggestionsBody")}
                </EmptyState>
              ) : (
                <ul className="divide-y divide-rule">
                  {suggestions.map((suggestion, index) => (
                    <li key={suggestion.advisorId} className="grid gap-space-md px-space-lg py-space-base sm:grid-cols-[minmax(0,1fr)_9rem_auto] sm:items-center">
                      <div className="flex min-w-0 items-start gap-space-md">
                        <Avatar initials={getInitials(suggestion.fullName)} />
                        <div className="min-w-0">
                          <p className="flex flex-wrap items-center gap-2 font-semibold text-text">
                            {suggestion.fullName}
                            {index === 0 ? (
                              <span className="rounded-full bg-ink px-2 text-[0.75rem] font-semibold leading-5 text-white">
                                {t("bestFit")}
                              </span>
                            ) : null}
                          </p>
                          <p className="truncate text-[0.8125rem] text-text-faint">
                            {suggestion.facultyName}
                            {suggestion.specialisedSubjectCodes.length
                              ? ` · ${suggestion.specialisedSubjectCodes.join(", ")}`
                              : ""}
                          </p>
                          <ul className="mt-1.5 flex flex-wrap gap-1.5">
                            {reasonTags(suggestion, t).map((reason) => (
                              <li
                                key={reason}
                                className="rounded-md border border-rule bg-paper-tint px-1.5 py-0.5 text-[0.75rem] text-text-muted"
                              >
                                {reason}
                              </li>
                            ))}
                          </ul>
                        </div>
                      </div>
                      <div aria-label={t("openCases", { count: suggestion.openCaseCount })}>
                        <p className="text-[0.8125rem] text-text-muted">
                          <span className="font-mono font-medium text-text">{suggestion.openCaseCount}</span>{" "}
                          {t("openCasesLabel")}
                        </p>
                        <span aria-hidden className="mt-1 block h-1 rounded-full bg-rule">
                          <span
                            className="block h-1 rounded-full bg-text-faint"
                            style={{ width: `${Math.round((suggestion.openCaseCount / maxLoad) * 100)}%` }}
                          />
                        </span>
                      </div>
                      <Button
                        variant={index === 0 ? "primary" : "secondary"}
                        loading={assigning === suggestion.advisorId}
                        disabled={assigning !== null}
                        onClick={() => void assign(suggestion)}
                      >
                        {t("assign")}
                      </Button>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </div>
        ) : !loading && queue.length === 0 ? null : (
          <div className="hidden rounded-lg border border-dashed border-rule-strong p-space-xl text-center text-sm text-text-faint lg:block">
            <Icon name="arrow_back" className="mr-1 align-middle text-[18px]" />
            {t("pick")}
          </div>
        )}
      </div>
      {confirmDialog}
    </div>
  );
}

function reasonTags(
  suggestion: Suggestion,
  t: ReturnType<typeof useTranslations<"ui.matching">>,
): string[] {
  if (suggestion.matchReasons.length) return suggestion.matchReasons;
  const tags: string[] = [];
  if (suggestion.isFamiliarAdvisor) tags.push(t("reasonFamiliar"));
  if (suggestion.isSubjectSpecialist) tags.push(t("reasonSpecialist"));
  if (suggestion.isGeneralAdvisor) tags.push(t("reasonGeneral"));
  if (suggestion.isSameFaculty) tags.push(t("reasonFaculty"));
  return tags;
}
