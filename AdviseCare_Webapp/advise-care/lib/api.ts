import { headers } from "next/headers";

/**
 * Lớp truy cập dữ liệu duy nhất của webapp.
 * Hôm nay nó gọi các Route Handler nội bộ (`/api/*`) đang trả mock; khi
 * AdviseCare_API (.NET) sẵn sàng, chỉ cần đặt NEXT_PUBLIC_API_BASE_URL — mọi
 * trang giữ nguyên vì payload đã dùng đúng tên trường của schema.
 */
async function resolveBaseUrl(): Promise<string> {
  const configured = process.env.NEXT_PUBLIC_API_BASE_URL;
  if (configured) return configured.replace(/\/$/, "");

  const requestHeaders = await headers();
  const host =
    requestHeaders.get("x-forwarded-host") ??
    requestHeaders.get("host") ??
    "localhost:3000";
  const protocol =
    requestHeaders.get("x-forwarded-proto") ??
    (host.startsWith("localhost") ? "http" : "https");
  return `${protocol}://${host}`;
}

export async function apiGet<T>(path: string): Promise<T> {
  const response = await fetch(`${await resolveBaseUrl()}${path}`, {
    cache: "no-store",
    headers: { accept: "application/json" },
  });

  if (!response.ok) {
    throw new Error(`AdviseCare API ${path} returned ${response.status}`);
  }

  return (await response.json()) as T;
}
