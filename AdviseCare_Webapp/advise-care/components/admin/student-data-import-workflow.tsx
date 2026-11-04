"use client";

import { useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import { ApiError, isRecord } from "@/lib/api-contract";
import { clientApiRequest } from "@/lib/client-api";
import { Button } from "@/components/ui/button";
import { Panel } from "@/components/ui/panel";

const mappingKeys = [
  "studentIdentifier",
  "dataAttribute",
  "value",
  "term",
  "validFrom",
] as const;

type MappingKey = (typeof mappingKeys)[number];
type ColumnMapping = Record<MappingKey, string>;

const emptyMapping: ColumnMapping = {
  studentIdentifier: "",
  dataAttribute: "",
  value: "",
  term: "",
  validFrom: "",
};

export function StudentDataImportWorkflow() {
  const t = useTranslations("workspace");
  const [file, setFile] = useState<File | null>(null);
  const [headers, setHeaders] = useState<string[]>([]);
  const [sampleRows, setSampleRows] = useState<unknown[]>([]);
  const [mapping, setMapping] = useState<ColumnMapping>(emptyMapping);
  const [validation, setValidation] = useState<unknown>(null);
  const [importResult, setImportResult] = useState<unknown>(null);
  const [pendingStep, setPendingStep] = useState<"preview" | "validate" | "import" | null>(
    null,
  );
  const [error, setError] = useState("");

  const mappingComplete = useMemo(
    () => mappingKeys.every((key) => mapping[key] !== ""),
    [mapping],
  );
  const validationIsSuccessful =
    isRecord(validation) &&
    validation.isValid === true &&
    typeof validation.validRowCount === "number" &&
    validation.validRowCount > 0;

  function resetForFile(nextFile: File | null) {
    setFile(nextFile);
    setHeaders([]);
    setSampleRows([]);
    setMapping(emptyMapping);
    setValidation(null);
    setImportResult(null);
    setError("");
  }

  async function preview() {
    if (!file) return;
    setPendingStep("preview");
    setError("");
    try {
      const payload = await sendFile("StudentData/import/preview", file);
      if (!isRecord(payload)) {
        throw new ApiError({
          message: t("unexpectedResponse"),
          status: 502,
          code: "unexpected_preview_response",
        });
      }
      const nextHeaders = stringArray(payload.headers ?? payload.columns);
      const nextSamples = Array.isArray(payload.sampleRows)
        ? payload.sampleRows
        : [];
      if (nextHeaders.length === 0) {
        throw new ApiError({
          message: t("noPreviewHeaders"),
          status: 502,
          code: "missing_preview_headers",
        });
      }
      setHeaders(nextHeaders);
      setSampleRows(nextSamples.slice(0, 5));
      setMapping(
        Object.fromEntries(
          mappingKeys.map((key) => [
            key,
            nextHeaders.find(
              (header) => header.toLowerCase() === key.toLowerCase(),
            ) ?? "",
          ]),
        ) as ColumnMapping,
      );
      setValidation(null);
      setImportResult(null);
    } catch (previewError) {
      setError(messageForError(previewError, t));
    } finally {
      setPendingStep(null);
    }
  }

  async function validate() {
    if (!file || !mappingComplete) return;
    setPendingStep("validate");
    setError("");
    setValidation(null);
    setImportResult(null);
    try {
      setValidation(
        await sendFile("StudentData/import/validate", file, mapping),
      );
    } catch (validationError) {
      setError(messageForError(validationError, t));
    } finally {
      setPendingStep(null);
    }
  }

  async function importData() {
    if (!file || !mappingComplete || !validationIsSuccessful) return;
    setPendingStep("import");
    setError("");
    setImportResult(null);
    try {
      const result = await sendFile("StudentData/import", file, mapping);
      setImportResult(result);
      if (isRecord(result) && result.isSuccess === false) {
        setError(
          typeof result.message === "string"
            ? result.message
            : t("actionFailed"),
        );
      }
    } catch (importError) {
      setError(messageForError(importError, t));
    } finally {
      setPendingStep(null);
    }
  }

  return (
    <Panel className="p-space-lg">
      <h2 className="font-headline-md text-headline-md text-on-surface">
        {t("studentDataImport")}
      </h2>
      <p className="mt-space-xs font-body-sm text-body-sm text-on-surface-variant">
        {t("importWorkflowDescription")}
      </p>

      <div className="mt-space-md grid gap-space-md">
        <label className="block">
          <span className="mb-space-xs block font-label-sm text-label-sm font-medium text-on-surface">
            {t("file")}
          </span>
          <input
            accept=".xlsx,.csv"
            className={inputClass}
            onChange={(event) =>
              resetForFile(event.target.files?.[0] ?? null)
            }
            type="file"
          />
        </label>

        <Button
          disabled={!file || pendingStep !== null}
          onClick={() => void preview()}
          type="button"
          variant="secondary"
        >
          {pendingStep === "preview" ? t("previewing") : t("preview")}
        </Button>

        {headers.length ? (
          <>
            <section aria-labelledby="student-import-mapping">
              <h3
                className="font-label-md text-label-md font-semibold text-on-surface"
                id="student-import-mapping"
              >
                {t("columnMapping")}
              </h3>
              <p className="mb-space-md mt-space-xs font-body-sm text-body-sm text-on-surface-variant">
                {t("mappingDescription")}
              </p>
              <div className="grid gap-space-md sm:grid-cols-2">
                {mappingKeys.map((key) => (
                  <label className="block" key={key}>
                    <span className="mb-space-xs block font-label-sm text-label-sm font-medium text-on-surface">
                      {t(key)}
                    </span>
                    <select
                      className={inputClass}
                      onChange={(event) => {
                        setMapping((current) => ({
                          ...current,
                          [key]: event.target.value,
                        }));
                        setValidation(null);
                        setImportResult(null);
                      }}
                      required
                      value={mapping[key]}
                    >
                      <option value="">—</option>
                      {headers.map((header) => (
                        <option key={header} value={header}>
                          {header}
                        </option>
                      ))}
                    </select>
                  </label>
                ))}
              </div>
            </section>

            {sampleRows.length ? (
              <section aria-label={t("sampleRows")}>
                <h3 className="font-label-md text-label-md font-semibold text-on-surface">
                  {t("sampleRows")}
                </h3>
                <pre className="mt-space-sm max-h-56 overflow-auto whitespace-pre-wrap break-words rounded bg-surface-container-low p-space-md font-caption text-caption text-on-surface-variant">
                  {JSON.stringify(sampleRows, null, 2)}
                </pre>
              </section>
            ) : null}

            <div className="flex flex-wrap gap-space-sm">
              <Button
                disabled={!file || !mappingComplete || pendingStep !== null}
                onClick={() => void validate()}
                type="button"
                variant="secondary"
              >
                {pendingStep === "validate" ? t("validating") : t("validate")}
              </Button>
              <Button
                disabled={
                  !validationIsSuccessful ||
                  pendingStep !== null ||
                  importResult !== null
                }
                onClick={() => void importData()}
                type="button"
              >
                {pendingStep === "import" ? t("importing") : t("import")}
              </Button>
            </div>
          </>
        ) : null}

        {error ? (
          <p
            aria-live="polite"
            className="rounded border border-status-urgent-border bg-status-urgent-surface p-space-md font-body-sm text-body-sm text-status-urgent-text"
            role="alert"
          >
            {error}
          </p>
        ) : null}
        {validation !== null ? (
          <section aria-label={t("validationResult")}>
            <h3 className="font-label-md text-label-md font-semibold text-on-surface">
              {t("validationResult")}
            </h3>
            <pre className="mt-space-sm max-h-80 overflow-auto whitespace-pre-wrap break-words rounded bg-surface-container-low p-space-md font-caption text-caption text-on-surface-variant">
              {JSON.stringify(validation, null, 2)}
            </pre>
          </section>
        ) : null}
        {importResult !== null ? (
          <section aria-label={t("importResult")}>
            <h3 className="font-label-md text-label-md font-semibold text-on-surface">
              {isRecord(importResult) && importResult.isSuccess === false
                ? t("actionFailed")
                : t("importResult")}
            </h3>
            <pre className="mt-space-sm max-h-80 overflow-auto whitespace-pre-wrap break-words rounded bg-surface-container-low p-space-md font-caption text-caption text-on-surface-variant">
              {JSON.stringify(importResult, null, 2)}
            </pre>
          </section>
        ) : null}
      </div>
    </Panel>
  );
}

async function sendFile(
  path: string,
  file: File,
  mapping?: ColumnMapping,
): Promise<unknown> {
  const body = new FormData();
  body.append("file", file);
  if (mapping) body.append("columnMapping", JSON.stringify(mapping));
  return clientApiRequest<unknown>(path, { method: "POST", body });
}

function stringArray(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value.filter((item): item is string => typeof item === "string");
}

const inputClass =
  "w-full rounded border border-outline-variant bg-surface-container-lowest px-space-md py-2 font-body-sm text-body-sm text-on-surface focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary";

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
