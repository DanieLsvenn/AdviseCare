"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { clientApiGet, clientApiRequest } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { formatDateTime } from "@/lib/format";
import { apiMessage, flags as toFlags, ruleText, type FlagRecord } from "@/components/take-on/take-on-api";
import { RequestsButton, TakeOnRequestsDrawer, type TakeOnTarget } from "@/components/head/take-on-requests-drawer";
import { SearchPicker } from "@/components/workspace/search-picker";
import { Button } from "@/components/ui/button";
import { Drawer } from "@/components/ui/drawer";
import { controlClass } from "@/components/ui/field";
import { Notice } from "@/components/ui/notice";
import { PageHeader } from "@/components/ui/page-header";
import { Pager, usePaged } from "@/components/ui/pager";
import { usePrompt } from "@/components/ui/prompt-dialog";
import { EmptyState, ErrorState, SkeletonRows } from "@/components/ui/states";
import { StatusValue } from "@/components/ui/status-value";

const STATUS_FILTERS = ["open", "dismissed", "caseOpened", "cleared", "all"] as const;
type StatusFilter = (typeof STATUS_FILTERS)[number];

/**
 * The Programme Head's "Students in need" tab: the early-support flags raised
 * by the server check, most urgent first. Each open flag can be dismissed
 * (it stays in the list as dismissed), turned into a case for an advisor, or
 * handed to one of the advisors who asked for it (Requests pop-up, red dot
 * while requests are waiting). The student's value is never shown.
 */
export function StudentsInNeed() {
  const t = useTranslations("ui.studentsInNeed");
  const tu = useTranslations("ui");
  const tw = useTranslations("workspace");
  const { prompt, element: promptDialog } = usePrompt();
  const [status, setStatus] = useState<StatusFilter>("open");
  const [items, setItems] = useState<FlagRecord[]>([]);
  const paged = usePaged(items, 20);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [flash, setFlash] = useState<{ ok: boolean; message: string } | null>(null);
  const [checking, setChecking] = useState(false);
  const [requestsTarget, setRequestsTarget] = useState<TakeOnTarget | null>(null);
  const [openCaseFor, setOpenCaseFor] = useState<FlagRecord | null>(null);
  const [busy, setBusy] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const payload = await clientApiGet<unknown>("IndicatorFlags/search", {
        query: status === "all" ? {} : { status },
      });
      setItems(toFlags(payload));
    } catch {
      setError(tw("apiErrorNetwork"));
    } finally {
      setLoading(false);
    }
  }, [status, tw]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  async function runCheck() {
    setChecking(true);
    setFlash(null);
    try {
      const result = await clientApiRequest<Record<string, unknown>>("IndicatorFlags/check", { method: "POST" });
      setFlash({
        ok: true,
        message: t("checked", {
          raised: typeof result?.raisedCount === "number" ? result.raisedCount : 0,
          cleared: typeof result?.clearedCount === "number" ? result.clearedCount : 0,
        }),
      });
      await load();
    } catch (cause) {
      setFlash({ ok: false, message: apiMessage(cause, tw("apiErrorNetwork")) });
    } finally {
      setChecking(false);
    }
  }

  async function dismiss(flag: FlagRecord) {
    const reason = await prompt({
      title: t("dismissTitle", { student: flag.studentFullName }),
      body: t("dismissBody"),
      label: t("dismissReason"),
      required: true,
      confirmLabel: t("dismiss"),
      cancelLabel: tu("cancel"),
      destructive: true,
      icon: "do_not_disturb_on",
    });
    if (reason === null) return;
    setBusy(flag.indicatorFlagId);
    setFlash(null);
    try {
      await clientApiRequest(`IndicatorFlags/${encodeURIComponent(flag.indicatorFlagId)}/dismiss`, {
        method: "PUT",
        body: { dismissReason: reason },
      });
      setFlash({ ok: true, message: t("dismissed", { student: flag.studentFullName }) });
      await load();
    } catch (cause) {
      setFlash({ ok: false, message: apiMessage(cause, tw("apiErrorNetwork")) });
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="flex flex-col gap-space-lg">
      <PageHeader
        title={t("title")}
        description={t("description")}
        actions={
          <>
            <Button variant="secondary" icon="refresh" disabled={loading} onClick={() => void load()}>
              {tw("refresh")}
            </Button>
            <Button icon="manage_search" loading={checking} onClick={() => void runCheck()}>
              {t("runCheck")}
            </Button>
          </>
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

      <section className="overflow-hidden rounded-lg border border-rule bg-paper">
        <div className="flex flex-wrap items-center gap-space-md border-b border-rule px-space-lg py-space-md">
          <label htmlFor="flag-status" className="text-[0.8125rem] text-text-faint">
            {t("show")}
          </label>
          <select
            id="flag-status"
            className={cn(controlClass, "h-9 w-auto pr-8")}
            value={status}
            onChange={(event) => setStatus(event.target.value as StatusFilter)}
          >
            {STATUS_FILTERS.map((value) => (
              <option key={value} value={value}>
                {t(`filter.${value}`)}
              </option>
            ))}
          </select>
          <span className="ml-auto text-[0.8125rem] text-text-faint">{t("order")}</span>
        </div>

        {error ? (
          <ErrorState title={error} action={<Button size="sm" variant="secondary" onClick={() => void load()}>{tu("retry")}</Button>} />
        ) : loading ? (
          <SkeletonRows rows={5} columns={4} label={tw("loading")} />
        ) : items.length === 0 ? (
          <EmptyState icon="verified" title={t("emptyTitle")}>
            {t("emptyBody")}
          </EmptyState>
        ) : (
          <ul className="divide-y divide-rule">
            {paged.items.map((flag) => (
              <li key={flag.indicatorFlagId} className="flex flex-wrap items-start gap-x-space-lg gap-y-space-md px-space-lg py-space-base">
                <FlagSummary flag={flag} />
                <div className="flex flex-wrap items-center gap-space-sm">
                  {flag.status === "open" ? (
                    <>
                      <RequestsButton
                        count={flag.pendingTakeOnRequestCount}
                        label={t("requests")}
                        onClick={() =>
                          setRequestsTarget({
                            kind: "flag",
                            id: flag.indicatorFlagId,
                            studentName: flag.studentFullName,
                            title: `${flag.indicatorName} · ${ruleText(flag)}`,
                          })
                        }
                      />
                      <Button size="sm" variant="secondary" icon="note_add" disabled={busy !== null} onClick={() => setOpenCaseFor(flag)}>
                        {t("openCase")}
                      </Button>
                      <Button size="sm" variant="tertiary" icon="do_not_disturb_on" loading={busy === flag.indicatorFlagId} disabled={busy !== null} onClick={() => void dismiss(flag)}>
                        {t("dismiss")}
                      </Button>
                    </>
                  ) : (
                    <FlagOutcome flag={flag} />
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
        <Pager {...paged} className="border-t border-rule px-space-lg py-space-sm" />
      </section>

      <TakeOnRequestsDrawer
        target={requestsTarget}
        onClose={() => setRequestsTarget(null)}
        onChanged={() => void load()}
      />
      <OpenCaseDrawer
        flag={openCaseFor}
        onClose={() => setOpenCaseFor(null)}
        onDone={(message) => {
          setOpenCaseFor(null);
          setFlash({ ok: true, message });
          void load();
        }}
      />
      {promptDialog}
    </div>
  );
}

/** Who was flagged, by which rule, for which term and subject; never the value. */
export function FlagSummary({ flag }: { flag: FlagRecord }) {
  const t = useTranslations("ui.studentsInNeed");
  return (
    <div className="min-w-0 flex-1 basis-80">
      <p className="flex flex-wrap items-center gap-2">
        <span className="font-semibold text-text">{flag.studentFullName}</span>
        <span className="font-mono text-[0.8125rem] text-text-faint">{flag.studentCode}</span>
        <StatusValue field="severity" value={flag.severity} size="sm" emphasize />
        <StatusValue field="priority" value={flag.priority} size="sm" />
        {flag.status !== "open" ? <StatusValue field="status" value={flag.status} size="sm" /> : null}
      </p>
      <p className="mt-0.5 text-sm text-text">
        <span className="font-semibold">{flag.indicatorName}</span>
        <span className="text-text-muted"> · {ruleText(flag)}</span>
      </p>
      <p className="mt-0.5 text-[0.8125rem] text-text-muted">
        {[flag.termName, flag.subjectCode ? `${flag.subjectCode} — ${flag.subjectName}` : "", flag.categoryName, flag.facultyName]
          .filter(Boolean)
          .join(" · ")}
      </p>
      <p className="mt-0.5 font-mono text-[0.75rem] text-text-faint">{t("flaggedAt", { date: formatDateTime(flag.flaggedAt) })}</p>
    </div>
  );
}

function FlagOutcome({ flag }: { flag: FlagRecord }) {
  const t = useTranslations("ui.studentsInNeed");
  if (flag.status === "dismissed") {
    return (
      <p className="max-w-sm text-[0.8125rem] text-text-muted">
        {t("dismissedBy", { name: flag.resolvedByFullName, date: formatDateTime(flag.resolvedAt) })}
        {flag.dismissReason ? <span className="block text-text">{flag.dismissReason}</span> : null}
      </p>
    );
  }
  if (flag.status === "caseOpened") {
    return <p className="text-[0.8125rem] text-text-muted">{t("caseOpenedBy", { name: flag.resolvedByFullName, date: formatDateTime(flag.resolvedAt) })}</p>;
  }
  return <p className="text-[0.8125rem] text-text-muted">{t("clearedAt", { date: formatDateTime(flag.resolvedAt) })}</p>;
}

/** Open a case from a flag for an advisor of the rule's support type. */
function OpenCaseDrawer({
  flag,
  onClose,
  onDone,
}: {
  flag: FlagRecord | null;
  onClose: () => void;
  onDone: (message: string) => void;
}) {
  const t = useTranslations("ui.studentsInNeed");
  const tu = useTranslations("ui");
  const tw = useTranslations("workspace");
  const [advisorId, setAdvisorId] = useState("");
  const [reason, setReason] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const role = flag?.supportType === "wellbeing" ? "Wellbeing Advisor" : "Academic Advisor";

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!flag || !advisorId) return;
    setPending(true);
    setError("");
    try {
      await clientApiRequest(`IndicatorFlags/${encodeURIComponent(flag.indicatorFlagId)}/open-case`, {
        method: "PUT",
        body: { advisorId, reason: reason.trim() },
      });
      setAdvisorId("");
      setReason("");
      onDone(t("caseOpened", { student: flag.studentFullName }));
    } catch (cause) {
      setError(apiMessage(cause, tw("apiErrorNetwork")));
    } finally {
      setPending(false);
    }
  }

  return (
    <Drawer
      open={flag !== null}
      onClose={() => {
        setError("");
        onClose();
      }}
      title={t("openCaseTitle", { student: flag?.studentFullName ?? "" })}
      description={flag ? `${flag.indicatorName} · ${ruleText(flag)}` : undefined}
      closeLabel={tu("close")}
    >
      {flag ? (
        <form className="flex flex-col gap-space-md" onSubmit={(event) => void submit(event)}>
          <p className="text-sm text-text-muted">{t("openCaseBody")}</p>
          <div>
            <label htmlFor="open-case-advisor" className="mb-1 block text-sm font-semibold text-text">
              {t("advisor")}
            </label>
            <SearchPicker
              id="open-case-advisor"
              source={{
                path: "Accounts/staff/search",
                valueKey: "accountId",
                labelKeys: ["fullName"],
                detailKeys: ["email"],
                searchKeys: ["fullName", "email"],
                query: { roleNames: role },
              }}
              value={advisorId}
              onChange={setAdvisorId}
              required
            />
            <p className="mt-1 text-[0.75rem] text-text-faint">{t("advisorHint", { role })}</p>
          </div>
          <div>
            <label htmlFor="open-case-reason" className="mb-1 block text-sm font-semibold text-text">
              {t("reasonOptional")}
            </label>
            <textarea
              id="open-case-reason"
              maxLength={1000}
              className={cn(controlClass, "min-h-20 w-full py-2")}
              value={reason}
              onChange={(event) => setReason(event.target.value)}
            />
          </div>
          {error ? <Notice tone="urgent" icon="error" title={error} /> : null}
          <Button type="submit" icon="note_add" loading={pending} disabled={!advisorId}>
            {t("openCase")}
          </Button>
        </form>
      ) : null}
    </Drawer>
  );
}
