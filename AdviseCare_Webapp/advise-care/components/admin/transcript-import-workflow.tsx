"use client";

import { useCallback, useEffect, useMemo, useRef, useState, type DragEvent } from "react";
import { useTranslations } from "next-intl";
import { ApiError, isRecord } from "@/lib/api-contract";
import { clientApiGet, clientApiRequest } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { formatDateTime } from "@/lib/format";
import { Button } from "@/components/ui/button";
import { useConfirm } from "@/components/ui/confirm-dialog";
import { Icon } from "@/components/ui/icon";
import { Notice } from "@/components/ui/notice";
import { Pager, usePaged } from "@/components/ui/pager";
import { EmptyState } from "@/components/ui/states";
import { StatusChip } from "@/components/ui/status-chip";

const MAX_FILE_BYTES = 5 * 1024 * 1024;
const MAX_FILES = 200;
const PAGE_SIZE = 20;

/** Fields of TranscriptFileResponse (AdviseCare.Application/Contracts/Responses/StudentData). */
interface FileCheck {
  fileName: string;
  importType: string;
  studentCode: string;
  studentName: string;
  fullNameInFile: string;
  rowCount: number;
  insertCount: number;
  replaceCount: number;
  skippedRowCount: number;
  isValid: boolean;
  errors: string[];
  warnings: string[];
}

/** One selected PDF and, once read and checked, its result (null until the next check). */
interface Selected {
  key: string;
  file: File;
  check: FileCheck | null;
}

interface ImportResult {
  importedFileCount: number;
  insertedCount: number;
  replacedCount: number;
}

interface HistoryItem {
  activityLogId: string;
  activityDetail: string;
  occurredAt: string;
}

const text = (value: unknown) => (typeof value === "string" ? value : "");
const count = (value: unknown) => (typeof value === "number" ? value : 0);
const strings = (value: unknown) => (Array.isArray(value) ? value.filter((item): item is string => typeof item === "string") : []);
const records = (value: unknown) => (Array.isArray(value) ? value.filter(isRecord) : []);
const keyOf = (file: File) => `${file.name}|${file.size}|${file.lastModified}`;

function toFileCheck(item: Record<string, unknown>): FileCheck {
  return {
    fileName: text(item.fileName),
    importType: text(item.importType),
    studentCode: text(item.studentCode),
    studentName: text(item.studentName),
    fullNameInFile: text(item.fullNameInFile),
    rowCount: count(item.rowCount),
    insertCount: count(item.insertCount),
    replaceCount: count(item.replaceCount),
    skippedRowCount: count(item.skippedRowCount),
    isValid: item.isValid === true,
    errors: strings(item.errors),
    warnings: strings(item.warnings),
  };
}

function formatSize(bytes: number) {
  return bytes >= 1024 * 1024 ? `${(bytes / 1024 / 1024).toFixed(1)} MB` : `${Math.max(1, Math.round(bytes / 1024))} KB`;
}

/**
 * Import of per-subject student data from transcript PDFs (one student per
 * file: the university's grade transcript → "Subject mark", or the attendance
 * file of the same layout → "Attendance rate").
 *
 * Same model as the Excel/CSV import, nothing is kept on the server between
 * the steps: select or drop any number of PDFs → "Read and check" sends them
 * to the preview route, which returns every file as valid or with its errors →
 * remove (or replace) the files with errors here → "Import" sends the files
 * again and the API writes them all, or nothing if one is still invalid.
 */
export function TranscriptImportWorkflow() {
  const t = useTranslations("ui.transcriptImport");
  const tu = useTranslations("ui");
  const { confirm, element: confirmDialog } = useConfirm();
  const inputRef = useRef<HTMLInputElement>(null);
  const [selected, setSelected] = useState<Selected[]>([]);
  const [dragging, setDragging] = useState(false);
  const [busy, setBusy] = useState<"check" | "import" | null>(null);
  const [error, setError] = useState("");
  const [result, setResult] = useState<ImportResult | null>(null);
  const [filter, setFilter] = useState<"all" | "invalid" | "warnings">("all");
  const [history, setHistory] = useState<HistoryItem[]>([]);

  const messageOf = useCallback(
    (cause: unknown) => (cause instanceof ApiError && cause.message ? cause.message : t("networkError")),
    [t],
  );

  // Recent transcript imports, from the admin activity log (transcriptImported)
  const loadHistory = useCallback(async () => {
    try {
      const payload = await clientApiGet<unknown>("ActivityLogs/search", { query: { activityType: "transcriptImported" } });
      setHistory(
        records(payload)
          .map((item) => ({
            activityLogId: text(item.activityLogId),
            activityDetail: text(item.activityDetail),
            occurredAt: text(item.occurredAt),
          }))
          .sort((a, b) => b.occurredAt.localeCompare(a.occurredAt))
          .slice(0, 8),
      );
    } catch {
      setHistory([]);
    }
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => void loadHistory(), 0);
    return () => window.clearTimeout(timer);
  }, [loadHistory]);

  const checked = selected.length > 0 && selected.every((item) => item.check !== null);
  const invalidCount = selected.filter((item) => item.check && !item.check.isValid).length;
  const warningCount = selected.filter((item) => item.check && item.check.warnings.length > 0).length;
  const insertCount = selected.reduce((sum, item) => sum + (item.check?.isValid ? item.check.insertCount : 0), 0);
  const replaceCount = selected.reduce((sum, item) => sum + (item.check?.isValid ? item.check.replaceCount : 0), 0);
  const selectedBytes = selected.reduce((sum, item) => sum + item.file.size, 0);
  const oversized = selected.filter((item) => item.file.size > MAX_FILE_BYTES).length;

  function addSelection(files: FileList | File[] | null) {
    if (!files) return;
    const pdfs = Array.from(files).filter((file) => file.name.toLowerCase().endsWith(".pdf"));
    setSelected((current) => {
      const known = new Set(current.map((item) => item.key));
      return [...current, ...pdfs.filter((file) => !known.has(keyOf(file))).map((file) => ({ key: keyOf(file), file, check: null }))];
    });
    setResult(null);
    setError(pdfs.length < Array.from(files).length ? t("onlyPdf") : "");
  }

  function onDrop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault();
    setDragging(false);
    if (busy === null) addSelection(event.dataTransfer.files);
  }

  function remove(key: string) {
    setSelected((current) => current.filter((item) => item.key !== key));
  }

  function clear() {
    setSelected([]);
    setFilter("all");
    setError("");
    if (inputRef.current) inputRef.current.value = "";
  }

  function body() {
    const form = new FormData();
    for (const item of selected) form.append("files", item.file);
    return form;
  }

  // The API answers in the order the files were sent
  function applyChecks(payload: unknown) {
    const files = records(isRecord(payload) ? payload.files : null).map(toFileCheck);
    setSelected((current) => current.map((item, index) => ({ ...item, check: files[index] ?? null })));
  }

  async function check() {
    if (selected.length === 0) return;
    if (selected.length > MAX_FILES) {
      setError(t("tooMany", { max: MAX_FILES }));
      return;
    }
    setBusy("check");
    setError("");
    setResult(null);
    try {
      applyChecks(await clientApiRequest<unknown>("StudentData/transcripts/preview", { method: "POST", body: body(), timeoutMs: 120_000 }));
      setFilter("all");
    } catch (cause) {
      setError(messageOf(cause));
    } finally {
      setBusy(null);
    }
  }

  async function runImport() {
    const ok = await confirm({
      title: t("commitConfirmTitle"),
      body: t("commitConfirmBody", { files: selected.length, values: insertCount, replaced: replaceCount }),
      confirmLabel: t("commit", { count: selected.length }),
      cancelLabel: tu("cancel"),
      icon: "upload",
    });
    if (!ok) return;
    setBusy("import");
    setError("");
    try {
      const payload = await clientApiRequest<unknown>("StudentData/transcripts/import", { method: "POST", body: body(), timeoutMs: 120_000 });
      const r = isRecord(payload) ? payload : {};
      if (r.isSuccess === true) {
        setResult({
          importedFileCount: count(r.importedFileCount),
          insertedCount: count(r.insertedCount),
          replacedCount: count(r.replacedCount),
        });
        clear();
      } else {
        // Something changed since the check (e.g. a subject was archived): show the new errors, nothing was written
        applyChecks(payload);
        setError(t("importRefused"));
      }
    } catch (cause) {
      setError(messageOf(cause));
    } finally {
      setBusy(null);
      void loadHistory();
    }
  }

  return (
    <div className="flex flex-col gap-space-lg">
      <section className="rounded-lg border border-rule bg-paper" aria-labelledby="transcript-import-title">
        <header className="flex flex-wrap items-start justify-between gap-space-md border-b border-rule px-space-lg py-space-md">
          <div className="flex min-w-0 items-start gap-space-sm">
            <Icon name="school" className="mt-0.5 text-[20px] text-ink" />
            <div className="min-w-0">
              <h2 id="transcript-import-title" className="text-base font-semibold text-text">
                {t("title")}
              </h2>
              <p className="mt-0.5 max-w-[72ch] text-sm text-text-muted">{t("description")}</p>
            </div>
          </div>
          <a
            href="/templates/attendance-transcript-sample.pdf"
            download
            className="inline-flex items-center gap-1 text-sm font-semibold text-ink hover:underline"
          >
            <Icon name="download" className="text-[18px]" />
            {t("attendanceSample")}
          </a>
        </header>

        <div className="flex flex-col gap-space-md px-space-lg py-space-base">
          <div
            onDragOver={(event) => {
              event.preventDefault();
              setDragging(true);
            }}
            onDragLeave={() => setDragging(false)}
            onDrop={onDrop}
            className={cn(
              "flex flex-col items-center gap-space-sm rounded-lg border border-dashed px-space-lg py-space-lg text-center transition-colors",
              dragging ? "border-ink bg-ink-wash" : "border-rule-strong bg-paper-tint",
            )}
          >
            <Icon name="upload_file" className="text-[28px] text-ink" />
            <p className="text-[0.9375rem] font-semibold text-text">{selected.length ? t("dropMore") : t("drop")}</p>
            <p className="max-w-[60ch] text-[0.8125rem] leading-5 text-text-faint">{t("dropHint")}</p>
            <input
              ref={inputRef}
              type="file"
              accept=".pdf,application/pdf"
              multiple
              className="sr-only"
              id="transcript-files"
              onChange={(event) => addSelection(event.target.files)}
            />
            <Button
              size="sm"
              variant="secondary"
              icon="folder_open"
              disabled={busy !== null}
              onClick={() => inputRef.current?.click()}
            >
              {t("choose")}
            </Button>
          </div>

          {selected.length > 0 ? (
            <div className="flex flex-wrap items-center gap-space-sm">
              <p className="mr-auto text-sm text-text">
                {t("selected", { count: selected.length })}{" "}
                <span className="font-mono text-text-faint">· {formatSize(selectedBytes)}</span>
                {oversized > 0 ? (
                  <span className="ml-space-sm text-status-attention-text">{t("oversized", { count: oversized })}</span>
                ) : null}
              </p>
              <Button size="sm" variant="tertiary" disabled={busy !== null} onClick={clear}>
                {t("clearSelection")}
              </Button>
              <Button
                icon="fact_check"
                variant={checked ? "secondary" : "primary"}
                loading={busy === "check"}
                disabled={busy !== null}
                onClick={() => void check()}
              >
                {t("readAndCheck")}
              </Button>
            </div>
          ) : null}

          {error ? <Notice tone="urgent" icon="error" title={error} /> : null}

          {result ? (
            <Notice tone="ontrack" icon="check_circle" title={t("doneTitle", { count: result.importedFileCount })}>
              {t("doneBody", { values: result.insertedCount, replaced: result.replacedCount })}
            </Notice>
          ) : null}

          {selected.some((item) => item.check) ? (
            <CheckedFiles
              selected={selected}
              filter={filter}
              setFilter={setFilter}
              checked={checked}
              invalidCount={invalidCount}
              warningCount={warningCount}
              insertCount={insertCount}
              replaceCount={replaceCount}
              busy={busy}
              onRemove={remove}
              onImport={() => void runImport()}
            />
          ) : null}
        </div>
      </section>

      <section className="rounded-lg border border-rule bg-paper" aria-labelledby="transcript-history-title">
        <header className="border-b border-rule px-space-lg py-space-md">
          <h2 id="transcript-history-title" className="text-base font-semibold text-text">
            {t("historyTitle")}
          </h2>
        </header>
        {history.length ? (
          <ul className="divide-y divide-rule">
            {history.map((item) => (
              <li key={item.activityLogId} className="flex flex-wrap items-center gap-x-space-md gap-y-1 px-space-lg py-space-sm">
                <span className="font-mono text-[0.8125rem] text-text-faint">{formatDateTime(item.occurredAt)}</span>
                <span className="text-sm text-text">{item.activityDetail}</span>
              </li>
            ))}
          </ul>
        ) : (
          <EmptyState icon="history" title={t("historyEmpty")} />
        )}
      </section>
      {confirmDialog}
    </div>
  );
}

function CheckedFiles({
  selected,
  filter,
  setFilter,
  checked,
  invalidCount,
  warningCount,
  insertCount,
  replaceCount,
  busy,
  onRemove,
  onImport,
}: {
  selected: Selected[];
  filter: "all" | "invalid" | "warnings";
  setFilter: (value: "all" | "invalid" | "warnings") => void;
  checked: boolean;
  invalidCount: number;
  warningCount: number;
  insertCount: number;
  replaceCount: number;
  busy: string | null;
  onRemove: (key: string) => void;
  onImport: () => void;
}) {
  const t = useTranslations("ui.transcriptImport");
  const shown = useMemo(
    () =>
      selected
        .filter((item) =>
          filter === "invalid"
            ? item.check !== null && !item.check.isValid
            : filter === "warnings"
              ? (item.check?.warnings.length ?? 0) > 0
              : true,
        )
        // Problems first, so a long list does not hide its few bad files
        .sort((a, b) => Number(b.check !== null && !b.check.isValid) - Number(a.check !== null && !a.check.isValid)),
    [selected, filter],
  );
  const paged = usePaged(shown, PAGE_SIZE);
  const canImport = checked && invalidCount === 0 && selected.length > 0;

  return (
    <section aria-labelledby="transcript-check-title" className="flex flex-col gap-space-md">
      <h3 id="transcript-check-title" className="text-sm font-semibold text-text">
        {t("checkTitle")}
      </h3>

      <dl className="grid grid-cols-2 gap-px overflow-hidden rounded-md border border-rule bg-rule sm:grid-cols-4">
        {[
          { label: t("countTotal"), value: selected.length },
          { label: t("countValues"), value: insertCount, tone: "text-status-ontrack-text" },
          { label: t("countInvalid"), value: invalidCount, tone: invalidCount ? "text-red-ink" : "" },
          { label: t("countWarnings"), value: warningCount, tone: warningCount ? "text-status-attention-text" : "" },
        ].map((tile) => (
          <div key={tile.label} className="bg-paper px-space-md py-space-sm">
            <dt className="text-[0.8125rem] text-text-muted">{tile.label}</dt>
            <dd className={cn("mt-0.5 font-mono text-xl font-semibold text-text", tile.tone)}>{tile.value}</dd>
          </div>
        ))}
      </dl>

      {!checked ? (
        <Notice tone="attention" icon="refresh" title={t("staleTitle")}>
          {t("staleBody")}
        </Notice>
      ) : invalidCount > 0 ? (
        <Notice tone="attention" icon="error" title={t("invalidBlocksTitle", { count: invalidCount })}>
          {t("invalidBlocksBody")}
        </Notice>
      ) : null}

      <div role="group" aria-label={t("filterLabel")} className="flex flex-wrap gap-1.5">
        {(
          [
            ["all", t("filterAll", { count: selected.length })],
            ["invalid", t("filterInvalid", { count: invalidCount })],
            ["warnings", t("filterWarnings", { count: warningCount })],
          ] as const
        ).map(([value, label]) => (
          <button
            key={value}
            type="button"
            aria-pressed={filter === value}
            onClick={() => {
              setFilter(value);
              paged.setPage(1);
            }}
            className={cn(
              "h-8 rounded-md border px-3 text-[0.8125rem] font-semibold transition-colors",
              filter === value ? "border-ink bg-ink-wash text-ink" : "border-rule text-text-muted hover:border-rule-strong",
            )}
          >
            {label}
          </button>
        ))}
      </div>

      <div className="overflow-x-auto rounded-lg border border-rule">
        <table className="w-full min-w-[760px] border-collapse text-left text-sm">
          <thead className="bg-paper-tint">
            <tr className="border-b border-rule text-[0.75rem] font-semibold whitespace-nowrap text-text-faint">
              <th scope="col" className="h-9 px-space-md">{t("colFile")}</th>
              <th scope="col" className="h-9 px-space-sm">{t("colStudent")}</th>
              <th scope="col" className="h-9 px-space-sm">{t("colType")}</th>
              <th scope="col" className="h-9 px-space-sm text-right">{t("colRows")}</th>
              <th scope="col" className="h-9 px-space-sm text-right">{t("colChanges")}</th>
              <th scope="col" className="h-9 px-space-md">{t("colResult")}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-rule">
            {paged.items.map(({ key, file, check }) => (
              <tr key={key} className="align-top">
                <td className="max-w-[16rem] px-space-md py-space-sm">
                  <span className="block truncate font-mono text-[0.8125rem] text-text" title={file.name}>
                    {file.name}
                  </span>
                </td>
                <td className="px-space-sm py-space-sm">
                  {check?.studentCode ? (
                    <>
                      <span className="font-mono text-[0.8125rem] text-text">{check.studentCode}</span>
                      <span className="block text-[0.8125rem] text-text-muted">{check.studentName || check.fullNameInFile}</span>
                    </>
                  ) : (
                    <span className="text-text-faint">—</span>
                  )}
                </td>
                <td className="whitespace-nowrap px-space-sm py-space-sm text-text-muted">
                  {check?.importType === "grade" ? t("typeGrade") : check?.importType === "attendance" ? t("typeAttendance") : "—"}
                </td>
                <td className="px-space-sm py-space-sm text-right font-mono text-text">
                  {check ? check.rowCount : "—"}
                  {check?.skippedRowCount ? (
                    <span className="block whitespace-nowrap text-[0.75rem] text-text-faint">+{check.skippedRowCount} {t("skipped")}</span>
                  ) : null}
                </td>
                <td className="whitespace-nowrap px-space-sm py-space-sm text-right font-mono text-[0.8125rem] text-text-muted">
                  {check?.isValid ? t("changes", { values: check.insertCount, replaced: check.replaceCount }) : "—"}
                </td>
                <td className="px-space-md py-space-sm">
                  {check ? (
                    <StatusChip
                      size="sm"
                      tone={!check.isValid ? "urgent" : check.warnings.length ? "attention" : "ontrack"}
                      label={check.isValid ? t("fileStatus.valid") : t("fileStatus.invalid")}
                    />
                  ) : (
                    <StatusChip size="sm" tone="sealed" label={t("fileStatus.unchecked")} />
                  )}
                  {check && (check.errors.length || check.warnings.length) ? (
                    <ul className="mt-1 flex max-w-[28rem] flex-col gap-0.5 text-[0.8125rem] leading-5">
                      {check.errors.slice(0, 6).map((message) => (
                        <li key={`e-${message}`} className="text-red-ink">{message}</li>
                      ))}
                      {check.errors.length > 6 ? (
                        <li className="text-red-ink">{t("moreErrors", { count: check.errors.length - 6 })}</li>
                      ) : null}
                      {check.warnings.map((message) => (
                        <li key={`w-${message}`} className="text-status-attention-text">{message}</li>
                      ))}
                    </ul>
                  ) : null}
                  <Button
                    size="sm"
                    variant="quiet"
                    icon="delete"
                    className="mt-1"
                    disabled={busy !== null}
                    onClick={() => onRemove(key)}
                  >
                    {t("removeFile")}
                  </Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {shown.length === 0 ? <p className="px-space-lg py-space-md text-sm text-text-muted">{t("noFilesForFilter")}</p> : null}
      </div>
      <Pager {...paged} />

      <div className="flex flex-wrap items-center gap-space-sm border-t border-rule pt-space-md">
        <p className="mr-auto max-w-[60ch] text-[0.8125rem] leading-5 text-text-faint">
          {t("commitHint")} {canImport ? t("commitSummary", { values: insertCount, replaced: replaceCount }) : ""}
        </p>
        <Button icon="upload" disabled={busy !== null || !canImport} loading={busy === "import"} onClick={onImport}>
          {t("commit", { count: selected.length })}
        </Button>
      </div>
    </section>
  );
}
