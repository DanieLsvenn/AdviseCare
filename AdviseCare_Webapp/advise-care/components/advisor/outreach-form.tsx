"use client";

import { useCallback, useEffect, useState, type FormEvent, type KeyboardEvent, type ReactNode } from "react";
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
type Priority = "low" | "medium" | "high" | "urgent";

interface Category {
  id: string;
  name: string;
  description: string;
  supportType: SupportType;
}

interface Student {
  id: string;
  name: string;
  code: string;
  email: string;
  faculty: string;
}

const PRIORITIES: Priority[] = ["low", "medium", "high", "urgent"];
const REASON_MAX = 4000;
const RESULT_LIMIT = 20;

/**
 * Advisor-initiated outreach (EX3): the advisor opens a case for a student who
 * has not asked for help. The student is notified, and no data is visible until
 * the student consents.
 */
export function OutreachForm({ allowedTypes }: { allowedTypes: SupportType[] }) {
  const t = useTranslations("ui.outreach");
  const tw = useTranslations("workspace");
  const [categories, setCategories] = useState<Category[]>([]);
  const [subjects, setSubjects] = useState<Array<{ id: string; label: string }>>([]);
  const [query, setQuery] = useState("");
  const [students, setStudents] = useState<Student[]>([]);
  const [searched, setSearched] = useState(false);
  const [truncated, setTruncated] = useState(false);
  const [student, setStudent] = useState<Student | null>(null);
  const [categoryId, setCategoryId] = useState("");
  const [subjectId, setSubjectId] = useState("");
  const [priority, setPriority] = useState<Priority>("medium");
  const [reason, setReason] = useState("");
  const [loading, setLoading] = useState(true);
  const [searching, setSearching] = useState(false);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const [createdCaseId, setCreatedCaseId] = useState<string | null>(null);

  const typesKey = allowedTypes.join(",");

  const loadCategories = useCallback(async () => {
    const allowed = typesKey.split(",");
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
          .filter((category) => category.id && category.name && allowed.includes(category.supportType)),
      );
    } catch (loadError) {
      setError(messageFor(loadError, tw));
    } finally {
      setLoading(false);
    }
  }, [typesKey, tw]);

  useEffect(() => {
    const timer = window.setTimeout(() => void loadCategories(), 0);
    return () => window.clearTimeout(timer);
  }, [loadCategories]);

  async function searchStudents() {
    const term = query.trim();
    if (!term) return;
    setSearching(true);
    setError("");
    setStudent(null);
    try {
      // The API filters name and student code separately, so ask for both and merge.
      const [byName, byCode] = await Promise.all([
        clientApiGet<unknown>("StudentProfiles/search", { query: { fullName: term } }),
        clientApiGet<unknown>("StudentProfiles/search", { query: { studentCode: term } }),
      ]);
      const merged = new Map<string, Student>();
      for (const record of [...listOf(byName), ...listOf(byCode)]) {
        const id = str(record.studentId ?? record.id);
        if (!id || merged.has(id)) continue;
        merged.set(id, {
          id,
          name: str(record.fullName),
          code: str(record.studentCode),
          email: str(record.email),
          faculty: str(record.facultyName),
        });
      }
      const all = [...merged.values()];
      setStudents(all.slice(0, RESULT_LIMIT));
      setTruncated(all.length > RESULT_LIMIT);
      setSearched(true);
    } catch (searchError) {
      setError(messageFor(searchError, tw));
    } finally {
      setSearching(false);
    }
  }

  function onSearchKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key !== "Enter") return;
    event.preventDefault();
    void searchStudents();
  }

  async function chooseCategory(nextId: string) {
    setCategoryId(nextId);
    setSubjectId("");
    setError("");
    const next = categories.find((item) => item.id === nextId);
    if (next?.supportType !== "academic" || subjects.length > 0) return;
    try {
      const payload = await clientApiGet<unknown>("Subjects/search", {
        query: { status: "active", pageSize: 200 },
      });
      setSubjects(
        listOf(payload)
          .map((record) => ({
            id: str(record.subjectId ?? record.id),
            label: [str(record.subjectCode), str(record.subjectName ?? record.name)]
              .filter(Boolean)
              .join(" — "),
          }))
          .filter((subject) => subject.id && subject.label),
      );
    } catch (loadError) {
      setError(messageFor(loadError, tw));
    }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!student) return;
    setPending(true);
    setError("");
    try {
      const response = await clientApiPost<unknown>("Cases", {
        studentId: student.id,
        supportCategoryId: categoryId,
        ...(subjectId ? { subjectId } : {}),
        requestReason: reason.trim(),
        priority,
      });
      if (isRecord(response) && response.isSuccess === false) {
        throw new ApiError({
          message: typeof response.message === "string" ? response.message : tw("actionFailed"),
          status: 400,
          code: "case_creation_failed",
        });
      }
      setCreatedCaseId(str(unwrap(response)?.caseId) || "");
    } catch (submitError) {
      setError(messageFor(submitError, tw));
    } finally {
      setPending(false);
    }
  }

  const category = categories.find((item) => item.id === categoryId);
  const needsSubject = category?.supportType === "academic";
  const ready = Boolean(student) && Boolean(categoryId) && reason.trim().length > 0;

  if (createdCaseId !== null) {
    return (
      <div className="flex flex-col gap-space-lg">
        <section className="rounded-lg border border-rule bg-paper p-space-xl">
          <span className="flex size-11 items-center justify-center rounded-full bg-status-ontrack-surface text-status-ontrack-text">
            <Icon name="check" className="text-[24px]" />
          </span>
          <h1 className="mt-space-base text-2xl font-semibold text-text">{t("sentTitle")}</h1>
          <p className="mt-1 max-w-[60ch] text-[0.9375rem] leading-6 text-text-muted">
            {t("sentBody", { name: student?.name ?? "" })}
          </p>
          <div className="mt-space-xl flex flex-wrap gap-space-sm">
            {createdCaseId ? (
              <ButtonLink href={`/advisor/cases/${createdCaseId}`} iconAfter="arrow_forward">
                {t("openCase")}
              </ButtonLink>
            ) : null}
            <ButtonLink href="/advisor/caseload" variant="secondary">
              {t("backToCases")}
            </ButtonLink>
          </div>
        </section>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-space-lg">
      <PageHeader title={t("title")} description={t("description")} />

      <form onSubmit={submit} className="flex flex-col gap-space-lg">
        <Step number={1} title={t("studentTitle")} hint={t("studentHint")}>
          <div className="flex max-w-xl gap-space-sm">
            <input
              type="search"
              aria-label={t("studentTitle")}
              className={cn(controlClass, "h-11 min-w-0 flex-1")}
              placeholder={t("searchPlaceholder")}
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              onKeyDown={onSearchKeyDown}
            />
            <Button
              type="button"
              variant="secondary"
              size="lg"
              icon="search"
              loading={searching}
              disabled={!query.trim()}
              onClick={() => void searchStudents()}
            >
              {tw("search")}
            </Button>
          </div>

          {searched && students.length === 0 ? (
            <Notice className="mt-space-base" tone="sealed" title={t("noStudents")} />
          ) : null}

          {students.length > 0 ? (
            <>
              <div
                role="radiogroup"
                aria-label={t("studentTitle")}
                className="mt-space-base flex flex-col divide-y divide-rule rounded-lg border border-rule"
              >
                {students.map((option) => (
                  <label
                    key={option.id}
                    className={cn(
                      "flex cursor-pointer items-start gap-space-md px-space-base py-space-md transition-colors first:rounded-t-lg last:rounded-b-lg",
                      student?.id === option.id ? "bg-ink-wash/60" : "hover:bg-paper-tint",
                    )}
                  >
                    <input
                      type="radio"
                      name="student"
                      value={option.id}
                      checked={student?.id === option.id}
                      onChange={() => setStudent(option)}
                      className="mt-1 size-4 accent-[#2142b8]"
                    />
                    <span className="min-w-0">
                      <span className="block font-semibold text-text">
                        {option.name || option.code}
                        {option.code && option.name ? (
                          <span className="ml-2 font-mono text-[0.8125rem] font-normal text-text-muted">
                            {option.code}
                          </span>
                        ) : null}
                      </span>
                      <span className="mt-0.5 block text-sm text-text-muted">
                        {[option.faculty, option.email].filter(Boolean).join(" · ")}
                      </span>
                    </span>
                  </label>
                ))}
              </div>
              {truncated ? (
                <p className="mt-space-sm text-[0.8125rem] text-text-faint">{t("refineSearch")}</p>
              ) : null}
            </>
          ) : null}
        </Step>

        <Step number={2} title={t("categoryTitle")} hint={t("categoryHint")} disabled={!student} lockedHint={t("lockedStudent")}>
          {loading ? (
            <SkeletonRows rows={3} columns={2} label={tw("loading")} />
          ) : categories.length === 0 ? (
            <Notice tone="sealed" title={t("noCategories")} />
          ) : (
            <div
              role="radiogroup"
              aria-label={t("categoryTitle")}
              className="flex flex-col divide-y divide-rule rounded-lg border border-rule"
            >
              {categories.map((option) => (
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
              <label htmlFor="outreach-subject" className="mb-1.5 block text-sm font-semibold text-text">
                {t("subject")}
              </label>
              <select
                id="outreach-subject"
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
            </div>
          ) : null}
        </Step>

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
          <p className="mt-1 text-right font-mono text-[0.75rem] text-text-faint">
            {reason.length}/{REASON_MAX}
          </p>
          <div className="mt-space-base max-w-xs">
            <label htmlFor="outreach-priority" className="mb-1.5 block text-sm font-semibold text-text">
              {tw("priority")}
            </label>
            <select
              id="outreach-priority"
              className={cn(controlClass, "h-11 w-full")}
              value={priority}
              onChange={(event) => setPriority(event.target.value as Priority)}
            >
              {PRIORITIES.map((option) => (
                <option key={option} value={option}>
                  {tw(option)}
                </option>
              ))}
            </select>
          </div>
        </Step>

        <Notice tone="info" icon="lock" title={t("consentNoticeTitle")}>
          {t("consentNoticeBody")}
        </Notice>

        {error ? <Notice tone="urgent" icon="error" title={error} /> : null}

        <div className="flex flex-wrap items-center justify-between gap-space-md rounded-lg border border-rule bg-paper px-space-lg py-space-base">
          <p className="min-w-0 flex-1 text-sm text-text-muted">
            {student ? t("summary", { name: student.name || student.code }) : t("summaryNone")}
          </p>
          <div className="flex flex-wrap gap-space-sm">
            <Link
              href="/advisor/caseload"
              className="inline-flex h-11 items-center rounded-md px-5 text-base font-semibold text-text-muted hover:bg-ink-wash hover:text-text"
            >
              {t("cancel")}
            </Link>
            <Button type="submit" size="lg" disabled={!ready} loading={pending} iconAfter="send">
              {t("send")}
            </Button>
          </div>
        </div>
      </form>
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
      aria-describedby={disabled && lockedHint ? `outreach-step-${number}-locked` : undefined}
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
            <span id={`outreach-step-${number}-locked`} className="block text-sm leading-6 text-text-faint">
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

function messageFor(error: unknown, t: (key: "apiErrorNetwork" | "actionFailed") => string) {
  if (error instanceof ApiError && error.status < 500 && error.message) return error.message;
  return error instanceof ApiError && error.status < 500 ? t("actionFailed") : t("apiErrorNetwork");
}
