"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { clientApiGet, clientApiRequest } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { formatDateTime } from "@/lib/format";
import {
  apiMessage,
  flags as toFlags,
  takeOnCases,
  takeOnRequests,
  ruleText,
  type FlagRecord,
  type TakeOnCaseRecord,
  type TakeOnRequestRecord,
} from "@/components/take-on/take-on-api";
import { FlagSummary } from "@/components/head/students-in-need";
import { Button } from "@/components/ui/button";
import { useConfirm } from "@/components/ui/confirm-dialog";
import { controlClass } from "@/components/ui/field";
import { Notice } from "@/components/ui/notice";
import { PageHeader } from "@/components/ui/page-header";
import { Pager, usePaged } from "@/components/ui/pager";
import { usePrompt } from "@/components/ui/prompt-dialog";
import { EmptyState, ErrorState, SkeletonRows } from "@/components/ui/states";
import { StatusValue } from "@/components/ui/status-value";
import { TabBar } from "@/components/ui/tabs";

type Tab = "cases" | "flags" | "mine";
const MY_FILTERS = ["", "pending", "approved", "declined", "withdrawn", "cancelled"] as const;

/**
 * The advisor's "Students in need" tab: unassigned cases students created and
 * open early-support flags, both of the advisor's own support type, each with
 * "Ask to take on"; and "My requests" with the Programme Head's decisions.
 * Only directory data, the student's reason and the names of the shared data
 * attributes are shown, never their values.
 */
export function AdvisorStudentsInNeed() {
  const t = useTranslations("ui.takeOn");
  const tu = useTranslations("ui");
  const tw = useTranslations("workspace");
  const { prompt, element: promptDialog } = usePrompt();
  const { confirm, element: confirmDialog } = useConfirm();
  const [tab, setTab] = useState<Tab>("cases");
  const [cases, setCases] = useState<TakeOnCaseRecord[]>([]);
  const [flagItems, setFlagItems] = useState<FlagRecord[]>([]);
  const [mine, setMine] = useState<TakeOnRequestRecord[]>([]);
  const [myFilter, setMyFilter] = useState<(typeof MY_FILTERS)[number]>("");
  const pagedCases = usePaged(cases, 10);
  const pagedFlags = usePaged(flagItems, 10);
  const pagedMine = usePaged(mine, 10);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [flash, setFlash] = useState<{ ok: boolean; message: string } | null>(null);
  const [busy, setBusy] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const [casePayload, flagPayload, minePayload] = await Promise.all([
        clientApiGet<unknown>("Cases/take-on"),
        clientApiGet<unknown>("IndicatorFlags"),
        clientApiGet<unknown>("TakeOnRequests/me", { query: myFilter ? { status: myFilter } : {} }),
      ]);
      setCases(takeOnCases(casePayload));
      setFlagItems(toFlags(flagPayload));
      setMine(takeOnRequests(minePayload));
    } catch {
      setError(tw("apiErrorNetwork"));
    } finally {
      setLoading(false);
    }
  }, [myFilter, tw]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  async function ask(target: { caseId?: string; indicatorFlagId?: string }, student: string, item: string) {
    const note = await prompt({
      title: t("askTitle", { student }),
      body: t("askBody", { item }),
      label: t("note"),
      placeholder: t("notePlaceholder"),
      hint: t("noteHint"),
      confirmLabel: t("send"),
      cancelLabel: tu("cancel"),
      icon: "group_add",
    });
    if (note === null) return;
    setBusy(target.caseId ?? target.indicatorFlagId ?? "");
    setFlash(null);
    try {
      await clientApiRequest("TakeOnRequests", { method: "POST", body: { ...target, note } });
      setFlash({ ok: true, message: t("sent", { student }) });
      await load();
    } catch (cause) {
      setFlash({ ok: false, message: apiMessage(cause, tw("apiErrorNetwork")) });
    } finally {
      setBusy(null);
    }
  }

  async function withdraw(takeOnRequestId: string, student: string) {
    const ok = await confirm({
      title: t("withdrawTitle", { student }),
      body: t("withdrawBody"),
      confirmLabel: t("withdraw"),
      cancelLabel: tu("cancel"),
      destructive: true,
      icon: "undo",
    });
    if (!ok) return;
    setBusy(takeOnRequestId);
    setFlash(null);
    try {
      await clientApiRequest(`TakeOnRequests/${encodeURIComponent(takeOnRequestId)}/withdraw`, { method: "PUT" });
      setFlash({ ok: true, message: t("withdrawn") });
      await load();
    } catch (cause) {
      setFlash({ ok: false, message: apiMessage(cause, tw("apiErrorNetwork")) });
    } finally {
      setBusy(null);
    }
  }

  async function editNote(request: TakeOnRequestRecord) {
    const note = await prompt({
      title: t("editNoteTitle"),
      label: t("note"),
      initialValue: request.note,
      hint: t("noteHint"),
      confirmLabel: t("saveNote"),
      cancelLabel: tu("cancel"),
    });
    if (note === null) return;
    setBusy(request.takeOnRequestId);
    try {
      await clientApiRequest(`TakeOnRequests/${encodeURIComponent(request.takeOnRequestId)}`, { method: "PUT", body: { note } });
      await load();
    } catch (cause) {
      setFlash({ ok: false, message: apiMessage(cause, tw("apiErrorNetwork")) });
    } finally {
      setBusy(null);
    }
  }

  const pendingMine = mine.filter((item) => item.status === "pending").length;

  /** "Ask to take on", or the advisor's own pending request with Withdraw. */
  function action(myRequestId: string | null, myStatus: string, student: string, onAsk: () => void, key: string) {
    if (myRequestId && myStatus === "pending") {
      return (
        <div className="flex flex-wrap items-center gap-space-sm">
          <StatusValue field="status" value="pending" size="sm" emphasize />
          <span className="text-[0.8125rem] text-text-muted">{t("requestSent")}</span>
          <Button size="sm" variant="tertiary" icon="undo" disabled={busy !== null} onClick={() => void withdraw(myRequestId, student)}>
            {t("withdraw")}
          </Button>
        </div>
      );
    }
    return (
      <div className="flex flex-wrap items-center gap-space-sm">
        {myStatus === "declined" ? <span className="text-[0.8125rem] text-text-muted">{t("declinedBefore")}</span> : null}
        <Button size="sm" icon="group_add" loading={busy === key} disabled={busy !== null} onClick={onAsk}>
          {t("ask")}
        </Button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-space-lg">
      <PageHeader
        title={t("advisorTitle")}
        description={t("advisorDescription")}
        actions={
          <Button variant="secondary" icon="refresh" disabled={loading} onClick={() => void load()}>
            {tw("refresh")}
          </Button>
        }
      />
      {flash ? (
        <Notice
          tone={flash.ok ? "ontrack" : "urgent"}
          icon={flash.ok ? "check_circle" : "error"}
          title={flash.message}
          actions={<Button size="sm" variant="tertiary" icon="close" aria-label={tu("close")} onClick={() => setFlash(null)} />}
        />
      ) : null}
      <TabBar
        label={t("advisorTitle")}
        active={tab}
        onChange={(key) => setTab(key as Tab)}
        tabs={[
          { key: "cases", label: t("tabCases"), badge: cases.length },
          { key: "flags", label: t("tabFlags"), badge: flagItems.length },
          { key: "mine", label: t("tabMine"), badge: pendingMine },
        ]}
      />

      <section className="overflow-hidden rounded-lg border border-rule bg-paper">
        {tab === "mine" ? (
          <div className="flex flex-wrap items-center gap-space-md border-b border-rule px-space-lg py-space-md">
            <label htmlFor="my-status" className="text-[0.8125rem] text-text-faint">
              {t("status")}
            </label>
            <select
              id="my-status"
              className={cn(controlClass, "h-9 w-auto pr-8")}
              value={myFilter}
              onChange={(event) => setMyFilter(event.target.value as (typeof MY_FILTERS)[number])}
            >
              {MY_FILTERS.map((value) => (
                <option key={value || "all"} value={value}>
                  {value === "pending" ? t("pending") : value ? tu(`status.${value}`) : t("allStatuses")}
                </option>
              ))}
            </select>
          </div>
        ) : (
          <p className="border-b border-rule px-space-lg py-space-sm text-[0.8125rem] text-text-faint">{t("privacyNote")}</p>
        )}

        {error ? (
          <ErrorState title={error} action={<Button size="sm" variant="secondary" onClick={() => void load()}>{tu("retry")}</Button>} />
        ) : loading ? (
          <SkeletonRows rows={4} columns={3} label={tw("loading")} />
        ) : tab === "cases" ? (
          cases.length === 0 ? (
            <EmptyState icon="done_all" title={t("noCasesTitle")}>
              {t("noCasesBody")}
            </EmptyState>
          ) : (
            <>
              <ul className="divide-y divide-rule">
                {pagedCases.items.map((item) => (
                  <li key={item.caseId} className="flex flex-wrap items-start gap-x-space-lg gap-y-space-md px-space-lg py-space-base">
                    <div className="min-w-0 flex-1 basis-80">
                      <p className="flex flex-wrap items-center gap-2">
                        <span className="font-semibold text-text">{item.studentFullName}</span>
                        <span className="font-mono text-[0.8125rem] text-text-faint">{item.studentCode}</span>
                        <StatusValue field="severity" value={item.severity || "notRated"} size="sm" emphasize />
                        <StatusValue field="priority" value={item.priority} size="sm" />
                      </p>
                      <p className="mt-0.5 text-sm text-text">
                        <span className="font-semibold">{item.categoryName}</span>
                        <span className="text-text-muted">
                          {item.subjectCode ? ` · ${item.subjectCode} — ${item.subjectName}` : ""}
                          {item.facultyName ? ` · ${item.facultyName}` : ""}
                        </span>
                      </p>
                      <p className="mt-1 line-clamp-3 whitespace-pre-line text-sm text-text-muted">{item.requestReason}</p>
                      {item.sharedAttributeNames.length ? (
                        <p className="mt-1.5 flex flex-wrap items-center gap-1.5 text-[0.75rem] text-text-faint">
                          {t("shared")}
                          {item.sharedAttributeNames.map((name) => (
                            <span key={name} className="rounded-md border border-rule bg-paper-tint px-1.5 py-0.5 text-text-muted">
                              {name}
                            </span>
                          ))}
                        </p>
                      ) : null}
                      <p className="mt-1 font-mono text-[0.75rem] text-text-faint">{t("openedAt", { date: formatDateTime(item.openedAt) })}</p>
                    </div>
                    {action(item.myTakeOnRequestId, item.myTakeOnRequestStatus, item.studentFullName, () => void ask({ caseId: item.caseId }, item.studentFullName, item.categoryName), item.caseId)}
                  </li>
                ))}
              </ul>
              <Pager {...pagedCases} className="border-t border-rule px-space-lg py-space-sm" />
            </>
          )
        ) : tab === "flags" ? (
          flagItems.length === 0 ? (
            <EmptyState icon="verified" title={t("noFlagsTitle")}>
              {t("noFlagsBody")}
            </EmptyState>
          ) : (
            <>
              <ul className="divide-y divide-rule">
                {pagedFlags.items.map((flag) => (
                  <li key={flag.indicatorFlagId} className="flex flex-wrap items-start gap-x-space-lg gap-y-space-md px-space-lg py-space-base">
                    <FlagSummary flag={flag} />
                    {action(flag.myTakeOnRequestId, flag.myTakeOnRequestStatus, flag.studentFullName, () => void ask({ indicatorFlagId: flag.indicatorFlagId }, flag.studentFullName, `${flag.indicatorName} · ${ruleText(flag)}`), flag.indicatorFlagId)}
                  </li>
                ))}
              </ul>
              <Pager {...pagedFlags} className="border-t border-rule px-space-lg py-space-sm" />
            </>
          )
        ) : mine.length === 0 ? (
          <EmptyState icon="inbox" title={t("noMineTitle")}>
            {t("noMineBody")}
          </EmptyState>
        ) : (
          <>
            <ul className="divide-y divide-rule">
              {pagedMine.items.map((request) => (
                <li key={request.takeOnRequestId} className="flex flex-wrap items-start gap-x-space-lg gap-y-space-md px-space-lg py-space-base">
                  <div className="min-w-0 flex-1 basis-80">
                    <p className="flex flex-wrap items-center gap-2">
                      <span className="font-semibold text-text">{request.studentFullName}</span>
                      <span className="font-mono text-[0.8125rem] text-text-faint">{request.studentCode}</span>
                      <StatusValue field="status" value={request.status} size="sm" emphasize label={request.status === "pending" ? t("pending") : undefined} />
                    </p>
                    <p className="mt-0.5 text-sm text-text">
                      <span className="text-text-muted">{tu(`status.${request.targetType}`)} · </span>
                      <span className="font-semibold">{request.targetType === "flag" ? request.indicatorName : request.categoryName}</span>
                      <span className="text-text-muted">
                        {[request.termName, request.subjectCode].filter(Boolean).map((part) => ` · ${part}`).join("")}
                      </span>
                    </p>
                    {request.note ? <p className="mt-1 whitespace-pre-line text-sm text-text-muted">{request.note}</p> : null}
                    {request.status === "declined" ? (
                      <p className="mt-1 text-sm text-status-urgent-text">
                        {t("declinedBy", { name: request.decidedByFullName })} {request.declineReason}
                      </p>
                    ) : null}
                    <p className="mt-1 font-mono text-[0.75rem] text-text-faint">
                      {t("sentAt", { date: formatDateTime(request.createdAt) })}
                      {request.decidedAt ? ` · ${t("decidedAt", { date: formatDateTime(request.decidedAt) })}` : ""}
                    </p>
                  </div>
                  {request.status === "pending" ? (
                    <div className="flex flex-wrap gap-space-sm">
                      <Button size="sm" variant="secondary" icon="edit" disabled={busy !== null} onClick={() => void editNote(request)}>
                        {t("editNote")}
                      </Button>
                      <Button size="sm" variant="tertiary" icon="undo" disabled={busy !== null} onClick={() => void withdraw(request.takeOnRequestId, request.studentFullName)}>
                        {t("withdraw")}
                      </Button>
                    </div>
                  ) : null}
                </li>
              ))}
            </ul>
            <Pager {...pagedMine} className="border-t border-rule px-space-lg py-space-sm" />
          </>
        )}
      </section>
      {promptDialog}
      {confirmDialog}
    </div>
  );
}
