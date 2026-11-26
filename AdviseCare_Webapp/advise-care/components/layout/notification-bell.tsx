"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { Link, usePathname, useRouter } from "@/i18n/navigation";
import { clientApiGet, clientApiRequest } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { formatDateTime } from "@/lib/format";
import { listOf, str } from "@/components/case/case-api";
import { Icon } from "@/components/ui/icon";
import { notificationHref, workspaceFor } from "@/components/layout/notification-links";

interface NotificationItem {
  notificationId: string;
  title: string;
  message: string;
  redirectUrl: string;
  createdAt: string;
  isRead: boolean;
}

const REFRESH_MS = 60_000;

/**
 * The bell: a red unread count (refreshed every minute and on every page
 * change) and a drop-down of the latest five notifications with "Mark all
 * read" and "See all". Opening one marks it read and goes to its item.
 */
export function NotificationBell({ tone, roles }: { tone: "paper" | "cover"; roles: string[] }) {
  const t = useTranslations("ui.bell");
  const tc = useTranslations("common");
  const pathname = usePathname();
  const router = useRouter();
  const [count, setCount] = useState(0);
  const [open, setOpen] = useState(false);
  const [items, setItems] = useState<NotificationItem[]>([]);
  const [loading, setLoading] = useState(false);
  const boxRef = useRef<HTMLDivElement>(null);

  const refreshCount = useCallback(async () => {
    try {
      const value = await clientApiGet<unknown>("Notifications/me/unread-count", { redirectOnUnauthorized: false });
      setCount(typeof value === "number" ? value : Number(value) || 0);
    } catch {
      // The badge is a hint only; a failed refresh keeps the last count
    }
  }, []);

  useEffect(() => {
    const first = window.setTimeout(() => void refreshCount(), 0);
    const timer = window.setInterval(() => void refreshCount(), REFRESH_MS);
    return () => {
      window.clearTimeout(first);
      window.clearInterval(timer);
    };
  }, [refreshCount, pathname]);

  useEffect(() => {
    if (!open) return;
    const close = (event: MouseEvent) => {
      if (boxRef.current && !boxRef.current.contains(event.target as Node)) setOpen(false);
    };
    const escape = (event: KeyboardEvent) => {
      if (event.key === "Escape") setOpen(false);
    };
    document.addEventListener("mousedown", close);
    document.addEventListener("keydown", escape);
    return () => {
      document.removeEventListener("mousedown", close);
      document.removeEventListener("keydown", escape);
    };
  }, [open]);

  async function toggle() {
    const next = !open;
    setOpen(next);
    if (!next) return;
    setLoading(true);
    try {
      const payload = await clientApiGet<unknown>("Notifications/me", { query: { page: 1, pageSize: 5 } });
      setItems(
        listOf(payload).map((record) => ({
          notificationId: str(record.notificationId),
          title: str(record.title),
          message: str(record.message),
          redirectUrl: str(record.redirectUrl),
          createdAt: str(record.createdAt),
          isRead: record.isRead === true,
        })),
      );
    } catch {
      setItems([]);
    } finally {
      setLoading(false);
    }
  }

  async function openItem(item: NotificationItem) {
    setOpen(false);
    if (!item.isRead) {
      await clientApiRequest(`Notifications/me/${encodeURIComponent(item.notificationId)}/read`, { method: "PUT" }).catch(() => undefined);
      void refreshCount();
    }
    router.push(notificationHref(item.redirectUrl, workspaceFor(pathname, roles)));
  }

  async function markAllRead() {
    await clientApiRequest("Notifications/me/read-all", { method: "PUT" }).catch(() => undefined);
    setItems((current) => current.map((item) => ({ ...item, isRead: true })));
    setCount(0);
  }

  const button =
    tone === "cover"
      ? "text-cover-ink hover:bg-cover-raised hover:text-white"
      : "text-text-muted hover:bg-ink-wash hover:text-text";

  return (
    <div ref={boxRef} className="relative">
      <button
        type="button"
        aria-label={count ? t("labelUnread", { count }) : tc("notifications")}
        aria-expanded={open}
        aria-haspopup="true"
        onClick={() => void toggle()}
        className={cn("relative flex size-9 items-center justify-center rounded-md transition-colors", button)}
      >
        <Icon name="notifications" filled={open} className="text-[21px]" />
        {count ? (
          <span className="absolute right-0.5 top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-ink px-1 font-mono text-[0.625rem] font-semibold leading-none text-white ring-2 ring-paper">
            {count > 99 ? "99+" : count}
          </span>
        ) : null}
      </button>
      {open ? (
        <div className="absolute right-0 top-full z-50 mt-1 w-[min(22rem,calc(100vw-2rem))] overflow-hidden rounded-lg border border-rule bg-paper text-text shadow-popover">
          <div className="flex items-center justify-between border-b border-rule px-space-base py-space-sm">
            <p className="text-sm font-semibold">{tc("notifications")}</p>
            <button type="button" className="text-[0.8125rem] font-semibold text-ink hover:underline disabled:text-text-faint" disabled={!count} onClick={() => void markAllRead()}>
              {t("markAllRead")}
            </button>
          </div>
          {loading ? (
            <p className="px-space-base py-space-md text-sm text-text-faint">{t("loading")}</p>
          ) : items.length === 0 ? (
            <p className="px-space-base py-space-md text-sm text-text-faint">{t("empty")}</p>
          ) : (
            <ul className="max-h-96 divide-y divide-rule overflow-y-auto">
              {items.map((item) => (
                <li key={item.notificationId}>
                  <button
                    type="button"
                    onClick={() => void openItem(item)}
                    className={cn("flex w-full gap-space-sm px-space-base py-space-sm text-left hover:bg-paper-tint", item.isRead ? "" : "bg-ink-wash/40")}
                  >
                    <span aria-hidden className={cn("mt-1.5 size-2 shrink-0 rounded-full", item.isRead ? "bg-transparent" : "bg-ink")} />
                    <span className="min-w-0">
                      <span className="block truncate text-sm font-semibold">{item.title}</span>
                      <span className="block line-clamp-2 text-[0.8125rem] text-text-muted">{item.message}</span>
                      <span className="block font-mono text-[0.6875rem] text-text-faint">{formatDateTime(item.createdAt)}</span>
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
          <Link href="/notifications" onClick={() => setOpen(false)} className="block border-t border-rule px-space-base py-space-sm text-center text-[0.8125rem] font-semibold text-ink hover:bg-paper-tint">
            {t("seeAll")}
          </Link>
        </div>
      ) : null}
    </div>
  );
}

