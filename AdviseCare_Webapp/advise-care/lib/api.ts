import { cookies } from "next/headers";
import {
  ApiError,
  apiErrorFromResponse,
  appendQuery,
  normalizeApiPath,
  parseApiResponse,
  serializeApiBody,
  isFormBody,
  type ApiRequestOptions,
} from "./api-contract";
import { SESSION_COOKIE_NAME } from "./session";

function getApiBaseUrl(): string {
  const configured = process.env.ADVISECARE_API_BASE_URL;
  if (!configured) {
    throw new ApiError({
      message: "ADVISECARE_API_BASE_URL is not configured on the server.",
      status: 503,
      code: "api_not_configured",
    });
  }

  let parsed: URL;
  try {
    parsed = new URL(configured);
  } catch {
    throw new ApiError({
      message: "ADVISECARE_API_BASE_URL must be an absolute URL.",
      status: 503,
      code: "invalid_api_configuration",
    });
  }

  if (
    !["http:", "https:"].includes(parsed.protocol) ||
    parsed.username ||
    parsed.password ||
    parsed.search ||
    parsed.hash
  ) {
    throw new ApiError({
      message: "ADVISECARE_API_BASE_URL is not a valid API origin.",
      status: 503,
      code: "invalid_api_configuration",
    });
  }

  return parsed.toString().replace(/\/+$/, "");
}

export async function apiRequest<T>(
  path: string,
  options: ApiRequestOptions = {},
): Promise<T> {
  const normalizedPath = normalizeApiPath(path);
  const url = appendQuery(
    `${getApiBaseUrl()}/api/${normalizedPath}`,
    options.query,
  );
  const sessionToken = (await cookies()).get(SESSION_COOKIE_NAME)?.value;
  const headers = new Headers({ accept: "application/json" });
  if (sessionToken) headers.set("authorization", `Bearer ${sessionToken}`);

  const body = serializeApiBody(options.body);
  if (body !== undefined && !isFormBody(options.body)) {
    headers.set("content-type", "application/json");
  }

  const controller = new AbortController();
  const timeout = setTimeout(
    () => controller.abort(),
    options.timeoutMs ?? 30_000,
  );
  const handleCallerAbort = () => controller.abort(options.signal?.reason);
  if (options.signal?.aborted) {
    handleCallerAbort();
  } else {
    options.signal?.addEventListener("abort", handleCallerAbort, { once: true });
  }

  try {
    const response = await fetch(url, {
      method: options.method ?? "GET",
      headers,
      body,
      cache: "no-store",
      redirect: "manual",
      signal: controller.signal,
    });

    if (!response.ok) throw await apiErrorFromResponse(response);
    return await parseApiResponse<T>(response);
  } catch (error) {
    if (error instanceof ApiError) throw error;
    if (controller.signal.aborted) {
      throw new ApiError({
        message: "The API request timed out or was cancelled.",
        status: 504,
        code: "request_timeout",
      });
    }
    throw new ApiError({
      message: "The AdviseCare API could not be reached.",
      status: 502,
      code: "api_unavailable",
      details: error instanceof Error ? error.message : undefined,
    });
  } finally {
    clearTimeout(timeout);
    options.signal?.removeEventListener("abort", handleCallerAbort);
  }
}

export function apiGet<T>(
  path: string,
  options: Omit<ApiRequestOptions, "method" | "body"> = {},
): Promise<T> {
  return apiRequest<T>(path, { ...options, method: "GET" });
}
