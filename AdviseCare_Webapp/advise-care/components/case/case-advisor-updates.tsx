"use client";

import { Fragment, useCallback, useEffect, useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { ApiError, isRecord } from "@/lib/api-contract";
import { clientApiGet, clientApiRequest } from "@/lib/client-api";
import { byDate, listOf, toCaseAdvisorUpdate, unwrap, type CaseAdvisorUpdateRecord } from "@/components/case/case-api";
import { formatDateTime } from "@/lib/format";
import { Button } from "@/components/ui/button";
import { controlClass } from "@/components/ui/field";
import { Icon } from "@/components/ui/icon";
import { EmptyState, ErrorState, SkeletonRows } from "@/components/ui/states";

type Audience = "student" | "advisor";

export function CaseAdvisorUpdates({
  caseId,
  audience,
  closed = false,
  className = "",
}: {
  caseId: string;
  audience: Audience;
  closed?: boolean;
  className?: string;
}) {
  const t = useTranslations("ui.caseUpdates");
  const [updates, setUpdates] = useState<CaseAdvisorUpdateRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [draft, setDraft] = useState("");
  const [saving, setSaving] = useState(false);
  const [submitError, setSubmitError] = useState("");
  const [statusMessage, setStatusMessage] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    setLoadError("");
    try {
      const payload = await clientApiGet<unknown>(
        `Cases/${encodeURIComponent(caseId)}/advisor-updates`,
      );
      setUpdates(byDate(listOf(payload).map(toCaseAdvisorUpdate), (item) => item.createdAt));
    } catch (cause) {
      setLoadError(
        cause instanceof ApiError && cause.status < 500 && cause.message
          ? cause.message
          : t("loadError"),
      );
    } finally {
      setLoading(false);
    }
  }, [caseId, t]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  async function publish(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const messageText = draft.trim();
    if (!messageText || saving || closed) return;

    setSaving(true);
    setSubmitError("");
    setStatusMessage("");
    try {
      const payload = await clientApiRequest<unknown>(
        `Cases/${encodeURIComponent(caseId)}/advisor-updates`,
        {
          method: "POST",
          body: { messageText },
          redirectOnUnauthorized: true,
        },
      );
      if (isRecord(payload) && payload.isSuccess === false) {
        throw new Error(typeof payload.message === "string" ? payload.message : t("sendError"));
      }

      const created = unwrap(payload);
      if (!created) throw new Error(t("sendError"));

      const update = toCaseAdvisorUpdate(created);
      if (!update.caseAdvisorUpdateId) throw new Error(t("sendError"));
      setUpdates((current) => byDate([...current, update], (item) => item.createdAt));
      setDraft("");
      setStatusMessage(t("sent"));
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

  return (
    <section className={`rounded-lg border border-rule bg-paper p-space-lg ${className}`}>
      <div className="mb-space-md flex items-start gap-space-sm">
        <Icon name="mark_chat_read" className="mt-0.5 text-[20px] text-ink" />
        <div>
          <h2 className="text-base font-semibold text-text">{t("title")}</h2>
          <p className="mt-1 text-[0.8125rem] leading-5 text-text-muted">
            {audience === "student" ? t("studentIntro") : t("advisorIntro")}
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
      ) : updates.length ? (
        <ol className="flex flex-col gap-space-sm" aria-label={t("title")}>
          {updates.map((update) => (
            <li
              key={update.caseAdvisorUpdateId}
              className="rounded-md border border-rule bg-paper-tint p-space-base"
            >
              <div className="flex flex-wrap items-baseline justify-between gap-x-space-md gap-y-1">
                <p className="text-sm font-semibold text-text">
                  {update.authorFullName || t("advisorLabel")}
                </p>
                <time
                  dateTime={update.createdAt}
                  className="font-mono text-[0.75rem] text-text-faint"
                >
                  {formatDateTime(update.createdAt)}
                </time>
              </div>
              <p className="mt-space-sm whitespace-pre-wrap break-words text-sm leading-6 text-text">
                {renderMessage(update.messageText)}
              </p>
            </li>
          ))}
        </ol>
      ) : (
        <EmptyState icon="forum" title={audience === "student" ? t("emptyStudent") : t("emptyAdvisor")}>
          {audience === "student" ? t("emptyStudentHint") : t("emptyAdvisorHint")}
        </EmptyState>
      )}

      {audience === "student" ? (
        <p className="mt-space-md border-t border-rule pt-space-md text-[0.8125rem] leading-5 text-text-muted">
          {t("studentFollowUp")}
        </p>
      ) : closed ? (
        <p className="mt-space-md border-t border-rule pt-space-md text-[0.8125rem] leading-5 text-text-muted">
          {t("closedHint")}
        </p>
      ) : (
        <form onSubmit={publish} className="mt-space-md border-t border-rule pt-space-md">
          <label htmlFor={`advisor-update-${caseId}`} className="mb-1.5 block text-sm font-semibold text-text">
            {t("sendLabel")}
          </label>
          <textarea
            id={`advisor-update-${caseId}`}
            className={`${controlClass} min-h-24 w-full py-2 leading-6`}
            maxLength={1000}
            placeholder={t("placeholder")}
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            required
          />
          <div className="mt-space-sm flex flex-wrap items-center justify-between gap-space-sm">
            <p aria-live="polite" className="text-[0.8125rem] text-text-faint">
              {submitError || statusMessage || t("limitHint")}
              <span className="ml-space-sm font-mono">{draft.length}/1000</span>
            </p>
            <Button type="submit" loading={saving} disabled={!draft.trim() || closed} icon="send">
              {t("send")}
            </Button>
          </div>
        </form>
      )}
    </section>
  );
}

function renderMessage(message: string) {
  const parts = message.split(/(https?:\/\/[^\s]+)/g);
  return parts.map((part, index) =>
    /^https?:\/\//i.test(part) ? (
      <Fragment key={`${index}-${part}`}>
        <a
          href={part}
          target="_blank"
          rel="noopener noreferrer"
          className="break-all text-ink underline underline-offset-2"
        >
          {part}
        </a>
      </Fragment>
    ) : (
      <Fragment key={`${index}-${part}`}>{part}</Fragment>
    ),
  );
}
