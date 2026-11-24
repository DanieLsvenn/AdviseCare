"use client";

import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { ApiError } from "@/lib/api-contract";
import { clientApiGet, clientApiRequest } from "@/lib/client-api";
import { listOf, unwrap } from "@/components/case/case-api";
import {
  addDays,
  durationOptions,
  formatDuration,
  formatLocalDate,
  startTimeOptions,
  toMeetupRequest,
  toMeetupSchedule,
  vietnamToday,
  weekday,
  type MeetupRequestRecord,
  type MeetupScheduleRecord,
} from "@/components/case/meetup-api";
import { Button } from "@/components/ui/button";
import { Field, Select, TextArea, TextInput } from "@/components/ui/field";
import { Icon } from "@/components/ui/icon";
import { StatusChip } from "@/components/ui/status-chip";
import { ErrorState, SkeletonRows } from "@/components/ui/states";
import type { StatusTone } from "@/lib/types";
import { cn } from "@/lib/cn";

const REQUEST_TONES: Record<string, StatusTone> = {
  pending: "attention",
  accepted: "ontrack",
  declined: "urgent",
  cancelled: "sealed",
};
const SCHEDULE_TONES: Record<string, StatusTone> = {
  scheduled: "info",
  completed: "ontrack",
  missed: "urgent",
  cancelled: "sealed",
};

interface SlotDraft {
  meetupDate: string;
  startTime: string;
  durationMinutes: number;
  location: string;
  room: string;
}

/**
 * Meet-ups of a case (MEETUPREQUESTS, MEETUPSCHEDULES). While the case is in
 * stage contacted or followUp, its student or its advisor asks to meet; the
 * other side accepts, changes the date (the request goes back) or declines with
 * a reason. Accepting schedules the meet-up and moves a contacted case to
 * follow-up. Times are Vietnam time, Monday to Friday, 08:30-11:30 and
 * 13:30-18:00, in 15-minute steps.
 */
export function CaseMeetups({
  caseId,
  viewerId,
  audience,
  isParticipant,
  stage,
  closed,
  onCaseChanged,
  className = "",
}: {
  caseId: string;
  viewerId: string;
  audience: "student" | "staff";
  /** The student of the case or its current advisor: the only ones who arrange meet-ups. */
  isParticipant: boolean;
  stage: string;
  closed: boolean;
  /** Called after an accept, which may move the case to follow-up. */
  onCaseChanged?: () => void;
  className?: string;
}) {
  const t = useTranslations("ui.caseMeetups");
  const [requests, setRequests] = useState<MeetupRequestRecord[]>([]);
  const [schedules, setSchedules] = useState<MeetupScheduleRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [mode, setMode] = useState<"" | "new" | "reschedule" | "decline">("");
  const [cancelling, setCancelling] = useState("");
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState("");
  const [statusMessage, setStatusMessage] = useState("");
  // When the meet-ups were last loaded: decides whether a meet-up has started (cancel before, mark held or missed after)
  const [loadedAt, setLoadedAt] = useState(0);

  const load = useCallback(async () => {
    setLoading(true);
    setLoadError("");
    try {
      const [requestPayload, schedulePayload] = await Promise.all([
        clientApiGet<unknown>("MeetupRequests/search", { query: { caseId } }),
        clientApiGet<unknown>("MeetupSchedules/search", { query: { caseId } }),
      ]);
      setRequests(listOf(requestPayload).map(toMeetupRequest));
      setSchedules(listOf(schedulePayload).map(toMeetupSchedule));
      setLoadedAt(Date.now());
    } catch (cause) {
      setLoadError(errorText(cause, t("loadError")));
    } finally {
      setLoading(false);
    }
  }, [caseId, t]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  const pending = requests.find((item) => item.status === "pending") ?? null;
  const answered = requests.filter((item) => item.status === "declined" || item.status === "cancelled").slice(0, 5);
  const stageAllows = !closed && (stage === "contacted" || stage === "followUp");
  const canRequest = isParticipant && stageAllows && !pending;

  async function run(action: () => Promise<unknown>, done: string, changedCase = false) {
    setBusy(true);
    setActionError("");
    setStatusMessage("");
    try {
      const payload = await action();
      if (payload !== undefined && unwrap(payload) === null) throw new Error(t("actionError"));
      setMode("");
      setCancelling("");
      setStatusMessage(done);
      await load();
      if (changedCase) onCaseChanged?.();
    } catch (cause) {
      setActionError(errorText(cause, t("actionError")));
    } finally {
      setBusy(false);
    }
  }

  function send(path: string, body?: Record<string, unknown>, method: "POST" | "PUT" = "PUT") {
    return clientApiRequest<unknown>(path, { method, body, redirectOnUnauthorized: true });
  }

  return (
    <section className={cn("rounded-lg border border-rule bg-paper p-space-lg", className)}>
      <div className="mb-space-md flex flex-wrap items-start justify-between gap-space-sm">
        <div className="flex items-start gap-space-sm">
          <Icon name="event" className="mt-0.5 text-[20px] text-ink" />
          <div>
            <h2 className="text-base font-semibold text-text">{t("title")}</h2>
            <p className="mt-1 text-[0.8125rem] leading-5 text-text-muted">{t("intro")}</p>
          </div>
        </div>
        {canRequest && mode !== "new" ? (
          <Button size="sm" icon="add" onClick={() => setMode("new")}>
            {t("request")}
          </Button>
        ) : null}
      </div>

      {loading ? (
        <div className="rounded-md border border-rule">
          <SkeletonRows rows={2} columns={2} label={t("loading")} />
        </div>
      ) : loadError ? (
        <ErrorState
          title={loadError}
          action={
            <Button size="sm" variant="secondary" onClick={() => void load()}>
              {t("retry")}
            </Button>
          }
        />
      ) : (
        <div className="flex flex-col gap-space-md">
          {mode === "new" ? (
            <SlotForm
              title={t("newTitle")}
              submitLabel={t("sendRequest")}
              withReason
              busy={busy}
              onCancel={() => setMode("")}
              onSubmit={(draft, reason) =>
                void run(
                  () => send("MeetupRequests", { caseId, ...draft, room: draft.room || null, reason }, "POST"),
                  t("requestSent"),
                )
              }
            />
          ) : null}

          {pending ? (
            <div className="rounded-md border border-status-attention-border bg-status-attention-surface/40 p-space-base">
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <p className="text-sm font-semibold text-text">{t("pendingTitle")}</p>
                <StatusChip
                  tone="attention"
                  size="sm"
                  label={pending.awaitingResponseFrom === viewerId ? t("yourAnswer") : t("waitingFor", { name: waitingName(pending) })}
                />
              </div>
              <MeetupWhen
                date={pending.localDate}
                start={pending.localStartTime}
                end={pending.localEndTime}
                minutes={pending.durationMinutes}
                location={pending.location}
                room={pending.room}
              />
              <p className="mt-space-sm text-sm leading-6 text-text">{pending.reason}</p>
              <p className="mt-1 text-[0.8125rem] text-text-faint">
                {t("proposedBy", { name: pending.proposedByFullName })}
                {pending.rescheduleCount ? ` · ${t("rescheduled", { count: pending.rescheduleCount })}` : ""}
              </p>

              {isParticipant && stageAllows && pending.awaitingResponseFrom === viewerId && mode === "" ? (
                <div className="mt-space-md flex flex-wrap gap-space-sm">
                  <Button
                    size="sm"
                    icon="check"
                    loading={busy}
                    onClick={() =>
                      void run(() => send(`MeetupRequests/${pending.meetupRequestId}/accept`), t("accepted"), true)
                    }
                  >
                    {t("accept")}
                  </Button>
                  <Button size="sm" variant="secondary" icon="edit_calendar" onClick={() => setMode("reschedule")}>
                    {t("changeDate")}
                  </Button>
                  <Button size="sm" variant="tertiary" icon="close" onClick={() => setMode("decline")}>
                    {t("decline")}
                  </Button>
                </div>
              ) : null}
              {isParticipant && pending.proposedBy === viewerId && mode === "" ? (
                <div className="mt-space-md">
                  <Button
                    size="sm"
                    variant="tertiary"
                    icon="undo"
                    loading={busy}
                    onClick={() => void run(() => send(`MeetupRequests/${pending.meetupRequestId}/cancel`), t("withdrawn"))}
                  >
                    {t("withdraw")}
                  </Button>
                </div>
              ) : null}

              {mode === "reschedule" ? (
                <SlotForm
                  className="mt-space-md"
                  title={t("changeDateTitle")}
                  submitLabel={t("proposeDate")}
                  initial={{
                    meetupDate: pending.localDate,
                    startTime: pending.localStartTime,
                    durationMinutes: pending.durationMinutes,
                    location: pending.location,
                    room: pending.room,
                  }}
                  busy={busy}
                  onCancel={() => setMode("")}
                  onSubmit={(draft) =>
                    void run(
                      () => send(`MeetupRequests/${pending.meetupRequestId}`, { ...draft, room: draft.room || null }),
                      t("dateChanged"),
                    )
                  }
                />
              ) : null}
              {mode === "decline" ? (
                <ReasonForm
                  label={t("declineReason")}
                  submitLabel={t("decline")}
                  busy={busy}
                  onCancel={() => setMode("")}
                  onSubmit={(reason) =>
                    void run(
                      () => send(`MeetupRequests/${pending.meetupRequestId}/decline`, { declineReason: reason }),
                      t("declined"),
                    )
                  }
                />
              ) : null}
            </div>
          ) : null}

          {schedules.length ? (
            <ul className="flex flex-col divide-y divide-rule rounded-md border border-rule" aria-label={t("scheduled")}>
              {schedules.map((item) => {
                const started = Date.parse(withZone(item.scheduledStartAt)) <= loadedAt;
                const isAdvisor = item.advisorId === viewerId;
                const mine = isAdvisor || item.studentId === viewerId;
                return (
                  <li key={item.meetupScheduleId} className="p-space-base">
                    <div className="flex flex-wrap items-center justify-between gap-space-sm">
                      <MeetupWhen
                        date={item.localDate}
                        start={item.localStartTime}
                        end={item.localEndTime}
                        minutes={item.durationMinutes}
                        location={item.location}
                        room={item.room}
                        compact
                      />
                      <StatusChip tone={SCHEDULE_TONES[item.status] ?? "sealed"} size="sm" label={t(`scheduleStatus.${item.status}`)} />
                    </div>
                    <p className="mt-1 text-[0.8125rem] text-text-muted">
                      {audience === "student" ? item.advisorFullName : item.studentFullName} · {item.reason}
                    </p>
                    {item.status === "cancelled" && item.cancelReason ? (
                      <p className="mt-1 text-[0.8125rem] text-text-faint">
                        {t("cancelledBy", { name: item.cancelledByFullName, reason: item.cancelReason })}
                      </p>
                    ) : null}
                    {item.status === "scheduled" && mine ? (
                      cancelling === item.meetupScheduleId ? (
                        <ReasonForm
                          label={t("cancelReason")}
                          submitLabel={t("cancelMeetup")}
                          busy={busy}
                          onCancel={() => setCancelling("")}
                          onSubmit={(reason) =>
                            void run(
                              () => send(`MeetupSchedules/${item.meetupScheduleId}/cancel`, { cancelReason: reason }),
                              t("meetupCancelled"),
                            )
                          }
                        />
                      ) : (
                        <div className="mt-space-sm flex flex-wrap gap-space-sm">
                          {!started ? (
                            <Button size="sm" variant="tertiary" icon="event_busy" onClick={() => setCancelling(item.meetupScheduleId)}>
                              {t("cancelMeetup")}
                            </Button>
                          ) : null}
                          {started && isAdvisor ? (
                            <>
                              <Button
                                size="sm"
                                variant="secondary"
                                icon="task_alt"
                                loading={busy}
                                onClick={() => void run(() => send(`MeetupSchedules/${item.meetupScheduleId}/complete`), t("markedCompleted"))}
                              >
                                {t("markCompleted")}
                              </Button>
                              <Button
                                size="sm"
                                variant="tertiary"
                                icon="person_off"
                                loading={busy}
                                onClick={() => void run(() => send(`MeetupSchedules/${item.meetupScheduleId}/missed`), t("markedMissed"))}
                              >
                                {t("markMissed")}
                              </Button>
                            </>
                          ) : null}
                        </div>
                      )
                    ) : null}
                  </li>
                );
              })}
            </ul>
          ) : !pending && mode !== "new" ? (
            <p className="text-sm text-text-muted">{t("none")}</p>
          ) : null}

          {answered.length ? (
            <details className="text-[0.8125rem] text-text-muted">
              <summary className="cursor-pointer font-semibold text-text">{t("earlierRequests", { count: answered.length })}</summary>
              <ul className="mt-space-sm flex flex-col gap-1.5">
                {answered.map((item) => (
                  <li key={item.meetupRequestId} className="flex flex-wrap items-center gap-space-sm">
                    <StatusChip tone={REQUEST_TONES[item.status] ?? "sealed"} size="sm" label={t(`requestStatus.${item.status}`)} />
                    <span className="font-mono">
                      {formatLocalDate(item.localDate)} {item.localStartTime}-{item.localEndTime}
                    </span>
                    {item.declineReason ? <span>· {item.declineReason}</span> : null}
                  </li>
                ))}
              </ul>
            </details>
          ) : null}
        </div>
      )}

      <p aria-live="polite" className={cn("mt-space-md text-[0.8125rem]", actionError ? "text-red-ink" : "text-text-faint")}>
        {actionError ||
          statusMessage ||
          (closed
            ? t("closedHint")
            : !stageAllows
              ? t("stageHint")
              : !isParticipant
                ? t("readOnlyHint")
                : t("rulesHint"))}
      </p>
    </section>
  );

  function waitingName(item: MeetupRequestRecord) {
    return item.awaitingResponseFrom === item.studentId ? item.studentFullName : item.advisorFullName;
  }
}

function MeetupWhen({
  date,
  start,
  end,
  minutes,
  location,
  room,
  compact = false,
}: {
  date: string;
  start: string;
  end: string;
  minutes: number;
  location: string;
  room: string;
  compact?: boolean;
}) {
  return (
    <div className={cn("flex flex-wrap items-center gap-x-space-md gap-y-1 text-sm text-text", compact ? "" : "mt-space-sm")}>
      <span className="inline-flex items-center gap-1 font-mono">
        <Icon name="calendar_today" className="text-[16px] text-text-faint" />
        {formatLocalDate(date)} {start}-{end}
      </span>
      <span className="text-text-faint">{formatDuration(minutes)}</span>
      <span className="inline-flex items-center gap-1">
        <Icon name="location_on" className="text-[16px] text-text-faint" />
        {room ? `${location}, ${room}` : location}
      </span>
    </div>
  );
}

function SlotForm({
  title,
  submitLabel,
  initial,
  withReason = false,
  busy,
  onSubmit,
  onCancel,
  className = "",
}: {
  title: string;
  submitLabel: string;
  initial?: SlotDraft;
  withReason?: boolean;
  busy: boolean;
  onSubmit: (draft: SlotDraft, reason: string) => void;
  onCancel: () => void;
  className?: string;
}) {
  const t = useTranslations("ui.caseMeetups");
  const today = vietnamToday();
  const starts = useMemo(() => startTimeOptions(), []);
  const [draft, setDraft] = useState<SlotDraft>(
    initial ?? { meetupDate: nextWeekday(addDays(today, 1)), startTime: "09:00", durationMinutes: 60, location: "", room: "" },
  );
  const [reason, setReason] = useState("");
  const durations = durationOptions(draft.startTime);
  const weekend = draft.meetupDate ? [0, 6].includes(weekday(draft.meetupDate)) : false;

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (weekend) return;
    onSubmit({ ...draft, location: draft.location.trim(), room: draft.room.trim() }, reason.trim());
  }

  return (
    <form onSubmit={submit} className={cn("rounded-md border border-rule bg-paper-tint p-space-base", className)}>
      <p className="mb-space-sm text-sm font-semibold text-text">{title}</p>
      <div className="grid gap-space-sm sm:grid-cols-3">
        <Field label={t("date")} required hint={weekend ? <span className="text-red-ink">{t("weekdaysOnly")}</span> : t("weekdaysOnly")}>
          <TextInput
            type="date"
            min={today}
            value={draft.meetupDate}
            onChange={(event) => setDraft({ ...draft, meetupDate: event.target.value })}
            aria-invalid={weekend}
            required
          />
        </Field>
        <Field label={t("startTime")} required>
          <Select
            className="w-full"
            value={draft.startTime}
            onChange={(event) => {
              const startTime = event.target.value;
              const allowed = durationOptions(startTime);
              setDraft({
                ...draft,
                startTime,
                durationMinutes: allowed.includes(draft.durationMinutes) ? draft.durationMinutes : allowed[allowed.length - 1] ?? 15,
              });
            }}
          >
            {starts.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </Select>
        </Field>
        <Field label={t("duration")} required>
          <Select
            className="w-full"
            value={draft.durationMinutes}
            onChange={(event) => setDraft({ ...draft, durationMinutes: Number(event.target.value) })}
          >
            {durations.map((value) => (
              <option key={value} value={value}>
                {formatDuration(value)}
              </option>
            ))}
          </Select>
        </Field>
        <Field label={t("location")} required className="sm:col-span-2">
          <TextInput
            maxLength={200}
            placeholder={t("locationPlaceholder")}
            value={draft.location}
            onChange={(event) => setDraft({ ...draft, location: event.target.value })}
            required
          />
        </Field>
        <Field label={t("room")}>
          <TextInput
            maxLength={50}
            placeholder={t("roomPlaceholder")}
            value={draft.room}
            onChange={(event) => setDraft({ ...draft, room: event.target.value })}
          />
        </Field>
        {withReason ? (
          <Field label={t("reason")} required className="sm:col-span-3">
            <TextArea
              maxLength={1000}
              className="min-h-20"
              placeholder={t("reasonPlaceholder")}
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              required
            />
          </Field>
        ) : null}
      </div>
      <p className="mt-space-sm text-[0.75rem] text-text-faint">{t("windowsHint")}</p>
      <div className="mt-space-sm flex flex-wrap justify-end gap-space-sm">
        <Button size="sm" variant="tertiary" onClick={onCancel} disabled={busy}>
          {t("back")}
        </Button>
        <Button size="sm" type="submit" loading={busy} disabled={weekend}>
          {submitLabel}
        </Button>
      </div>
    </form>
  );
}

function ReasonForm({
  label,
  submitLabel,
  busy,
  onSubmit,
  onCancel,
}: {
  label: string;
  submitLabel: string;
  busy: boolean;
  onSubmit: (reason: string) => void;
  onCancel: () => void;
}) {
  const t = useTranslations("ui.caseMeetups");
  const [reason, setReason] = useState("");
  return (
    <form
      className="mt-space-md"
      onSubmit={(event) => {
        event.preventDefault();
        if (reason.trim()) onSubmit(reason.trim());
      }}
    >
      <Field label={label} required>
        <TextArea maxLength={1000} className="min-h-16" value={reason} onChange={(event) => setReason(event.target.value)} required />
      </Field>
      <div className="mt-space-sm flex flex-wrap justify-end gap-space-sm">
        <Button size="sm" variant="tertiary" onClick={onCancel} disabled={busy}>
          {t("back")}
        </Button>
        <Button size="sm" variant="destructive" type="submit" loading={busy} disabled={!reason.trim()}>
          {submitLabel}
        </Button>
      </div>
    </form>
  );
}

/** The date itself if it is Monday to Friday, else the next Monday. */
function nextWeekday(date: string): string {
  const day = weekday(date);
  return day === 6 ? addDays(date, 2) : day === 0 ? addDays(date, 1) : date;
}

/** API timestamps are UTC; one without an offset is UTC too. */
function withZone(value: string): string {
  return /[zZ]|[+-]\d{2}:?\d{2}$/.test(value) ? value : `${value}Z`;
}

function errorText(cause: unknown, fallback: string): string {
  if (cause instanceof ApiError && cause.status < 500 && cause.message) return cause.message;
  if (cause instanceof Error && !(cause instanceof ApiError) && cause.message) return cause.message;
  return fallback;
}
