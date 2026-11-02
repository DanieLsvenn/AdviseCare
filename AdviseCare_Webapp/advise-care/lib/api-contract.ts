export type ApiMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

export type ApiQuery = Record<
  string,
  string | number | boolean | null | undefined
>;

export type ApiBody =
  | Record<string, unknown>
  | FormData
  | Blob
  | ArrayBuffer
  | URLSearchParams
  | string
  | null;

export interface ApiRequestOptions {
  method?: ApiMethod;
  query?: ApiQuery;
  body?: ApiBody;
  signal?: AbortSignal;
  timeoutMs?: number;
  redirectOnUnauthorized?: boolean;
}

export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;
  readonly fieldErrors: Record<string, string>;
  readonly details?: string;

  constructor({
    message,
    status,
    code,
    fieldErrors = {},
    details,
  }: {
    message: string;
    status: number;
    code?: string;
    fieldErrors?: Record<string, string>;
    details?: string;
  }) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
    this.fieldErrors = fieldErrors;
    this.details = details;
  }
}

export function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

export function normalizeApiPath(path: string): string {
  const cleanPath = path.replace(/^\/+|\/+$/g, "");
  const segments = cleanPath.split("/");

  if (
    !cleanPath ||
    cleanPath.startsWith("api/") ||
    cleanPath.includes("\\") ||
    segments.some((segment) => !segment || segment === "." || segment === "..")
  ) {
    throw new ApiError({
      message: "Invalid API path.",
      status: 400,
      code: "invalid_api_path",
    });
  }

  return segments.map((segment) => encodeURIComponent(segment)).join("/");
}

export function appendQuery(path: string, query?: ApiQuery): string {
  if (!query) return path;

  const entries = Object.entries(query).filter(
    (entry): entry is [string, string | number | boolean] =>
      entry[1] !== undefined && entry[1] !== null && entry[1] !== "",
  );
  if (entries.length === 0) return path;

  const params = new URLSearchParams();
  for (const [key, value] of entries) {
    params.set(key, String(value));
  }

  return `${path}?${params.toString()}`;
}

export function serializeApiBody(body: ApiBody | undefined): BodyInit | undefined {
  if (body === undefined || body === null) return undefined;
  if (
    typeof body === "string" ||
    body instanceof FormData ||
    body instanceof Blob ||
    body instanceof ArrayBuffer ||
    body instanceof URLSearchParams
  ) {
    return body;
  }
  return JSON.stringify(body);
}

export function isFormBody(body: ApiBody | undefined): body is FormData {
  return body instanceof FormData;
}

export async function apiErrorFromResponse(
  response: Response,
): Promise<ApiError> {
  const text = await response.text();
  let payload: unknown;

  if (text) {
    try {
      payload = JSON.parse(text) as unknown;
    } catch {
      payload = undefined;
    }
  }

  const body = isRecord(payload) ? payload : undefined;
  const message =
    (typeof body?.message === "string" && body.message) ||
    (typeof body?.detail === "string" && body.detail) ||
    (typeof body?.title === "string" && body.title) ||
    (text && !body ? text.slice(0, 1000) : "") ||
    `Request failed with status ${response.status}.`;

  return new ApiError({
    message,
    status: response.status,
    code:
      (typeof body?.code === "string" && body.code) ||
      (typeof body?.errorCode === "string" && body.errorCode) ||
      undefined,
    fieldErrors: extractFieldErrors(body?.errors),
    details: typeof body?.details === "string" ? body.details : undefined,
  });
}

function extractFieldErrors(value: unknown): Record<string, string> {
  if (!isRecord(value)) return {};

  const result: Record<string, string> = {};
  for (const [field, messages] of Object.entries(value)) {
    const message = Array.isArray(messages)
      ? messages.find((item): item is string => typeof item === "string")
      : messages;
    if (typeof message === "string") result[field] = message;
  }
  return result;
}

export async function parseApiResponse<T>(response: Response): Promise<T> {
  if (response.status === 204 || response.headers.get("content-length") === "0") {
    return undefined as T;
  }

  const contentType = response.headers.get("content-type")?.toLowerCase() ?? "";
  if (contentType.includes("json")) {
    try {
      return (await response.json()) as T;
    } catch {
      throw new ApiError({
        message: "The API returned an unreadable JSON response.",
        status: 502,
        code: "invalid_api_response",
      });
    }
  }

  return (await response.blob()) as T;
}
