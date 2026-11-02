"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { ApiError, isRecord } from "@/lib/api-contract";
import { clientApiGet, clientApiPost } from "@/lib/client-api";
import { Button } from "@/components/ui/button";
import { Panel } from "@/components/ui/panel";

interface SupportCategory {
  id: string;
  name: string;
  supportType: string;
}

interface ShareableAttribute {
  id: string;
  name: string;
  description: string;
  alreadyShared: boolean;
}

export function SupportRequestForm() {
  const t = useTranslations("workspace");
  const [categories, setCategories] = useState<SupportCategory[]>([]);
  const [subjects, setSubjects] = useState<Array<{ id: string; name: string }>>(
    [],
  );
  const [attributes, setAttributes] = useState<ShareableAttribute[]>([]);
  const [categoryId, setCategoryId] = useState("");
  const [subjectId, setSubjectId] = useState("");
  const [reason, setReason] = useState("");
  const [sharedIds, setSharedIds] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const [result, setResult] = useState("");

  const loadCategories = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const payload = await clientApiGet<unknown>("SupportCategories/search", {
        query: { status: "active" },
      });
      setCategories(
        recordsFrom(payload)
          .map((record) => ({
            id: stringValue(record.supportCategoryId ?? record.id),
            name: stringValue(
              record.categoryName ?? record.name ?? record.supportType,
            ),
            supportType: stringValue(record.supportType).toLowerCase(),
          }))
          .filter((category) => category.id && category.name),
      );
    } catch (loadError) {
      setError(messageForError(loadError, t));
    } finally {
      setLoading(false);
    }
  }, [t]);

  useEffect(() => {
    let active = true;
    const timer = window.setTimeout(() => {
      if (active) void loadCategories();
    }, 0);
    return () => {
      active = false;
      window.clearTimeout(timer);
    };
  }, [loadCategories]);

  const selectedCategory = categories.find(
    (category) => category.id === categoryId,
  );

  async function selectCategory(nextId: string) {
    setCategoryId(nextId);
    setSubjectId("");
    setSharedIds([]);
    setAttributes([]);
    setError("");
    const category = categories.find((item) => item.id === nextId);
    if (!category) return;

    try {
      const [shareablePayload, subjectPayload] = await Promise.all([
        clientApiGet<unknown>(
          `SupportCategories/${encodeURIComponent(nextId)}/shareable-attributes`,
        ),
        category.supportType === "academic"
          ? clientApiGet<unknown>("Subjects/search", {
              query: { status: "active" },
            })
          : Promise.resolve([]),
      ]);
      setAttributes(
        recordsFrom(shareablePayload)
          .map((record) => ({
            id: stringValue(record.dataAttributeId ?? record.id),
            name: stringValue(record.attributeName ?? record.name),
            description: stringValue(record.description),
            alreadyShared: record.alreadyShared === true,
          }))
          .filter((attribute) => attribute.id && attribute.name),
      );
      setSubjects(
        recordsFrom(subjectPayload)
          .map((record) => ({
            id: stringValue(record.subjectId ?? record.id),
            name: stringValue(
              record.subjectName ?? record.subjectCode ?? record.name,
            ),
          }))
          .filter((subject) => subject.id && subject.name),
      );
    } catch (loadError) {
      setError(messageForError(loadError, t));
    }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setError("");
    setResult("");
    try {
      const response = await clientApiPost<unknown>("Cases", {
        supportCategoryId: categoryId,
        ...(subjectId ? { subjectId } : {}),
        requestReason: reason,
        sharedAttributeIds: sharedIds,
      });
      if (isRecord(response) && response.isSuccess === false) {
        throw new ApiError({
          message:
            typeof response.message === "string"
              ? response.message
              : t("actionFailed"),
          status: 400,
          code: "case_creation_failed",
        });
      }
      setResult(JSON.stringify(response, null, 2));
      setReason("");
      setSharedIds([]);
    } catch (submitError) {
      setError(messageForError(submitError, t));
    } finally {
      setPending(false);
    }
  }

  return (
    <Panel className="p-space-lg">
      <h2 className="font-headline-md text-headline-md text-on-surface">
        {t("supportRequest")}
      </h2>
      {error ? (
        <p
          aria-live="polite"
          className="mt-space-md rounded border border-status-urgent-border bg-status-urgent-surface p-space-md font-body-sm text-body-sm text-status-urgent-text"
          role="alert"
        >
          {error}
        </p>
      ) : null}
      {loading ? (
        <p className="mt-space-md text-on-surface-variant" role="status">
          {t("loading")}
        </p>
      ) : (
        <form className="mt-space-md grid gap-space-md" onSubmit={submit}>
          <label className="block">
            <span className="mb-space-xs block font-label-sm text-label-sm font-medium text-on-surface">
              {t("supportCategoryId")}
            </span>
            <select
              className={inputClass}
              onChange={(event) => void selectCategory(event.target.value)}
              required
              value={categoryId}
            >
              <option value="">—</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name} ({category.supportType})
                </option>
              ))}
            </select>
          </label>

          {selectedCategory?.supportType === "academic" ? (
            <label className="block">
              <span className="mb-space-xs block font-label-sm text-label-sm font-medium text-on-surface">
                {t("subjectId")}
              </span>
              <select
                className={inputClass}
                onChange={(event) => setSubjectId(event.target.value)}
                required
                value={subjectId}
              >
                <option value="">—</option>
                {subjects.map((subject) => (
                  <option key={subject.id} value={subject.id}>
                    {subject.name}
                  </option>
                ))}
              </select>
            </label>
          ) : null}

          <label className="block">
            <span className="mb-space-xs block font-label-sm text-label-sm font-medium text-on-surface">
              {t("requestReason")}
            </span>
            <textarea
              className={`${inputClass} min-h-32`}
              maxLength={4000}
              onChange={(event) => setReason(event.target.value)}
              required
              value={reason}
            />
          </label>

          {attributes.length ? (
            <fieldset className="grid gap-space-sm">
              <legend className="font-label-sm text-label-sm font-medium text-on-surface">
                {t("sharedAttributeIds")}
              </legend>
              {attributes.map((attribute) => (
                <label
                  className="flex items-start gap-space-sm rounded border border-outline-variant/40 p-space-sm font-body-sm text-body-sm text-on-surface"
                  key={attribute.id}
                >
                  <input
                    checked={sharedIds.includes(attribute.id)}
                    disabled={attribute.alreadyShared}
                    onChange={(event) =>
                      setSharedIds((current) =>
                        event.target.checked
                          ? [...current, attribute.id]
                          : current.filter((id) => id !== attribute.id),
                      )
                    }
                    type="checkbox"
                  />
                  <span>
                    <span className="block font-medium">{attribute.name}</span>
                    {attribute.description ? (
                      <span className="text-on-surface-variant">
                        {attribute.description}
                      </span>
                    ) : null}
                    {attribute.alreadyShared ? (
                      <span className="ml-space-sm text-on-surface-variant">
                        {t("alreadyShared")}
                      </span>
                    ) : null}
                  </span>
                </label>
              ))}
            </fieldset>
          ) : null}

          <Button disabled={pending || !categoryId} type="submit">
            {pending ? t("submitting") : t("submit")}
          </Button>
        </form>
      )}
      {result ? (
        <div className="mt-space-lg rounded border border-status-ontrack-border bg-status-ontrack-surface p-space-md">
          <p className="font-label-sm text-label-sm font-semibold text-status-ontrack-text">
            {t("actionSucceeded")}
          </p>
          <pre className="mt-space-sm max-h-80 overflow-auto whitespace-pre-wrap break-words font-caption text-caption text-on-surface">
            {result}
          </pre>
        </div>
      ) : null}
    </Panel>
  );
}

const inputClass =
  "w-full rounded border border-outline-variant bg-surface-container-lowest px-space-md py-2 font-body-sm text-body-sm text-on-surface focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary";

function recordsFrom(value: unknown): Array<Record<string, unknown>> {
  if (Array.isArray(value)) return value.filter(isRecord);
  if (!isRecord(value)) return [];
  for (const key of ["items", "data", "results", "records", "value"]) {
    if (Array.isArray(value[key])) return value[key].filter(isRecord);
  }
  for (const nested of Object.values(value)) {
    if (Array.isArray(nested)) return nested.filter(isRecord);
  }
  return [];
}

function stringValue(value: unknown): string {
  return typeof value === "string" || typeof value === "number"
    ? String(value)
    : "";
}

function messageForError(
  error: unknown,
  translate: (
    key:
      | "apiError400"
      | "apiError401"
      | "apiError403"
      | "apiError404"
      | "apiError409"
      | "apiError5xx"
      | "apiErrorNetwork",
  ) => string,
): string {
  if (!(error instanceof ApiError)) return translate("apiErrorNetwork");
  if (error.status === 400) return translate("apiError400");
  if (error.status === 401) return translate("apiError401");
  if (error.status === 403) return translate("apiError403");
  if (error.status === 404) return translate("apiError404");
  if (error.status === 409) return translate("apiError409");
  if (error.status >= 500) return translate("apiError5xx");
  return error.message;
}
