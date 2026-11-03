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
import { useRouter } from "@/i18n/navigation";
import { ApiError, isRecord, type ApiMethod, type ApiQuery } from "@/lib/api-contract";
import { clientApiGet, clientApiRequest } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { moveRowFocus } from "@/lib/keyboard";
import { formatDate, formatTime, parseDate, shortId } from "@/lib/format";
import { statusFields, statusKey } from "@/lib/status";
import { Button, ButtonLink } from "@/components/ui/button";
import { useConfirm } from "@/components/ui/confirm-dialog";
import { Drawer } from "@/components/ui/drawer";
import { controlClass } from "@/components/ui/field";
import { Icon } from "@/components/ui/icon";
import { Notice } from "@/components/ui/notice";
import { PageHeader } from "@/components/ui/page-header";
import { EmptyState, ErrorState, SkeletonRows } from "@/components/ui/states";
import { StatusValue } from "@/components/ui/status-value";
import {
  bodyFromRecord,
  camelCase,
  createRowKey,
  displayValue,
  errorMessage,
  extractOptions,
  extractRecords,
  extractTotal,
  getPathValue,
  getScopedQuery,
  hasRequiredQueryValues,
  isIdKey,
  isLongTextKey,
  makeBody,
  pascalCase,
  readRecordValue,
  recordTitle,
  resolvePath,
} from "./workspace-helpers";

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
  hint?: string;
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
  query?: ApiQuery;
}

export interface WorkspaceColumn {
  key: string;
  label: string;
  /** Override the automatic cell format. */
  format?: "text" | "status" | "date" | "datetime" | "id" | "number" | "identity" | "label";
}

export interface WorkspaceOperation {
  label: string;
  /** One line under the operation's heading in the record drawer. */
  description?: string;
  method: ApiMethod;
  path: string;
  fields?: WorkspaceField[];
  confirm?: boolean;
  /** The operation removes or blocks something (red button, warning dialog). */
  destructive?: boolean;
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
  /** Rows link to a page of their own instead of opening the drawer. */
  rowHref?: string;
  /** Main action of the screen when it lives on another page. */
  primaryLink?: { label: string; href: string; icon?: string };
  pageSize?: number;
  pathValues?: Record<string, unknown>;
  recordFilter?: { key: string; value: string };
  /** Copy for the empty list: what will appear here and how it gets here. */
  emptyTitle?: string;
  emptyBody?: string;
  /** Second and later workspaces on a page render without the page title. */
  embedded?: boolean;
}

type UiT = ReturnType<typeof useTranslations<"ui">>;

/**
 * A list screen generated from a definition: title row, one filter toolbar,
 * the table, and a right-hand drawer to create a record or work on the one
 * that was clicked. Every API state (loading, empty, error, needs filters)
 * keeps the list's frame.
 */
export function ApiWorkspace({
  definition,
}: {
  definition: ApiWorkspaceDefinition;
}) {
  const t = useTranslations("workspace");
  const tu = useTranslations("ui");
  const router = useRouter();
  const listPath = useMemo(() => {
    try {
      return resolvePath(definition.listPath, definition.pathValues ?? {});
    } catch {
      return definition.listPath;
    }
  }, [definition.listPath, definition.pathValues]);
  const scopedQuery = useMemo(
    () => getScopedQuery(definition.queryFields, definition.pathValues),
    [definition.pathValues, definition.queryFields],
  );
  const recordFilter = definition.recordFilter;
  const queryFields = useMemo(
    () =>
      (definition.queryFields ?? []).filter(
        (field) =>
          !Object.prototype.hasOwnProperty.call(definition.pathValues ?? {}, field.key),
      ),
    [definition.pathValues, definition.queryFields],
  );
  const requiredQueryFields = useMemo(
    () => queryFields.filter((field) => field.required),
    [queryFields],
  );
  const [records, setRecords] = useState<Record<string, unknown>[]>([]);
  const [total, setTotal] = useState<number | undefined>(undefined);
  const [selectedRecord, setSelectedRecord] = useState<Record<string, unknown> | null>(null);
  const [creating, setCreating] = useState(false);
  const [globalOperation, setGlobalOperation] = useState<WorkspaceOperation | null>(null);
  const [query, setQuery] = useState<ApiQuery>(() => ({
    ...scopedQuery,
    ...(definition.pageSize ? { page: 1, pageSize: definition.pageSize } : {}),
  }));
  const requiredQueryValuesPresent = hasRequiredQueryValues(
    query,
    requiredQueryFields,
    scopedQuery,
  );
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [filterValues, setFilterValues] = useState<Record<string, string | boolean>>({});

  const loadRecords = useCallback(
    async (nextQuery: ApiQuery = query) => {
      if (!hasRequiredQueryValues(nextQuery, requiredQueryFields, scopedQuery)) return;

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
                String(readRecordValue(record, recordFilter.key) ?? "") === recordFilter.value,
            )
          : result;
        setRecords(visibleRecords);
        setTotal(recordFilter ? visibleRecords.length : extractTotal(payload));
      } catch (error) {
        setLoadError(errorMessage(error, t));
        setRecords([]);
        setTotal(undefined);
      } finally {
        setLoading(false);
      }
    },
    [definition.collectionKey, listPath, query, requiredQueryFields, recordFilter, scopedQuery, t],
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
            !Object.prototype.hasOwnProperty.call(definition.pathValues ?? {}, field.key),
        ),
      }
    : undefined;
  const columns = definition.columns;
  const rowKeyOccurrences = new Map<string, number>();
  const hasFiltersApplied = queryFields.some((field) => {
    const value = query[field.key];
    return value !== undefined && value !== "" && value !== false;
  });
  const recordsAreOpenable = Boolean(definition.rowHref || definition.rowOperations?.length || columns.length);

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextQuery: ApiQuery = {};
    for (const [key, value] of Object.entries(filterValues)) {
      if (value !== "" && value !== false) nextQuery[key] = value;
    }
    if (definition.pageSize) {
      nextQuery.page = 1;
      nextQuery.pageSize = definition.pageSize;
    }
    setQuery(nextQuery);
  }

  function clearFilters() {
    setFilterValues({});
    setQuery(definition.pageSize ? { page: 1, pageSize: definition.pageSize } : {});
  }

  function changePage(page: number) {
    setQuery({ ...query, page, pageSize: definition.pageSize ?? query.pageSize });
  }

  function openRecord(record: Record<string, unknown>) {
    if (definition.rowHref) {
      try {
        router.push(resolvePath(definition.rowHref, record));
        return;
      } catch {
        // fall through to the drawer when the record lacks the id
      }
    }
    setSelectedRecord(record);
  }

  const page = Number(query.page ?? 1);
  const pageSize = definition.pageSize;
  const countLabel =
    total !== undefined
      ? tu("workspace.recordCount", { count: total })
      : tu("workspace.recordCount", { count: records.length });

  return (
    <div className="flex flex-col gap-space-lg">
      {definition.embedded ? null : (
        <PageHeader
          title={definition.title}
          description={definition.description}
          actions={
            <>
              {definition.globalOperations?.map((operation) => (
                <Button
                  key={`${operation.method}:${operation.path}`}
                  variant="secondary"
                  onClick={() => setGlobalOperation(operation)}
                >
                  {operation.label}
                </Button>
              ))}
              {createOperation ? (
                <Button icon="add" onClick={() => setCreating(true)}>
                  {createOperation.label}
                </Button>
              ) : null}
              {definition.primaryLink ? (
                <ButtonLink href={definition.primaryLink.href} icon={definition.primaryLink.icon ?? "add"}>
                  {definition.primaryLink.label}
                </ButtonLink>
              ) : null}
            </>
          }
        />
      )}

      <section className="overflow-hidden rounded-lg border border-rule bg-paper">
        <div className="flex flex-wrap items-center gap-x-space-md gap-y-space-sm border-b border-rule px-space-lg py-space-md">
          {definition.embedded ? (
            <h2 className="mr-auto text-base font-semibold text-text">{definition.title}</h2>
          ) : null}
          {queryFields.length ? (
            <FilterBar
              fields={queryFields}
              values={filterValues}
              onChange={(key, value) =>
                setFilterValues((current) => ({ ...current, [key]: value }))
              }
              onSubmit={applyFilters}
              onClear={clearFilters}
              disabled={loading && requiredQueryValuesPresent}
              canClear={hasFiltersApplied || Object.keys(filterValues).length > 0}
            />
          ) : null}
          <div className="ml-auto flex items-center gap-space-sm">
            {requiredQueryValuesPresent && !loading && !loadError ? (
              <span className="text-[0.8125rem] text-text-faint">{countLabel}</span>
            ) : null}
            <Button
              aria-label={t("refresh")}
              title={t("refresh")}
              disabled={loading || !requiredQueryValuesPresent}
              icon="refresh"
              onClick={() => void loadRecords()}
              size="sm"
              variant="quiet"
            />
            {definition.embedded && createOperation ? (
              <Button icon="add" size="sm" onClick={() => setCreating(true)}>
                {createOperation.label}
              </Button>
            ) : null}
          </div>
        </div>

        {!requiredQueryValuesPresent ? (
          <EmptyState icon="tune" title={tu("workspace.needsFiltersTitle")}>
            {t("completeRequiredFilters")}
          </EmptyState>
        ) : loadError ? (
          <ErrorState
            title={loadError}
            action={
              <Button onClick={() => void loadRecords()} size="sm" variant="secondary">
                {t("retry")}
              </Button>
            }
          />
        ) : loading ? (
          <SkeletonRows columns={Math.min(columns.length, 6)} label={t("loading")} />
        ) : records.length === 0 ? (
          hasFiltersApplied ? (
            <EmptyState
              icon="filter_alt_off"
              title={tu("workspace.noMatchesTitle")}
              action={
                <Button size="sm" variant="secondary" onClick={clearFilters}>
                  {tu("workspace.clearFilters")}
                </Button>
              }
            >
              {tu("workspace.noMatchesBody")}
            </EmptyState>
          ) : (
            <EmptyState
              title={definition.emptyTitle ?? tu("workspace.emptyTitle")}
              action={
                createOperation ? (
                  <Button icon="add" size="sm" onClick={() => setCreating(true)}>
                    {createOperation.label}
                  </Button>
                ) : undefined
              }
            >
              {definition.emptyBody ?? tu("workspace.emptyBody")}
            </EmptyState>
          )
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-left text-sm">
              <thead>
                <tr className="border-b border-rule">
                  {columns.map((column) => (
                    <th
                      className={cn(
                        "h-10 whitespace-nowrap px-space-md text-[0.8125rem] font-semibold text-text-faint first:pl-space-lg last:pr-space-lg",
                        columnKind(column) === "number" && "text-right",
                      )}
                      key={column.key}
                      scope="col"
                    >
                      {column.label}
                    </th>
                  ))}
                  {recordsAreOpenable ? (
                    <th className="w-10 pr-space-md" scope="col">
                      <span className="sr-only">{t("actions")}</span>
                    </th>
                  ) : null}
                </tr>
              </thead>
              <tbody className="divide-y divide-rule" onKeyDown={moveRowFocus}>
                {records.map((record, index) => {
                  const key = createRowKey(record, index, rowKeyOccurrences);
                  const isSelected = selectedRecord === record;
                  return (
                    <tr
                      className={cn(
                        "group relative cursor-pointer transition-colors duration-100 focus-within:bg-paper-tint focus-within:outline-2 focus-within:-outline-offset-2 focus-within:outline-ink",
                        isSelected ? "bg-ink-wash" : "hover:bg-paper-tint",
                      )}
                      key={key}
                      onClick={() => openRecord(record)}
                    >
                      {columns.map((column, columnIndex) => (
                        <td
                          className={cn(
                            "h-11 max-w-80 px-space-md py-2 align-middle first:pl-space-lg",
                            columnKind(column) === "number" && "text-right",
                          )}
                          key={column.key}
                        >
                          {columnIndex === 0 ? (
                            // The first cell is the row's one focus stop; its
                            // ::after stretches over the whole row so a click
                            // anywhere opens the record, and Tab / j / k move
                            // between rows instead of between cells.
                            <button
                              type="button"
                              data-row-open
                              aria-label={`${tu("workspace.openRecord")}: ${recordTitle(record) ?? displayValue(getPathValue(record, column.key))}`}
                              className="text-left after:absolute after:inset-0 after:content-[''] focus-visible:outline-none"
                              onClick={(event) => {
                                event.stopPropagation();
                                openRecord(record);
                              }}
                            >
                              <Cell column={column} record={record} tu={tu} />
                            </button>
                          ) : (
                            <Cell column={column} record={record} tu={tu} />
                          )}
                        </td>
                      ))}
                      {recordsAreOpenable ? (
                        <td className="w-10 pr-space-md text-right align-middle">
                          <Icon
                            name="chevron_right"
                            className="text-[20px] text-rule-strong transition-colors group-hover:text-ink"
                          />
                        </td>
                      ) : null}
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {pageSize && requiredQueryValuesPresent && !loadError && (records.length > 0 || page > 1) ? (
          <div className="flex items-center justify-between gap-space-sm border-t border-rule px-space-lg py-space-sm">
            <span className="font-mono text-[0.8125rem] text-text-faint">
              {total !== undefined && records.length > 0
                ? tu("workspace.range", {
                    from: (page - 1) * pageSize + 1,
                    to: (page - 1) * pageSize + records.length,
                    total,
                  })
                : `${t("page")} ${page}`}
            </span>
            <div className="flex gap-space-sm">
              <Button
                disabled={loading || page <= 1}
                icon="chevron_left"
                onClick={() => changePage(Math.max(1, page - 1))}
                size="sm"
                variant="secondary"
              >
                {t("previous")}
              </Button>
              <Button
                disabled={
                  loading ||
                  (total !== undefined
                    ? page * pageSize >= total
                    : records.length < pageSize)
                }
                iconAfter="chevron_right"
                onClick={() => changePage(page + 1)}
                size="sm"
                variant="secondary"
              >
                {t("next")}
              </Button>
            </div>
          </div>
        ) : null}
      </section>

      {createOperation ? (
        <Drawer
          open={creating}
          onClose={() => setCreating(false)}
          title={createOperation.label}
          description={createOperation.description ?? definition.title}
          closeLabel={tu("close")}
        >
          <ApiOperationForm
            definition={createOperation}
            bodyDefaults={definition.pathValues}
            pathValues={definition.pathValues}
            onComplete={async () => {
              await loadRecords();
            }}
          />
        </Drawer>
      ) : null}

      {globalOperation ? (
        <Drawer
          open
          onClose={() => setGlobalOperation(null)}
          title={globalOperation.label}
          description={globalOperation.description}
          closeLabel={tu("close")}
        >
          <ApiOperationForm
            definition={globalOperation}
            pathValues={definition.pathValues}
            onComplete={() => loadRecords()}
          />
        </Drawer>
      ) : null}

      <Drawer
        open={selectedRecord !== null}
        onClose={() => setSelectedRecord(null)}
        title={
          selectedRecord
            ? (recordTitle(selectedRecord) ?? tu("workspace.recordDetails"))
            : ""
        }
        description={definition.title}
        closeLabel={tu("close")}
        width="lg"
      >
        {selectedRecord ? (
          <RecordDetail
            columns={columns}
            record={selectedRecord}
            operations={definition.rowOperations ?? []}
            pathValues={definition.pathValues}
            onComplete={() => loadRecords()}
          />
        ) : null}
      </Drawer>
    </div>
  );
}

/* ------------------------------------------------------------------------ */
/* Filters                                                                   */
/* ------------------------------------------------------------------------ */

const INLINE_FILTERS = 3;

function FilterBar({
  fields,
  values,
  onChange,
  onSubmit,
  onClear,
  disabled,
  canClear,
}: {
  fields: WorkspaceField[];
  values: Record<string, string | boolean>;
  onChange: (key: string, value: string | boolean) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onClear: () => void;
  disabled: boolean;
  canClear: boolean;
}) {
  const t = useTranslations("workspace");
  const tu = useTranslations("ui");
  // Required filters always show; the rest fold after the first three.
  const ordered = [...fields].sort((a, b) => Number(b.required ?? false) - Number(a.required ?? false));
  const [expanded, setExpanded] = useState(false);
  const visible = expanded ? ordered : ordered.slice(0, Math.max(INLINE_FILTERS, ordered.filter((f) => f.required).length));
  const hidden = ordered.length - visible.length;

  return (
    <form className="flex min-w-0 flex-1 flex-wrap items-end gap-space-sm" onSubmit={onSubmit}>
      {visible.map((field) => (
        <div
          key={field.key}
          className={cn(
            field.type === "checkbox" ? "pb-2" : "w-full sm:w-44",
            field.type === "date" && "sm:w-40",
          )}
        >
          <WorkspaceInput
            compact
            field={field}
            onChange={(value) => onChange(field.key, value as string | boolean)}
            value={values[field.key] ?? (field.type === "checkbox" ? false : "")}
          />
        </div>
      ))}
      <div className="flex items-center gap-1.5">
        <Button disabled={disabled} type="submit" size="sm" variant="secondary" icon="filter_alt">
          {t("applyFilters")}
        </Button>
        {hidden > 0 || expanded ? (
          <Button size="sm" variant="quiet" onClick={() => setExpanded(!expanded)}>
            {expanded ? tu("workspace.fewerFilters") : tu("workspace.moreFilters", { count: hidden })}
          </Button>
        ) : null}
        {canClear ? (
          <Button size="sm" variant="tertiary" onClick={onClear}>
            {t("clearFilters")}
          </Button>
        ) : null}
      </div>
    </form>
  );
}

/* ------------------------------------------------------------------------ */
/* Cells                                                                     */
/* ------------------------------------------------------------------------ */

function columnKind(column: WorkspaceColumn): NonNullable<WorkspaceColumn["format"]> {
  if (column.format) return column.format;
  if (statusFields.has(column.key)) return "status";
  if (isIdKey(column.key)) return "id";
  if (/(At|Date|From|To|Until)$/.test(column.key)) return "datetime";
  if (/(Count|Cases|Rate|Total|Days|Number)$/.test(column.key) || /^(total|count)/.test(column.key)) {
    return "number";
  }
  if (/^(studentName|fullName|accountName)$/.test(column.key)) return "identity";
  if (/(supportType|Type|format|channel)$/.test(column.key)) return "label";
  return "text";
}

function Cell({
  column,
  record,
  tu,
}: {
  column: WorkspaceColumn;
  record: Record<string, unknown>;
  tu: UiT;
}) {
  const raw = getPathValue(record, column.key) ?? readRecordValue(record, column.key);
  return <FormattedValue kind={columnKind(column)} fieldKey={column.key} value={raw} tu={tu} />;
}

function FormattedValue({
  kind,
  fieldKey,
  value,
  tu,
  full,
}: {
  kind: NonNullable<WorkspaceColumn["format"]>;
  fieldKey: string;
  value: unknown;
  tu: UiT;
  full?: boolean;
}) {
  if (value === null || value === undefined || value === "") {
    return <span className="text-rule-strong">—</span>;
  }
  if (typeof value === "boolean") {
    return <span className="text-text-muted">{value ? tu("yes") : tu("no")}</span>;
  }
  if (Array.isArray(value)) {
    if (value.length === 0) return <span className="text-rule-strong">—</span>;
    return (
      <span className="flex flex-wrap gap-1">
        {value.map((item, index) => (
          <span
            key={index}
            className="rounded-md border border-rule bg-paper-tint px-1.5 py-0.5 text-[0.8125rem] text-text-muted"
          >
            {displayValue(item)}
          </span>
        ))}
      </span>
    );
  }
  if (typeof value === "string" && kind === "status") {
    return <StatusValue field={fieldKey} value={value} size="sm" />;
  }
  if (typeof value === "string" && kind === "id") {
    return full ? (
      <span className="break-all font-mono text-[0.8125rem] text-text-muted">{value}</span>
    ) : (
      <CopyableId value={value} />
    );
  }
  if ((kind === "datetime" || kind === "date") && parseDate(value)) {
    return (
      <span className="whitespace-nowrap font-mono text-[0.8125rem] text-text-muted">
        {formatDate(value)}
        {kind === "datetime" && typeof value === "string" && value.length > 10 ? (
          <span className="ml-1.5 text-text-faint">{formatTime(value)}</span>
        ) : null}
      </span>
    );
  }
  if (typeof value === "number") {
    return (
      <span className="font-mono text-[0.8125rem] tabular-nums text-text">
        {Number.isInteger(value) ? value.toLocaleString("en-US") : value.toFixed(1)}
      </span>
    );
  }
  if (isRecord(value)) {
    return <span className="text-text-faint">—</span>;
  }
  const text = String(value);
  if (kind === "label") {
    const key = statusKey(text);
    return (
      <span className="text-text-muted">
        {tu.has(`status.${key}`) ? tu(`status.${key}`) : text}
      </span>
    );
  }
  if (kind === "identity") {
    return <span className="font-semibold text-text">{text}</span>;
  }
  return (
    <span className={cn("text-text-muted", full ? "whitespace-pre-line break-words" : "line-clamp-2")}>
      {text}
    </span>
  );
}

function CopyableId({ value }: { value: string }) {
  const tu = useTranslations("ui");
  const [copied, setCopied] = useState(false);
  return (
    <span className="inline-flex items-center gap-1 whitespace-nowrap">
      <span title={value} className="font-mono text-[0.8125rem] text-text-muted">
        {shortId(value)}
        {value.length > 12 ? "…" : ""}
      </span>
      <button
        type="button"
        aria-label={copied ? tu("copied") : tu("copy")}
        title={copied ? tu("copied") : tu("copy")}
        className="relative z-10 flex size-6 items-center justify-center rounded text-text-faint opacity-0 transition hover:bg-ink-wash hover:text-ink focus-visible:opacity-100 group-hover:opacity-100"
        onClick={(event) => {
          event.stopPropagation();
          void navigator.clipboard?.writeText(value).then(() => {
            setCopied(true);
            window.setTimeout(() => setCopied(false), 1500);
          });
        }}
      >
        <Icon name={copied ? "check" : "content_copy"} className="text-[14px]" />
      </button>
    </span>
  );
}

/* ------------------------------------------------------------------------ */
/* Record drawer                                                             */
/* ------------------------------------------------------------------------ */

function RecordDetail({
  columns,
  record,
  operations,
  pathValues,
  onComplete,
}: {
  columns: WorkspaceColumn[];
  record: Record<string, unknown>;
  operations: WorkspaceOperation[];
  pathValues?: Record<string, unknown>;
  onComplete: () => Promise<void>;
}) {
  const tu = useTranslations("ui");
  const shownKeys = new Set(columns.map((column) => column.key.toLowerCase()));
  const extraFields = Object.entries(record).filter(
    ([key, value]) =>
      !shownKeys.has(key.toLowerCase()) &&
      (typeof value !== "object" || value === null || Array.isArray(value)),
  );
  const [openOperation, setOpenOperation] = useState<number | null>(
    operations.length === 1 ? 0 : null,
  );

  return (
    <div className="flex flex-col gap-space-xl">
      <FieldList
        entries={columns.map((column) => [column.key, column.label, getPathValue(record, column.key) ?? readRecordValue(record, column.key), columnKind(column)])}
      />

      {extraFields.length ? (
        <details className="group rounded-lg border border-rule">
          <summary className="flex cursor-pointer list-none items-center justify-between px-space-base py-space-sm text-sm font-semibold text-text-muted hover:text-text">
            {tu("workspace.allFields")}
            <Icon name="expand_more" className="text-[20px] transition-transform group-open:rotate-180" />
          </summary>
          <div className="border-t border-rule px-space-base py-space-md">
            <FieldList
              entries={extraFields.map(([key, value]) => [
                key,
                humanizeKey(key),
                value,
                columnKind({ key, label: key }),
              ])}
            />
          </div>
        </details>
      ) : null}

      {operations.length ? (
        <section>
          <h3 className="mb-space-sm text-sm font-semibold text-text">{tu("workspace.actions")}</h3>
          <div className="divide-y divide-rule rounded-lg border border-rule">
            {operations.map((operation, index) => {
              const isOpen = openOperation === index;
              return (
                <div key={`${operation.method}:${operation.path}:${index}`}>
                  <button
                    type="button"
                    aria-expanded={isOpen}
                    onClick={() => setOpenOperation(isOpen ? null : index)}
                    className="flex w-full items-center gap-space-md px-space-base py-space-md text-left transition-colors hover:bg-paper-tint"
                  >
                    <Icon
                      name={operationIcon(operation)}
                      className={cn(
                        "text-[20px]",
                        operation.destructive ? "text-red-ink" : "text-ink",
                      )}
                    />
                    <span className="min-w-0 flex-1">
                      <span className="block text-sm font-semibold text-text">{operation.label}</span>
                      {operation.description ? (
                        <span className="block text-[0.8125rem] text-text-faint">
                          {operation.description}
                        </span>
                      ) : null}
                    </span>
                    <Icon
                      name="expand_more"
                      className={cn("text-[20px] text-text-faint transition-transform", isOpen && "rotate-180")}
                    />
                  </button>
                  {isOpen ? (
                    <div className="border-t border-rule bg-paper-tint/60 px-space-base pb-space-base">
                      <ApiOperationForm
                        definition={operation}
                        bodyDefaults={bodyFromRecord(operation, record)}
                        pathValues={{ ...pathValues, ...record }}
                        onComplete={onComplete}
                      />
                    </div>
                  ) : null}
                </div>
              );
            })}
          </div>
        </section>
      ) : null}
    </div>
  );
}

function FieldList({
  entries,
}: {
  entries: Array<[string, string, unknown, NonNullable<WorkspaceColumn["format"]>]>;
}) {
  const tu = useTranslations("ui");
  return (
    <dl className="grid gap-x-space-lg gap-y-space-md sm:grid-cols-2">
      {entries.map(([key, label, value, kind]) => (
        <div key={key} className={cn("min-w-0", isLongTextKey(key) && "sm:col-span-2")}>
          <dt className="text-[0.8125rem] text-text-faint">{label}</dt>
          <dd className="mt-0.5 text-sm">
            <FormattedValue kind={kind} fieldKey={key} value={value} tu={tu} full />
          </dd>
        </div>
      ))}
    </dl>
  );
}

function humanizeKey(key: string): string {
  const spaced = key.replace(/([a-z0-9])([A-Z])/g, "$1 $2");
  return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
}

function operationIcon(operation: WorkspaceOperation): string {
  if (operation.destructive || operation.method === "DELETE") return "block";
  if (operation.method === "GET") return "visibility";
  if (operation.method === "POST") return "add_circle";
  return "edit";
}

/* ------------------------------------------------------------------------ */
/* Operation form                                                            */
/* ------------------------------------------------------------------------ */

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
  const tu = useTranslations("ui");
  const { confirm, element: confirmDialog } = useConfirm();
  const [values, setValues] = useState<Record<string, unknown>>({});
  const [pending, setPending] = useState(false);
  const [feedback, setFeedback] = useState<{ ok: boolean; message: string } | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [result, setResult] = useState<unknown>(undefined);
  const isRead = definition.method === "GET";

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFeedback(null);
    setResult(undefined);
    setFieldErrors({});

    if (definition.confirm || definition.destructive) {
      const ok = await confirm({
        title: definition.label,
        body: definition.description ?? t("confirmAction"),
        confirmLabel: definition.label,
        cancelLabel: tu("cancel"),
        destructive: definition.destructive || definition.method === "DELETE",
      });
      if (!ok) return;
    }

    setPending(true);
    try {
      const body = isRead ? undefined : makeBody(definition.fields ?? [], values, bodyDefaults);
      const response = await clientApiRequest<unknown>(
        resolvePath(definition.path, { ...pathValues, ...values }),
        { method: definition.method, body, redirectOnUnauthorized: true },
      );
      if (isRecord(response) && response.isSuccess === false) {
        setFeedback({
          ok: false,
          message: typeof response.message === "string" ? response.message : t("actionFailed"),
        });
        return;
      }
      setResult(response);
      if (!isRead) {
        setFeedback({
          ok: true,
          message:
            isRecord(response) && typeof response.message === "string" && response.message
              ? response.message
              : tu("workspace.saved"),
        });
        setValues({});
        await onComplete?.();
      }
    } catch (error) {
      setFeedback({ ok: false, message: errorMessage(error, t) });
      if (error instanceof ApiError) setFieldErrors(error.fieldErrors);
    } finally {
      setPending(false);
    }
  }

  return (
    <>
      <form className="grid gap-space-md pt-space-md sm:grid-cols-2" onSubmit={submit}>
        {(definition.fields ?? []).map((field) => (
          <div
            key={field.key}
            className={cn(
              (field.type === "textarea" || field.type === "list" || field.type === "file") && "sm:col-span-2",
            )}
          >
            <WorkspaceInput
              field={field}
              onChange={(value) => setValues((current) => ({ ...current, [field.key]: value }))}
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
          </div>
        ))}
        <div className="flex flex-col items-start gap-space-sm sm:col-span-2">
          <Button
            loading={pending}
            type="submit"
            variant={definition.destructive ? "destructive" : isRead ? "secondary" : "primary"}
            icon={isRead ? "visibility" : undefined}
          >
            {pending ? t("submitting") : isRead ? tu("workspace.load") : definition.label}
          </Button>
          {feedback ? (
            <Notice
              className="w-full"
              tone={feedback.ok ? "ontrack" : "urgent"}
              icon={feedback.ok ? "check_circle" : "error"}
              title={feedback.message}
            />
          ) : null}
        </div>
      </form>
      {isRead && result !== undefined ? <ReadResult value={result} /> : null}
      {confirmDialog}
    </>
  );
}

/** A GET operation's answer, shown as fields or a small table, never raw JSON. */
function ReadResult({ value }: { value: unknown }) {
  const tu = useTranslations("ui");
  const records = Array.isArray(value)
    ? value.filter(isRecord)
    : isRecord(value)
      ? (extractRecords(value) ?? [value])
      : [];
  if (records.length === 0) {
    return <p className="mt-space-md text-sm text-text-faint">{tu("workspace.emptyTitle")}</p>;
  }
  return (
    <div className="mt-space-md flex flex-col gap-space-sm">
      {records.map((record, index) => (
        <div key={index} className="rounded-lg border border-rule bg-paper p-space-base">
          <FieldList
            entries={Object.entries(record)
              .filter(([, item]) => !isRecord(item))
              .map(([key, item]) => [key, humanizeKey(key), item, columnKind({ key, label: key })])}
          />
        </div>
      ))}
    </div>
  );
}

/* ------------------------------------------------------------------------ */
/* Inputs                                                                    */
/* ------------------------------------------------------------------------ */

export function WorkspaceInput({
  field,
  value,
  onChange,
  error,
  compact,
}: {
  field: WorkspaceField;
  value: unknown;
  onChange: (value: unknown) => void;
  error?: string;
  /** Filter-bar density: smaller label and control. */
  compact?: boolean;
}) {
  const t = useTranslations("workspace");
  const formId = useId();
  const id = `workspace-${formId}-${field.key}`;
  const errorId = `${id}-error`;
  const optionSource = field.optionsFrom;
  const [loadedOptions, setLoadedOptions] = useState<{
    source: WorkspaceOptionSource;
    options: Array<{ label: string; value: string }>;
    error?: string;
  } | null>(null);
  const loadedForSource = loadedOptions?.source === optionSource ? loadedOptions : null;
  const remoteOptions = loadedForSource?.options ?? [];
  const optionsError = loadedForSource?.error ?? "";
  const optionsLoading = optionSource !== undefined && loadedForSource === null;
  const control = cn(controlClass, "w-full", compact ? "h-8 text-[0.8125rem]" : "h-10");

  useEffect(() => {
    if (!optionSource) return;
    let active = true;
    const controller = new AbortController();
    void clientApiGet<unknown>(optionSource.path, {
      signal: controller.signal,
      query: optionSource.query,
    })
      .then((payload) => {
        const options = extractOptions(payload, optionSource);
        if (!active) return;
        setLoadedOptions(
          options
            ? { source: optionSource, options }
            : { source: optionSource, options: [], error: t("unexpectedResponse") },
        );
      })
      .catch((loadError: unknown) => {
        if (!active) return;
        setLoadedOptions({ source: optionSource, options: [], error: errorMessage(loadError, t) });
      });
    return () => {
      active = false;
      controller.abort();
    };
  }, [optionSource, t]);

  if (field.type === "checkbox") {
    return (
      <label className="flex h-8 items-center gap-2 text-sm text-text">
        <input
          checked={value === true}
          className="size-4 accent-[#2142b8]"
          id={id}
          name={field.key}
          onChange={(event) => onChange(event.target.checked)}
          type="checkbox"
        />
        {field.label}
        {error ? <span className="text-red-ink">{error}</span> : null}
      </label>
    );
  }

  const describedBy = error || optionsError ? errorId : undefined;

  return (
    <div>
      <label
        htmlFor={id}
        className={cn(
          "block text-text",
          compact ? "mb-1 text-[0.75rem] font-semibold text-text-faint" : "mb-1.5 text-sm font-semibold",
        )}
      >
        {field.label}
        {field.required ? <span aria-hidden className="text-red-ink"> *</span> : null}
      </label>
      {field.type === "textarea" || field.type === "list" ? (
        <textarea
          aria-describedby={describedBy}
          aria-invalid={error ? true : undefined}
          className={cn(controlClass, "min-h-28 w-full py-2 leading-6")}
          id={id}
          maxLength={4000}
          name={field.key}
          onChange={(event) => onChange(event.target.value)}
          placeholder={field.placeholder}
          required={field.required}
          value={typeof value === "string" ? value : ""}
        />
      ) : field.type === "select" ? (
        <select
          aria-describedby={describedBy}
          aria-invalid={error ? true : undefined}
          className={cn(control, "pr-8")}
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
              : compact
                ? t("any")
                : "—"}
          </option>
          {(optionSource ? remoteOptions : (field.options ?? [])).map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      ) : field.type === "file" ? (
        <input
          accept={field.accept}
          aria-describedby={describedBy}
          className="block w-full rounded-md border border-dashed border-rule-strong bg-paper-tint px-3 py-3 text-sm text-text-muted file:mr-3 file:rounded-md file:border-0 file:bg-ink file:px-3 file:py-1.5 file:text-sm file:font-semibold file:text-white hover:border-ink"
          id={id}
          name={field.key}
          onChange={(event) => onChange(event.target.files?.[0] ?? null)}
          required={field.required}
          type="file"
        />
      ) : (
        <input
          aria-describedby={describedBy}
          aria-invalid={error ? true : undefined}
          className={control}
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
      {field.hint && !compact ? (
        <p className="mt-1 text-[0.8125rem] text-text-faint">{field.hint}</p>
      ) : null}
      {error || optionsError ? (
        <p id={errorId} role="alert" className="mt-1 text-[0.8125rem] text-red-ink">
          {error || optionsError}
        </p>
      ) : null}
    </div>
  );
}

