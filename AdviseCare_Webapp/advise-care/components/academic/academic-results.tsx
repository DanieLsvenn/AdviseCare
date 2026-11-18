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

/** Fields of AcademicResultsResponse (AdviseCare.Application/Contracts/Responses/AcademicResult). */
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
  curriculumTerm: number | null;
  isInCurriculum: boolean;
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
  studentCode: string;
  fullName: string;
  curriculumCode: string;
  isGradeShared: boolean;
  isAttendanceShared: boolean;
  minAttendanceRate: number;
  passingGrade: number;
  lastUpdatedAt: string;
  summary: Summary;
  terms: TermResults[];
}

const num = (value: unknown): number | null => (typeof value === "number" && Number.isFinite(value) ? value : null);
const text = (value: unknown): string => (typeof value === "string" ? value : "");
const rec = (value: unknown): Record<string, unknown> => (isRecord(value) ? value : {});
const list = (value: unknown): Record<string, unknown>[] => (Array.isArray(value) ? value.filter(isRecord) : []);

function toSummary(value: unknown): Summary {
  const r = rec(value);
  return {
    subjectCount: num(r.subjectCount) ?? 0,
    passedCount: num(r.passedCount) ?? 0,
    gradedCount: num(r.gradedCount) ?? 0,
    averageGrade: num(r.averageGrade),
    lowGradeCount: num(r.lowGradeCount) ?? 0,
    attendanceCount: num(r.attendanceCount) ?? 0,
    averageAttendanceRate: num(r.averageAttendanceRate),
    lowAttendanceCount: num(r.lowAttendanceCount) ?? 0,
  };
}

function toResults(payload: unknown): AcademicResults {
  const r = rec(payload);
  return {
    studentCode: text(r.studentCode),
    fullName: text(r.fullName),
    curriculumCode: text(r.curriculumCode),
    isGradeShared: r.isGradeShared === true,
    isAttendanceShared: r.isAttendanceShared === true,
    minAttendanceRate: num(r.minAttendanceRate) ?? 80,
    passingGrade: num(r.passingGrade) ?? 5,
    lastUpdatedAt: text(r.lastUpdatedAt),
    summary: toSummary(r.summary),
    terms: list(r.terms).map((term) => ({
      termId: text(term.termId),
      termName: text(term.termName),
      summary: toSummary(term.summary),
      results: list(term.results).map((item) => ({
        id: text(item.studentSubjectResultId) || `${text(item.subjectCode)}-${text(term.termId)}`,
        subjectCode: text(item.subjectCode),
        subjectName: text(item.subjectName),
        curriculumTerm: num(item.curriculumTerm),
        isInCurriculum: item.isInCurriculum !== false,
        grade: num(item.grade),
        attendanceRate: num(item.attendanceRate),
        absenceRate: num(item.absenceRate),
        status: text(item.status),
      })),
    })),
  };
}

const statusTone: Record<string, StatusTone> = {
  passed: "ontrack",
  notPassed: "urgent",
  attendanceFail: "urgent",
  studying: "info",
  exempt: "sealed",
  notStarted: "sealed",
};

const numberFormat = new Intl.NumberFormat("en-US", { minimumFractionDigits: 1, maximumFractionDigits: 2 });
const formatNumber = (value: number) => numberFormat.format(value);
const formatPercent = (value: number) => `${new Intl.NumberFormat("en-US", { maximumFractionDigits: 1 }).format(value)}%`;

/**
 * Per-subject grade and attendance, one block per term (newest first, the two
 * latest open). The student sees everything; staff see only the part the
 * student shared (GPA → grade and status, Attendance rate → attendance), read
 * through the access guard and written to the access log by the API.
 *
 * A subject is marked when its grade is under the passing grade or its
 * attendance under the minimum (absence over 20%): text + icon, never colour alone.
 */
export function AcademicResultsView({
  audience,
  studentId,
  caseId,
  className,
}: {
  audience: "student" | "staff";
  studentId?: string;
  caseId?: string;
  className?: string;
}) {
  const t = useTranslations("ui.academicResults");
  const [data, setData] = useState<AcademicResults | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [termFilter, setTermFilter] = useState("all");

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const payload =
        audience === "student"
          ? await clientApiGet<unknown>("AcademicResults/me")
          : await clientApiGet<unknown>(`AcademicResults/students/${encodeURIComponent(studentId ?? "")}`, {
              query: caseId ? { caseId } : undefined,
            });
      setData(toResults(payload));
    } catch (cause) {
      setError(cause instanceof ApiError && cause.status < 500 && cause.message ? cause.message : t("loadError"));
    } finally {
      setLoading(false);
    }
  }, [audience, caseId, studentId, t]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

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
            {data.curriculumCode ? (
              <span>
                {t("curriculum")} <span className="font-mono text-text-muted">{data.curriculumCode}</span>
              </span>
            ) : null}
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
                    <span className="block text-[0.75rem] text-text-faint">
                      {item.isInCurriculum
                        ? item.curriculumTerm !== null
                          ? t("curriculumTerm", { term: item.curriculumTerm })
                          : t("inCurriculum")
                        : t("outsideCurriculum")}
                    </span>
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
