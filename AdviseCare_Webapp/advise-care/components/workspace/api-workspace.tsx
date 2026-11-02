"use client";

import {
  useCallback,
  useEffect,
  useId,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import { useTranslations } from "next-intl";
import { ApiError, isRecord, type ApiMethod, type ApiQuery } from "@/lib/api-contract";
import { clientApiGet, clientApiRequest } from "@/lib/client-api";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/ui/page-header";
import { Panel } from "@/components/ui/panel";

export interface WorkspaceField {
  key: string;
  label: string;
  type?:
    | "text"
    | "email"
    | "number"
    | "date"
    | "datetime-local"
    | "textarea"
    | "select"
    | "checkbox"
    | "file"
    | "list";
  required?: boolean;
  options?: Array<{ label: string; value: string }>;
  accept?: string;
  placeholder?: string;
  pathOnly?: boolean;
  optionsFrom?: WorkspaceOptionSource;
}

export interface WorkspaceOptionSource {
  path: string;
  collectionKey?: string;
  valueKey: string;
  labelKeys: string[];
  match?: Record<string, string | number | boolean>;
  requiredValueKeys?: string[];
}

export interface WorkspaceColumn {
  key: string;
  label: string;
}

export interface WorkspaceOperation {
  label: string;
  method: ApiMethod;
  path: string;
  fields?: WorkspaceField[];
  confirm?: boolean;
  bodyFromRecord?: Record<string, { key: string; asArray?: boolean }>;
}

export interface ApiWorkspaceDefinition {
  title: string;
  description?: string;
  listPath: string;
  collectionKey?: string;
  columns: WorkspaceColumn[];
  queryFields?: WorkspaceField[];
  create?: WorkspaceOperation;
  globalOperations?: WorkspaceOperation[];
  rowOperations?: WorkspaceOperation[];
  pageSize?: number;
  pathValues?: Record<string, unknown>;
  recordFilter?: { key: string; value: string };
}

export function ApiWorkspace({
  definition,
}: {
  definition: ApiWorkspaceDefinition;
}) {
  const t = useTranslations("workspace");
  const listPath = resolvePath(
    definition.listPath,
    definition.pathValues ?? {},
  );
  const scopedQuery = useMemo(
    () => getScopedQuery(definition.queryFields, definition.pathValues),
    [definition.pathValues, definition.queryFields],
  );
  const recordFilter = definition.recordFilter;
  const queryFields = useMemo(
    () =>
      (definition.queryFields ?? []).filter(
        (field) =>
          !Object.prototype.hasOwnProperty.call(
            definition.pathValues ?? {},
            field.key,
          ),
      ),
    [definition.pathValues, definition.queryFields],
  );
  const requiredQueryFields = useMemo(
    () => queryFields.filter((field) => field.required),
    [queryFields],
  );
  const [records, setRecords] = useState<Record<string, unknown>[]>([]);
  const [selectedRecord, setSelectedRecord] =
    useState<Record<string, unknown> | null>(null);
  const [query, setQuery] = useState<ApiQuery>(() => ({
      ...scopedQuery,
      ...(definition.pageSize
        ? { page: 1, pageSize: definition.pageSize }
        : {}),
    }));
  const requiredQueryValuesPresent = hasRequiredQueryValues(
    query,
    requiredQueryFields,
    scopedQuery,
  );
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");

  const loadRecords = useCallback(
    async (nextQuery: ApiQuery = query) => {
      if (
        !hasRequiredQueryValues(
          nextQuery,
          requiredQueryFields,
          scopedQuery,
        )
      ) {
        return;
      }

      setLoading(true);
      setLoadError("");
      try {
        const payload = await clientApiGet<unknown>(listPath, {
          query: { ...nextQuery, ...scopedQuery },
        });
        const result = extractRecords(payload, definition.collectionKey);
        if (!result) {
          throw new ApiError({
            message: t("unexpectedResponse"),
            status: 502,
            code: "unexpected_collection",
          });
        }
        const visibleRecords = recordFilter
          ? result.filter(
              (record) =>
                String(readRecordValue(record, recordFilter.key) ?? "") ===
                recordFilter.value,
            )
          : result;
        setRecords(visibleRecords);
        setSelectedRecord(null);
      } catch (error) {
        setLoadError(errorMessage(error, t));
        setRecords([]);
      } finally {
        setLoading(false);
      }
    },
    [
      definition.collectionKey,
      listPath,
      query,
      requiredQueryFields,
      recordFilter,
      scopedQuery,
      t,
    ],
  );

  useEffect(() => {
    if (!requiredQueryValuesPresent) return;

    let active = true;
    const timer = window.setTimeout(() => {
      if (active) void loadRecords();
    }, 0);
    return () => {
      active = false;
      window.clearTimeout(timer);
    };
  }, [loadRecords, requiredQueryValuesPresent]);

  const createOperation = definition.create
    ? {
        ...definition.create,
        fields: definition.create.fields?.filter(
          (field) =>
            !Object.prototype.hasOwnProperty.call(
              definition.pathValues ?? {},
              field.key,
            ),
        ),
      }
    : undefined;
  const columns = definition.columns;
  const rowKeyOccurrences = new Map<string, number>();
  const [filterValues, setFilterValues] = useState<Record<string, string | boolean>>({});

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextQuery: ApiQuery = { ...filterValues };
    if (definition.pageSize) {
      nextQuery.page = 1;
      nextQuery.pageSize = definition.pageSize;
    }
    setQuery(nextQuery);
  }

  function changePage(page: number) {
    const nextQuery = {
      ...query,
      page,
      pageSize: definition.pageSize ?? query.pageSize,
    };
    setQuery(nextQuery);
  }

  return (
    <div className="flex flex-col gap-space-lg">
      <PageHeader title={definition.title} description={definition.description} />

      {queryFields.length ? (
        <Panel className="p-space-lg">
          <form
            className="grid gap-space-md sm:grid-cols-2 xl:grid-cols-4"
            onSubmit={applyFilters}
          >
            {queryFields.map((field) => (
              <WorkspaceInput
                field={field}
                key={field.key}
                onChange={(value) =>
                  setFilterValues((current) => ({
                    ...current,
                    [field.key]: value as string | boolean,
                  }))
                }
                value={filterValues[field.key] ?? ""}
              />
            ))}
            <div className="flex items-end gap-space-sm">
              <Button disabled={loading} type="submit">
                {t("applyFilters")}
              </Button>
              <Button
                onClick={() => {
                  setFilterValues({});
                  const emptyQuery = definition.pageSize
                    ? { page: 1, pageSize: definition.pageSize }
                    : {};
                  setQuery(emptyQuery);
                }}
                type="button"
                variant="secondary"
              >
                {t("clearFilters")}
              </Button>
            </div>
          </form>
        </Panel>
      ) : null}

      {createOperation ? (
        <Panel className="p-space-lg">
          <h2 className="font-headline-md text-headline-md text-on-surface">
            {createOperation.label}
          </h2>
          <ApiOperationForm
            definition={createOperation}
            bodyDefaults={definition.pathValues}
            pathValues={definition.pathValues}
            onComplete={() => loadRecords()}
          />
        </Panel>
      ) : null}

      {definition.globalOperations?.length ? (
        <Panel className="p-space-lg">
          <h2 className="font-headline-md text-headline-md text-on-surface">
            {t("actions")}
          </h2>
          <div className="mt-space-md grid gap-space-lg lg:grid-cols-2">
            {definition.globalOperations.map((operation) => (
              <section
                className="rounded border border-outline-variant/40 p-space-md"
                key={`${operation.method}:${operation.path}`}
              >
                <ApiOperationForm
                  definition={operation}
                  pathValues={definition.pathValues}
                  onComplete={() => loadRecords()}
                />
              </section>
            ))}
          </div>
        </Panel>
      ) : null}

      <Panel className="overflow-hidden">
        <div className="flex flex-wrap items-center justify-between gap-space-md border-b border-outline-variant/40 px-space-lg py-space-md">
          <h2 className="font-headline-md text-headline-md text-on-surface">
            {definition.title}
          </h2>
          <Button
            disabled={loading || !requiredQueryValuesPresent}
            icon="refresh"
            onClick={() => void loadRecords()}
            size="sm"
            variant="secondary"
          >
            {t("refresh")}
          </Button>
        </div>

        {!requiredQueryValuesPresent ? (
          <p
            aria-live="polite"
            className="px-space-lg py-space-xl text-on-surface-variant"
            role="status"
          >
            {t("completeRequiredFilters")}
          </p>
        ) : loadError ? (
          <div
            aria-live="polite"
            className="m-space-lg rounded border border-status-urgent-border bg-status-urgent-surface p-space-md font-body-sm text-body-sm text-status-urgent-text"
            role="alert"
          >
            <p>{loadError}</p>
            <Button
              className="mt-space-sm"
              onClick={() => void loadRecords()}
              size="sm"
              variant="secondary"
            >
              {t("retry")}
            </Button>
          </div>
        ) : loading ? (
          <p aria-live="polite" className="px-space-lg py-space-xl text-on-surface-variant" role="status">
            {t("loading")}
          </p>
        ) : records.length === 0 ? (
          <p className="px-space-lg py-space-xl text-on-surface-variant">
            {t("noRecords")}
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-left">
              <thead className="bg-surface-container-low">
                <tr>
                  {columns.map((column) => (
                    <th
                      className="whitespace-nowrap border-b border-outline-variant/40 px-space-md py-space-sm font-label-sm text-label-sm font-semibold text-on-surface-variant"
                      key={column.key}
                      scope="col"
                    >
                      {column.label}
                    </th>
                  ))}
                  {definition.rowOperations?.length ? (
                    <th
                      className="border-b border-outline-variant/40 px-space-md py-space-sm font-label-sm text-label-sm font-semibold text-on-surface-variant"
                      scope="col"
                    >
                      {t("actions")}
                    </th>
                  ) : null}
                </tr>
              </thead>
              <tbody>
                {records.map((record, index) => {
                  const key = createRowKey(record, index, rowKeyOccurrences);
                  const isSelected = selectedRecord === record;
                  return (
                    <tr
                      className={
                        isSelected
                          ? "bg-surface-container-low"
                          : "bg-surface-container-lowest"
                      }
                      key={key}
                    >
                      {columns.map((column) => (
                        <td
                          className="max-w-96 border-b border-outline-variant/30 px-space-md py-space-sm align-top font-body-sm text-body-sm text-on-surface"
                          key={column.key}
                        >
                          {displayValue(getPathValue(record, column.key))}
                        </td>
                      ))}
                      {definition.rowOperations?.length ? (
                        <td className="border-b border-outline-variant/30 px-space-md py-space-sm align-top">
                          <Button
                            aria-pressed={isSelected}
                            onClick={() =>
                              setSelectedRecord(isSelected ? null : record)
                            }
                            size="sm"
                            variant={isSelected ? "primary" : "secondary"}
                          >
                            {t("selectRecord")}
                          </Button>
                        </td>
                      ) : null}
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {definition.pageSize ? (
          <div className="flex items-center justify-end gap-space-sm border-t border-outline-variant/40 px-space-lg py-space-md">
            <Button
              disabled={loading || Number(query.page ?? 1) <= 1}
              onClick={() => changePage(Math.max(1, Number(query.page ?? 1) - 1))}
              size="sm"
              variant="secondary"
            >
              {t("previous")}
            </Button>
            <span className="font-caption text-caption text-on-surface-variant">
              {t("page")} {String(query.page ?? 1)}
            </span>
            <Button
              disabled={loading || records.length < definition.pageSize}
              onClick={() => changePage(Number(query.page ?? 1) + 1)}
              size="sm"
              variant="secondary"
            >
              {t("next")}
            </Button>
          </div>
        ) : null}
      </Panel>

      {requiredQueryValuesPresent &&
      selectedRecord &&
      definition.rowOperations?.length ? (
        <Panel className="p-space-lg">
          <h2 className="font-headline-md text-headline-md text-on-surface">
            {t("actions")}
          </h2>
          <div className="mt-space-md grid gap-space-lg lg:grid-cols-2">
            {definition.rowOperations.map((operation) => (
              <section
                className="rounded border border-outline-variant/40 p-space-md"
                key={`${operation.method}:${operation.path}`}
              >
                <h3 className="font-label-md text-label-md font-semibold text-on-surface">
                  {operation.label}
                </h3>
                <ApiOperationForm
                  definition={operation}
                  bodyDefaults={bodyFromRecord(operation, selectedRecord)}
                  pathValues={{
                    ...definition.pathValues,
                    ...selectedRecord,
                  }}
                  onComplete={() => loadRecords()}
                />
              </section>
            ))}
          </div>
        </Panel>
      ) : null}
    </div>
  );
}

export function ApiOperationForm({
  definition,
  bodyDefaults = {},
  pathValues = {},
  onComplete,
}: {
  definition: WorkspaceOperation;
  bodyDefaults?: Record<string, unknown>;
  pathValues?: Record<string, unknown>;
  onComplete?: () => Promise<void>;
}) {
  const t = useTranslations("workspace");
  const [values, setValues] = useState<Record<string, unknown>>({});
  const [pending, setPending] = useState(false);
  const [feedback, setFeedback] = useState("");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [feedbackIsError, setFeedbackIsError] = useState(false);
  const [resultDetails, setResultDetails] = useState("");

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFeedback("");
    setResultDetails("");
    setFieldErrors({});

    if (definition.confirm && !window.confirm(t("confirmAction"))) return;

    setPending(true);
    try {
      const body = makeBody(definition.fields ?? [], values, bodyDefaults);
      const result = await clientApiRequest<unknown>(
        resolvePath(definition.path, { ...pathValues, ...values }),
        {
        method: definition.method,
        body,
        redirectOnUnauthorized: true,
        },
      );
      if (isRecord(result) && result.isSuccess === false) {
        setFeedback(
        typeof result.message === "string"
          ? result.message
          : t("actionFailed"),
        );
        setFeedbackIsError(true);
        setResultDetails(JSON.stringify(result, null, 2));
        return;
      }
      setFeedback(t("actionSucceeded"));
      setFeedbackIsError(false);
      setResultDetails(
        result === undefined ? "" : JSON.stringify(result, null, 2),
      );
      setValues({});
      await onComplete?.();
    } catch (error) {
      setFeedback(errorMessage(error, t));
      setFeedbackIsError(true);
      if (error instanceof ApiError) setFieldErrors(error.fieldErrors);
    } finally {
      setPending(false);
    }
  }

  return (
    <form className="mt-space-md grid gap-space-md sm:grid-cols-2" onSubmit={submit}>
      {(definition.fields ?? []).map((field) => (
        <WorkspaceInput
          field={field}
          key={field.key}
          onChange={(value) =>
            setValues((current) => ({ ...current, [field.key]: value }))
          }
          value={
            values[field.key] ??
            bodyDefaults[field.key] ??
            (field.type === "checkbox" ? false : "")
          }
          error={
            fieldErrors[field.key] ??
            fieldErrors[pascalCase(field.key)] ??
            fieldErrors[camelCase(field.key)]
          }
        />
      ))}
      <div className="flex flex-col items-start gap-space-sm sm:col-span-2">
        <Button disabled={pending} type="submit">
          {pending ? t("submitting") : definition.label}
        </Button>
        {feedback ? (
          <p
            aria-live="polite"
            className={
              feedbackIsError
                ? "font-body-sm text-body-sm text-status-urgent-text"
                : "font-body-sm text-body-sm text-status-ontrack-text"
            }
            role={feedbackIsError ? "alert" : "status"}
          >
            {feedback}
          </p>
        ) : null}
        {resultDetails ? (
          <pre className="max-h-80 max-w-full overflow-auto whitespace-pre-wrap break-words rounded bg-surface-container-low p-space-sm font-caption text-caption text-on-surface-variant">
            {resultDetails}
          </pre>
        ) : null}
      </div>
    </form>
  );
}

export function WorkspaceInput({
  field,
  value,
  onChange,
  error,
}: {
  field: WorkspaceField;
  value: unknown;
  onChange: (value: unknown) => void;
  error?: string;
}) {
  const t = useTranslations("workspace");
  const formId = useId();
  const id = `workspace-${formId}-${field.key}`;
  const optionSource = field.optionsFrom;
  const [loadedOptions, setLoadedOptions] = useState<{
    source: WorkspaceOptionSource;
    options: Array<{ label: string; value: string }>;
    error?: string;
  } | null>(null);
  const loadedForSource =
    loadedOptions?.source === optionSource ? loadedOptions : null;
  const remoteOptions = loadedForSource?.options ?? [];
  const optionsError = loadedForSource?.error ?? "";
  const optionsLoading =
    optionSource !== undefined && loadedForSource === null;
  const baseClass =
    "w-full rounded border border-outline-variant bg-surface-container-lowest px-space-md py-2 font-body-sm text-body-sm text-on-surface focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary";

  useEffect(() => {
    if (!optionSource) return;

    let active = true;
    const controller = new AbortController();
    void clientApiGet<unknown>(optionSource.path, {
      signal: controller.signal,
    })
      .then((payload) => {
        const options = extractOptions(payload, optionSource);
        if (!active) return;
        if (!options) {
          setLoadedOptions({
            source: optionSource,
            options: [],
            error: t("unexpectedResponse"),
          });
          return;
        }
        setLoadedOptions({ source: optionSource, options });
      })
      .catch((loadError: unknown) => {
        if (!active) return;
        setLoadedOptions({
          source: optionSource,
          options: [],
          error: errorMessage(loadError, t),
        });
      });

    return () => {
      active = false;
      controller.abort();
    };
  }, [optionSource, t]);

  if (field.type === "checkbox") {
    return (
      <label className="flex items-center gap-space-sm font-body-sm text-body-sm text-on-surface">
        <input
          checked={value === true}
          id={id}
          name={field.key}
          onChange={(event) => onChange(event.target.checked)}
          type="checkbox"
        />
        {field.label}
        {error ? <span className="text-status-urgent-text">{error}</span> : null}
      </label>
    );
  }

  return (
    <label className="block">
      <span className="mb-space-xs block font-label-sm text-label-sm font-medium text-on-surface">
        {field.label}
        {field.required ? <span aria-hidden="true"> *</span> : null}
      </span>
      {field.type === "textarea" || field.type === "list" ? (
        <textarea
          className={`${baseClass} min-h-24`}
          id={id}
          maxLength={4000}
          name={field.key}
          onChange={(event) => onChange(event.target.value)}
          placeholder={field.placeholder}
          required={field.required}
          value={typeof value === "string" ? value : ""}
        />
      ) : field.type === "select" ? (
        <>
          <select
            className={baseClass}
            id={id}
            name={field.key}
            onChange={(event) => onChange(event.target.value)}
            required={field.required}
            value={typeof value === "string" ? value : ""}
          >
            <option value="">
              {optionSource
                ? optionsLoading
                  ? t("loadingStudentDataValues")
                  : optionsError
                    ? t("studentDataOptionsUnavailable")
                    : remoteOptions.length
                      ? t("selectStudentDataValue")
                      : t("noCorrectableStudentData")
                : "—"}
            </option>
            {(optionSource ? remoteOptions : field.options ?? []).map(
              (option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ),
            )}
          </select>
          {optionsError ? (
            <span
              className="mt-1 block font-caption text-caption text-status-urgent-text"
              role="alert"
            >
              {optionsError}
            </span>
          ) : null}
        </>
      ) : field.type === "file" ? (
        <input
          accept={field.accept}
          className={baseClass}
          id={id}
          name={field.key}
          onChange={(event) => onChange(event.target.files?.[0] ?? null)}
          required={field.required}
          type="file"
        />
      ) : (
        <input
          className={baseClass}
          id={id}
          maxLength={field.type === "text" || field.type === "email" ? 255 : undefined}
          name={field.key}
          onChange={(event) => onChange(event.target.value)}
          placeholder={field.placeholder}
          required={field.required}
          type={field.type ?? "text"}
          value={typeof value === "string" || typeof value === "number" ? value : ""}
        />
      )}
      {error ? (
        <span className="mt-1 block font-caption text-caption text-status-urgent-text">
          {error}
        </span>
      ) : null}
    </label>
  );
}

function makeBody(
  fields: WorkspaceField[],
  values: Record<string, unknown>,
  defaults: Record<string, unknown>,
): FormData | Record<string, unknown> | undefined {
  if (fields.length === 0 && Object.keys(defaults).length === 0) return undefined;
  const containsFile = fields.some((field) => field.type === "file");
  const body: FormData | Record<string, unknown> = containsFile
    ? new FormData()
    : { ...defaults };

  if (body instanceof FormData) {
    for (const [key, value] of Object.entries(defaults)) {
      body.append(key, typeof value === "string" ? value : JSON.stringify(value));
    }
  }

  for (const field of fields) {
    if (field.pathOnly) continue;
    const value = values[field.key];
    if (value === undefined || value === null) continue;

    let normalized: unknown = value;
    if (field.type === "list") {
      normalized =
        typeof value === "string"
          ? value
              .split(/[\n,]/)
              .map((item) => item.trim())
              .filter(Boolean)
          : value;
    } else if (field.type === "number" && typeof value === "string") {
      normalized = value === "" ? "" : Number(value);
    }

    if (body instanceof FormData) {
      if (value instanceof File) {
        body.append(field.key, value);
      } else if (normalized !== "") {
        body.append(
          field.key,
          typeof normalized === "string"
            ? normalized
            : JSON.stringify(normalized),
        );
      }
    } else {
      body[field.key] = normalized;
    }
  }

  return body;
}

function hasRequiredQueryValues(
  query: ApiQuery,
  requiredFields: WorkspaceField[],
  scopedQuery: ApiQuery,
): boolean {
  return requiredFields.every((field) => {
    const value = query[field.key] ?? scopedQuery[field.key];
    return value !== undefined && value !== null && value !== "";
  });
}

function getScopedQuery(
  queryFields: WorkspaceField[] | undefined,
  pathValues: Record<string, unknown> | undefined,
): ApiQuery {
  const query: ApiQuery = {};
  for (const field of queryFields ?? []) {
    const value = pathValues?.[field.key];
    if (
      typeof value === "string" ||
      typeof value === "number" ||
      typeof value === "boolean"
    ) {
      query[field.key] = value;
    }
  }
  return query;
}

function readRecordValue(
  record: Record<string, unknown>,
  key: string,
): unknown {
  const expected = key.toLowerCase();
  const entry = Object.entries(record).find(
    ([name]) => name.toLowerCase() === expected,
  );
  return entry?.[1];
}

function bodyFromRecord(
  operation: WorkspaceOperation,
  record: Record<string, unknown>,
): Record<string, unknown> {
  const body: Record<string, unknown> = {};
  const sources = new Map<
    string,
    { key: string; asArray?: boolean }
  >();
  for (const field of operation.fields ?? []) {
    sources.set(field.key, { key: field.key });
  }
  for (const [field, source] of Object.entries(operation.bodyFromRecord ?? {})) {
    sources.set(field, source);
  }
  for (const [field, source] of sources) {
    const value = record[source.key];
    if (value !== undefined && value !== null) {
      body[field] = source.asArray ? [value] : value;
    }
  }
  return body;
}

function extractRecords(
  payload: unknown,
  collectionKey?: string,
): Record<string, unknown>[] | null {
  if (Array.isArray(payload)) {
    return payload.filter(isRecord);
  }
  if (!isRecord(payload)) return null;

  const keys = collectionKey
    ? [collectionKey, "items", "data", "results", "records", "value"]
    : ["items", "data", "results", "records", "value"];
  for (const key of keys) {
    const records = payload[key];
    if (Array.isArray(records)) return records.filter(isRecord);
  }
  for (const value of Object.values(payload)) {
    if (Array.isArray(value)) return value.filter(isRecord);
  }
  return recordIdentity(payload) ? [payload] : null;
}

function extractOptions(
  payload: unknown,
  source: WorkspaceOptionSource,
): Array<{ label: string; value: string }> | null {
  const records = extractRecords(payload, source.collectionKey);
  if (!records) return null;

  return records.flatMap((record) => {
    const matches = Object.entries(source.match ?? {}).every(
      ([key, expected]) => readRecordValue(record, key) === expected,
    );
    if (!matches) return [];

    const optionValue = readRecordValue(record, source.valueKey);
    if (
      (typeof optionValue !== "string" && typeof optionValue !== "number") ||
      String(optionValue).length === 0 ||
      (source.requiredValueKeys ?? []).some((key) => {
        const requiredValue = readRecordValue(record, key);
        return (
          requiredValue === null ||
          requiredValue === undefined ||
          requiredValue === ""
        );
      })
    ) {
      return [];
    }

    const labelParts = source.labelKeys
      .map((key) => readRecordValue(record, key))
      .filter(
        (value) => value !== null && value !== undefined && value !== "",
      )
      .map(displayValue);

    return [
      {
        label: labelParts.join(" · ") || String(optionValue),
        value: String(optionValue),
      },
    ];
  });
}

function recordIdentity(record: Record<string, unknown>): string | undefined {
  const accountId = record.accountId;
  const roleId = record.roleId;
  if (
    (typeof accountId === "string" || typeof accountId === "number") &&
    (typeof roleId === "string" || typeof roleId === "number")
  ) {
    return `account-role:${accountId}:${roleId}`;
  }

  const keys = [
    "caseNoteId",
    "caseChangeRequestId",
    "correctionRequestId",
    "emergencyAccessRequestId",
    "dataAttributeId",
    "consentPurposeId",
    "accessPolicyId",
    "studentDataId",
    "referralId",
    "erasureRequestId",
    "notificationId",
    "externalServiceId",
    "interventionId",
    "supportGrantId",
    "sessionId",
    "facultyId",
    "subjectId",
    "programmeId",
    "academicTermId",
    "roleId",
    "id",
    "consentId",
    "caseId",
    "accountId",
  ];
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" || typeof value === "number") {
      return String(value);
    }
  }
  return undefined;
}

function createRowKey(
  record: Record<string, unknown>,
  index: number,
  occurrences: Map<string, number>,
): string {
  const identity = recordIdentity(record);
  if (identity === undefined) return JSON.stringify([null, index]);

  const occurrence = occurrences.get(identity) ?? 0;
  occurrences.set(identity, occurrence + 1);
  return JSON.stringify([identity, occurrence]);
}

function resolvePath(
  template: string,
  record: Record<string, unknown>,
): string {
  return template.replace(/:([A-Za-z][A-Za-z0-9]*)/g, (placeholder, key: string) => {
    const value =
      record[key] ??
      record[`${key[0]?.toLowerCase() ?? ""}${key.slice(1)}`] ??
      record[`${key[0]?.toUpperCase() ?? ""}${key.slice(1)}`];
    if (typeof value !== "string" && typeof value !== "number") {
      throw new ApiError({
        message: "The selected record does not contain the required identifier.",
        status: 400,
        code: "missing_record_identifier",
      });
    }
    return encodeURIComponent(String(value));
  });
}

function getPathValue(
  record: Record<string, unknown>,
  path: string,
): unknown {
  return path.split(".").reduce<unknown>((current, key) => {
    return isRecord(current) ? current[key] : undefined;
  }, record);
}

function displayValue(value: unknown): string {
  if (value === null || value === undefined || value === "") return "—";
  if (typeof value === "string") {
    const date = new Date(value);
    if (
      value.length >= 20 &&
      value.includes("T") &&
      !Number.isNaN(date.getTime())
    ) {
      return new Intl.DateTimeFormat(undefined, {
        dateStyle: "medium",
        timeStyle: "short",
        timeZone: "Asia/Ho_Chi_Minh",
      }).format(date);
    }
    return value;
  }
  if (typeof value === "number" || typeof value === "boolean") {
    return String(value);
  }
  if (Array.isArray(value)) return value.map(displayValue).join(", ");
  return "[record]";
}

type WorkspaceErrorKey =
  | "apiError400"
  | "apiError401"
  | "apiError403"
  | "apiError404"
  | "apiError409"
  | "apiError5xx"
  | "apiErrorNetwork"
  | "actionFailed";

function errorMessage(
  error: unknown,
  translate: (key: WorkspaceErrorKey) => string,
): string {
  if (!(error instanceof ApiError)) return translate("apiErrorNetwork");
  if (error.status === 400) return translate("apiError400");
  if (error.status === 401) return translate("apiError401");
  if (error.status === 403) return translate("apiError403");
  if (error.status === 404) return translate("apiError404");
  if (error.status === 409) return translate("apiError409");
  if (error.status >= 500 || error.status === 0) {
    return translate(error.status === 502 ? "apiErrorNetwork" : "apiError5xx");
  }
  return error.message || translate("actionFailed");
}

function pascalCase(value: string): string {
  return value.length === 0 ? value : `${value[0]?.toUpperCase()}${value.slice(1)}`;
}

function camelCase(value: string): string {
  return value.length === 0 ? value : `${value[0]?.toLowerCase()}${value.slice(1)}`;
}
