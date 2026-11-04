import { ApiError, isRecord, type ApiQuery } from "@/lib/api-contract";
import { formatDateTime, parseDate } from "@/lib/format";
import type {
  WorkspaceField,
  WorkspaceOperation,
  WorkspaceOption,
  WorkspaceOptionSource,
} from "./api-workspace";

/* Pure helpers behind ApiWorkspace: building request bodies, reading the API's
   collection shapes and resolving ":param" paths. No React in this file. */

export function makeBody(
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

export function hasRequiredQueryValues(
  query: ApiQuery,
  requiredFields: WorkspaceField[],
  scopedQuery: ApiQuery,
): boolean {
  return requiredFields.every((field) => {
    const value = query[field.key] ?? scopedQuery[field.key];
    return value !== undefined && value !== null && value !== "";
  });
}

export function getScopedQuery(
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

export function readRecordValue(
  record: Record<string, unknown>,
  key: string,
): unknown {
  const expected = key.toLowerCase();
  const entry = Object.entries(record).find(
    ([name]) => name.toLowerCase() === expected,
  );
  return entry?.[1];
}

export function bodyFromRecord(
  operation: WorkspaceOperation,
  record: Record<string, unknown>,
): Record<string, unknown> {
  const body: Record<string, unknown> = {};
  const sources = new Map<
    string,
    { key: string; asArray?: boolean }
  >();
  for (const field of operation.fields ?? []) {
    if (!field.noPrefill) sources.set(field.key, { key: field.key });
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

/**
 * Turns records that nest their rows (a purpose with its attributes) into one
 * row per nested item. Each row keeps the parent's own fields, and the item's
 * fields win on a name clash.
 */
export function expandRecords(
  records: Record<string, unknown>[],
  key: string,
): Record<string, unknown>[] {
  const expected = key.toLowerCase();
  return records.flatMap((parent) => {
    const nested = Object.entries(parent).find(
      ([name]) => name.toLowerCase() === expected,
    );
    if (!nested || !Array.isArray(nested[1])) return [];
    const inherited = Object.fromEntries(
      Object.entries(parent).filter(([name]) => name !== nested[0]),
    );
    return nested[1].filter(isRecord).map((child) => ({ ...inherited, ...child }));
  });
}

export function extractRecords(
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
  // Fallback: the first array of records. Arrays of plain values (e.g. the
  // `roles: string[]` of Auth/me) are fields of a single record, not a list.
  for (const value of Object.values(payload)) {
    if (Array.isArray(value) && (value.length === 0 || value.some(isRecord))) {
      return value.filter(isRecord);
    }
  }
  return recordIdentity(payload) ? [payload] : null;
}

export function extractOptions(
  payload: unknown,
  source: WorkspaceOptionSource,
): WorkspaceOption[] | null {
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

    const joinParts = (keys: string[]) =>
      keys
        .map((key) => readRecordValue(record, key))
        .filter(
          (value) => value !== null && value !== undefined && value !== "",
        )
        .map(displayValue)
        .join(" · ");
    const detail = joinParts(source.detailKeys ?? []);

    return [
      {
        label: joinParts(source.labelKeys) || String(optionValue),
        value: String(optionValue),
        ...(detail ? { detail } : {}),
      },
    ];
  });
}

export function recordIdentity(record: Record<string, unknown>): string | undefined {
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

export function createRowKey(
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

export function resolvePath(
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

export function getPathValue(
  record: Record<string, unknown>,
  path: string,
): unknown {
  return path.split(".").reduce<unknown>((current, key) => {
    return isRecord(current) ? current[key] : undefined;
  }, record);
}

export function displayValue(value: unknown): string {
  if (value === null || value === undefined || value === "") return "—";
  if (typeof value === "string") {
    return parseDate(value) && value.length >= 19 ? formatDateTime(value) : value;
  }
  if (typeof value === "number" || typeof value === "boolean") {
    return String(value);
  }
  if (Array.isArray(value)) return value.map(displayValue).join(", ");
  return "—";
}

export type WorkspaceErrorKey =
  | "apiError400"
  | "apiError401"
  | "apiError403"
  | "apiError404"
  | "apiError409"
  | "apiError5xx"
  | "apiErrorNetwork"
  | "actionFailed";

export function errorMessage(
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

export function pascalCase(value: string): string {
  return value.length === 0 ? value : `${value[0]?.toUpperCase()}${value.slice(1)}`;
}

export function camelCase(value: string): string {
  return value.length === 0 ? value : `${value[0]?.toLowerCase()}${value.slice(1)}`;
}

/** totalCount of a paged response ({ totalCount, page, pageSize, items }). */
export function extractTotal(payload: unknown): number | undefined {
  if (!isRecord(payload)) return undefined;
  const total = payload.totalCount ?? payload.TotalCount ?? payload.total;
  return typeof total === "number" ? total : undefined;
}

/** The field that names a record in a heading (a person, a category…). */
export function recordTitle(record: Record<string, unknown>): string | undefined {
  for (const key of [
    "studentName",
    "fullName",
    "accountName",
    "categoryName",
    "purposeName",
    "attributeName",
    "serviceName",
    "subjectName",
    "facultyName",
    "programmeName",
    "termName",
    "title",
    "name",
  ]) {
    const value = readRecordValue(record, key);
    if (typeof value === "string" && value.trim()) return value;
  }
  return undefined;
}

/** Keys that hold identifiers (shown short, in mono, with a copy button). */
export function isIdKey(key: string): boolean {
  return /(^id$|Id$)/.test(key);
}

/** Keys that hold long free text (shown full width in detail views). */
export function isLongTextKey(key: string): boolean {
  return /(reason|text|description|notes|message|justification|response|outcome|summary)$/i.test(key);
}
