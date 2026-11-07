"use client";

import { useMemo, useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { ApiError, isRecord } from "@/lib/api-contract";
import { clientApiRequest } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { Button } from "@/components/ui/button";
import { controlClass } from "@/components/ui/field";
import { Icon } from "@/components/ui/icon";
import { Notice } from "@/components/ui/notice";
import { errorMessage } from "@/components/workspace/workspace-helpers";

/* ------------------------------------------------------------------------ */
/* Shapes shared by the account and student-data imports                     */
/* ------------------------------------------------------------------------ */

export interface ImportRow {
  /** Stable key for React while rows are added and deleted. */
  id: number;
  values: Record<string, string>;
}

export interface RowProblem {
  rowIndex: number | null; // null = about the file, not one row
  messages: string[];
}

export interface CheckOutcome {
  ok: boolean;
  problems: RowProblem[];
  /** Rows that will be left out without failing the import (e.g. archived attribute). */
  warnings?: RowProblem[];
  summary?: string;
}

export interface ImportOutcome {
  ok: boolean;
  message: string;
  problems?: RowProblem[];
}

/** First data row of a file is line 2 (line 1 is the header). */
export const FIRST_DATA_LINE = 2;

/**
 * Upload → editable preview → check → import, for any file the API reads as
 * a header row plus data rows. The edited rows go back to the API as a .csv
 * file, so every fix made in the table is what gets checked and imported.
 */
export function ImportEditor({
  title,
  description,
  accept,
  templateColumns,
  templateFileName,
  preview,
  check,
  runImport,
  beforeTable,
  collapsible = false,
  canCheck = true,
}: {
  title: string;
  description: ReactNode;
  accept: string;
  templateColumns?: string[];
  templateFileName?: string;
  /** Read the uploaded file: its headers, every row, and (optionally) the problems already found. */
  preview: (file: File) => Promise<{
    headers: string[];
    rows: Array<Record<string, string>>;
    rowNumbers: number[];
    outcome?: CheckOutcome;
  }>;
  /** Check the edited rows (sent as CSV). */
  check: (csv: File, headers: string[]) => Promise<CheckOutcome>;
  /** Import the edited rows (sent as CSV). */
  runImport: (csv: File, headers: string[]) => Promise<ImportOutcome>;
  /** Extra controls between the file and the table (e.g. column mapping). */
  beforeTable?: (headers: string[], invalidate: () => void) => ReactNode;
  collapsible?: boolean;
  /** False while extra controls are incomplete (e.g. mapping not finished). */
  canCheck?: boolean;
}) {
  const t = useTranslations("workspace");
  const [open, setOpen] = useState(!collapsible);
  const [file, setFile] = useState<File | null>(null);
  const [headers, setHeaders] = useState<string[]>([]);
  const [rows, setRows] = useState<ImportRow[]>([]);
  const [nextId, setNextId] = useState(0);
  const [outcome, setOutcome] = useState<CheckOutcome | null>(null);
  const [checkedVersion, setCheckedVersion] = useState(-1);
  const [version, setVersion] = useState(0);
  const [pending, setPending] = useState<"preview" | "check" | "import" | null>(null);
  const [error, setError] = useState("");
  const [result, setResult] = useState<ImportOutcome | null>(null);
  const [onlyProblems, setOnlyProblems] = useState(false);
  const [page, setPage] = useState(0);

  // A 404 here means the API server runs a build without the import endpoints,
  // not that a record is missing.
  const importErrorMessage = (cause: unknown) =>
    cause instanceof ApiError && cause.status === 404
      ? t("importEndpointMissing")
      : errorMessage(cause, t);

  const problemsByRow = useMemo(() => {
    const map = new Map<number, string[]>();
    for (const problem of outcome?.problems ?? []) {
      if (problem.rowIndex === null) continue;
      map.set(problem.rowIndex, [...(map.get(problem.rowIndex) ?? []), ...problem.messages]);
    }
    return map;
  }, [outcome]);
  const warningsByRow = useMemo(() => {
    const map = new Map<number, string[]>();
    for (const warning of outcome?.warnings ?? []) {
      if (warning.rowIndex === null) continue;
      map.set(warning.rowIndex, [...(map.get(warning.rowIndex) ?? []), ...warning.messages]);
    }
    return map;
  }, [outcome]);
  const fileProblems = (outcome?.problems ?? []).filter((problem) => problem.rowIndex === null);
  const upToDate = outcome !== null && checkedVersion === version;
  const canImport = upToDate && outcome.ok && rows.length > 0 && result?.ok !== true;

  const visible = rows
    .map((row, index) => ({ row, index }))
    .filter(({ index }) => !onlyProblems || problemsByRow.has(index));
  const pageCount = Math.max(1, Math.ceil(visible.length / PAGE_SIZE));
  const currentPage = Math.min(page, pageCount - 1);
  const pageRows = visible.slice(currentPage * PAGE_SIZE, (currentPage + 1) * PAGE_SIZE);

  function invalidate() {
    setVersion((value) => value + 1);
    setResult(null);
  }

  function reset(nextFile: File | null) {
    setFile(nextFile);
    setHeaders([]);
    setRows([]);
    setOutcome(null);
    setResult(null);
    setError("");
    setPage(0);
    setOnlyProblems(false);
  }

  async function loadPreview() {
    if (!file) return;
    setPending("preview");
    setError("");
    setResult(null);
    try {
      const answer = await preview(file);
      const loaded = answer.rows.map((values, index) => ({ id: index, values: normalize(values, answer.headers) }));
      setHeaders(answer.headers);
      setRows(loaded);
      setNextId(loaded.length);
      setPage(0);
      const nextVersion = version + 1;
      setVersion(nextVersion);
      if (answer.outcome) {
        // Problems of the original file point at its row numbers.
        const byNumber = new Map(answer.rowNumbers.map((number, index) => [number, index]));
        const remap = (items: RowProblem[] | undefined) =>
          items?.map((item) => ({
            ...item,
            rowIndex: item.rowIndex === null ? null : (byNumber.get(item.rowIndex) ?? null),
          }));
        setOutcome({
          ...answer.outcome,
          problems: remap(answer.outcome.problems) ?? [],
          warnings: remap(answer.outcome.warnings),
        });
        setCheckedVersion(nextVersion);
      } else {
        setOutcome(null);
      }
    } catch (cause) {
      setError(importErrorMessage(cause));
    } finally {
      setPending(null);
    }
  }

  async function runCheck() {
    setPending("check");
    setError("");
    setResult(null);
    try {
      const checked = await check(toCsvFile(headers, rows, file), headers);
      setOutcome(checked);
      setCheckedVersion(version);
      if (!checked.ok) setOnlyProblems(checked.problems.some((problem) => problem.rowIndex !== null));
    } catch (cause) {
      setError(importErrorMessage(cause));
    } finally {
      setPending(null);
    }
  }

  async function doImport() {
    setPending("import");
    setError("");
    try {
      const outcomeOfImport = await runImport(toCsvFile(headers, rows, file), headers);
      setResult(outcomeOfImport);
      if (!outcomeOfImport.ok && outcomeOfImport.problems) {
        setOutcome({ ok: false, problems: outcomeOfImport.problems });
        setCheckedVersion(version);
      }
    } catch (cause) {
      setError(importErrorMessage(cause));
    } finally {
      setPending(null);
    }
  }

  function editCell(index: number, header: string, value: string) {
    setRows((current) =>
      current.map((row, rowIndex) =>
        rowIndex === index ? { ...row, values: { ...row.values, [header]: value } } : row,
      ),
    );
    invalidate();
  }

  function deleteRow(index: number) {
    setRows((current) => current.filter((_, rowIndex) => rowIndex !== index));
    // Row positions shift, so the old problems no longer line up.
    setOutcome(null);
    invalidate();
  }

  function addRow() {
    setRows((current) => [
      ...current,
      { id: nextId, values: Object.fromEntries(headers.map((header) => [header, ""])) },
    ]);
    setNextId((value) => value + 1);
    setOnlyProblems(false);
    setPage(Math.floor(rows.length / PAGE_SIZE));
    invalidate();
  }

  function downloadTemplate() {
    if (!templateColumns) return;
    const blob = new Blob(["﻿" + templateColumns.join(",") + "\r\n"], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = templateFileName ?? "template.csv";
    link.click();
    URL.revokeObjectURL(url);
  }

  const body = (
    <div className="flex flex-col gap-space-md px-space-lg pb-space-lg">
      <div className="flex flex-wrap items-end gap-space-sm">
        <label className="min-w-64 flex-1">
          <span className="mb-1.5 block text-sm font-semibold text-text">{t("file")}</span>
          <input
            accept={accept}
            className="block w-full rounded-md border border-dashed border-rule-strong bg-paper-tint px-3 py-2.5 text-sm text-text-muted file:mr-3 file:rounded-md file:border-0 file:bg-ink file:px-3 file:py-1.5 file:text-sm file:font-semibold file:text-white hover:border-ink"
            onChange={(event) => reset(event.target.files?.[0] ?? null)}
            type="file"
          />
        </label>
        <Button
          disabled={!file || pending !== null}
          loading={pending === "preview"}
          onClick={() => void loadPreview()}
          icon="table_view"
          variant="secondary"
        >
          {t("importPreview")}
        </Button>
        {templateColumns ? (
          <Button onClick={downloadTemplate} icon="download" variant="quiet">
            {t("importTemplate")}
          </Button>
        ) : null}
      </div>

      {headers.length ? beforeTable?.(headers, invalidate) : null}

      {headers.length ? (
        <section aria-label={t("importPreview")} className="flex flex-col gap-space-sm">
          <div className="flex flex-wrap items-center gap-space-sm">
            <h3 className="mr-auto text-sm font-semibold text-text">
              {t("importRowsTitle", { count: rows.length })}
            </h3>
            {problemsByRow.size ? (
              <label className="flex items-center gap-2 text-[0.8125rem] text-text-muted">
                <input
                  type="checkbox"
                  className="size-4 accent-[#2142b8]"
                  checked={onlyProblems}
                  onChange={(event) => {
                    setOnlyProblems(event.target.checked);
                    setPage(0);
                  }}
                />
                {t("importOnlyProblems", { count: problemsByRow.size })}
              </label>
            ) : null}
            <Button size="sm" variant="secondary" icon="add" onClick={addRow}>
              {t("importAddRow")}
            </Button>
          </div>

          <div className="overflow-x-auto rounded-lg border border-rule">
            <table className="w-full border-collapse text-left text-sm">
              <thead className="bg-paper-tint">
                <tr className="border-b border-rule">
                  <th scope="col" className="h-9 w-12 px-space-sm text-[0.75rem] font-semibold text-text-faint">#</th>
                  {headers.map((header) => (
                    <th key={header} scope="col" className="h-9 whitespace-nowrap px-space-sm font-mono text-[0.75rem] font-semibold text-text-faint">
                      {header}
                    </th>
                  ))}
                  <th scope="col" className="sticky right-0 w-10 bg-paper-tint">
                    <span className="sr-only">{t("actions")}</span>
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-rule">
                {pageRows.map(({ row, index }) => {
                  const problems = problemsByRow.get(index);
                  const warnings = warningsByRow.get(index);
                  return (
                    <RowView
                      key={row.id}
                      index={index}
                      headers={headers}
                      row={row}
                      problems={problems}
                      warnings={warnings}
                      stale={!upToDate}
                      onEdit={editCell}
                      onDelete={deleteRow}
                      deleteLabel={t("importDeleteRow")}
                    />
                  );
                })}
                {pageRows.length === 0 ? (
                  <tr>
                    <td colSpan={headers.length + 2} className="px-space-md py-space-lg text-center text-sm text-text-faint">
                      {t("importNoRows")}
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </table>
          </div>

          {pageCount > 1 ? (
            <div className="flex items-center justify-end gap-space-sm">
              <span className="font-mono text-[0.8125rem] text-text-faint">
                {currentPage + 1} / {pageCount}
              </span>
              <Button size="sm" variant="secondary" icon="chevron_left" disabled={currentPage === 0} onClick={() => setPage(currentPage - 1)}>
                {t("previous")}
              </Button>
              <Button size="sm" variant="secondary" iconAfter="chevron_right" disabled={currentPage >= pageCount - 1} onClick={() => setPage(currentPage + 1)}>
                {t("next")}
              </Button>
            </div>
          ) : null}

          <div className="flex flex-wrap items-center gap-space-sm">
            <Button
              variant="secondary"
              icon="fact_check"
              disabled={pending !== null || !canCheck || rows.length === 0}
              loading={pending === "check"}
              onClick={() => void runCheck()}
            >
              {outcome ? t("importCheckAgain") : t("importCheck")}
            </Button>
            <Button
              icon="upload"
              disabled={!canImport || pending !== null}
              loading={pending === "import"}
              onClick={() => void doImport()}
            >
              {t("importRun", { count: rows.length })}
            </Button>
            <span className="text-[0.8125rem] text-text-faint">
              {!outcome
                ? t("importStepCheck")
                : !upToDate
                  ? t("importStepStale")
                  : outcome.ok
                    ? (outcome.summary ?? t("importStepReady"))
                    : t("importStepFix", { count: problemsByRow.size || fileProblems.length })}
            </span>
          </div>
        </section>
      ) : null}

      {fileProblems.length ? (
        <Notice tone="urgent" icon="error" title={t("importFileProblems")}>
          <ul className="list-disc pl-5">
            {fileProblems.flatMap((problem) => problem.messages).map((message, index) => (
              <li key={index}>{message}</li>
            ))}
          </ul>
        </Notice>
      ) : null}
      {error ? <Notice tone="urgent" icon="error" title={error} /> : null}
      {result ? (
        <Notice tone={result.ok ? "ontrack" : "urgent"} icon={result.ok ? "check_circle" : "error"} title={result.message} />
      ) : null}
    </div>
  );

  return (
    <section className="rounded-lg border border-rule bg-paper">
      {collapsible ? (
        <button
          type="button"
          aria-expanded={open}
          onClick={() => setOpen(!open)}
          className="flex w-full items-center gap-space-md px-space-lg py-space-md text-left"
        >
          <Icon name="upload_file" className="text-[20px] text-ink" />
          <span className="min-w-0 flex-1">
            <span className="block text-base font-semibold text-text">{title}</span>
            <span className="block text-[0.8125rem] text-text-faint">{description}</span>
          </span>
          <Icon name="expand_more" className={cn("text-[20px] text-text-faint transition-transform", open && "rotate-180")} />
        </button>
      ) : (
        <header className="px-space-lg pb-space-md pt-space-lg">
          <h2 className="text-base font-semibold text-text">{title}</h2>
          <p className="mt-1 max-w-[72ch] text-[0.875rem] leading-6 text-text-muted">{description}</p>
        </header>
      )}
      {open ? body : null}
    </section>
  );
}

const PAGE_SIZE = 50;

function RowView({
  index,
  headers,
  row,
  problems,
  warnings,
  stale,
  onEdit,
  onDelete,
  deleteLabel,
}: {
  index: number;
  headers: string[];
  row: ImportRow;
  problems?: string[];
  warnings?: string[];
  stale: boolean;
  onEdit: (index: number, header: string, value: string) => void;
  onDelete: (index: number) => void;
  deleteLabel: string;
}) {
  const hasProblem = Boolean(problems?.length);
  return (
    <>
      <tr className={cn(hasProblem && "bg-status-urgent-surface/40")}>
        <td className="px-space-sm align-middle font-mono text-[0.75rem] text-text-faint">
          {hasProblem ? (
            <Icon name="error" className="mr-0.5 align-[-3px] text-[14px] text-red-ink" />
          ) : null}
          {index + 1}
        </td>
        {headers.map((header) => (
          <td key={header} className="min-w-36 px-1 py-1">
            <input
              aria-label={`${header} ${index + 1}`}
              aria-invalid={hasProblem ? true : undefined}
              className={cn(controlClass, "h-8 w-full text-[0.8125rem]")}
              value={row.values[header] ?? ""}
              onChange={(event) => onEdit(index, header, event.target.value)}
            />
          </td>
        ))}
        <td className={cn("sticky right-0 px-1 text-center", hasProblem ? "bg-status-urgent-surface" : "bg-paper")}>
          <button
            type="button"
            title={deleteLabel}
            aria-label={`${deleteLabel} ${index + 1}`}
            onClick={() => onDelete(index)}
            className="inline-flex size-8 items-center justify-center rounded-md text-text-faint hover:bg-status-urgent-surface hover:text-red-ink"
          >
            <Icon name="delete" className="text-[18px]" />
          </button>
        </td>
      </tr>
      {problems?.length || warnings?.length ? (
        <tr className={cn(hasProblem ? "bg-status-urgent-surface/40" : "bg-status-attention-surface/40")}>
          <td />
          <td colSpan={headers.length + 1} className={cn("px-space-sm pb-2 text-[0.8125rem]", stale && "opacity-60")}>
            {problems?.map((message, messageIndex) => (
              <p key={`p${messageIndex}`} className="text-red-ink">{message}</p>
            ))}
            {warnings?.map((message, messageIndex) => (
              <p key={`w${messageIndex}`} className="text-status-attention-text">{message}</p>
            ))}
          </td>
        </tr>
      ) : null}
    </>
  );
}

/* ------------------------------------------------------------------------ */
/* Helpers                                                                   */
/* ------------------------------------------------------------------------ */

function normalize(values: Record<string, string>, headers: string[]): Record<string, string> {
  // The API matches headers case-insensitively; keep the header's own spelling.
  const lower = new Map(Object.entries(values).map(([key, value]) => [key.toLowerCase(), value]));
  return Object.fromEntries(headers.map((header) => [header, String(lower.get(header.toLowerCase()) ?? "")]));
}

function csvCell(value: string): string {
  // One physical line per row keeps the API's row numbers equal to table positions.
  const flat = value.replace(/\r?\n/g, " ");
  return /[",]/.test(flat) || /^\s|\s$/.test(flat) ? `"${flat.replace(/"/g, '""')}"` : flat;
}

export function toCsvFile(headers: string[], rows: ImportRow[], source: File | null): File {
  const lines = [
    headers.map(csvCell).join(","),
    ...rows.map((row) => headers.map((header) => csvCell(row.values[header] ?? "")).join(",")),
  ];
  const base = (source?.name ?? "import").replace(/\.[^.]+$/, "");
  return new File(["﻿" + lines.join("\r\n") + "\r\n"], `${base}.csv`, { type: "text/csv" });
}

export async function postFile(path: string, file: File, extra?: Record<string, string>): Promise<unknown> {
  const body = new FormData();
  body.append("file", file);
  for (const [key, value] of Object.entries(extra ?? {})) body.append(key, value);
  return clientApiRequest<unknown>(path, { method: "POST", body, timeoutMs: 120_000 });
}

/** Turns the API's per-row problems into table positions (data line 2 = row 0). */
export function rowProblems(
  value: unknown,
  pick: (item: Record<string, unknown>) => string[],
  toIndex: (rowNumber: number) => number | null = (rowNumber) =>
    rowNumber < FIRST_DATA_LINE ? null : rowNumber - FIRST_DATA_LINE,
): RowProblem[] {
  if (!Array.isArray(value)) return [];
  return value.filter(isRecord).map((item) => {
    const rowNumber = typeof item.rowNumber === "number" ? item.rowNumber : 0;
    return { rowIndex: toIndex(rowNumber), messages: pick(item) };
  });
}

export function previewShape(payload: unknown, unexpected: string) {
  if (!isRecord(payload)) {
    throw new ApiError({ message: unexpected, status: 502, code: "unexpected_preview_response" });
  }
  const headers = Array.isArray(payload.headers)
    ? payload.headers.filter((item): item is string => typeof item === "string")
    : [];
  const source = Array.isArray(payload.rows) ? payload.rows : Array.isArray(payload.sampleRows) ? payload.sampleRows : [];
  const rows = source.filter(isRecord);
  return {
    headers,
    rows: rows.map((row) =>
      isRecord(row.values)
        ? Object.fromEntries(Object.entries(row.values).map(([key, value]) => [key, String(value ?? "")]))
        : {},
    ),
    rowNumbers: rows.map((row, index) => (typeof row.rowNumber === "number" ? row.rowNumber : index + FIRST_DATA_LINE)),
    payload,
  };
}
