"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import { ApiError, isRecord } from "@/lib/api-contract";
import { clientApiGet } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { formatDateTime } from "@/lib/format";
import { AcademicResultsView, termOrder, type StudentDataSource } from "@/components/academic/academic-results";
import { Button } from "@/components/ui/button";
import { Drawer } from "@/components/ui/drawer";
import { controlClass } from "@/components/ui/field";
import { Icon } from "@/components/ui/icon";
import { EmptyState, ErrorState, SkeletonRows } from "@/components/ui/states";

/** One StudentDataValueResponse as the page uses it. */
interface DataValue {
  key: string;
  dataAttributeId: string;
  attributeName: string;
  termId: string;
  termName: string;
  subjectId: string;
  subjectCode: string;
  subjectName: string;
  value: string | null;
  validFrom: string;
  dataType: string;
  summaryMethod: string;
  isErased: boolean;
  isMissing: boolean;
  isShared: boolean;
}

/** One data attribute in one term: its value, or its per-subject values and their sum/average. */
interface AttributeRow {
  key: string;
  attributeName: string;
  termName: string;
  dataType: string;
  summaryMethod: string;
  isSubjectSpecific: boolean;
  single: DataValue | null;
  subjects: DataValue[];
  summary: number | null;
  countedCount: number;
}

interface TermGroup {
  termId: string;
  termName: string;
  rows: AttributeRow[];
}

const text = (value: unknown): string => (typeof value === "string" ? value : "");
const numberOf = (value: string | null): number | null => {
  const parsed = value !== null && value.trim() ? Number(value) : NaN;
  return Number.isFinite(parsed) ? parsed : null;
};
const numberFormat = new Intl.NumberFormat("en-US", { maximumFractionDigits: 2 });

function toValues(payload: unknown): DataValue[] {
  const values = isRecord(payload) && Array.isArray(payload.values) ? payload.values.filter(isRecord) : [];
  return values.map((item, index) => ({
    key: text(item.studentDataId) || `${text(item.dataAttributeId)}-${text(item.termId)}-${text(item.subjectId)}-${index}`,
    dataAttributeId: text(item.dataAttributeId),
    attributeName: text(item.attributeName),
    termId: text(item.termId),
    termName: text(item.termName),
    subjectId: text(item.subjectId),
    subjectCode: text(item.subjectCode),
    subjectName: text(item.subjectName),
    value: typeof item.value === "string" ? item.value : null,
    validFrom: text(item.validFrom),
    dataType: text(item.dataType) || "numeric",
    summaryMethod: text(item.summaryMethod),
    isErased: item.isErased === true,
    isMissing: item.isMissing === true,
    isShared: item.isShared !== false,
  }));
}

/** Terms newest first; in each term one row per data attribute (A-Z), per-subject values summed or averaged by the attribute's summaryMethod. */
function toTerms(values: DataValue[]): TermGroup[] {
  const terms = new Map<string, { termName: string; attributes: Map<string, DataValue[]> }>();
  for (const item of values) {
    const term = terms.get(item.termId) ?? { termName: item.termName, attributes: new Map<string, DataValue[]>() };
    terms.set(item.termId, term);
    const list = term.attributes.get(item.dataAttributeId) ?? [];
    list.push(item);
    term.attributes.set(item.dataAttributeId, list);
  }
  return [...terms.entries()]
    .map(([termId, term]) => ({
      termId,
      termName: term.termName,
      rows: [...term.attributes.entries()]
        .map(([attributeId, list]) => {
          const first = list[0];
          const isSubjectSpecific = list.some((item) => item.subjectId);
          const subjects = isSubjectSpecific
            ? list.filter((item) => item.subjectId).sort((a, b) => a.subjectCode.localeCompare(b.subjectCode))
            : [];
          const counted = subjects
            .filter((item) => !item.isErased && !item.isMissing)
            .map((item) => numberOf(item.value))
            .filter((value): value is number => value !== null);
          const total = counted.reduce((sum, value) => sum + value, 0);
          const summary =
            first.dataType !== "numeric" || !counted.length
              ? null
              : first.summaryMethod === "sum"
                ? total
                : first.summaryMethod === "average"
                  ? total / counted.length
                  : null;
          return {
            key: `${termId}-${attributeId}`,
            attributeName: first.attributeName,
            termName: term.termName,
            dataType: first.dataType,
            summaryMethod: first.summaryMethod,
            isSubjectSpecific,
            single: isSubjectSpecific ? null : first,
            subjects,
            summary,
            countedCount: counted.length,
          };
        })
        .sort((a, b) => a.attributeName.localeCompare(b.attributeName)),
    }))
    .sort((a, b) => termOrder(b.termName) - termOrder(a.termName));
}

/**
 * The student's data on one page, read once (QC-259): the academic results
 * (per-subject mark and attendance) and, under them, every data attribute per
 * term. A per-subject attribute shows one row with its total or average (the
 * attribute's summaryMethod) and opens a drawer with the value of each subject.
 * Staff see only what the student shared; the API checks consent and logs the read.
 */
export function StudentDataView({
  audience,
  studentId,
  caseId,
}: {
  audience: "student" | "staff";
  studentId?: string;
  caseId?: string;
}) {
  const t = useTranslations("ui.privateData");
  const [payload, setPayload] = useState<unknown>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [termFilter, setTermFilter] = useState("all");
  const [openRow, setOpenRow] = useState<AttributeRow | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      setPayload(
        audience === "student"
          ? await clientApiGet<unknown>("StudentData/me")
          : await clientApiGet<unknown>(`StudentData/students/${encodeURIComponent(studentId ?? "")}`, {
              query: caseId ? { caseId } : undefined,
            }),
      );
    } catch (cause) {
      setPayload(null);
      setError(cause instanceof ApiError && cause.status < 500 && cause.message ? cause.message : t("loadError"));
    } finally {
      setLoading(false);
    }
  }, [audience, caseId, studentId, t]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  const source: StudentDataSource = useMemo(
    () => ({ payload, loading, error, reload: () => void load() }),
    [error, load, loading, payload],
  );
  const terms = useMemo(() => toTerms(toValues(payload)), [payload]);
  const shown = termFilter === "all" ? terms : terms.filter((term) => term.termId === termFilter);

  return (
    <div className="flex flex-col gap-space-lg">
      <AcademicResultsView audience={audience} source={source} />

      <section className="rounded-lg border border-rule bg-paper" aria-labelledby="private-data-title">
        <header className="flex flex-wrap items-start justify-between gap-space-md border-b border-rule px-space-lg py-space-md">
          <div className="flex min-w-0 items-start gap-space-sm">
            <Icon name="lock_person" className="mt-0.5 text-[20px] text-ink" />
            <div className="min-w-0">
              <h2 id="private-data-title" className="text-base font-semibold text-text">
                {t("title")}
              </h2>
              <p className="mt-0.5 max-w-[68ch] text-sm text-text-muted">
                {audience === "student" ? t("introStudent") : t("introStaff")}
              </p>
            </div>
          </div>
          {terms.length > 1 ? (
            <label className="flex items-center gap-space-sm text-sm text-text-muted">
              <span>{t("term")}</span>
              <select className={cn(controlClass, "h-9 pr-8")} value={termFilter} onChange={(event) => setTermFilter(event.target.value)}>
                <option value="all">{t("allTerms")}</option>
                {terms.map((term) => (
                  <option key={term.termId} value={term.termId}>
                    {term.termName}
                  </option>
                ))}
              </select>
            </label>
          ) : null}
        </header>

        {loading ? (
          <SkeletonRows rows={4} columns={4} label={t("loading")} />
        ) : error ? (
          <ErrorState
            title={error}
            action={
              <Button size="sm" variant="secondary" onClick={() => void load()}>
                {t("retry")}
              </Button>
            }
          />
        ) : terms.length === 0 ? (
          <EmptyState icon="lock_person" title={t("emptyTitle")}>
            {t("emptyBody")}
          </EmptyState>
        ) : (
          <div className="flex flex-col gap-space-sm px-space-lg py-space-base">
            {shown.map((term, index) => (
              <details key={term.termId} open={termFilter !== "all" || index === 0} className="group rounded-md border border-rule">
                <summary className="flex cursor-pointer list-none items-center gap-space-md px-space-md py-space-sm hover:bg-paper-tint [&::-webkit-details-marker]:hidden">
                  <Icon name="chevron_right" className="text-[18px] text-text-faint transition-transform group-open:rotate-90" />
                  <span className="text-[0.9375rem] font-semibold text-text">{term.termName}</span>
                  <span className="text-[0.8125rem] text-text-muted">{t("attributeCount", { count: term.rows.length })}</span>
                </summary>
                <div className="overflow-x-auto border-t border-rule">
                  <table className="w-full min-w-[560px] border-collapse text-left text-sm">
                    <caption className="sr-only">{t("tableCaption", { term: term.termName })}</caption>
                    <thead className="bg-paper-tint">
                      <tr className="border-b border-rule text-[0.75rem] font-semibold text-text-faint">
                        <th scope="col" className="h-9 px-space-md">{t("attribute")}</th>
                        <th scope="col" className="h-9 px-space-sm">{t("value")}</th>
                        <th scope="col" className="h-9 px-space-sm">{t("validFrom")}</th>
                        <th scope="col" className="h-9 px-space-md text-right"><span className="sr-only">{t("details")}</span></th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-rule">
                      {term.rows.map((row) => (
                        <tr key={row.key} className="align-top">
                          <td className="px-space-md py-space-sm font-semibold text-text">{row.attributeName}</td>
                          <td className="px-space-sm py-space-sm">
                            {row.single ? <ValueText item={row.single} audience={audience} /> : <SummaryText row={row} />}
                          </td>
                          <td className="px-space-sm py-space-sm font-mono text-[0.8125rem] text-text-muted">
                            {row.single?.validFrom ? formatDateTime(row.single.validFrom) : "—"}
                          </td>
                          <td className="px-space-md py-space-sm text-right">
                            {row.isSubjectSpecific ? (
                              <Button size="sm" variant="secondary" onClick={() => setOpenRow(row)}>
                                {t("viewSubjects", { count: row.subjects.length })}
                              </Button>
                            ) : null}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </details>
            ))}
          </div>
        )}
      </section>

      <Drawer
        open={openRow !== null}
        onClose={() => setOpenRow(null)}
        title={openRow ? `${openRow.attributeName} · ${openRow.termName}` : ""}
        description={openRow ? <SummaryText row={openRow} /> : null}
        closeLabel={t("close")}
        width="lg"
      >
        {openRow ? (
          <table className="w-full border-collapse text-left text-sm">
            <caption className="sr-only">{t("subjectsCaption", { attribute: openRow.attributeName, term: openRow.termName })}</caption>
            <thead>
              <tr className="border-b border-rule text-[0.75rem] font-semibold text-text-faint">
                <th scope="col" className="h-9 pr-space-sm">{t("subject")}</th>
                <th scope="col" className="h-9 px-space-sm">{t("value")}</th>
                <th scope="col" className="h-9 pl-space-sm">{t("validFrom")}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-rule">
              {openRow.subjects.map((item) => (
                <tr key={item.key} className="align-top">
                  <td className="py-space-sm pr-space-sm">
                    <span className="font-mono text-[0.8125rem] font-semibold text-text">{item.subjectCode}</span>
                    <span className="ml-space-sm text-text">{item.subjectName}</span>
                  </td>
                  <td className="px-space-sm py-space-sm">
                    <ValueText item={item} audience={audience} />
                  </td>
                  <td className="py-space-sm pl-space-sm font-mono text-[0.8125rem] text-text-muted">
                    {item.validFrom ? formatDateTime(item.validFrom) : "—"}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : null}
      </Drawer>
    </div>
  );
}

/** A value, or why there is none (erased, missing, not shared). */
function ValueText({ item, audience }: { item: DataValue; audience: "student" | "staff" }) {
  const t = useTranslations("ui.privateData");
  const tu = useTranslations("ui");
  if (item.isErased) return <span className="text-text-faint">{t("erased")}</span>;
  if (item.isMissing) return <span className="text-status-attention-text">{t("missing")}</span>;
  if (audience === "staff" && !item.isShared) {
    return (
      <span className="inline-flex items-center gap-1 text-text-faint">
        <Icon name="lock" className="text-[15px]" />
        {t("notShared")}
      </span>
    );
  }
  if (item.value === null || item.value === "") return <span className="text-text-faint">—</span>;
  if (item.dataType === "boolean") {
    const yes = ["true", "1", "yes"].includes(item.value.trim().toLowerCase());
    return <span className="text-text">{yes ? tu("yes") : tu("no")}</span>;
  }
  const number = item.dataType === "numeric" ? numberOf(item.value) : null;
  return <span className={number !== null ? "font-mono text-text" : "text-text"}>{number !== null ? numberFormat.format(number) : item.value}</span>;
}

/** "Average 7.25 · 6 of 7 subjects" for a per-subject attribute, or the number of subjects when it has no summary. */
function SummaryText({ row }: { row: AttributeRow }) {
  const t = useTranslations("ui.privateData");
  if (row.summary === null) {
    return <span className="text-text-muted">{t("subjectCount", { count: row.subjects.length })}</span>;
  }
  return (
    <span className="text-text-muted">
      {row.summaryMethod === "sum" ? t("total") : t("average")}{" "}
      <span className="font-mono font-semibold text-text">{numberFormat.format(row.summary)}</span>
      {" · "}
      {t("countedOf", { counted: row.countedCount, count: row.subjects.length })}
    </span>
  );
}
