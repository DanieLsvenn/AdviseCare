"use client";

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

export async function clientApiRequest<T>(
  path: string,
  options: ApiRequestOptions = {},
): Promise<T> {
  const normalizedPath = normalizeApiPath(path);
  const url = appendQuery(`/api/${normalizedPath}`, options.query);
  const headers = new Headers({ accept: "application/json" });
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
      credentials: "same-origin",
      signal: controller.signal,
    });

    if (!response.ok) {
      const error = await apiErrorFromResponse(response);
      if (
        error.status === 401 &&
        options.redirectOnUnauthorized !== false &&
        typeof window !== "undefined"
      ) {
        const locale = document.documentElement.lang || "en";
        window.location.assign(`/${locale}/sign-in?reason=expired`);
      }
      throw error;
    }

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

export function clientApiGet<T>(
  path: string,
  options: Omit<ApiRequestOptions, "method" | "body"> = {},
): Promise<T> {
  return clientApiRequest<T>(path, { ...options, method: "GET" });
}

export function clientApiPost<T>(
  path: string,
  body?: ApiRequestOptions["body"],
  options: Omit<ApiRequestOptions, "method" | "body"> = {},
): Promise<T> {
  return clientApiRequest<T>(path, { ...options, method: "POST", body });
}

export function clientApiPut<T>(
  path: string,
  body?: ApiRequestOptions["body"],
  options: Omit<ApiRequestOptions, "method" | "body"> = {},
): Promise<T> {
  return clientApiRequest<T>(path, { ...options, method: "PUT", body });
}

export function clientApiDelete<T>(
  path: string,
  options: Omit<ApiRequestOptions, "method" | "body"> = {},
): Promise<T> {
  return clientApiRequest<T>(path, { ...options, method: "DELETE" });
}
