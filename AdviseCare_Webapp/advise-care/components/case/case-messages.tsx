"use client";

import { Fragment, useCallback, useEffect, useRef, useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { ApiError, isRecord } from "@/lib/api-contract";
import { clientApiGet, clientApiRequest } from "@/lib/client-api";
import {
  byDate,
  listOf,
  toCaseMessage,
  unwrap,
  type CaseMessageAttachment,
  type CaseMessageRecord,
} from "@/components/case/case-api";
import { formatDateTime } from "@/lib/format";
import { Button } from "@/components/ui/button";
import { controlClass } from "@/components/ui/field";
import { Icon } from "@/components/ui/icon";
import { EmptyState, ErrorState, SkeletonRows } from "@/components/ui/states";
import { cn } from "@/lib/cn";

const MESSAGE_MAX = 1000;
// Same limits as the API (CaseMessageService): checked here first so a wrong file never travels.
const MAX_FILES = 5;
const MAX_FILE_BYTES = 10 * 1024 * 1024;
const FILE_TYPES = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".png", ".jpg", ".jpeg", ".gif", ".webp"];

/**
 * The case thread (CASEMESSAGES): the student of the case and its current
 * advisor both write here, oldest first; each message notifies the other side.
 * Messages cannot be edited or deleted, and nothing can be written while the
 * case is closed. Private case notes are never shown here.
 */
export function CaseMessages({
  caseId,
  viewerId,
  audience,
  canWrite,
  closed = false,
  waitingForAdvisor = false,
  className = "",
}: {
  caseId: string;
  viewerId: string;
  audience: "student" | "staff";
  canWrite: boolean;
  closed?: boolean;
  waitingForAdvisor?: boolean;
  className?: string;
}) {
  const t = useTranslations("ui.caseMessages");
  const [messages, setMessages] = useState<CaseMessageRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [draft, setDraft] = useState("");
  const [saving, setSaving] = useState(false);
  const [submitError, setSubmitError] = useState("");
  const [statusMessage, setStatusMessage] = useState("");
  const [files, setFiles] = useState<File[]>([]);
  const [fileError, setFileError] = useState("");
  const fileInput = useRef<HTMLInputElement>(null);

  function addFiles(list: FileList | null) {
    if (!list?.length) return;
    const next = [...files];
    let problem = "";
    for (const file of Array.from(list)) {
      const extension = file.name.includes(".") ? file.name.slice(file.name.lastIndexOf(".")).toLowerCase() : "";
      if (!FILE_TYPES.includes(extension)) problem = t("fileTypeError", { name: file.name });
      else if (file.size === 0) problem = t("fileEmptyError", { name: file.name });
      else if (file.size > MAX_FILE_BYTES) problem = t("fileSizeError", { name: file.name });
      else if (next.length >= MAX_FILES) problem = t("fileCountError", { count: MAX_FILES });
      else if (!next.some((item) => item.name === file.name && item.size === file.size)) next.push(file);
    }
    setFiles(next);
    setFileError(problem);
    if (fileInput.current) fileInput.current.value = "";
  }

  const load = useCallback(async () => {
    setLoading(true);
    setLoadError("");
    try {
      const payload = await clientApiGet<unknown>("CaseMessages/search", { query: { caseId } });
      setMessages(byDate(listOf(payload).map(toCaseMessage), (item) => item.createdAt));
    } catch (cause) {
      setLoadError(
        cause instanceof ApiError && cause.status < 500 && cause.message ? cause.message : t("loadError"),
      );
    } finally {
      setLoading(false);
    }
  }, [caseId, t]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  async function send(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const messageText = draft.trim();
    if (!messageText || saving || !canWrite) return;

    setSaving(true);
    setSubmitError("");
    setStatusMessage("");
    try {
      let body: FormData | Record<string, unknown> = { caseId, messageText };
      if (files.length) {
        const form = new FormData();
        form.append("caseId", caseId);
        form.append("messageText", messageText);
        for (const file of files) form.append("files", file);
        body = form;
      }
      const payload = await clientApiRequest<unknown>(files.length ? "CaseMessages/with-files" : "CaseMessages", {
        method: "POST",
        body,
        redirectOnUnauthorized: true,
        ...(files.length ? { timeoutMs: 120_000 } : {}),
      });
      if (isRecord(payload) && payload.isSuccess === false) {
        throw new Error(typeof payload.message === "string" ? payload.message : t("sendError"));
      }
      const created = unwrap(payload);
      const message = created ? toCaseMessage(created) : null;
      if (!message?.caseMessageId) throw new Error(t("sendError"));
      setMessages((current) => byDate([...current, message], (item) => item.createdAt));
      setDraft("");
      setFiles([]);
      setFileError("");
      setStatusMessage(audience === "student" ? t("sentStudent") : t("sentAdvisor"));
    } catch (cause) {
      setSubmitError(
        cause instanceof ApiError && cause.status < 500 && cause.message
          ? cause.message
          : cause instanceof Error && !(cause instanceof ApiError)
            ? cause.message
            : t("sendError"),
      );
    } finally {
      setSaving(false);
    }
  }

  const readOnlyHint = closed
    ? t("closedHint")
    : waitingForAdvisor
      ? t("waitingHint")
      : audience === "staff" && !canWrite
        ? t("readOnlyHint")
        : "";

  return (
    <section className={cn("rounded-lg border border-rule bg-paper p-space-lg", className)}>
      <div className="mb-space-md flex items-start gap-space-sm">
        <Icon name="forum" className="mt-0.5 text-[20px] text-ink" />
        <div>
          <h2 className="text-base font-semibold text-text">{t("title")}</h2>
          <p className="mt-1 text-[0.8125rem] leading-5 text-text-muted">
            {audience === "student" ? t("studentIntro") : t("staffIntro")}
          </p>
        </div>
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
      ) : messages.length ? (
        <ol className="flex flex-col gap-space-sm" aria-label={t("title")}>
          {messages.map((message) => {
            const mine = message.authorId === viewerId;
            return (
              <li
                key={message.caseMessageId}
                className={cn(
                  "max-w-[92%] rounded-md border p-space-base",
                  mine ? "self-end border-ink/30 bg-paper-tint" : "self-start border-rule bg-paper",
                )}
              >
                <div className="flex flex-wrap items-baseline justify-between gap-x-space-md gap-y-1">
                  <p className="text-sm font-semibold text-text">
                    {mine ? t("you") : message.authorFullName || t(message.authorRole === "student" ? "studentLabel" : "advisorLabel")}
                    <span className="ml-space-sm text-[0.75rem] font-normal text-text-faint">
                      {t(message.authorRole === "student" ? "studentLabel" : "advisorLabel")}
                    </span>
                  </p>
                  <time dateTime={message.createdAt} className="font-mono text-[0.75rem] text-text-faint">
                    {formatDateTime(message.createdAt)}
                  </time>
                </div>
                <p className="mt-space-sm whitespace-pre-wrap break-words text-sm leading-6 text-text">
                  {renderMessage(message.messageText)}
                </p>
                {message.attachments.length ? (
                  <ul className="mt-space-sm flex flex-wrap gap-1.5" aria-label={t("attachments")}>
                    {message.attachments.map((file) => (
                      <li key={file.caseMessageAttachmentId}>
                        <AttachmentLink file={file} downloadLabel={t("download")} />
                      </li>
                    ))}
                  </ul>
                ) : null}
              </li>
            );
          })}
        </ol>
      ) : (
        <EmptyState icon="forum" title={t("empty")}>
          {audience === "student" ? t("emptyStudentHint") : t("emptyStaffHint")}
        </EmptyState>
      )}

      {canWrite ? (
        <form onSubmit={send} className="mt-space-md border-t border-rule pt-space-md">
          <label htmlFor={`case-message-${caseId}`} className="mb-1.5 block text-sm font-semibold text-text">
            {audience === "student" ? t("sendLabelStudent") : t("sendLabelAdvisor")}
          </label>
          <textarea
            id={`case-message-${caseId}`}
            className={`${controlClass} min-h-24 w-full py-2 leading-6`}
            maxLength={MESSAGE_MAX}
            placeholder={audience === "student" ? t("placeholderStudent") : t("placeholderAdvisor")}
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            required
          />
          <div className="mt-space-sm flex flex-wrap items-center gap-space-sm">
            <input
              ref={fileInput}
              id={`case-message-files-${caseId}`}
              type="file"
              multiple
              accept={FILE_TYPES.join(",")}
              className="sr-only"
              onChange={(event) => addFiles(event.target.files)}
            />
            <Button
              size="sm"
              variant="secondary"
              icon="attach_file"
              disabled={files.length >= MAX_FILES || saving}
              onClick={() => fileInput.current?.click()}
            >
              {t("attach")}
            </Button>
            <span className="text-[0.75rem] text-text-faint">{t("attachHint", { count: MAX_FILES })}</span>
          </div>
          {files.length ? (
            <ul className="mt-space-sm flex flex-wrap gap-1.5" aria-label={t("attachments")}>
              {files.map((file, index) => (
                <li
                  key={`${file.name}-${file.size}`}
                  className="inline-flex items-center gap-1.5 rounded-md border border-rule bg-paper-tint py-1 pl-2 pr-1 text-[0.8125rem] text-text"
                >
                  <Icon name={fileIcon(file.name)} className="text-[16px] text-text-faint" />
                  <span className="max-w-56 truncate">{file.name}</span>
                  <span className="font-mono text-[0.75rem] text-text-faint">{formatSize(file.size)}</span>
                  <button
                    type="button"
                    aria-label={`${t("removeFile")} ${file.name}`}
                    title={t("removeFile")}
                    onClick={() => setFiles(files.filter((_, position) => position !== index))}
                    className="inline-flex size-6 items-center justify-center rounded text-text-faint hover:bg-status-urgent-surface hover:text-red-ink"
                  >
                    <Icon name="close" className="text-[16px]" />
                  </button>
                </li>
              ))}
            </ul>
          ) : null}
          {fileError ? (
            <p role="alert" className="mt-1 text-[0.8125rem] text-red-ink">{fileError}</p>
          ) : null}
          <div className="mt-space-sm flex flex-wrap items-center justify-between gap-space-sm">
            <p aria-live="polite" className="text-[0.8125rem] text-text-faint">
              {submitError || statusMessage || t("limitHint")}
              <span className="ml-space-sm font-mono">
                {draft.length}/{MESSAGE_MAX}
              </span>
            </p>
            <Button type="submit" loading={saving} disabled={!draft.trim()} icon="send">
              {t("send")}
            </Button>
          </div>
        </form>
      ) : readOnlyHint ? (
        <p className="mt-space-md border-t border-rule pt-space-md text-[0.8125rem] leading-5 text-text-muted">
          {readOnlyHint}
        </p>
      ) : null}
    </section>
  );
}

function AttachmentLink({ file, downloadLabel }: { file: CaseMessageAttachment; downloadLabel: string }) {
  return (
    <a
      href={`/api/CaseMessages/attachments/${encodeURIComponent(file.caseMessageAttachmentId)}`}
      download={file.fileName}
      title={`${downloadLabel}: ${file.fileName}`}
      className="inline-flex items-center gap-1.5 rounded-md border border-rule bg-paper px-2 py-1 text-[0.8125rem] text-ink hover:border-ink hover:bg-ink-wash"
    >
      <Icon name={fileIcon(file.fileName)} className="text-[16px]" />
      <span className="max-w-56 truncate">{file.fileName}</span>
      <span className="font-mono text-[0.75rem] text-text-faint">{formatSize(file.fileSize)}</span>
    </a>
  );
}

function fileIcon(name: string): string {
  const lower = name.toLowerCase();
  if (/\.(png|jpe?g|gif|webp)$/.test(lower)) return "image";
  if (/\.(xlsx?|csv)$/.test(lower)) return "table_chart";
  if (/\.pdf$/.test(lower)) return "picture_as_pdf";
  return "description";
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function renderMessage(message: string) {
  const parts = message.split(/(https?:\/\/[^\s]+)/g);
  return parts.map((part, index) =>
    /^https?:\/\//i.test(part) ? (
      <Fragment key={`${index}-${part}`}>
        <a href={part} target="_blank" rel="noopener noreferrer" className="break-all text-ink underline underline-offset-2">
          {part}
        </a>
      </Fragment>
    ) : (
      <Fragment key={`${index}-${part}`}>{part}</Fragment>
    ),
  );
}
