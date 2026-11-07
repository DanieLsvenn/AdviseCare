/**
 * Dates are shown the way the contact book writes them — day first — in the
 * university's time zone, whichever UI language is active.
 */
const TIME_ZONE = "Asia/Ho_Chi_Minh";

export function parseDate(value: unknown): Date | null {
  if (value instanceof Date) return Number.isNaN(value.getTime()) ? null : value;
  if (typeof value !== "string" || value.length < 10) return null;
  if (!/^\d{4}-\d{2}-\d{2}/.test(value)) return null;
  // API timestamps are UTC; a bare "2026-11-03T08:00:00" without an offset is UTC too.
  const normalized =
    value.length > 10 && !/[zZ]|[+-]\d{2}:?\d{2}$/.test(value) ? `${value}Z` : value;
  const date = new Date(normalized);
  return Number.isNaN(date.getTime()) ? null : date;
}

function parts(date: Date) {
  const formatted = new Intl.DateTimeFormat("en-GB", {
    timeZone: TIME_ZONE,
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).formatToParts(date);
  const get = (type: Intl.DateTimeFormatPartTypes) =>
    formatted.find((part) => part.type === type)?.value ?? "";
  return {
    day: get("day"),
    month: get("month"),
    year: get("year"),
    hour: get("hour"),
    minute: get("minute"),
  };
}

/** 03/11/2026 */
export function formatDate(value: unknown): string {
  const date = parseDate(value);
  if (!date) return typeof value === "string" ? value : "";
  const { day, month, year } = parts(date);
  return `${day}/${month}/${year}`;
}

/** 14:20 */
export function formatTime(value: unknown): string {
  const date = parseDate(value);
  if (!date) return "";
  const { hour, minute } = parts(date);
  return `${hour}:${minute}`;
}

/** 03/11/2026 14:20 — or just the date for date-only values. */
export function formatDateTime(value: unknown): string {
  if (typeof value === "string" && /^\d{4}-\d{2}-\d{2}$/.test(value)) {
    return formatDate(value);
  }
  const date = parseDate(value);
  if (!date) return typeof value === "string" ? value : "";
  return `${formatDate(date)} ${formatTime(date)}`;
}

/** Whole days between a timestamp and now (0 = today). */
export function daysSince(value: unknown, now = Date.now()): number | null {
  const date = parseDate(value);
  if (!date) return null;
  return Math.max(0, Math.floor((now - date.getTime()) / 86_400_000));
}

/** Short, copyable form of a GUID for display: first 8 characters. */
export function shortId(value: string): string {
  return value.length > 12 ? value.slice(0, 8) : value;
}
