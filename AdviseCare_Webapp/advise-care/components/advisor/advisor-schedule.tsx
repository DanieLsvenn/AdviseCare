"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { ApiError } from "@/lib/api-contract";
import { clientApiGet } from "@/lib/client-api";
import { listOf } from "@/components/case/case-api";
import {
  DAY_END,
  DAY_START,
  TIME_WINDOWS,
  addDays,
  formatLocalDate,
  fromMinutes,
  mondayOf,
  toMeetupRequest,
  toMeetupSchedule,
  toMinutes,
  vietnamToday,
  type MeetupRequestRecord,
  type MeetupScheduleRecord,
} from "@/components/case/meetup-api";
import { Button } from "@/components/ui/button";
import { Icon } from "@/components/ui/icon";
import { PageHeader } from "@/components/ui/page-header";
import { ErrorState, SkeletonRows } from "@/components/ui/states";
import { cn } from "@/lib/cn";

/** Pixels per minute: the 08:30-18:00 day is about 680px tall. */
const SCALE = 1.2;
const DAYS = [0, 1, 2, 3, 4];

const BLOCK_CLASSES: Record<string, string> = {
  scheduled: "border-ink bg-ink-wash text-text",
  completed: "border-status-ontrack-border bg-status-ontrack-surface text-status-ontrack-text",
  missed: "border-status-urgent-border bg-status-urgent-surface text-status-urgent-text",
  pending: "border-dashed border-status-attention-border bg-status-attention-surface/60 text-status-attention-text",
};

interface Block {
  key: string;
  caseId: string;
  date: string;
  start: number;
  end: number;
  title: string;
  detail: string;
  status: string;
}

/**
 * The advisor's week (Monday to Friday, Vietnam time): each scheduled,
 * completed or missed meet-up is a block from its start to its end, and a
 * pending request is a dashed block. Only the bookable hours are drawn
 * (08:30-11:30 and 13:30-18:00); the lunch break is shaded.
 */
export function AdvisorSchedule() {
  const t = useTranslations("ui.advisorSchedule");
  const [monday, setMonday] = useState(() => mondayOf(vietnamToday()));
  const [blocks, setBlocks] = useState<Block[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const friday = addDays(monday, 4);
  const today = vietnamToday();

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const query = { fromDate: monday, toDate: friday };
      const [schedulePayload, requestPayload] = await Promise.all([
        clientApiGet<unknown>("MeetupSchedules/search", { query }),
        clientApiGet<unknown>("MeetupRequests/search", { query: { ...query, status: "pending" } }),
      ]);
      const schedules = listOf(schedulePayload)
        .map(toMeetupSchedule)
        .filter((item) => item.status !== "cancelled")
        .map((item) => fromSchedule(item));
      const pending = listOf(requestPayload)
        .map(toMeetupRequest)
        .map((item) => fromRequest(item, t("pendingDetail")));
      setBlocks([...schedules, ...pending]);
    } catch (cause) {
      setError(cause instanceof ApiError && cause.status < 500 && cause.message ? cause.message : t("loadError"));
    } finally {
      setLoading(false);
    }
  }, [monday, friday, t]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  const height = (DAY_END - DAY_START) * SCALE;
  const marks: number[] = [];
  for (let minute = DAY_START; minute <= DAY_END; minute += 30) marks.push(minute);
  const lunch = { start: TIME_WINDOWS[0].end, end: TIME_WINDOWS[1].start };

  return (
    <div className="flex flex-col gap-space-lg">
      <PageHeader
        title={t("title")}
        description={t("description")}
        actions={
          <div className="flex flex-wrap items-center gap-space-sm">
            <Button size="sm" variant="secondary" icon="chevron_left" onClick={() => setMonday(addDays(monday, -7))}>
              {t("previous")}
            </Button>
            <Button size="sm" variant="secondary" onClick={() => setMonday(mondayOf(today))}>
              {t("thisWeek")}
            </Button>
            <Button size="sm" variant="secondary" iconAfter="chevron_right" onClick={() => setMonday(addDays(monday, 7))}>
              {t("next")}
            </Button>
          </div>
        }
      />

      <section className="rounded-lg border border-rule bg-paper p-space-lg">
        <div className="mb-space-md flex flex-wrap items-center justify-between gap-space-sm">
          <p className="font-mono text-sm text-text">
            {formatLocalDate(monday)} – {formatLocalDate(friday)}
          </p>
          <ul className="flex flex-wrap gap-space-md text-[0.75rem] text-text-muted" aria-label={t("legend")}>
            {(["scheduled", "pending", "completed", "missed"] as const).map((status) => (
              <li key={status} className="inline-flex items-center gap-1.5">
                <span aria-hidden className={cn("inline-block size-3 rounded-sm border", BLOCK_CLASSES[status])} />
                {t(`status.${status}`)}
              </li>
            ))}
          </ul>
        </div>

        {loading ? (
          <SkeletonRows rows={4} columns={5} label={t("loading")} />
        ) : error ? (
          <ErrorState
            title={error}
            action={
              <Button size="sm" variant="secondary" onClick={() => void load()}>
                {t("retry")}
              </Button>
            }
          />
        ) : (
          <div className="overflow-x-auto">
            <div className="grid min-w-[720px] grid-cols-[56px_repeat(5,minmax(0,1fr))]">
              <div />
              {DAYS.map((offset) => {
                const date = addDays(monday, offset);
                return (
                  <div
                    key={date}
                    className={cn(
                      "border-b border-rule px-space-sm pb-space-sm text-center text-[0.8125rem] font-semibold",
                      date === today ? "text-ink" : "text-text",
                    )}
                  >
                    {t(`days.${offset}`)}
                    <span className="block font-mono text-[0.75rem] font-normal text-text-faint">{formatLocalDate(date).slice(0, 5)}</span>
                  </div>
                );
              })}

              <div className="relative" style={{ height }}>
                {marks.map((minute) => (
                  <span
                    key={minute}
                    className="absolute right-space-sm -translate-y-1/2 font-mono text-[0.6875rem] text-text-faint"
                    style={{ top: (minute - DAY_START) * SCALE }}
                  >
                    {fromMinutes(minute)}
                  </span>
                ))}
              </div>

              {DAYS.map((offset) => {
                const date = addDays(monday, offset);
                const dayBlocks = blocks.filter((block) => block.date === date);
                return (
                  <div key={date} className={cn("relative border-l border-rule", date === today ? "bg-ink-wash/30" : "")} style={{ height }}>
                    {marks.map((minute) => (
                      <span
                        key={minute}
                        aria-hidden
                        className={cn("absolute inset-x-0 border-t", minute % 60 === 30 ? "border-rule" : "border-rule/50")}
                        style={{ top: (minute - DAY_START) * SCALE }}
                      />
                    ))}
                    <div
                      className="absolute inset-x-0 flex items-center justify-center bg-surface-container/70 text-[0.6875rem] text-text-faint"
                      style={{ top: (lunch.start - DAY_START) * SCALE, height: (lunch.end - lunch.start) * SCALE }}
                    >
                      {t("lunch")}
                    </div>
                    {dayBlocks.map((block) => (
                      <Link
                        key={block.key}
                        href={`/advisor/cases/${block.caseId}`}
                        title={`${block.title} · ${fromMinutes(block.start)}-${fromMinutes(block.end)} · ${block.detail}`}
                        className={cn(
                          "absolute inset-x-1 overflow-hidden rounded-md border px-1.5 py-1 text-[0.75rem] leading-4 hover:shadow-sm focus:outline-none focus:ring-2 focus:ring-ink/30",
                          BLOCK_CLASSES[block.status] ?? BLOCK_CLASSES.scheduled,
                        )}
                        style={{
                          top: (Math.max(block.start, DAY_START) - DAY_START) * SCALE + 1,
                          height: Math.max((Math.min(block.end, DAY_END) - Math.max(block.start, DAY_START)) * SCALE - 2, 16),
                        }}
                      >
                        <span className="block font-mono">
                          {fromMinutes(block.start)}-{fromMinutes(block.end)}
                        </span>
                        <span className="block truncate font-semibold">{block.title}</span>
                        <span className="block truncate">{block.detail}</span>
                      </Link>
                    ))}
                  </div>
                );
              })}
            </div>
          </div>
        )}
        {!loading && !error && !blocks.length ? (
          <p className="mt-space-md flex items-center gap-1.5 text-sm text-text-muted">
            <Icon name="event_available" className="text-[18px]" />
            {t("empty")}
          </p>
        ) : null}
      </section>
    </div>
  );
}

/** A scheduled, completed or missed meet-up as a block. */
function fromSchedule(item: MeetupScheduleRecord): Block {
  return {
    key: item.meetupScheduleId,
    caseId: item.caseId,
    date: item.localDate,
    start: toMinutes(item.localStartTime),
    end: toMinutes(item.localEndTime),
    title: item.studentFullName,
    detail: item.room ? `${item.location}, ${item.room}` : item.location,
    status: item.status,
  };
}

/** A pending request as a dashed block. */
function fromRequest(item: MeetupRequestRecord, pendingDetail: string): Block {
  return {
    key: item.meetupRequestId,
    caseId: item.caseId,
    date: item.localDate,
    start: toMinutes(item.localStartTime),
    end: toMinutes(item.localEndTime),
    title: item.studentFullName,
    detail: pendingDetail,
    status: "pending",
  };
}
