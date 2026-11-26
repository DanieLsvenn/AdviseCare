"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import { ApiError, isRecord } from "@/lib/api-contract";
import { clientApiGet } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { formatDateTime } from "@/lib/format";
import { termOrder } from "@/components/academic/academic-results";
import { Button } from "@/components/ui/button";
import { Drawer } from "@/components/ui/drawer";
import { controlClass } from "@/components/ui/field";
import { Icon } from "@/components/ui/icon";
import { Pager, usePaged } from "@/components/ui/pager";
import { EmptyState, ErrorState, SkeletonRows } from "@/components/ui/states";
import { StatusChip } from "@/components/ui/status-chip";

/** One SearchStudentDataResponse row (never the value itself). */
interface LedgerRow {
  studentDataId: string;
  studentId: string;
  studentFullName: string;
  attributeName: string;
  subjectCode: string;
  termId: string;
  termName: string;
  validFrom: string;
  transactionFrom: string;
  erasedAt: string;
  isMissing: boolean;
  status: string;
}

interface StudentGroup {
  studentId: string;
  studentFullName: string;
  rows: LedgerRow[];
  termCount: number;
  missingCount: number;
  correctionCount: number;
  erasedCount: number;
  lastRecordedAt: string;
}

interface Option {
  value: string;
  label: string;
}

const text = (value: unknown): string => (typeof value === "string" ? value : "");
const listOf = (payload: unknown) => (Array.isArray(payload) ? payload.filter(isRecord) : []);

function toRow(item: Record<string, unknown>): LedgerRow {
  return {
    studentDataId: text(item.studentDataId),
    studentId: text(item.studentId),
    studentFullName: text(item.studentFullName),
    attributeName: text(item.attributeName),
    subjectCode: text(item.subjectCode),
    termId: text(item.termId),
    termName: text(item.termName),
    validFrom: text(item.validFrom),
    transactionFrom: text(item.transactionFrom),
    erasedAt: text(item.erasedAt),
    isMissing: item.isMissing === true,
    status: text(item.status) || "original",
  };
}

function toGroups(rows: LedgerRow[]): StudentGroup[] {
  const map = new Map<string, LedgerRow[]>();
  for (const row of rows) map.set(row.studentId, [...(map.get(row.studentId) ?? []), row]);
  return [...map.entries()]
    .map(([studentId, list]) => ({
      studentId,
      studentFullName: list[0].studentFullName,
      rows: list,
      termCount: new Set(list.map((row) => row.termId)).size,
      missingCount: list.filter((row) => row.isMissing).length,
      correctionCount: list.filter((row) => row.status === "correction").length,
      erasedCount: list.filter((row) => row.erasedAt).length,
      lastRecordedAt: list.reduce((latest, row) => (row.transactionFrom > latest ? row.transactionFrom : latest), ""),
    }))
    .sort((a, b) => a.studentFullName.localeCompare(b.studentFullName));
}

/**
 * The System Administrator's student data ledger, one row per student (never
 * a value: they are encrypted and shown only to staff with consent). Opening a
 * student shows their rows in a sheet that can be widened, grouped by term
 * (newest open), each row with its subject, valid-from and recorded-at dates.
 */
export function StudentDataLedger() {
  const t = useTranslations("ui.dataLedger");
  const [rows, setRows] = useState<LedgerRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [terms, setTerms] = useState<Option[]>([]);
  const [attributes, setAttributes] = useState<Option[]>([]);
  const [termId, setTermId] = useState("");
  const [dataAttributeId, setDataAttributeId] = useState("");
  const [status, setStatus] = useState("");
  const [onlyMissing, setOnlyMissing] = useState(false);
  const [studentQuery, setStudentQuery] = useState("");
  const [openStudentId, setOpenStudentId] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    void Promise.all([
      clientApiGet<unknown>("AcademicTerms").catch(() => []),
      clientApiGet<unknown>("DataAttributes").catch(() => []),
    ]).then(([termPayload, attributePayload]) => {
      if (!active) return;
      setTerms(
        listOf(termPayload)
          .map((item) => ({ value: text(item.termId), label: text(item.termName) }))
          .sort((a, b) => termOrder(b.label) - termOrder(a.label)),
      );
      setAttributes(
        listOf(attributePayload)
          .map((item) => ({ value: text(item.dataAttributeId), label: text(item.attributeName) }))
          .sort((a, b) => a.label.localeCompare(b.label)),
      );
    });
    return () => {
      active = false;
    };
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const payload = await clientApiGet<unknown>("StudentData/search", {
        query: {
          termId: termId || undefined,
          dataAttributeId: dataAttributeId || undefined,
          status: status || undefined,
          isMissing: onlyMissing ? true : undefined,
        },
      });
      setRows(listOf(payload).map(toRow));
    } catch (cause) {
      setRows([]);
      setError(cause instanceof ApiError && cause.status < 500 && cause.message ? cause.message : t("loadError"));
    } finally {
      setLoading(false);
    }
  }, [dataAttributeId, onlyMissing, status, t, termId]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  const groups = useMemo(() => toGroups(rows), [rows]);
  const query = studentQuery.trim().toLowerCase();
  const shown = useMemo(
    () => (query ? groups.filter((group) => group.studentFullName.toLowerCase().includes(query)) : groups),
    [groups, query],
  );
  const paged = usePaged(shown, 20);
  const openGroup = groups.find((group) => group.studentId === openStudentId) ?? null;

  return (
    <section className="rounded-lg border border-rule bg-paper" aria-labelledby="data-ledger-title">
      <header className="flex flex-wrap items-start justify-between gap-space-md border-b border-rule px-space-lg py-space-md">
        <div className="min-w-0">
          <h2 id="data-ledger-title" className="text-base font-semibold text-text">
            {t("title")}
          </h2>
          <p className="mt-0.5 max-w-[72ch] text-sm text-text-muted">{t("description")}</p>
        </div>
        <Button size="sm" variant="secondary" icon="refresh" onClick={() => void load()} disabled={loading}>
          {t("refresh")}
        </Button>
      </header>

      <div className="grid gap-space-md border-b border-rule px-space-lg py-space-md sm:grid-cols-2 lg:grid-cols-5">
        <label className="block">
          <span className="mb-1 block text-[0.8125rem] font-semibold text-text">{t("student")}</span>
          <input
            type="search"
            className={cn(controlClass, "h-9 w-full")}
            placeholder={t("studentPlaceholder")}
            value={studentQuery}
            onChange={(event) => setStudentQuery(event.target.value)}
          />
        </label>
        <FilterSelect label={t("term")} value={termId} onChange={setTermId} options={terms} all={t("allTerms")} />
        <FilterSelect label={t("attribute")} value={dataAttributeId} onChange={setDataAttributeId} options={attributes} all={t("allAttributes")} />
        <FilterSelect
          label={t("rowKind")}
          value={status}
          onChange={setStatus}
          options={[
            { value: "original", label: t("original") },
            { value: "correction", label: t("correction") },
          ]}
          all={t("allKinds")}
        />
        <label className="flex items-end gap-2 pb-2 text-sm text-text">
          <input type="checkbox" className="size-4 accent-[#2142b8]" checked={onlyMissing} onChange={(event) => setOnlyMissing(event.target.checked)} />
          {t("onlyMissing")}
        </label>
      </div>

      {loading ? (
        <SkeletonRows rows={5} columns={5} label={t("loading")} />
      ) : error ? (
        <ErrorState
          title={error}
          action={
            <Button size="sm" variant="secondary" onClick={() => void load()}>
              {t("retry")}
            </Button>
          }
        />
      ) : shown.length === 0 ? (
        <EmptyState icon="dataset" title={t("emptyTitle")}>
          {t("emptyBody")}
        </EmptyState>
      ) : (
        <>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[720px] border-collapse text-left text-sm">
              <caption className="sr-only">{t("title")}</caption>
              <thead className="bg-paper-tint">
                <tr className="border-b border-rule text-[0.75rem] font-semibold text-text-faint">
                  <th scope="col" className="h-9 px-space-lg">{t("student")}</th>
                  <th scope="col" className="h-9 px-space-sm text-right">{t("rows")}</th>
                  <th scope="col" className="h-9 px-space-sm text-right">{t("terms")}</th>
                  <th scope="col" className="h-9 px-space-sm">{t("notes")}</th>
                  <th scope="col" className="h-9 px-space-sm">{t("lastRecorded")}</th>
                  <th scope="col" className="h-9 px-space-lg text-right"><span className="sr-only">{t("open")}</span></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-rule">
                {paged.items.map((group) => (
                  <tr key={group.studentId} className="align-middle hover:bg-paper-tint">
                    <td className="px-space-lg py-space-sm font-semibold text-text">{group.studentFullName}</td>
                    <td className="px-space-sm py-space-sm text-right font-mono">{group.rows.length}</td>
                    <td className="px-space-sm py-space-sm text-right font-mono">{group.termCount}</td>
                    <td className="px-space-sm py-space-sm">
                      <span className="flex flex-wrap gap-1">
                        {group.missingCount ? <StatusChip size="sm" tone="attention" label={t("missingCount", { count: group.missingCount })} /> : null}
                        {group.correctionCount ? <StatusChip size="sm" tone="info" label={t("correctionCount", { count: group.correctionCount })} /> : null}
                        {group.erasedCount ? <StatusChip size="sm" tone="sealed" label={t("erasedCount", { count: group.erasedCount })} /> : null}
                        {!group.missingCount && !group.correctionCount && !group.erasedCount ? <span className="text-text-faint">—</span> : null}
                      </span>
                    </td>
                    <td className="px-space-sm py-space-sm font-mono text-[0.8125rem] text-text-muted">
                      {group.lastRecordedAt ? formatDateTime(group.lastRecordedAt) : "—"}
                    </td>
                    <td className="px-space-lg py-space-sm text-right">
                      <Button size="sm" variant="secondary" iconAfter="chevron_right" onClick={() => setOpenStudentId(group.studentId)}>
                        {t("open")}
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pager {...paged} className="border-t border-rule px-space-lg py-space-sm" />
        </>
      )}

      <Drawer
        open={openGroup !== null}
        onClose={() => setOpenStudentId(null)}
        title={openGroup?.studentFullName ?? ""}
        description={openGroup ? t("drawerSummary", { rows: openGroup.rows.length, terms: openGroup.termCount }) : null}
        closeLabel={t("close")}
        width="lg"
        resizable
        resizeLabel={t("resize")}
      >
        {openGroup ? <StudentTerms group={openGroup} /> : null}
      </Drawer>
    </section>
  );
}

function FilterSelect({
  label,
  value,
  onChange,
  options,
  all,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: Option[];
  all: string;
}) {
  return (
    <label className="block">
      <span className="mb-1 block text-[0.8125rem] font-semibold text-text">{label}</span>
      <select className={cn(controlClass, "h-9 w-full pr-8")} value={value} onChange={(event) => onChange(event.target.value)}>
        <option value="">{all}</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  );
}

/** One student's rows by term, newest term open. */
function StudentTerms({ group }: { group: StudentGroup }) {
  const t = useTranslations("ui.dataLedger");
  const terms = useMemo(() => {
    const map = new Map<string, LedgerRow[]>();
    for (const row of group.rows) map.set(row.termId, [...(map.get(row.termId) ?? []), row]);
    return [...map.entries()]
      .map(([termId, list]) => ({
        termId,
        termName: list[0].termName,
        rows: [...list].sort(
          (a, b) =>
            a.attributeName.localeCompare(b.attributeName) ||
            a.subjectCode.localeCompare(b.subjectCode) ||
            b.transactionFrom.localeCompare(a.transactionFrom),
        ),
      }))
      .sort((a, b) => termOrder(b.termName) - termOrder(a.termName));
  }, [group]);

  return (
    <div className="flex flex-col gap-space-sm">
      {terms.map((term, index) => (
        <details key={term.termId} open={index === 0} className="group rounded-md border border-rule">
          <summary className="flex cursor-pointer list-none items-center gap-space-md px-space-md py-space-sm hover:bg-paper-tint [&::-webkit-details-marker]:hidden">
            <Icon name="chevron_right" className="text-[18px] text-text-faint transition-transform group-open:rotate-90" />
            <span className="text-[0.9375rem] font-semibold text-text">{term.termName}</span>
            <span className="text-[0.8125rem] text-text-muted">{t("rowCount", { count: term.rows.length })}</span>
          </summary>
          <div className="overflow-x-auto border-t border-rule">
            <table className="w-full min-w-[560px] border-collapse text-left text-sm">
              <caption className="sr-only">{t("termCaption", { term: term.termName })}</caption>
              <thead className="bg-paper-tint">
                <tr className="border-b border-rule text-[0.75rem] font-semibold text-text-faint">
                  <th scope="col" className="h-9 px-space-md">{t("attribute")}</th>
                  <th scope="col" className="h-9 px-space-sm">{t("subject")}</th>
                  <th scope="col" className="h-9 px-space-sm">{t("validFrom")}</th>
                  <th scope="col" className="h-9 px-space-sm">{t("recordedAt")}</th>
                  <th scope="col" className="h-9 px-space-md">{t("rowKind")}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-rule">
                {term.rows.map((row) => (
                  <tr key={row.studentDataId} className="align-top">
                    <td className="px-space-md py-space-sm text-text">{row.attributeName}</td>
                    <td className="px-space-sm py-space-sm font-mono text-[0.8125rem]">{row.subjectCode || "—"}</td>
                    <td className="px-space-sm py-space-sm font-mono text-[0.8125rem] text-text-muted">{row.validFrom ? formatDateTime(row.validFrom) : "—"}</td>
                    <td className="px-space-sm py-space-sm font-mono text-[0.8125rem] text-text-muted">{row.transactionFrom ? formatDateTime(row.transactionFrom) : "—"}</td>
                    <td className="px-space-md py-space-sm">
                      <span className="flex flex-wrap gap-1">
                        <StatusChip size="sm" tone={row.status === "correction" ? "info" : "sealed"} label={row.status === "correction" ? t("correction") : t("original")} />
                        {row.isMissing ? <StatusChip size="sm" tone="attention" label={t("missing")} /> : null}
                        {row.erasedAt ? <StatusChip size="sm" tone="sealed" label={t("erasedAt", { date: formatDateTime(row.erasedAt) })} /> : null}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </details>
      ))}
    </div>
  );
}
