"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import { ApiError, isRecord } from "@/lib/api-contract";
import { clientApiGet } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { formatDateTime } from "@/lib/format";
import type { StatusTone } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { controlClass } from "@/components/ui/field";
import { Icon } from "@/components/ui/icon";
import { Notice } from "@/components/ui/notice";
import { EmptyState, ErrorState, SkeletonRows } from "@/components/ui/states";
import { StatusChip } from "@/components/ui/status-chip";

/** Per-subject results built from the StudentData values (StudentDataValueResponse) of the two per-subject data attributes. */
interface Summary {
  subjectCount: number;
  passedCount: number;
  gradedCount: number;
  averageGrade: number | null;
  lowGradeCount: number;
  attendanceCount: number;
  averageAttendanceRate: number | null;
  lowAttendanceCount: number;
}

interface SubjectResult {
  id: string;
  subjectCode: string;
  subjectName: string;
  grade: number | null;
  attendanceRate: number | null;
  absenceRate: number | null;
  status: string;
}

interface TermResults {
  termId: string;
  termName: string;
  summary: Summary;
  results: SubjectResult[];
}

interface AcademicResults {
  isGradeShared: boolean;
  isAttendanceShared: boolean;
  minAttendanceRate: number;
  passingGrade: number;
  lastUpdatedAt: string;
  summary: Summary;
  terms: TermResults[];
}

/** Data attributes (DATAATTRIBUTES.attributeName) the transcript import fills, and the marking thresholds (FPT rule: at least 80% attendance; pass mark 5/10). */
const GRADE_ATTRIBUTE = "Subject mark";
const ATTENDANCE_ATTRIBUTE = "Attendance rate";
const MIN_ATTENDANCE_RATE = 80;
const PASSING_GRADE = 5;
const SEASONS: Record<string, number> = { spring: 1, summer: 2, fall: 3 };

const text = (value: unknown): string => (typeof value === "string" ? value : "");
const list = (value: unknown): Record<string, unknown>[] => (Array.isArray(value) ? value.filter(isRecord) : []);
const numberOf = (value: unknown): number | null => {
  const parsed = typeof value === "string" && value.trim() ? Number(value) : NaN;
  return Number.isFinite(parsed) ? parsed : null;
};
const average = (values: number[]) => (values.length ? values.reduce((sum, value) => sum + value, 0) / values.length : null);

/** "Fall 2026" → 2026.3, so terms sort newest first; unknown names go last. */
export function termOrder(termName: string) {
  const match = /^(spring|summer|fall)\s*(\d{4})$/i.exec(termName.trim());
  return match ? Number(match[2]) + SEASONS[match[1].toLowerCase()] / 10 : 0;
}

/** Passed / not passed / attendance fail, worked out from the mark and the attendance; empty while there is no mark. */
function statusOf(grade: number | null, attendanceRate: number | null) {
  if (attendanceRate !== null && attendanceRate < MIN_ATTENDANCE_RATE) return "attendanceFail";
  if (grade === null) return "";
  return grade >= PASSING_GRADE ? "passed" : "notPassed";
}

function summarize(results: SubjectResult[]): Summary {
  const grades = results.map((item) => item.grade).filter((value): value is number => value !== null);
  const rates = results.map((item) => item.attendanceRate).filter((value): value is number => value !== null);
  return {
    subjectCount: results.length,
    passedCount: results.filter((item) => item.status === "passed").length,
    gradedCount: grades.length,
    averageGrade: average(grades),
    lowGradeCount: grades.filter((value) => value < PASSING_GRADE).length,
    attendanceCount: rates.length,
    averageAttendanceRate: average(rates),
    lowAttendanceCount: rates.filter((value) => value < MIN_ATTENDANCE_RATE).length,
  };
}

/** Groups the per-subject Subject mark and Attendance rate values (StudentData/me or StudentData/students/{id}) by term and subject. */
function toResults(payload: unknown, audience: "student" | "staff"): AcademicResults {
  const values = list(isRecord(payload) ? payload.values : null).filter(
    (item) => text(item.subjectId) && [GRADE_ATTRIBUTE, ATTENDANCE_ATTRIBUTE].includes(text(item.attributeName)),
  );
  // A value not shared with staff comes back with isShared = false and no value; the student always sees everything
  const sharedFor = (name: string) =>
    audience === "student" || values.some((item) => text(item.attributeName) === name && item.isShared === true);
  const isGradeShared = sharedFor(GRADE_ATTRIBUTE);
  const isAttendanceShared = sharedFor(ATTENDANCE_ATTRIBUTE);

  const terms = new Map<string, { termName: string; subjects: Map<string, SubjectResult> }>();
  let lastUpdatedAt = "";
  for (const item of values) {
    const termId = text(item.termId);
    const term = terms.get(termId) ?? { termName: text(item.termName), subjects: new Map<string, SubjectResult>() };
    terms.set(termId, term);
    const subjectId = text(item.subjectId);
    const result = term.subjects.get(subjectId) ?? {
      id: `${subjectId}-${termId}`,
      subjectCode: text(item.subjectCode),
      subjectName: text(item.subjectName),
      grade: null,
      attendanceRate: null,
      absenceRate: null,
      status: "",
    };
    term.subjects.set(subjectId, result);
    const value = item.isMissing === true || item.isErased === true ? null : numberOf(item.value);
    if (text(item.attributeName) === GRADE_ATTRIBUTE) result.grade = value;
    else result.attendanceRate = value;
    const recordedAt = text(item.transactionFrom);
    if (recordedAt > lastUpdatedAt) lastUpdatedAt = recordedAt;
  }

  const termResults = [...terms.entries()]
    .map(([termId, term]) => {
      const results = [...term.subjects.values()]
        .map((item) => ({
          ...item,
          absenceRate: item.attendanceRate === null ? null : 100 - item.attendanceRate,
          status: statusOf(item.grade, item.attendanceRate),
        }))
        .sort((a, b) => a.subjectCode.localeCompare(b.subjectCode));
      return { termId, termName: term.termName, summary: summarize(results), results };
    })
    .sort((a, b) => termOrder(b.termName) - termOrder(a.termName));

  return {
    isGradeShared,
    isAttendanceShared,
    minAttendanceRate: MIN_ATTENDANCE_RATE,
    passingGrade: PASSING_GRADE,
    lastUpdatedAt,
    summary: summarize(termResults.flatMap((term) => term.results)),
    terms: termResults,
  };
}

const statusTone: Record<string, StatusTone> = {
  passed: "ontrack",
  notPassed: "urgent",
  attendanceFail: "urgent",
};

const numberFormat = new Intl.NumberFormat("en-US", { minimumFractionDigits: 1, maximumFractionDigits: 2 });
const formatNumber = (value: number) => numberFormat.format(value);
const formatPercent = (value: number) => `${new Intl.NumberFormat("en-US", { maximumFractionDigits: 1 }).format(value)}%`;

/** Data already read by the page (StudentDataView), so the values are read once per page (QC-259). */
export interface StudentDataSource {
  payload: unknown;
  loading: boolean;
  error: string;
  reload: () => void;
}

/**
 * Per-subject mark and attendance, one block per term (newest first, the two
 * latest open), built from the per-subject data attributes Subject mark and
 * Attendance rate (filled by the transcript import or the Excel/CSV import).
 * The student sees everything; staff see only the attributes the student
 * shared, read through the access guard and written to the access log by the API.
 *
 * A subject is marked when its grade is under the passing grade or its
 * attendance under the minimum (absence over 20%): text + icon, never colour alone.
 */
export function AcademicResultsView({
  audience,
  studentId,
  caseId,
  className,
  source,
}: {
  audience: "student" | "staff";
  studentId?: string;
  caseId?: string;
  className?: string;
  /** When given, the view shows these values instead of reading them itself. */
  source?: StudentDataSource;
}) {
  const t = useTranslations("ui.academicResults");
  const [ownData, setOwnData] = useState<AcademicResults | null>(null);
  const [ownLoading, setOwnLoading] = useState(!source);
  const [ownError, setOwnError] = useState("");
  const [termFilter, setTermFilter] = useState("all");

  const load = useCallback(async () => {
    if (source) {
      source.reload();
      return;
    }
    setOwnLoading(true);
    setOwnError("");
    try {
      const payload =
        audience === "student"
          ? await clientApiGet<unknown>("StudentData/me")
          : await clientApiGet<unknown>(`StudentData/students/${encodeURIComponent(studentId ?? "")}`, {
              query: caseId ? { caseId } : undefined,
            });
      setOwnData(toResults(payload, audience));
    } catch (cause) {
      setOwnError(cause instanceof ApiError && cause.status < 500 && cause.message ? cause.message : t("loadError"));
    } finally {
      setOwnLoading(false);
    }
  }, [audience, caseId, source, studentId, t]);

  const hasSource = Boolean(source);
  useEffect(() => {
    if (hasSource) return;
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [hasSource, load]);

  const sourcePayload = source?.payload;
  const sourceData = useMemo(
    () => (hasSource && sourcePayload !== undefined && sourcePayload !== null ? toResults(sourcePayload, audience) : null),
    [audience, hasSource, sourcePayload],
  );
  const data = source ? sourceData : ownData;
  const loading = source ? source.loading : ownLoading;
  const error = source ? source.error : ownError;

  const terms = useMemo(
    () => (data ? (termFilter === "all" ? data.terms : data.terms.filter((term) => term.termId === termFilter)) : []),
    [data, termFilter],
  );
  const nothingShared = audience === "staff" && data !== null && !data.isGradeShared && !data.isAttendanceShared;

  return (
    <section className={cn("rounded-lg border border-rule bg-paper", className)} aria-labelledby="academic-results-title">
      <header className="flex flex-wrap items-start justify-between gap-space-md border-b border-rule px-space-lg py-space-md">
        <div className="flex min-w-0 items-start gap-space-sm">
          <Icon name="school" className="mt-0.5 text-[20px] text-ink" />
          <div className="min-w-0">
            <h2 id="academic-results-title" className="text-base font-semibold text-text">
              {t("title")}
            </h2>
            <p className="mt-0.5 max-w-[68ch] text-sm text-text-muted">
              {audience === "student" ? t("introStudent") : t("introStaff")}
            </p>
          </div>
        </div>
        {data && data.terms.length > 1 ? (
          <label className="flex items-center gap-space-sm text-sm text-text-muted">
            <span>{t("term")}</span>
            <select
              className={cn(controlClass, "h-9 pr-8")}
              value={termFilter}
              onChange={(event) => setTermFilter(event.target.value)}
            >
              <option value="all">{t("allTerms")}</option>
              {data.terms.map((term) => (
                <option key={term.termId} value={term.termId}>
                  {term.termName}
                </option>
              ))}
            </select>
          </label>
        ) : null}
      </header>

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
      ) : !data || nothingShared ? (
        <EmptyState icon="lock" title={t("notSharedTitle")}>
          {t("notSharedHint")}
        </EmptyState>
      ) : data.terms.length === 0 ? (
        <EmptyState icon="school" title={t("emptyTitle")}>
          {audience === "student" ? t("emptyHintStudent") : t("emptyHintStaff")}
        </EmptyState>
      ) : (
        <div className="flex flex-col gap-space-base px-space-lg py-space-base">
          <p className="flex flex-wrap gap-x-space-md gap-y-1 text-[0.8125rem] text-text-faint">
            {data.lastUpdatedAt ? (
              <span>
                {t("updatedAt")} <span className="font-mono text-text-muted">{formatDateTime(data.lastUpdatedAt)}</span>
              </span>
            ) : null}
          </p>

          {audience === "staff" && (!data.isGradeShared || !data.isAttendanceShared) ? (
            <Notice tone="sealed" icon="lock" title={data.isGradeShared ? t("onlyGradeShared") : t("onlyAttendanceShared")}>
              {t("partialSharedHint")}
            </Notice>
          ) : null}

          <SummaryTiles summary={data.summary} data={data} />

          <div className="flex flex-col gap-space-sm">
            {terms.map((term, index) => (
              <TermBlock key={term.termId} term={term} data={data} open={termFilter !== "all" || index < 2} />
            ))}
          </div>

          <p className="text-[0.75rem] leading-5 text-text-faint">
            {t("footnote", { minAttendance: data.minAttendanceRate, passingGrade: data.passingGrade })}
          </p>
        </div>
      )}
    </section>
  );
}

function SummaryTiles({ summary, data }: { summary: Summary; data: AcademicResults }) {
  const t = useTranslations("ui.academicResults");
  const notShared = t("notShared");
  const tiles = [
    {
      label: t("averageGrade"),
      value: !data.isGradeShared ? notShared : summary.averageGrade !== null ? formatNumber(summary.averageGrade) : "—",
      hint: data.isGradeShared ? t("averageGradeHint", { count: summary.gradedCount }) : "",
      mono: data.isGradeShared,
    },
    {
      label: t("passed"),
      value: !data.isGradeShared ? notShared : `${summary.passedCount}/${summary.subjectCount}`,
      hint: data.isGradeShared && summary.lowGradeCount > 0 ? t("lowGradeCount", { count: summary.lowGradeCount }) : "",
      warn: data.isGradeShared && summary.lowGradeCount > 0,
      mono: data.isGradeShared,
    },
    {
      label: t("averageAttendance"),
      value: !data.isAttendanceShared
        ? notShared
        : summary.averageAttendanceRate !== null
          ? formatPercent(summary.averageAttendanceRate)
          : "—",
      hint: data.isAttendanceShared ? t("attendanceHint", { count: summary.attendanceCount }) : "",
      mono: data.isAttendanceShared,
    },
    {
      label: t("lowAttendance", { minAttendance: data.minAttendanceRate }),
      value: !data.isAttendanceShared ? notShared : String(summary.lowAttendanceCount),
      hint: data.isAttendanceShared ? t("lowAttendanceHint", { maxAbsence: 100 - data.minAttendanceRate }) : "",
      warn: data.isAttendanceShared && summary.lowAttendanceCount > 0,
      mono: data.isAttendanceShared,
    },
  ];

  return (
    <dl className="grid grid-cols-2 gap-px overflow-hidden rounded-md border border-rule bg-rule lg:grid-cols-4">
      {tiles.map((tile) => (
        <div key={tile.label} className="bg-paper px-space-md py-space-sm">
          <dt className="text-[0.8125rem] text-text-muted">{tile.label}</dt>
          <dd
            className={cn(
              "mt-0.5 flex items-center gap-1 text-xl font-semibold leading-7",
              tile.mono ? "font-mono" : "text-base font-normal text-text-faint",
              tile.warn ? "text-status-attention-text" : tile.mono ? "text-text" : "",
            )}
          >
            {tile.warn ? <Icon name="warning" className="text-[18px]" /> : null}
            {tile.value}
          </dd>
          {tile.hint ? <dd className="text-[0.75rem] text-text-faint">{tile.hint}</dd> : null}
        </div>
      ))}
    </dl>
  );
}

function TermBlock({ term, data, open }: { term: TermResults; data: AcademicResults; open: boolean }) {
  const t = useTranslations("ui.academicResults");
  const ts = useTranslations("ui.academicResults.status");
  const warnings = (data.isGradeShared ? term.summary.lowGradeCount : 0) + (data.isAttendanceShared ? term.summary.lowAttendanceCount : 0);

  return (
    <details open={open} className="group rounded-md border border-rule">
      <summary className="flex cursor-pointer list-none flex-wrap items-center gap-x-space-md gap-y-1 px-space-md py-space-sm hover:bg-paper-tint [&::-webkit-details-marker]:hidden">
        <Icon name="chevron_right" className="text-[18px] text-text-faint transition-transform group-open:rotate-90" />
        <span className="text-[0.9375rem] font-semibold text-text">{term.termName}</span>
        <span className="text-[0.8125rem] text-text-muted">{t("subjectCount", { count: term.summary.subjectCount })}</span>
        {data.isGradeShared && term.summary.averageGrade !== null ? (
          <span className="text-[0.8125rem] text-text-muted">
            {t("averageShort")} <span className="font-mono text-text">{formatNumber(term.summary.averageGrade)}</span>
          </span>
        ) : null}
        {data.isAttendanceShared && term.summary.averageAttendanceRate !== null ? (
          <span className="text-[0.8125rem] text-text-muted">
            {t("attendanceShort")} <span className="font-mono text-text">{formatPercent(term.summary.averageAttendanceRate)}</span>
          </span>
        ) : null}
        {warnings > 0 ? (
          <span className="ml-auto inline-flex items-center gap-1 text-[0.8125rem] font-semibold text-status-attention-text">
            <Icon name="warning" className="text-[16px]" />
            {t("needsAttention", { count: warnings })}
          </span>
        ) : null}
      </summary>

      <div className="overflow-x-auto border-t border-rule">
        <table className="w-full min-w-[640px] table-fixed border-collapse text-left text-sm">
          <caption className="sr-only">{t("tableCaption", { term: term.termName })}</caption>
          {/* Same column widths in every term, so the terms line up when read one under another */}
          <colgroup>
            <col />
            {data.isGradeShared ? <col className="w-24" /> : null}
            {data.isAttendanceShared ? (
              <>
                <col className="w-28" />
                <col className="w-24" />
              </>
            ) : null}
            {data.isGradeShared ? <col className="w-40" /> : null}
          </colgroup>
          <thead className="bg-paper-tint">
            <tr className="border-b border-rule text-[0.75rem] font-semibold text-text-faint">
              <th scope="col" className="h-9 px-space-md">{t("subject")}</th>
              {data.isGradeShared ? (
                <th scope="col" className="h-9 px-space-sm text-right">{t("grade")}</th>
              ) : null}
              {data.isAttendanceShared ? (
                <>
                  <th scope="col" className="h-9 px-space-sm text-right">{t("attendance")}</th>
                  <th scope="col" className="h-9 px-space-sm text-right">{t("absence")}</th>
                </>
              ) : null}
              {data.isGradeShared ? (
                <th scope="col" className="h-9 px-space-md">{t("statusColumn")}</th>
              ) : null}
            </tr>
          </thead>
          <tbody className="divide-y divide-rule">
            {term.results.map((item) => {
              const lowGrade = item.grade !== null && item.grade < data.passingGrade;
              const lowAttendance = item.attendanceRate !== null && item.attendanceRate < data.minAttendanceRate;
              return (
                <tr key={item.id} className="align-top">
                  <td className="px-space-md py-space-sm">
                    <span className="font-mono text-[0.8125rem] font-semibold text-text">{item.subjectCode}</span>
                    <span className="ml-space-sm text-text">{item.subjectName}</span>
                  </td>
                  {data.isGradeShared ? (
                    <td className="px-space-sm py-space-sm text-right font-mono">
                      {item.grade === null ? (
                        <span className="text-text-faint" title={t("noGrade")}>—</span>
                      ) : (
                        <span className={cn("inline-flex items-center gap-1", lowGrade ? "font-semibold text-red-ink" : "text-text")}>
                          {lowGrade ? <Icon name="error" className="text-[15px]" label={t("lowGrade")} /> : null}
                          {formatNumber(item.grade)}
                        </span>
                      )}
                    </td>
                  ) : null}
                  {data.isAttendanceShared ? (
                    <>
                      <td className="px-space-sm py-space-sm text-right font-mono">
                        {item.attendanceRate === null ? (
                          <span className="text-text-faint">—</span>
                        ) : (
                          <span
                            className={cn(
                              "inline-flex items-center gap-1",
                              lowAttendance ? "font-semibold text-status-attention-text" : "text-text",
                            )}
                          >
                            {lowAttendance ? <Icon name="warning" className="text-[15px]" label={t("lowAttendanceLabel")} /> : null}
                            {formatPercent(item.attendanceRate)}
                          </span>
                        )}
                      </td>
                      <td className="px-space-sm py-space-sm text-right font-mono text-text-muted">
                        {item.absenceRate === null ? "—" : formatPercent(item.absenceRate)}
                      </td>
                    </>
                  ) : null}
                  {data.isGradeShared ? (
                    <td className="px-space-md py-space-sm">
                      {item.status ? (
                        <StatusChip size="sm" tone={statusTone[item.status] ?? "sealed"} label={ts.has(item.status) ? ts(item.status) : item.status} />
                      ) : (
                        <span className="text-text-faint">—</span>
                      )}
                    </td>
                  ) : null}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </details>
  );
}
