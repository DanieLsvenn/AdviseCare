"use client";

import { useCallback, useEffect, useMemo, useRef, useState, type DragEvent } from "react";
import { useTranslations } from "next-intl";
import { ApiError, isRecord } from "@/lib/api-contract";
import { clientApiDelete, clientApiGet, clientApiPost, clientApiRequest } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { formatDateTime } from "@/lib/format";
import type { StatusTone } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { useConfirm } from "@/components/ui/confirm-dialog";
import { Icon } from "@/components/ui/icon";
import { Notice } from "@/components/ui/notice";
import { Pager, usePaged } from "@/components/ui/pager";
import { EmptyState } from "@/components/ui/states";
import { StatusChip } from "@/components/ui/status-chip";

/** One upload request stays well under the API's and the host's body limits; a large batch goes in several. */
const CHUNK_FILES = 25;
const CHUNK_BYTES = 20 * 1024 * 1024;
const MAX_FILE_BYTES = 5 * 1024 * 1024;
const PAGE_SIZE = 20;

/** Fields of GetTranscriptImportBatchByIdResponse (AdviseCare.Application/Contracts/Responses/TranscriptImportBatch). */
interface ImportFile {
  transcriptImportFileId: string;
  fileName: string;
  importType: string;
  studentCode: string;
  studentName: string;
  fullNameInFile: string;
  rowCount: number;
  insertCount: number;
  replaceCount: number;
  skippedRowCount: number;
  status: string;
  errors: string[];
  warnings: string[];
}

interface Batch {
  transcriptImportBatchId: string;
  importedByName: string;
  createdAt: string;
  committedAt: string;
  status: string;
  totalFiles: number;
  validFiles: number;
  invalidFiles: number;
  committedFiles: number;
  insertCount: number;
  replaceCount: number;
  files: ImportFile[];
}

interface BatchSummary {
  transcriptImportBatchId: string;
  importedByName: string;
  createdAt: string;
  status: string;
  totalFiles: number;
  validFiles: number;
  invalidFiles: number;
  committedFiles: number;
}

interface CommitResult {
  committedFiles: number;
  insertedCount: number;
  replacedCount: number;
}

const text = (value: unknown) => (typeof value === "string" ? value : "");
const count = (value: unknown) => (typeof value === "number" ? value : 0);
const strings = (value: unknown) => (Array.isArray(value) ? value.filter((item): item is string => typeof item === "string") : []);
const records = (value: unknown) => (Array.isArray(value) ? value.filter(isRecord) : []);

function toBatch(payload: unknown): Batch {
  const r = isRecord(payload) ? payload : {};
  return {
    transcriptImportBatchId: text(r.transcriptImportBatchId),
    importedByName: text(r.importedByName),
    createdAt: text(r.createdAt),
    committedAt: text(r.committedAt),
    status: text(r.status),
    totalFiles: count(r.totalFiles),
    validFiles: count(r.validFiles),
    invalidFiles: count(r.invalidFiles),
    committedFiles: count(r.committedFiles),
    insertCount: count(r.insertCount),
    replaceCount: count(r.replaceCount),
    files: records(r.files).map((f) => ({
      transcriptImportFileId: text(f.transcriptImportFileId),
      fileName: text(f.fileName),
      importType: text(f.importType),
      studentCode: text(f.studentCode),
      studentName: text(f.studentName),
      fullNameInFile: text(f.fullNameInFile),
      rowCount: count(f.rowCount),
      insertCount: count(f.insertCount),
      replaceCount: count(f.replaceCount),
      skippedRowCount: count(f.skippedRowCount),
      status: text(f.status),
      errors: strings(f.errors),
      warnings: strings(f.warnings),
    })),
  };
}

function toSummary(item: Record<string, unknown>): BatchSummary {
  return {
    transcriptImportBatchId: text(item.transcriptImportBatchId),
    importedByName: text(item.importedByName),
    createdAt: text(item.createdAt),
    status: text(item.status),
    totalFiles: count(item.totalFiles),
    validFiles: count(item.validFiles),
    invalidFiles: count(item.invalidFiles),
    committedFiles: count(item.committedFiles),
  };
}

function toCommit(payload: unknown): CommitResult {
  const r = isRecord(payload) ? payload : {};
  return {
    committedFiles: count(r.committedFiles),
    insertedCount: count(r.insertedCount),
    replacedCount: count(r.replacedCount),
  };
}

/** Groups of at most CHUNK_FILES files and CHUNK_BYTES bytes (a single bigger file still goes alone). */
function chunked(files: File[]): File[][] {
  const groups: File[][] = [];
  let current: File[] = [];
  let bytes = 0;
  for (const file of files) {
    if (current.length > 0 && (current.length >= CHUNK_FILES || bytes + file.size > CHUNK_BYTES)) {
      groups.push(current);
      current = [];
      bytes = 0;
    }
    current.push(file);
    bytes += file.size;
  }
  if (current.length) groups.push(current);
  return groups;
}

const batchTone: Record<string, StatusTone> = { previewed: "info", committed: "ontrack", cancelled: "sealed" };
const fileTone: Record<string, StatusTone> = { valid: "ontrack", invalid: "urgent", committed: "ontrack" };

function formatSize(bytes: number) {
  return bytes >= 1024 * 1024 ? `${(bytes / 1024 / 1024).toFixed(1)} MB` : `${Math.max(1, Math.round(bytes / 1024))} KB`;
}

/**
 * Import of per-subject student data from transcript PDFs (one student per
 * file: the university's grade transcript → "Subject mark", or the attendance
 * file of the same layout → "Attendance rate").
 *
 * Select or drop any number of PDFs → they are sent in parts into one import
 * batch, where the API reads and checks each file → the preview lists every
 * file as valid or with its errors → invalid files are removed (or fixed files
 * added) → the import writes every file together, all-or-nothing.
 */
export function TranscriptImportWorkflow() {
  const t = useTranslations("ui.transcriptImport");
  const tu = useTranslations("ui");
  const { confirm, element: confirmDialog } = useConfirm();
  const inputRef = useRef<HTMLInputElement>(null);
  const [selected, setSelected] = useState<File[]>([]);
  const [dragging, setDragging] = useState(false);
  const [batch, setBatch] = useState<Batch | null>(null);
  const [progress, setProgress] = useState<{ sent: number; total: number } | null>(null);
  const [busy, setBusy] = useState<"upload" | "commit" | "cancel" | "open" | "remove" | null>(null);
  const [error, setError] = useState("");
  const [result, setResult] = useState<CommitResult | null>(null);
  const [filter, setFilter] = useState<"all" | "invalid" | "warnings">("all");
  const [history, setHistory] = useState<BatchSummary[]>([]);

  const messageOf = useCallback(
    (cause: unknown) => (cause instanceof ApiError && cause.message ? cause.message : t("networkError")),
    [t],
  );

  const loadHistory = useCallback(async () => {
    try {
      const payload = await clientApiGet<unknown>("TranscriptImportBatches", { query: { page: 1, pageSize: 8 } });
      setHistory(records(payload).map(toSummary));
    } catch {
      setHistory([]);
    }
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => void loadHistory(), 0);
    return () => window.clearTimeout(timer);
  }, [loadHistory]);

  const editable = batch === null || batch.status === "previewed";
  const selectedBytes = selected.reduce((sum, file) => sum + file.size, 0);
  const oversized = selected.filter((file) => file.size > MAX_FILE_BYTES).length;

  function addSelection(files: FileList | File[] | null) {
    if (!files) return;
    const pdfs = Array.from(files).filter((file) => file.name.toLowerCase().endsWith(".pdf"));
    setSelected((current) => {
      const known = new Set(current.map((file) => `${file.name}|${file.size}|${file.lastModified}`));
      return [...current, ...pdfs.filter((file) => !known.has(`${file.name}|${file.size}|${file.lastModified}`))];
    });
    setError(pdfs.length < Array.from(files).length ? t("onlyPdf") : "");
  }

  function onDrop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault();
    setDragging(false);
    if (editable && busy === null) addSelection(event.dataTransfer.files);
  }

  async function upload() {
    if (selected.length === 0) return;
    let current = batch && batch.status === "previewed" ? batch : null;
    const groups = chunked(selected);
    let sent = 0;
    setBusy("upload");
    setError("");
    setResult(null);
    setProgress({ sent: 0, total: selected.length });
    try {
      for (const group of groups) {
        const body = new FormData();
        for (const file of group) body.append("files", file);
        const path = current
          ? `TranscriptImportBatches/${encodeURIComponent(current.transcriptImportBatchId)}/files`
          : "TranscriptImportBatches";
        current = toBatch(await clientApiRequest<unknown>(path, { method: "POST", body, timeoutMs: 120_000 }));
        setBatch(current);
        sent += group.length;
        setProgress({ sent, total: selected.length });
      }
      setSelected([]);
      if (inputRef.current) inputRef.current.value = "";
    } catch (cause) {
      // Files sent before the failure are already in the batch; keep the rest selected to retry
      setSelected((files) => files.slice(sent));
      setError(messageOf(cause));
    } finally {
      setBusy(null);
      setProgress(null);
      void loadHistory();
    }
  }

  async function commit() {
    if (!batch) return;
    const ok = await confirm({
      title: t("commitConfirmTitle"),
      body: t("commitConfirmBody", { files: batch.totalFiles, values: batch.insertCount, replaced: batch.replaceCount }),
      confirmLabel: t("commit", { count: batch.totalFiles }),
      cancelLabel: tu("cancel"),
      icon: "upload",
    });
    if (!ok) return;
    setBusy("commit");
    setError("");
    try {
      const payload = await clientApiPost<unknown>(
        `TranscriptImportBatches/${encodeURIComponent(batch.transcriptImportBatchId)}/commit`,
        undefined,
        { timeoutMs: 120_000 },
      );
      setResult(toCommit(payload));
      setBatch(toBatch(await clientApiGet<unknown>(`TranscriptImportBatches/${encodeURIComponent(batch.transcriptImportBatchId)}`)));
    } catch (cause) {
      setError(messageOf(cause));
    } finally {
      setBusy(null);
      void loadHistory();
    }
  }

  async function cancel() {
    if (!batch) return;
    const ok = await confirm({
      title: t("cancelConfirmTitle"),
      body: t("cancelConfirmBody"),
      confirmLabel: t("cancelBatch"),
      cancelLabel: tu("cancel"),
      destructive: true,
    });
    if (!ok) return;
    setBusy("cancel");
    setError("");
    try {
      setBatch(toBatch(await clientApiPost<unknown>(`TranscriptImportBatches/${encodeURIComponent(batch.transcriptImportBatchId)}/cancel`)));
    } catch (cause) {
      setError(messageOf(cause));
    } finally {
      setBusy(null);
      void loadHistory();
    }
  }

  async function removeFile(fileId: string) {
    if (!batch) return;
    setBusy("remove");
    setError("");
    try {
      setBatch(
        toBatch(
          await clientApiDelete<unknown>(
            `TranscriptImportBatches/${encodeURIComponent(batch.transcriptImportBatchId)}/files/${encodeURIComponent(fileId)}`,
          ),
        ),
      );
    } catch (cause) {
      setError(messageOf(cause));
    } finally {
      setBusy(null);
      void loadHistory();
    }
  }

  async function openBatch(id: string) {
    setBusy("open");
    setError("");
    setResult(null);
    try {
      setBatch(toBatch(await clientApiGet<unknown>(`TranscriptImportBatches/${encodeURIComponent(id)}`)));
      setFilter("all");
    } catch (cause) {
      setError(messageOf(cause));
    } finally {
      setBusy(null);
    }
  }

  function startNew() {
    setBatch(null);
    setResult(null);
    setSelected([]);
    setFilter("all");
    setError("");
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
          {editable ? (
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
              <p className="text-[0.9375rem] font-semibold text-text">
                {batch ? t("dropMore") : t("drop")}
              </p>
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
          ) : null}

          {editable && selected.length > 0 ? (
            <div className="flex flex-wrap items-center gap-space-sm">
              <p className="mr-auto text-sm text-text">
                {t("selected", { count: selected.length })}{" "}
                <span className="font-mono text-text-faint">· {formatSize(selectedBytes)}</span>
                {oversized > 0 ? (
                  <span className="ml-space-sm text-status-attention-text">{t("oversized", { count: oversized })}</span>
                ) : null}
              </p>
              <Button size="sm" variant="tertiary" disabled={busy !== null} onClick={() => setSelected([])}>
                {t("clearSelection")}
              </Button>
              <Button icon="fact_check" loading={busy === "upload"} disabled={busy !== null} onClick={() => void upload()}>
                {batch ? t("addToBatch") : t("readAndCheck")}
              </Button>
            </div>
          ) : null}

          {progress ? (
            <div role="status" aria-live="polite" className="flex flex-col gap-1">
              <div className="h-1.5 overflow-hidden rounded-full bg-surface-container">
                <div
                  className="h-full rounded-full bg-ink transition-[width] duration-300"
                  style={{ width: `${Math.round((progress.sent / Math.max(progress.total, 1)) * 100)}%` }}
                />
              </div>
              <p className="font-mono text-[0.75rem] text-text-faint">
                {t("progress", { sent: progress.sent, total: progress.total })}
              </p>
            </div>
          ) : null}

          {error ? (
            <Notice tone="urgent" icon="error" title={error} />
          ) : null}

          {result ? (
            <Notice
              tone="ontrack"
              icon="check_circle"
              title={t("doneTitle", { count: result.committedFiles })}
              actions={
                <Button size="sm" variant="secondary" icon="add" onClick={startNew}>
                  {t("newBatch")}
                </Button>
              }
            >
              {t("doneBody", { values: result.insertedCount, replaced: result.replacedCount })}
            </Notice>
          ) : null}

          {batch ? (
            <BatchPreview
              batch={batch}
              filter={filter}
              setFilter={setFilter}
              busy={busy}
              onRemove={(fileId) => void removeFile(fileId)}
              onCommit={() => void commit()}
              onCancel={() => void cancel()}
              onNew={startNew}
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
              <li key={item.transcriptImportBatchId} className="flex flex-wrap items-center gap-x-space-md gap-y-1 px-space-lg py-space-sm">
                <span className="font-mono text-[0.8125rem] text-text-faint">{formatDateTime(item.createdAt)}</span>
                <StatusChip size="sm" tone={batchTone[item.status] ?? "sealed"} label={t(`batchStatus.${item.status === "committed" || item.status === "cancelled" ? item.status : "previewed"}`)} />
                <span className="text-sm text-text">
                  {t("historyFiles", { total: item.totalFiles, valid: item.validFiles + item.committedFiles, invalid: item.invalidFiles })}
                </span>
                <span className="text-[0.8125rem] text-text-faint">{item.importedByName}</span>
                <Button
                  size="sm"
                  variant="quiet"
                  className="ml-auto"
                  iconAfter="chevron_right"
                  disabled={busy !== null}
                  onClick={() => void openBatch(item.transcriptImportBatchId)}
                >
                  {item.status === "previewed" ? t("resume") : t("view")}
                </Button>
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

function BatchPreview({
  batch,
  filter,
  setFilter,
  busy,
  onRemove,
  onCommit,
  onCancel,
  onNew,
}: {
  batch: Batch;
  filter: "all" | "invalid" | "warnings";
  setFilter: (value: "all" | "invalid" | "warnings") => void;
  busy: string | null;
  onRemove: (fileId: string) => void;
  onCommit: () => void;
  onCancel: () => void;
  onNew: () => void;
}) {
  const t = useTranslations("ui.transcriptImport");
  const previewing = batch.status === "previewed";
  const withWarnings = batch.files.filter((file) => file.status !== "invalid" && file.warnings.length > 0).length;
  const shown = useMemo(
    () =>
      batch.files
        .filter((file) =>
          filter === "invalid" ? file.status === "invalid" : filter === "warnings" ? file.warnings.length > 0 : true,
        )
        // Problems first, so a long batch does not hide its few bad files
        .sort((a, b) => Number(b.status === "invalid") - Number(a.status === "invalid")),
    [batch.files, filter],
  );
  const paged = usePaged(shown, PAGE_SIZE);
  const statusKey = batch.status === "committed" || batch.status === "cancelled" ? batch.status : "previewed";

  return (
    <section aria-labelledby="transcript-batch-title" className="flex flex-col gap-space-md">
      <div className="flex flex-wrap items-center gap-x-space-md gap-y-1">
        <h3 id="transcript-batch-title" className="text-sm font-semibold text-text">
          {t("batchTitle")}
        </h3>
        <StatusChip size="sm" tone={batchTone[batch.status] ?? "sealed"} label={t(`batchStatus.${statusKey}`)} />
        <span className="font-mono text-[0.75rem] text-text-faint">
          {formatDateTime(batch.createdAt)}
          {batch.importedByName ? ` · ${batch.importedByName}` : ""}
        </span>
      </div>

      <dl className="grid grid-cols-2 gap-px overflow-hidden rounded-md border border-rule bg-rule sm:grid-cols-4">
        {[
          { label: t("countTotal"), value: batch.totalFiles },
          { label: previewing ? t("countValid") : t("countCommitted"), value: previewing ? batch.validFiles : batch.committedFiles, tone: "text-status-ontrack-text" },
          { label: t("countInvalid"), value: batch.invalidFiles, tone: batch.invalidFiles ? "text-red-ink" : "" },
          { label: t("countWarnings"), value: withWarnings, tone: withWarnings ? "text-status-attention-text" : "" },
        ].map((tile) => (
          <div key={tile.label} className="bg-paper px-space-md py-space-sm">
            <dt className="text-[0.8125rem] text-text-muted">{tile.label}</dt>
            <dd className={cn("mt-0.5 font-mono text-xl font-semibold text-text", tile.tone)}>{tile.value}</dd>
          </div>
        ))}
      </dl>

      {previewing && batch.invalidFiles > 0 ? (
        <Notice tone="attention" icon="error" title={t("invalidBlocksTitle", { count: batch.invalidFiles })}>
          {t("invalidBlocksBody")}
        </Notice>
      ) : null}

      <div role="group" aria-label={t("filterLabel")} className="flex flex-wrap gap-1.5">
        {(
          [
            ["all", t("filterAll", { count: batch.files.length })],
            ["invalid", t("filterInvalid", { count: batch.invalidFiles })],
            ["warnings", t("filterWarnings", { count: batch.files.filter((file) => file.warnings.length > 0).length })],
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
            {paged.items.map((file) => (
              <tr key={file.transcriptImportFileId} className="align-top">
                <td className="max-w-[16rem] px-space-md py-space-sm">
                  <span className="block truncate font-mono text-[0.8125rem] text-text" title={file.fileName}>
                    {file.fileName}
                  </span>
                </td>
                <td className="px-space-sm py-space-sm">
                  {file.studentCode ? (
                    <>
                      <span className="font-mono text-[0.8125rem] text-text">{file.studentCode}</span>
                      <span className="block text-[0.8125rem] text-text-muted">{file.studentName || file.fullNameInFile}</span>
                    </>
                  ) : (
                    <span className="text-text-faint">—</span>
                  )}
                </td>
                <td className="whitespace-nowrap px-space-sm py-space-sm text-text-muted">
                  {file.importType === "grade" ? t("typeGrade") : file.importType === "attendance" ? t("typeAttendance") : "—"}
                </td>
                <td className="px-space-sm py-space-sm text-right font-mono text-text">
                  {file.rowCount}
                  {file.skippedRowCount ? <span className="block whitespace-nowrap text-[0.75rem] text-text-faint">+{file.skippedRowCount} {t("skipped")}</span> : null}
                </td>
                <td className="whitespace-nowrap px-space-sm py-space-sm text-right font-mono text-[0.8125rem] text-text-muted">
                  {file.status === "invalid" ? "—" : t("changes", { values: file.insertCount, replaced: file.replaceCount })}
                </td>
                <td className="px-space-md py-space-sm">
                  <StatusChip
                    size="sm"
                    tone={file.status !== "invalid" && file.warnings.length ? "attention" : fileTone[file.status] ?? "sealed"}
                    label={t(`fileStatus.${file.status === "invalid" || file.status === "committed" ? file.status : "valid"}`)}
                  />
                  {file.errors.length || file.warnings.length ? (
                    <ul className="mt-1 flex max-w-[28rem] flex-col gap-0.5 text-[0.8125rem] leading-5">
                      {file.errors.slice(0, 6).map((message) => (
                        <li key={`e-${message}`} className="text-red-ink">{message}</li>
                      ))}
                      {file.errors.length > 6 ? (
                        <li className="text-red-ink">{t("moreErrors", { count: file.errors.length - 6 })}</li>
                      ) : null}
                      {file.warnings.map((message) => (
                        <li key={`w-${message}`} className="text-status-attention-text">{message}</li>
                      ))}
                    </ul>
                  ) : null}
                  {previewing ? (
                    <Button
                      size="sm"
                      variant="quiet"
                      icon="delete"
                      className="mt-1"
                      disabled={busy !== null}
                      loading={busy === "remove"}
                      onClick={() => onRemove(file.transcriptImportFileId)}
                    >
                      {t("removeFile")}
                    </Button>
                  ) : null}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {shown.length === 0 ? <p className="px-space-lg py-space-md text-sm text-text-muted">{t("noFilesForFilter")}</p> : null}
      </div>
      <Pager {...paged} />

      {previewing ? (
        <div className="flex flex-wrap items-center gap-space-sm border-t border-rule pt-space-md">
          <p className="mr-auto max-w-[60ch] text-[0.8125rem] leading-5 text-text-faint">{t("commitHint")}</p>
          <Button variant="secondary" icon="delete" disabled={busy !== null} loading={busy === "cancel"} onClick={onCancel}>
            {t("cancelBatch")}
          </Button>
          <Button
            icon="upload"
            disabled={busy !== null || batch.totalFiles === 0 || batch.invalidFiles > 0}
            loading={busy === "commit"}
            onClick={onCommit}
          >
            {t("commit", { count: batch.totalFiles })}
          </Button>
        </div>
      ) : (
        <div className="flex justify-end border-t border-rule pt-space-md">
          <Button variant="secondary" icon="add" onClick={onNew}>
            {t("newBatch")}
          </Button>
        </div>
      )}
    </section>
  );
}
