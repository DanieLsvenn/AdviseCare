"use client";

import { useCallback, useEffect, useState, type FormEvent, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { ApiError, isRecord } from "@/lib/api-contract";
import { clientApiGet, clientApiPost } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { listOf, str, unwrap } from "@/components/case/case-api";
import { Button, ButtonLink } from "@/components/ui/button";
import { controlClass } from "@/components/ui/field";
import { Icon } from "@/components/ui/icon";
import { Notice } from "@/components/ui/notice";
import { PageHeader } from "@/components/ui/page-header";
import { SkeletonRows } from "@/components/ui/states";

type SupportType = "academic" | "wellbeing";

interface Category {
  id: string;
  name: string;
  description: string;
  supportType: SupportType;
}

interface Shareable {
  purposeName: string;
  canShare: boolean;
  attributes: Array<{ id: string; name: string; description: string; alreadyShared: boolean }>;
}

const REASON_MAX = 4000;
const DRAFT_KEY = "advisecare.requestDraft";

function readDraft(): { type?: string; reason?: string } {
  try {
    const raw = window.localStorage.getItem(DRAFT_KEY);
    return raw ? (JSON.parse(raw) as { type?: string; reason?: string }) : {};
  } catch {
    return {};
  }
}

function writeDraft(value: { type: string; reason: string } | null) {
  try {
    if (value && (value.type || value.reason)) {
      window.localStorage.setItem(DRAFT_KEY, JSON.stringify(value));
    } else {
      window.localStorage.removeItem(DRAFT_KEY);
    }
  } catch {
    // Storage unavailable (private mode): the form still works, just without a draft.
  }
}

/**
 * The student's way in (SRS: "Request support"): kind of help → category
 * (and subject for academic help) → reason in their own words → which data
 * the advisor may see. Nothing is shared unless ticked.
 */
export function SupportRequestForm() {
  const t = useTranslations("ui.request");
  const tw = useTranslations("workspace");
  const [categories, setCategories] = useState<Category[]>([]);
  const [subjects, setSubjects] = useState<Array<{ id: string; label: string }>>([]);
  const [shareable, setShareable] = useState<Shareable | null>(null);
  const [type, setType] = useState<SupportType | "">("");
  const [categoryId, setCategoryId] = useState("");
  const [subjectId, setSubjectId] = useState("");
  const [reason, setReason] = useState("");
  const [sharedIds, setSharedIds] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [detailsLoading, setDetailsLoading] = useState(false);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const [createdCaseId, setCreatedCaseId] = useState<string | null>(null);
  const [restored, setRestored] = useState(false);

  const loadCategories = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const payload = await clientApiGet<unknown>("SupportCategories/search", {
        query: { status: "active", pageSize: 100 },
      });
      setCategories(
        listOf(payload)
          .map((record) => ({
            id: str(record.supportCategoryId ?? record.id),
            name: str(record.categoryName ?? record.name),
            description: str(record.description),
            supportType: (str(record.supportType).toLowerCase() === "wellbeing"
              ? "wellbeing"
              : "academic") as SupportType,
          }))
          .filter((category) => category.id && category.name),
      );
    } catch (loadError) {
      setError(messageFor(loadError, tw));
    } finally {
      setLoading(false);
    }
  }, [tw]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      const draft = readDraft();
      if (draft.reason) {
        setReason(draft.reason);
        setRestored(true);
      }
      if (draft.type === "academic" || draft.type === "wellbeing") setType(draft.type);
      void loadCategories();
    }, 0);
    return () => window.clearTimeout(timer);
  }, [loadCategories]);

  useEffect(() => {
    if (createdCaseId !== null) return;
    const timer = window.setTimeout(() => writeDraft({ type, reason }), 400);
    return () => window.clearTimeout(timer);
  }, [type, reason, createdCaseId]);

  async function chooseCategory(nextId: string) {
    setCategoryId(nextId);
    setSubjectId("");
    setSharedIds([]);
    setShareable(null);
    setError("");
    const category = categories.find((item) => item.id === nextId);
    if (!category) return;
    setDetailsLoading(true);
    try {
      const [shareablePayload, subjectPayload] = await Promise.all([
        clientApiGet<unknown>(
          `SupportCategories/${encodeURIComponent(nextId)}/shareable-attributes`,
        ),
        category.supportType === "academic" && subjects.length === 0
          ? clientApiGet<unknown>("Subjects/search", { query: { status: "active", pageSize: 200 } })
          : Promise.resolve(null),
      ]);
      const data = unwrap(shareablePayload);
      setShareable({
        purposeName: str(data?.purposeName),
        canShare: data?.canShare !== false,
        attributes: listOf(data?.attributes ?? data).map((record) => ({
          id: str(record.dataAttributeId ?? record.id),
          name: str(record.attributeName ?? record.name),
          description: str(record.attributeDescription ?? record.description),
          alreadyShared: record.alreadyShared === true,
        })),
      });
      if (subjectPayload) {
        setSubjects(
          listOf(subjectPayload)
            .map((record) => ({
              id: str(record.subjectId ?? record.id),
              label: [str(record.subjectCode), str(record.subjectName ?? record.name)]
                .filter(Boolean)
                .join(" — "),
            }))
            .filter((subject) => subject.id && subject.label),
        );
      }
    } catch (loadError) {
      setError(messageFor(loadError, tw));
    } finally {
      setDetailsLoading(false);
    }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setError("");
    try {
      const response = await clientApiPost<unknown>("Cases", {
        supportCategoryId: categoryId,
        ...(subjectId ? { subjectId } : {}),
        requestReason: reason.trim(),
        sharedAttributeIds: sharedIds,
      });
      if (isRecord(response) && response.isSuccess === false) {
        throw new ApiError({
          message: typeof response.message === "string" ? response.message : tw("actionFailed"),
          status: 400,
          code: "case_creation_failed",
        });
      }
      writeDraft(null);
      setCreatedCaseId(str(unwrap(response)?.caseId) || "");
    } catch (submitError) {
      setError(messageFor(submitError, tw));
    } finally {
      setPending(false);
    }
  }

  const visibleCategories = categories.filter((category) => category.supportType === type);
  const category = categories.find((item) => item.id === categoryId);
  const needsSubject = category?.supportType === "academic";
  const ready = Boolean(categoryId) && reason.trim().length > 0 && !detailsLoading;
  // What the advisor will actually be able to read: new ticks plus what was already shared.
  const visibleToAdvisor = (shareable?.attributes ?? []).filter(
    (attribute) => attribute.alreadyShared || sharedIds.includes(attribute.id),
  );

  if (createdCaseId !== null) {
    return (
      <div className="flex flex-col gap-space-lg">
        <section className="rounded-lg border border-rule bg-paper p-space-xl">
          <span className="flex size-11 items-center justify-center rounded-full bg-status-ontrack-surface text-status-ontrack-text">
            <Icon name="check" className="text-[24px]" />
          </span>
          <h1 className="mt-space-base text-2xl font-semibold text-text">{t("sentTitle")}</h1>
          <p className="mt-1 max-w-[60ch] text-[0.9375rem] leading-6 text-text-muted">{t("sentBody")}</p>
          <NextSteps className="mt-space-lg" />
          <div className="mt-space-xl flex flex-wrap gap-space-sm">
            {createdCaseId ? (
              <ButtonLink href={`/me/cases/${createdCaseId}`} iconAfter="arrow_forward">
                {t("openRequest")}
              </ButtonLink>
            ) : (
              <ButtonLink href="/me/cases" iconAfter="arrow_forward">
                {t("openRequests")}
              </ButtonLink>
            )}
          </div>
        </section>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-space-lg">
      <PageHeader title={t("title")} description={t("description")} />

      <form onSubmit={submit} className="flex flex-col gap-space-lg">
        {/* 1. Kind of help */}
        <Step number={1} title={t("typeTitle")} hint={t("typeHint")}>
          <div role="radiogroup" aria-label={t("typeTitle")} className="grid gap-space-md sm:grid-cols-2">
            {(["academic", "wellbeing"] as const).map((option) => (
              <label
                key={option}
                className={cn(
                  "flex cursor-pointer gap-space-md rounded-lg border p-space-base transition-colors",
                  type === option
                    ? "border-ink bg-ink-wash/60 shadow-[inset_0_0_0_1px_var(--color-ink)]"
                    : "border-rule-strong hover:border-text-faint",
                )}
              >
                <input
                  type="radio"
                  name="supportType"
                  value={option}
                  checked={type === option}
                  onChange={() => {
                    setType(option);
                    setCategoryId("");
                    setShareable(null);
                    setSharedIds([]);
                  }}
                  className="mt-1 size-4 accent-[#2142b8]"
                />
                <span>
                  <span className="block font-semibold text-text">{t(`type.${option}`)}</span>
                  <span className="mt-0.5 block text-sm leading-6 text-text-muted">
                    {t(`type.${option}Hint`)}
                  </span>
                </span>
              </label>
            ))}
          </div>
          <p className="mt-space-md flex items-start gap-2 text-[0.8125rem] leading-5 text-text-faint">
            <Icon name="info" className="mt-px text-[16px]" />
            {t("moneyNote")}
          </p>
        </Step>

        {/* 2. Category and subject */}
        <Step number={2} title={t("categoryTitle")} disabled={!type} lockedHint={t("lockedType")}>
          {loading ? (
            <SkeletonRows rows={3} columns={2} label={tw("loading")} />
          ) : visibleCategories.length === 0 && type ? (
            <Notice tone="sealed" title={t("noCategories")} />
          ) : (
            <div role="radiogroup" aria-label={t("categoryTitle")} className="flex flex-col divide-y divide-rule rounded-lg border border-rule">
              {visibleCategories.map((option) => (
                <label
                  key={option.id}
                  className={cn(
                    "flex cursor-pointer items-start gap-space-md px-space-base py-space-md transition-colors first:rounded-t-lg last:rounded-b-lg",
                    categoryId === option.id ? "bg-ink-wash/60" : "hover:bg-paper-tint",
                  )}
                >
                  <input
                    type="radio"
                    name="category"
                    value={option.id}
                    checked={categoryId === option.id}
                    onChange={() => void chooseCategory(option.id)}
                    className="mt-1 size-4 accent-[#2142b8]"
                  />
                  <span>
                    <span className="block font-semibold text-text">{option.name}</span>
                    {option.description ? (
                      <span className="mt-0.5 block text-sm text-text-muted">{option.description}</span>
                    ) : null}
                  </span>
                </label>
              ))}
            </div>
          )}

          {needsSubject ? (
            <div className="mt-space-base max-w-md">
              <label htmlFor="subject" className="mb-1.5 block text-sm font-semibold text-text">
                {t("subject")}
              </label>
              <select
                id="subject"
                className={cn(controlClass, "h-11 w-full")}
                value={subjectId}
                onChange={(event) => setSubjectId(event.target.value)}
              >
                <option value="">{t("subjectNone")}</option>
                {subjects.map((subject) => (
                  <option key={subject.id} value={subject.id}>
                    {subject.label}
                  </option>
                ))}
              </select>
              <p className="mt-1 text-[0.8125rem] text-text-faint">{t("subjectHint")}</p>
            </div>
          ) : null}
        </Step>

        {/* 3. Reason */}
        <Step number={3} title={t("reasonTitle")} hint={t("reasonHint")} disabled={!categoryId} lockedHint={t("lockedCategory")}>
          <textarea
            aria-label={t("reasonTitle")}
            className={cn(controlClass, "min-h-36 w-full py-3 text-[0.9375rem] leading-6")}
            maxLength={REASON_MAX}
            placeholder={t("reasonPlaceholder")}
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            required
          />
          <div className="mt-1 flex items-center justify-between gap-space-md text-[0.75rem] text-text-faint">
            <span>{restored ? t("draftRestored") : t("draftAutosave")}</span>
            <span className="font-mono">
              {reason.length}/{REASON_MAX}
            </span>
          </div>
        </Step>

        {/* 4. Data sharing */}
        <Step number={4} title={t("shareTitle")} disabled={!categoryId} lockedHint={t("lockedCategory")}>
          {detailsLoading ? (
            <SkeletonRows rows={2} columns={2} label={tw("loading")} />
          ) : shareable && shareable.canShare && shareable.attributes.length ? (
            <>
              <p className="mb-space-md text-sm leading-6 text-text-muted">
                {t.rich("shareBody", {
                  purpose: shareable.purposeName || t("thisPurpose"),
                  strong: (chunks) => <strong className="font-semibold text-text">{chunks}</strong>,
                })}
              </p>
              <div className="flex flex-col gap-space-sm">
                {shareable.attributes.map((attribute) => {
                  const checked = attribute.alreadyShared || sharedIds.includes(attribute.id);
                  return (
                    <label
                      key={attribute.id}
                      className={cn(
                        "flex items-start gap-space-md rounded-lg border px-space-base py-space-md",
                        attribute.alreadyShared
                          ? "border-rule bg-paper-tint"
                          : checked
                            ? "border-ink bg-ink-wash/40"
                            : "cursor-pointer border-rule-strong hover:border-text-faint",
                      )}
                    >
                      <input
                        type="checkbox"
                        className="mt-1 size-4 accent-[#2142b8]"
                        checked={checked}
                        disabled={attribute.alreadyShared}
                        onChange={(event) =>
                          setSharedIds((current) =>
                            event.target.checked
                              ? [...current, attribute.id]
                              : current.filter((id) => id !== attribute.id),
                          )
                        }
                      />
                      <span className="min-w-0 flex-1">
                        <span className="flex flex-wrap items-center gap-2 font-semibold text-text">
                          {attribute.name}
                          {attribute.alreadyShared ? (
                            <span className="rounded-full border border-status-ontrack-border bg-status-ontrack-surface px-2 text-[0.75rem] font-semibold text-status-ontrack-text">
                              {t("alreadyShared")}
                            </span>
                          ) : null}
                        </span>
                        {attribute.description ? (
                          <span className="mt-0.5 block text-sm text-text-muted">{attribute.description}</span>
                        ) : null}
                      </span>
                    </label>
                  );
                })}
              </div>
              <p className="mt-space-md text-[0.8125rem] leading-5 text-text-faint">{t("shareFootnote")}</p>
            </>
          ) : categoryId ? (
            <p className="text-sm text-text-muted">{t("nothingToShare")}</p>
          ) : null}
        </Step>

        {error ? <Notice tone="urgent" icon="error" title={error} /> : null}

        <div className="flex flex-wrap items-center justify-between gap-space-md rounded-lg border border-rule bg-paper px-space-lg py-space-base">
          <div className="min-w-0 flex-1 text-sm text-text-muted">
            {visibleToAdvisor.length ? (
              <>
                <p className="font-semibold text-text">
                  {t("summaryShared", { count: visibleToAdvisor.length })}
                </p>
                <p className="mt-0.5">
                  {visibleToAdvisor
                    .map((attribute) =>
                      attribute.alreadyShared
                        ? t("summaryAlready", { name: attribute.name })
                        : attribute.name,
                    )
                    .join(", ")}
                </p>
              </>
            ) : (
              <p>{t("summaryNothingShared")}</p>
            )}
          </div>
          <Button type="submit" size="lg" disabled={!ready} loading={pending} iconAfter="send">
            {t("send")}
          </Button>
        </div>
      </form>

      <section className="rounded-lg border border-rule bg-paper p-space-lg">
        <h2 className="text-base font-semibold text-text">{t("nextTitle")}</h2>
        <NextSteps className="mt-space-md" />
        <p className="mt-space-md text-sm text-text-muted">
          <Link className="font-semibold text-ink hover:underline" href="/me/cases">
            {t("seeRequests")}
          </Link>
        </p>
      </section>
    </div>
  );
}

function Step({
  number,
  title,
  hint,
  disabled,
  lockedHint,
  children,
}: {
  number: number;
  title: string;
  hint?: string;
  disabled?: boolean;
  /** Shown instead of the step's controls until the previous step is done. */
  lockedHint?: string;
  children: ReactNode;
}) {
  return (
    <fieldset
      disabled={disabled}
      aria-describedby={disabled && lockedHint ? `step-${number}-locked` : undefined}
      className="rounded-lg border border-rule bg-paper p-space-lg"
    >
      <legend className="float-left mb-space-base flex w-full items-start gap-space-md">
        <span
          aria-hidden
          className={cn(
            "flex size-7 shrink-0 items-center justify-center rounded-full border font-mono text-[0.8125rem] font-medium",
            disabled ? "border-rule-strong text-text-faint" : "border-ink text-ink",
          )}
        >
          {number}
        </span>
        <span>
          <span className={cn("block text-[1.0625rem] font-semibold leading-7", disabled ? "text-text-faint" : "text-text")}>
            {title}
          </span>
          {disabled && lockedHint ? (
            <span id={`step-${number}-locked`} className="block text-sm leading-6 text-text-faint">
              {lockedHint}
            </span>
          ) : hint ? (
            <span className="block text-sm leading-6 text-text-muted">{hint}</span>
          ) : null}
        </span>
      </legend>
      {disabled ? null : <div className="clear-both sm:pl-10">{children}</div>}
    </fieldset>
  );
}

function NextSteps({ className }: { className?: string }) {
  const t = useTranslations("ui.request");
  return (
    <ol className={cn("grid gap-space-md sm:grid-cols-3", className)}>
      {[1, 2, 3].map((step) => (
        <li key={step} className="border-t-2 border-rule pt-space-sm">
          <span className="font-mono text-[0.75rem] text-text-faint">0{step}</span>
          <p className="mt-0.5 text-sm font-semibold text-text">{t(`next${step}Title`)}</p>
          <p className="mt-0.5 text-[0.8125rem] leading-5 text-text-muted">{t(`next${step}Body`)}</p>
        </li>
      ))}
    </ol>
  );
}

function messageFor(error: unknown, t: (key: "apiErrorNetwork" | "actionFailed") => string) {
  if (error instanceof ApiError && error.status < 500 && error.message) return error.message;
  return error instanceof ApiError && error.status < 500 ? t("actionFailed") : t("apiErrorNetwork");
}
