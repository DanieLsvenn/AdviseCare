"use client";

import { useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { ApiError, type ApiQuery } from "@/lib/api-contract";
import { clientApiRequest } from "@/lib/client-api";
import { Button } from "@/components/ui/button";
import { Panel } from "@/components/ui/panel";
import {
  WorkspaceInput,
  type WorkspaceField,
} from "@/components/workspace/api-workspace";

export function ApiDownloadForm({
  title,
  path,
  fileName,
  fields = [],
}: {
  title: string;
  path: string;
  fileName: string | ((query: ApiQuery) => string);
  fields?: WorkspaceField[];
}) {
  const t = useTranslations("workspace");
  const [values, setValues] = useState<Record<string, string | boolean>>({});
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState("");
  const [hasError, setHasError] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setMessage("");

    try {
      const query: ApiQuery = {};
      for (const [key, value] of Object.entries(values)) {
        if (value !== "") query[key] = value;
      }

      const result = await clientApiRequest<unknown>(path, {
        method: "GET",
        query,
        redirectOnUnauthorized: true,
      });
      const blob =
        result instanceof Blob
          ? result
          : result === undefined
            ? new Blob([])
            : new Blob(
                [typeof result === "string" ? result : JSON.stringify(result, null, 2)],
                { type: "application/json;charset=utf-8" },
              );
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download =
        typeof fileName === "function" ? fileName(query) : fileName;
      anchor.click();
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
      setMessage(t("downloaded"));
      setHasError(false);
    } catch (error) {
      setMessage(downloadError(error, t));
      setHasError(true);
    } finally {
      setPending(false);
    }
  }

  return (
    <Panel className="p-space-lg">
      <h2 className="font-headline-md text-headline-md text-on-surface">
        {title}
      </h2>
      <form className="mt-space-md grid gap-space-md sm:grid-cols-2" onSubmit={submit}>
        {fields.map((field) => (
          <WorkspaceInput
            field={field}
            key={field.key}
            onChange={(value) =>
              setValues((current) => ({
                ...current,
                [field.key]: value as string | boolean,
              }))
            }
            value={values[field.key] ?? ""}
          />
        ))}
        <div className="flex flex-col items-start gap-space-sm sm:col-span-2">
          <Button disabled={pending} type="submit">
            {pending ? t("submitting") : t("download")}
          </Button>
          {message ? (
            <p
              aria-live="polite"
              className={
                hasError
                  ? "font-body-sm text-body-sm text-status-urgent-text"
                  : "font-body-sm text-body-sm text-status-ontrack-text"
              }
              role={hasError ? "alert" : "status"}
            >
              {message}
            </p>
          ) : null}
        </div>
      </form>
    </Panel>
  );
}

function downloadError(
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
