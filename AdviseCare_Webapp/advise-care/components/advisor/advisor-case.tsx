"use client";

import { useCallback, useEffect, useRef, useState, type FormEvent, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { ApiError, isRecord, type ApiMethod } from "@/lib/api-contract";
import { clientApiGet, clientApiRequest } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { formatDateTime } from "@/lib/format";
import {
  byDate,
  listOf,
  signerRole,
  str,
  toCase,
  toHistory,
  toNote,
  unwrap,
  type CaseNoteRecord,
  type CaseRecord,
  type StageHistoryRecord,
} from "@/components/case/case-api";
import { CaseProgress } from "@/components/case/case-progress";
import { Button } from "@/components/ui/button";
import { useConfirm } from "@/components/ui/confirm-dialog";
import { controlClass } from "@/components/ui/field";
import { Icon } from "@/components/ui/icon";
import { Ledger } from "@/components/ui/ledger";
import { Notice } from "@/components/ui/notice";
import { EmptyState, ErrorState, SkeletonRows } from "@/components/ui/states";
import { StatusValue } from "@/components/ui/status-value";

type Send = (
  path: string,
  method: ApiMethod,
  body: Record<string, unknown> | undefined,
  success: string,
) => Promise<boolean>;

/**
 * The advisor's working page for one case (SRS: view case detail, record
 * notes and agreed actions, advance stages, refer, request an advisor
 * change). Left: the request, the notes and the stage history, all as signed
 * ledger entries. Right: what the advisor can do next.
 */
export function AdvisorCase({ caseId }: { caseId: string }) {
  const t = useTranslations("ui.advisorCase");
  const tu = useTranslations("ui");
  const tw = useTranslations("workspace");
  const { confirm, element: confirmDialog } = useConfirm();
  const [record, setRecord] = useState<CaseRecord | null>(null);
  const [history, setHistory] = useState<StageHistoryRecord[]>([]);
  const [notes, setNotes] = useState<CaseNoteRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [missing, setMissing] = useState(false);
  const [flash, setFlash] = useState<{ ok: boolean; message: string } | null>(null);
  // Entries signed by the last action get their signature line drawn in.
  const knownIds = useRef<Set<string> | null>(null);
  const [freshIds, setFreshIds] = useState<Set<string>>(new Set());

  const load = useCallback(async () => {
    setError("");
    try {
      const [casePayload, historyPayload, notesPayload] = await Promise.all([
        clientApiGet<unknown>(`Cases/${encodeURIComponent(caseId)}`),
        clientApiGet<unknown>("CaseStageHistories/search", { query: { caseId } }),
        clientApiGet<unknown>("CaseNotes/search", { query: { caseId } }).catch(() => []),
      ]);
      const data = unwrap(casePayload);
      setRecord(data ? toCase(data) : null);
      const nextHistory = byDate(listOf(historyPayload).map(toHistory), (item) => item.changedAt);
      const nextNotes = byDate(listOf(notesPayload).map(toNote), (item) => item.createdAt);
      const signedIds = [
        ...nextHistory.map((item) => `h:${item.caseStageHistoryId || item.changedAt}`),
        ...nextNotes.filter((note) => note.isFinalized).map((note) => `n:${note.caseNoteId}`),
      ];
      if (knownIds.current) {
        const previous = knownIds.current;
        const fresh = new Set(signedIds.filter((id) => !previous.has(id)));
        setFreshIds(fresh);
        if (fresh.size) window.setTimeout(() => setFreshIds(new Set()), 4000);
      }
      knownIds.current = new Set(signedIds);
      setHistory(nextHistory);
      setNotes(nextNotes);
    } catch (cause) {
      if (cause instanceof ApiError && cause.status === 404) setMissing(true);
      else setError(tw("apiErrorNetwork"));
    } finally {
      setLoading(false);
    }
  }, [caseId, tw]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  const send: Send = async (path, method, body, success) => {
    setFlash(null);
    try {
      const response = await clientApiRequest<unknown>(path, {
        method,
        body,
        redirectOnUnauthorized: true,
      });
      if (isRecord(response) && response.isSuccess === false) {
        setFlash({ ok: false, message: str(response.message) || tw("actionFailed") });
        return false;
      }
      setFlash({ ok: true, message: success });
      await load();
      return true;
    } catch (cause) {
      setFlash({
        ok: false,
        message:
          cause instanceof ApiError && cause.status < 500 && cause.message
            ? cause.message
            : tw("apiErrorNetwork"),
      });
      return false;
    }
  };

  const back = (
    <Link href="/advisor/caseload" className="inline-flex items-center gap-1 text-sm font-semibold text-ink hover:underline">
      <Icon name="arrow_back" className="text-[18px]" />
      {t("back")}
    </Link>
  );

  if (loading || missing || !record) {
    return (
      <div className="flex flex-col gap-space-lg">
        {back}
        <div className="rounded-lg border border-rule bg-paper">
          {loading ? (
            <SkeletonRows rows={5} columns={3} label={tw("loading")} />
          ) : error ? (
            <ErrorState title={error} action={<Button size="sm" variant="secondary" onClick={() => void load()}>{tu("retry")}</Button>} />
          ) : (
            <EmptyState icon="search_off" title={t("notFoundTitle")}>
              {t("notFoundBody")}
            </EmptyState>
          )}
        </div>
      </div>
    );
  }

  const closed = record.currentStage === "closed";
  const ledgerLabels = {
    date: tu("ledger.date"),
    entry: tu("ledger.entry"),
    signature: tu("ledger.signature"),
    unsigned: tu("ledger.unsigned"),
    justSigned: tu("ledger.justSigned"),
  };

  return (
    <div className="flex flex-col gap-space-lg">
      {back}

      <header className="rounded-lg border border-rule bg-paper p-space-lg">
        <div className="flex flex-wrap items-start justify-between gap-space-md">
          <div className="min-w-0">
            <h1 className="text-2xl font-semibold leading-8 text-text">{record.studentName}</h1>
            <p className="mt-0.5 text-sm text-text-muted">
              <span className="font-mono">{record.studentCode}</span>
              {record.studentEmail ? (
                <>
                  {" · "}
                  <a className="text-ink hover:underline" href={`mailto:${record.studentEmail}`}>
                    {record.studentEmail}
                  </a>
                </>
              ) : null}
            </p>
            <p className="mt-space-sm text-[0.9375rem] text-text">
              <span className="font-semibold">{record.categoryName}</span>
              <span className="text-text-muted">
                {" · "}
                {tu(`status.${record.supportType.toLowerCase()}`)}
                {record.subjectName ? ` · ${record.subjectCode} — ${record.subjectName}` : ""}
              </span>
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-space-sm">
            <StatusValue field="priority" value={record.priority} />
            <StatusValue field="currentStage" value={record.currentStage} />
          </div>
        </div>
        <CaseProgress className="mt-space-lg" stage={record.currentStage} />
      </header>

      {flash ? (
        <Notice
          tone={flash.ok ? "ontrack" : "urgent"}
          icon={flash.ok ? "check_circle" : "error"}
          title={flash.message}
          actions={<Button size="sm" variant="tertiary" icon="close" aria-label={tu("close")} onClick={() => setFlash(null)} />}
        />
      ) : null}

      {/* Act before record: on narrow screens the next step comes right after
          the request; from 1024px the actions are a sticky right rail. */}
      <div className="grid gap-space-lg lg:grid-cols-[minmax(0,1fr)_22rem] lg:grid-rows-[auto_auto_1fr] lg:[grid-template-areas:'req_rail'_'notes_rail'_'hist_rail']">
        <section className="rounded-lg border border-rule bg-paper p-space-lg lg:[grid-area:req]">
            <div className="flex flex-wrap items-baseline justify-between gap-space-sm">
              <h2 className="text-base font-semibold text-text">{t("request")}</h2>
              <span className="font-mono text-[0.8125rem] text-text-faint">{formatDateTime(record.openedAt)}</span>
            </div>
            <p className="mt-space-sm whitespace-pre-line text-[0.9375rem] leading-7 text-text">{record.requestReason}</p>
            <p className="mt-space-md text-[0.8125rem] text-text-faint">
              {t("openedBy", { name: record.openedByName || record.studentName })}
            </p>
          </section>

        <aside
          aria-label={t("nextSteps")}
          className="flex flex-col gap-space-md lg:sticky lg:top-20 lg:[grid-area:rail] lg:self-start"
        >
          {closed ? (
            <ActionCard icon="lock_open" title={t("reopen")} hint={t("reopenHint")} defaultOpen>
              <ReasonForm
                submitLabel={t("reopen")}
                onSubmit={(reason) =>
                  send(`Cases/${record.caseId}/reopen`, "PUT", { reason }, t("reopened"))
                }
              />
            </ActionCard>
          ) : (
            <ActionCard icon="trending_flat" title={t("moveStage")} hint={t("moveStageHint")} defaultOpen>
              <StageForm
                current={record.currentStage}
                confirmFirst={(toStage) =>
                  toStage === "closed"
                    ? confirm({
                        title: t("closeConfirmTitle"),
                        body: t("closeConfirmBody"),
                        confirmLabel: t("closeCase"),
                        cancelLabel: tu("cancel"),
                        icon: "lock",
                      })
                    : Promise.resolve(true)
                }
                onSubmit={async (toStage, reason) => {
                  return send(
                    `Cases/${record.caseId}/stage`,
                    "PUT",
                    { toStage, reason },
                    t("stageMoved", { stage: tu(`status.${toStage.toLowerCase()}`) }),
                  );
                }}
              />
            </ActionCard>
          )}

          {/* Distill: one obvious next step; everything else behind "More actions". */}
          <details className="group/more rounded-lg border border-rule bg-paper">
            <summary className="flex min-h-12 cursor-pointer list-none items-center gap-space-md px-space-base py-space-sm">
              <Icon name="more_horiz" className="text-[20px] text-text-muted" />
              <span className="min-w-0 flex-1">
                <span className="block text-sm font-semibold text-text">{t("moreActions")}</span>
                <span className="block truncate text-[0.75rem] text-text-faint">{t("moreActionsHint")}</span>
              </span>
              <Icon name="expand_more" className="text-[20px] text-text-faint transition-transform group-open/more:rotate-180" />
            </summary>
            <div className="divide-y divide-rule border-t border-rule">
          <ActionCard plain icon="flag" title={t("priority")}>
            <PriorityForm
              current={record.priority}
              onSubmit={(priority) =>
                send(`Cases/${record.caseId}/priority`, "PUT", { priority }, t("priorityChanged"))
              }
            />
          </ActionCard>

          {!closed ? (
            <ActionCard plain icon="forward_to_inbox" title={t("refer")} hint={t("referHint")}>
              <ReferForm
                onSubmit={(toAccountId, referralReason, priority) =>
                  send(
                    "Referrals",
                    "POST",
                    { caseId: record.caseId, toAccountId, referralReason, priority },
                    t("referred"),
                  )
                }
              />
            </ActionCard>
          ) : null}

          {!closed ? (
            <ActionCard plain icon="swap_horiz" title={t("changeAdvisor")} hint={t("changeAdvisorHint")}>
              <ChangeAdvisorForm
                onSubmit={(proposedAdvisorId, reason) =>
                  send(
                    "CaseChangeRequests",
                    "POST",
                    { caseId: record.caseId, proposedAdvisorId, reason },
                    t("changeRequested"),
                  )
                }
              />
            </ActionCard>
          ) : null}

              <Link
                href={`/advisor/students/${record.studentId}`}
                className="flex min-h-12 items-center gap-space-md px-space-base py-space-sm text-sm font-semibold text-ink transition-colors hover:bg-paper-tint"
              >
                <Icon name="lab_profile" className="text-[20px]" />
                <span className="flex-1">{t("studentData")}</span>
                <Icon name="chevron_right" className="text-[20px]" />
              </Link>
            </div>
          </details>
          <p className="px-1 text-[0.75rem] leading-5 text-text-faint">{t("studentDataHint")}</p>
        </aside>
          <NotesSection
            className="lg:[grid-area:notes]"
            caseId={record.caseId}
            notes={notes}
            send={send}
            confirm={confirm}
            labels={ledgerLabels}
            closed={closed}
            freshIds={freshIds}
          />

          <section className="rounded-lg border border-rule bg-paper p-space-lg lg:[grid-area:hist]">
            <h2 className="mb-space-md text-base font-semibold text-text">{t("history")}</h2>
            {history.length ? (
              <Ledger
                labels={ledgerLabels}
                entries={history.map((item) => ({
                  id: item.caseStageHistoryId || `${item.changedAt}-${item.toStage}`,
                  at: item.changedAt,
                  title:
                    item.fromStage === item.toStage
                      ? item.toStage === "pending"
                        ? tu("case.event.opened")
                        : tu("case.event.updated")
                      : tu("case.event.moved", { to: tu(`status.${item.toStage.toLowerCase()}`) }),
                  body: item.reason,
                  signed: true,
                  signer: {
                    name: item.changedByName,
                    role: (() => {
                      const role = signerRole(item, record, "staff");
                      return role ? tu(`signer.${role}`) : undefined;
                    })(),
                  },
                  fresh: freshIds.has(`h:${item.caseStageHistoryId || item.changedAt}`),
                }))}
              />
            ) : (
              <p className="text-sm text-text-muted">{tu("case.noHistory")}</p>
            )}
          </section>
      </div>
      {confirmDialog}
    </div>
  );
}

/* ---------------------------------------------------------------------- */

function NotesSection({
  caseId,
  notes,
  send,
  confirm,
  labels,
  closed,
  freshIds,
  className,
}: {
  className?: string;
  caseId: string;
  notes: CaseNoteRecord[];
  send: Send;
  confirm: ReturnType<typeof useConfirm>["confirm"];
  labels: { date: string; entry: string; signature: string; unsigned: string; justSigned?: string };
  closed: boolean;
  freshIds: Set<string>;
}) {
  const t = useTranslations("ui.advisorCase");
  const tu = useTranslations("ui");
  const [draft, setDraft] = useState("");
  const [saving, setSaving] = useState(false);
  const [editing, setEditing] = useState<{ id: string; text: string } | null>(null);

  async function addNote(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!draft.trim()) return;
    setSaving(true);
    const ok = await send("CaseNotes", "POST", { caseId, noteText: draft.trim() }, t("noteSaved"));
    setSaving(false);
    if (ok) setDraft("");
  }

  async function finalize(note: CaseNoteRecord) {
    const ok = await confirm({
      title: t("finalizeTitle"),
      body: t("finalizeBody"),
      confirmLabel: t("finalize"),
      cancelLabel: tu("cancel"),
      icon: "draw",
    });
    if (ok) await send(`CaseNotes/${note.caseNoteId}/finalize`, "PUT", undefined, t("noteFinalized"));
  }

  return (
    <section className={cn("rounded-lg border border-rule bg-paper p-space-lg", className)}>
      <div className="mb-space-md flex flex-wrap items-baseline justify-between gap-space-sm">
        <h2 className="text-base font-semibold text-text">{t("notes")}</h2>
        <span className="text-[0.8125rem] text-text-faint">{t("notesPrivate")}</span>
      </div>

      {notes.length ? (
        <Ledger
          labels={labels}
          entries={notes.map((note) => {
            const isEditing = editing?.id === note.caseNoteId;
            return {
              id: note.caseNoteId,
              at: note.createdAt,
              body: isEditing ? (
                <textarea
                  aria-label={t("editNote")}
                  className={cn(controlClass, "min-h-28 w-full py-2 text-sm leading-6 text-text")}
                  maxLength={8000}
                  value={editing.text}
                  onChange={(event) => setEditing({ id: note.caseNoteId, text: event.target.value })}
                />
              ) : (
                <span className="text-text">{note.noteText}</span>
              ),
              signed: note.isFinalized,
              signer: { name: note.authorName, role: tu("signer.advisor") },
              fresh: freshIds.has(`n:${note.caseNoteId}`),
              actions: note.isFinalized ? undefined : isEditing ? (
                <>
                  <Button
                    size="sm"
                    onClick={async () => {
                      const ok = await send(
                        `CaseNotes/${note.caseNoteId}`,
                        "PUT",
                        { noteText: editing.text.trim() },
                        t("noteSaved"),
                      );
                      if (ok) setEditing(null);
                    }}
                  >
                    {t("saveDraft")}
                  </Button>
                  <Button size="sm" variant="tertiary" onClick={() => setEditing(null)}>
                    {tu("cancel")}
                  </Button>
                </>
              ) : (
                <>
                  <Button size="sm" variant="secondary" icon="draw" onClick={() => void finalize(note)}>
                    {t("finalize")}
                  </Button>
                  <Button size="sm" variant="tertiary" icon="edit" onClick={() => setEditing({ id: note.caseNoteId, text: note.noteText })}>
                    {t("editNote")}
                  </Button>
                </>
              ),
            };
          })}
        />
      ) : (
        <p className="text-sm text-text-muted">{t("noNotes")}</p>
      )}

      {!closed ? (
        <form onSubmit={addNote} className="mt-space-lg border-t border-rule pt-space-base">
          <label htmlFor="new-note" className="mb-1.5 block text-sm font-semibold text-text">
            {t("addNote")}
          </label>
          <textarea
            id="new-note"
            className={cn(controlClass, "min-h-28 w-full py-2 leading-6")}
            maxLength={8000}
            placeholder={t("notePlaceholder")}
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
          />
          <div className="mt-space-sm flex flex-wrap items-center justify-between gap-space-sm">
            <p className="text-[0.8125rem] text-text-faint">{t("draftHint")}</p>
            <Button type="submit" loading={saving} disabled={!draft.trim()} icon="note_add">
              {t("saveAsDraft")}
            </Button>
          </div>
        </form>
      ) : null}
    </section>
  );
}

function ActionCard({
  icon,
  title,
  hint,
  defaultOpen,
  plain,
  children,
}: {
  icon: string;
  title: string;
  hint?: string;
  defaultOpen?: boolean;
  /** Part of a grouped list: no box of its own. */
  plain?: boolean;
  children: ReactNode;
}) {
  return (
    <details open={defaultOpen} className={cn("group", plain ? "" : "rounded-lg border border-rule bg-paper")}>
      <summary className="flex min-h-12 cursor-pointer list-none items-center gap-space-md px-space-base py-space-sm">
        <Icon name={icon} className="text-[20px] text-ink" />
        <span className="flex-1 text-sm font-semibold text-text">{title}</span>
        <Icon name="expand_more" className="text-[20px] text-text-faint transition-transform group-open:rotate-180" />
      </summary>
      <div className="border-t border-rule px-space-base pb-space-base pt-space-md">
        {hint ? <p className="mb-space-md text-[0.8125rem] leading-5 text-text-muted">{hint}</p> : null}
        {children}
      </div>
    </details>
  );
}

function Label({ htmlFor, children }: { htmlFor: string; children: ReactNode }) {
  return (
    <label htmlFor={htmlFor} className="mb-1 block text-[0.8125rem] font-semibold text-text">
      {children}
    </label>
  );
}

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** Account IDs are typed until the API offers a staff directory: catch typos before sending. */
function idError(value: string, message: string): string | undefined {
  const trimmed = value.trim();
  return trimmed && !GUID.test(trimmed) ? message : undefined;
}

function useSubmit() {
  const [pending, setPending] = useState(false);
  async function run(task: () => Promise<boolean>) {
    setPending(true);
    try {
      return await task();
    } finally {
      setPending(false);
    }
  }
  return { pending, run };
}

const NEXT_STAGES: Record<string, string[]> = {
  assigned: ["contacted", "followUp", "closed"],
  contacted: ["followUp", "closed"],
  followUp: ["contacted", "closed"],
  referred: ["contacted", "followUp", "closed"],
};

function StageForm({
  current,
  confirmFirst,
  onSubmit,
}: {
  current: string;
  /** Asked before the button shows any pending state. */
  confirmFirst: (toStage: string) => Promise<boolean>;
  onSubmit: (toStage: string, reason: string) => Promise<boolean>;
}) {
  const t = useTranslations("ui.advisorCase");
  const tu = useTranslations("ui");
  const options = NEXT_STAGES[current] ?? ["contacted", "followUp", "closed"];
  const [toStage, setToStage] = useState(options[0] ?? "contacted");
  const [reason, setReason] = useState("");
  const { pending, run } = useSubmit();

  return (
    <form
      className="flex flex-col gap-space-md"
      onSubmit={async (event) => {
        event.preventDefault();
        if (!(await confirmFirst(toStage))) return;
        if (await run(() => onSubmit(toStage, reason.trim()))) setReason("");
      }}
    >
      <div role="radiogroup" aria-label={t("moveTo")} className="flex flex-wrap gap-1.5">
        {options.map((stage) => (
          <label
            key={stage}
            className={cn(
              "flex cursor-pointer items-center rounded-md border px-2.5 py-1.5 text-sm transition-colors pointer-coarse:min-h-11 pointer-coarse:px-3.5",
              toStage === stage ? "border-ink bg-ink-wash font-semibold text-ink" : "border-rule-strong text-text-muted hover:border-text-faint",
            )}
          >
            <input type="radio" className="sr-only" name="toStage" value={stage} checked={toStage === stage} onChange={() => setToStage(stage)} />
            {tu(`status.${stage.toLowerCase()}`)}
          </label>
        ))}
      </div>
      <div>
        <Label htmlFor="stage-reason">{t("whatHappened")}</Label>
        <textarea id="stage-reason" required maxLength={2000} className={cn(controlClass, "min-h-20 w-full py-2")} value={reason} onChange={(event) => setReason(event.target.value)} />
      </div>
      <Button type="submit" loading={pending} disabled={!reason.trim()}>
        {t("moveTo")} {tu(`status.${toStage.toLowerCase()}`).toLocaleLowerCase()}
      </Button>
    </form>
  );
}

function ReasonForm({ submitLabel, onSubmit }: { submitLabel: string; onSubmit: (reason: string) => Promise<boolean> }) {
  const t = useTranslations("ui.advisorCase");
  const [reason, setReason] = useState("");
  const { pending, run } = useSubmit();
  return (
    <form
      className="flex flex-col gap-space-md"
      onSubmit={async (event) => {
        event.preventDefault();
        if (await run(() => onSubmit(reason.trim()))) setReason("");
      }}
    >
      <div>
        <Label htmlFor="reopen-reason">{t("reason")}</Label>
        <textarea id="reopen-reason" required maxLength={2000} className={cn(controlClass, "min-h-20 w-full py-2")} value={reason} onChange={(event) => setReason(event.target.value)} />
      </div>
      <Button type="submit" loading={pending} disabled={!reason.trim()}>
        {submitLabel}
      </Button>
    </form>
  );
}

const PRIORITIES = ["low", "medium", "high", "urgent"];

function PriorityForm({ current, onSubmit }: { current: string; onSubmit: (priority: string) => Promise<boolean> }) {
  const t = useTranslations("ui.advisorCase");
  const tu = useTranslations("ui");
  const [priority, setPriority] = useState(current);
  const { pending, run } = useSubmit();
  return (
    <form
      className="flex items-end gap-space-sm"
      onSubmit={async (event) => {
        event.preventDefault();
        await run(() => onSubmit(priority));
      }}
    >
      <div className="flex-1">
        <Label htmlFor="priority">{t("priority")}</Label>
        <select id="priority" className={cn(controlClass, "h-9 w-full")} value={priority} onChange={(event) => setPriority(event.target.value)}>
          {PRIORITIES.map((value) => (
            <option key={value} value={value}>
              {tu(`status.${value}`)}
            </option>
          ))}
        </select>
      </div>
      <Button type="submit" variant="secondary" loading={pending} disabled={priority === current}>
        {t("save")}
      </Button>
    </form>
  );
}

function ReferForm({
  onSubmit,
}: {
  onSubmit: (toAccountId: string, reason: string, priority: string) => Promise<boolean>;
}) {
  const t = useTranslations("ui.advisorCase");
  const tu = useTranslations("ui");
  const [receiver, setReceiver] = useState("");
  const [reason, setReason] = useState("");
  const [priority, setPriority] = useState("medium");
  const { pending, run } = useSubmit();
  return (
    <form
      className="flex flex-col gap-space-md"
      onSubmit={async (event) => {
        event.preventDefault();
        if (await run(() => onSubmit(receiver.trim(), reason.trim(), priority))) {
          setReceiver("");
          setReason("");
        }
      }}
    >
      <div>
        <Label htmlFor="refer-to">{t("receiver")}</Label>
        <input
          id="refer-to"
          required
          spellCheck={false}
          autoComplete="off"
          aria-invalid={idError(receiver, "x") ? true : undefined}
          aria-describedby="refer-to-hint"
          placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
          className={cn(controlClass, "h-9 w-full font-mono text-[0.8125rem]")}
          value={receiver}
          onChange={(event) => setReceiver(event.target.value)}
        />
        <p id="refer-to-hint" className={cn("mt-1 text-[0.75rem]", idError(receiver, "x") ? "text-red-ink" : "text-text-faint")}>
          {idError(receiver, t("idInvalid")) ?? t("receiverHint")}
        </p>
      </div>
      <div>
        <Label htmlFor="refer-reason">{t("reason")}</Label>
        <textarea id="refer-reason" required maxLength={4000} className={cn(controlClass, "min-h-20 w-full py-2")} value={reason} onChange={(event) => setReason(event.target.value)} />
      </div>
      <div>
        <Label htmlFor="refer-priority">{t("priority")}</Label>
        <select id="refer-priority" className={cn(controlClass, "h-9 w-full")} value={priority} onChange={(event) => setPriority(event.target.value)}>
          {PRIORITIES.map((value) => (
            <option key={value} value={value}>
              {tu(`status.${value}`)}
            </option>
          ))}
        </select>
      </div>
      <Button type="submit" variant="secondary" loading={pending} disabled={!receiver.trim() || !reason.trim() || Boolean(idError(receiver, "x"))}>
        {t("sendReferral")}
      </Button>
    </form>
  );
}

function ChangeAdvisorForm({ onSubmit }: { onSubmit: (advisorId: string, reason: string) => Promise<boolean> }) {
  const t = useTranslations("ui.advisorCase");
  const [advisor, setAdvisor] = useState("");
  const [reason, setReason] = useState("");
  const { pending, run } = useSubmit();
  return (
    <form
      className="flex flex-col gap-space-md"
      onSubmit={async (event) => {
        event.preventDefault();
        if (await run(() => onSubmit(advisor.trim(), reason.trim()))) {
          setAdvisor("");
          setReason("");
        }
      }}
    >
      <div>
        <Label htmlFor="proposed-advisor">{t("proposedAdvisor")}</Label>
        <input
          id="proposed-advisor"
          required
          spellCheck={false}
          autoComplete="off"
          aria-invalid={idError(advisor, "x") ? true : undefined}
          aria-describedby="proposed-advisor-hint"
          placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
          className={cn(controlClass, "h-9 w-full font-mono text-[0.8125rem]")}
          value={advisor}
          onChange={(event) => setAdvisor(event.target.value)}
        />
        <p id="proposed-advisor-hint" className={cn("mt-1 text-[0.75rem]", idError(advisor, "x") ? "text-red-ink" : "text-text-faint")}>
          {idError(advisor, t("idInvalid")) ?? t("receiverHint")}
        </p>
      </div>
      <div>
        <Label htmlFor="change-reason">{t("reason")}</Label>
        <textarea id="change-reason" required maxLength={4000} className={cn(controlClass, "min-h-20 w-full py-2")} value={reason} onChange={(event) => setReason(event.target.value)} />
      </div>
      <Button type="submit" variant="secondary" loading={pending} disabled={!advisor.trim() || !reason.trim() || Boolean(idError(advisor, "x"))}>
        {t("sendChangeRequest")}
      </Button>
    </form>
  );
}
