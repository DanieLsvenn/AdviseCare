import { NextRequest, NextResponse } from "next/server";
import {
  isRecord,
  normalizeApiPath,
} from "@/lib/api-contract";
import {
  SESSION_COOKIE_NAME,
  SESSION_LIFETIME_SECONDS,
} from "@/lib/session";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

type RouteContext = {
  params: Promise<{ path: string[] }>;
};

const publicEndpoints = new Set([
  "Auth/login",
  "Auth/google",
  "Auth/link-google",
  "Auth/forgot-password",
  "Auth/reset-password",
]);
const sessionCreatingEndpoints = new Set([
  "Auth/login",
  "Auth/google",
  "Auth/link-google",
]);

async function handle(
  request: NextRequest,
  context: RouteContext,
): Promise<NextResponse> {
  const { path } = await context.params;
  let normalizedPath: string;
  try {
    normalizedPath = normalizeApiPath(path.join("/"));
  } catch {
    return NextResponse.json(
      { message: "Invalid API path." },
      { status: 400, headers: { "cache-control": "no-store" } },
    );
  }

  const method = request.method.toUpperCase();
  const isLogout = normalizedPath === "Auth/logout" && method === "POST";
  const isPublic = publicEndpoints.has(normalizedPath);

  if (method !== "GET" && !isSameOriginMutation(request)) {
    return NextResponse.json(
      { message: "This request must come from the AdviseCare application." },
      { status: 403, headers: { "cache-control": "no-store" } },
    );
  }

  const sessionToken = request.cookies.get(SESSION_COOKIE_NAME)?.value;
  if (!sessionToken && !isPublic) {
    const response = NextResponse.json(
      { message: "Your session has expired. Please sign in again." },
      { status: 401, headers: { "cache-control": "no-store" } },
    );
    if (isLogout) clearSessionCookie(response);
    return response;
  }

  const apiBase = process.env.ADVISECARE_API_BASE_URL;
  if (!apiBase) {
    const response = NextResponse.json(
      { message: "ADVISECARE_API_BASE_URL is not configured on the server." },
      { status: 503, headers: { "cache-control": "no-store" } },
    );
    if (isLogout) clearSessionCookie(response);
    return response;
  }

  let upstreamUrl: URL;
  try {
    const parsedBase = new URL(apiBase);
    if (
      !["http:", "https:"].includes(parsedBase.protocol) ||
      parsedBase.username ||
      parsedBase.password ||
      parsedBase.search ||
      parsedBase.hash
    ) {
      throw new Error("Invalid API origin.");
    }
    upstreamUrl = new URL(
      `api/${normalizedPath}${request.nextUrl.search}`,
      `${parsedBase.toString().replace(/\/+$/, "")}/`,
    );
  } catch {
    const response = NextResponse.json(
      { message: "ADVISECARE_API_BASE_URL must be a valid API origin." },
      { status: 503, headers: { "cache-control": "no-store" } },
    );
    if (isLogout) clearSessionCookie(response);
    return response;
  }

  const upstreamHeaders = new Headers();
  upstreamHeaders.set(
    "accept",
    request.headers.get("accept") ?? "application/json",
  );
  const contentType = request.headers.get("content-type");
  if (contentType) upstreamHeaders.set("content-type", contentType);
  if (sessionToken) {
    upstreamHeaders.set("authorization", `Bearer ${sessionToken}`);
  }

  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 120_000);
  const forwardCallerAbort = () => controller.abort(request.signal.reason);
  if (request.signal.aborted) {
    forwardCallerAbort();
  } else {
    request.signal.addEventListener("abort", forwardCallerAbort, {
      once: true,
    });
  }

  try {
    const init: RequestInit & { duplex?: "half" } = {
      method,
      headers: upstreamHeaders,
      cache: "no-store",
      redirect: "manual",
      signal: controller.signal,
    };
    if (method !== "GET" && method !== "HEAD") {
      init.body = request.body;
      init.duplex = "half";
    }

    const upstream = await fetch(upstreamUrl, init);
    const headers = new Headers({ "cache-control": "no-store" });
    headers.set("pragma", "no-cache");
    for (const name of ["content-type", "content-disposition"]) {
      const value = upstream.headers.get(name);
      if (value) headers.set(name, value);
    }

    if (upstream.status === 401) {
      const response = new NextResponse(upstream.body, {
        status: upstream.status,
        headers,
      });
      clearSessionCookie(response);
      return response;
    }

    if (isLogout) {
      const response = new NextResponse(upstream.body, {
        status: upstream.status,
        headers,
      });
      clearSessionCookie(response);
      return response;
    }

    if (upstream.ok && sessionCreatingEndpoints.has(normalizedPath)) {
      const payload: unknown = await upstream.json().catch(() => null);
      const token =
        isRecord(payload) && typeof payload.token === "string"
          ? payload.token
          : isRecord(payload) && typeof payload.Token === "string"
            ? payload.Token
            : undefined;

      if (!token || !isRecord(payload)) {
        const errorResponse = NextResponse.json(
          { message: "The sign-in response did not contain a session token." },
          { status: 502, headers: { "cache-control": "no-store" } },
        );
        clearSessionCookie(errorResponse);
        return errorResponse;
      }

      const safePayload = { ...payload };
      delete safePayload.token;
      delete safePayload.Token;
      const safeResponse = NextResponse.json(safePayload, {
        status: upstream.status,
        headers: { "cache-control": "no-store", pragma: "no-cache" },
      });
      safeResponse.cookies.set(SESSION_COOKIE_NAME, token, {
        httpOnly: true,
        secure: process.env.NODE_ENV === "production",
        sameSite: "lax",
        path: "/",
        maxAge: SESSION_LIFETIME_SECONDS,
      });
      return safeResponse;
    }

    return new NextResponse(upstream.body, {
      status: upstream.status,
      headers,
    });
  } catch {
    const timedOut = controller.signal.aborted;
    const response = NextResponse.json(
      {
        message: timedOut
          ? "The AdviseCare API request timed out."
          : "The AdviseCare API could not be reached.",
      },
      {
        status: timedOut ? 504 : 502,
        headers: { "cache-control": "no-store" },
      },
    );
    if (isLogout) clearSessionCookie(response);
    return response;
  } finally {
    clearTimeout(timeout);
    request.signal.removeEventListener("abort", forwardCallerAbort);
  }
}

function isSameOriginMutation(request: NextRequest): boolean {
  const origin = request.headers.get("origin");
  const referer = request.headers.get("referer");
  const fetchSite = request.headers.get("sec-fetch-site");

  if (fetchSite === "cross-site") return false;
  if (origin) return origin === request.nextUrl.origin;
  if (!referer) return false;

  try {
    return new URL(referer).origin === request.nextUrl.origin;
  } catch {
    return false;
  }
}

function clearSessionCookie(response: NextResponse): void {
  response.cookies.set(SESSION_COOKIE_NAME, "", {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: 0,
  });
}

export const GET = handle;
export const POST = handle;
export const PUT = handle;
export const PATCH = handle;
export const DELETE = handle;
