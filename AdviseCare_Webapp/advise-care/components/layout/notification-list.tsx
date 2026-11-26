"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import { usePathname, useRouter } from "@/i18n/navigation";
import { clientApiGet, clientApiRequest } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { formatDateTime } from "@/lib/format";
import { listOf, str } from "@/components/case/case-api";
import { notificationHref, workspaceFor } from "@/components/layout/notification-links";
import { apiMessage } from "@/components/take-on/take-on-api";
import { Button } from "@/components/ui/button";
import { useConfirm } from "@/components/ui/confirm-dialog";
import { Icon } from "@/components/ui/icon";
import { Notice } from "@/components/ui/notice";
import { Pager, usePaged } from "@/components/ui/pager";
import { EmptyState, ErrorState, SkeletonRows } from "@/components/ui/states";

interface NotificationItem {
  notificationId: string;
  title: string;
  message: string;
  redirectUrl: string;
  createdAt: string;
  isRead: boolean;
}

/**
 * Every notification of the signed-in account, newest first. Clicking one
 * marks it read and opens its item in the account's workspace (QC-290); each
 * row can also be marked read or removed on its own.
 */
export function NotificationList({ roles }: { roles: string[] }) {
  const t = useTranslations("ui.notificationList");
  const tu = useTranslations("ui");
  const pathname = usePathname();
  const router = useRouter();
  const { confirm, element: confirmDialog } = useConfirm();
  const [items, setItems] = useState<NotificationItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [actionError, setActionError] = useState("");
  const [onlyUnread, setOnlyUnread] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const payload = await clientApiGet<unknown>("Notifications/me");
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
    } catch (cause) {
      setItems([]);
      setError(apiMessage(cause, t("loadError")));
    } finally {
      setLoading(false);
    }
  }, [t]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  const shown = useMemo(() => (onlyUnread ? items.filter((item) => !item.isRead) : items), [items, onlyUnread]);
  const paged = usePaged(shown, 20);
  const unreadCount = items.filter((item) => !item.isRead).length;
  const readCount = items.length - unreadCount;
  const workspace = workspaceFor(pathname, roles);

  async function act(run: () => Promise<unknown>) {
    setActionError("");
    try {
      await run();
      await load();
    } catch (cause) {
      setActionError(apiMessage(cause, t("actionError")));
    }
  }

  async function open(item: NotificationItem) {
    if (!item.isRead) {
      await clientApiRequest(`Notifications/me/${encodeURIComponent(item.notificationId)}/read`, { method: "PUT" }).catch(() => undefined);
    }
    const href = notificationHref(item.redirectUrl, workspace);
    if (href === "/notifications") void load();
    else router.push(href);
  }

  async function remove(item: NotificationItem) {
    const ok = await confirm({
      title: t("removeTitle"),
      body: item.title,
      confirmLabel: t("remove"),
      cancelLabel: tu("cancel"),
      destructive: true,
      icon: "delete",
    });
    if (ok) await act(() => clientApiRequest(`Notifications/me/${encodeURIComponent(item.notificationId)}`, { method: "DELETE" }));
  }

  async function clearRead() {
    const ok = await confirm({
      title: t("clearTitle", { count: readCount }),
      body: t("clearBody"),
      confirmLabel: t("clearRead"),
      cancelLabel: tu("cancel"),
      destructive: true,
      icon: "delete_sweep",
    });
    if (ok) await act(() => clientApiRequest("Notifications/me/clear", { method: "DELETE" }));
  }

  return (
    <section className="rounded-lg border border-rule bg-paper" aria-labelledby="notification-list-title">
      <header className="flex flex-wrap items-start justify-between gap-space-md border-b border-rule px-space-lg py-space-md">
        <div className="min-w-0">
          <h1 id="notification-list-title" className="text-lg font-semibold text-text">
            {t("title")}
          </h1>
          <p className="mt-0.5 text-sm text-text-muted">{t("description", { count: unreadCount })}</p>
        </div>
        <div className="flex flex-wrap items-center gap-space-sm">
          <Button
            size="sm"
            variant="secondary"
            icon="done_all"
            disabled={!unreadCount}
            onClick={() => void act(() => clientApiRequest("Notifications/me/read-all", { method: "PUT" }))}
          >
            {t("markAllRead")}
          </Button>
          <Button size="sm" variant="secondary" icon="delete_sweep" disabled={!readCount} onClick={() => void clearRead()}>
            {t("clearRead")}
          </Button>
        </div>
      </header>

      <div className="flex items-center gap-space-md border-b border-rule px-space-lg py-space-sm">
        <label className="flex items-center gap-2 text-sm text-text">
          <input
            type="checkbox"
            className="size-4 accent-[#2142b8]"
            checked={onlyUnread}
            onChange={(event) => setOnlyUnread(event.target.checked)}
          />
          {t("onlyUnread")}
        </label>
      </div>

      {actionError ? (
        <div className="px-space-lg pt-space-md">
          <Notice tone="urgent" icon="error" title={actionError} />
        </div>
      ) : null}

      {loading ? (
        <SkeletonRows rows={5} columns={3} label={t("loading")} />
      ) : error ? (
        <ErrorState
          title={error}
          action={
            <Button size="sm" variant="secondary" onClick={() => void load()}>
              {tu("retry")}
            </Button>
          }
        />
      ) : shown.length === 0 ? (
        <EmptyState icon="notifications" title={onlyUnread ? t("noUnreadTitle") : t("emptyTitle")}>
          {t("emptyBody")}
        </EmptyState>
      ) : (
        <>
          <ul className="divide-y divide-rule">
            {paged.items.map((item) => (
              <li
                key={item.notificationId}
                className={cn("flex items-start gap-space-sm px-space-lg py-space-sm", item.isRead ? "" : "bg-ink-wash/40")}
              >
                <button
                  type="button"
                  onClick={() => void open(item)}
                  className="group flex min-w-0 flex-1 gap-space-sm rounded-md text-left"
                >
                  <span aria-hidden className={cn("mt-1.5 size-2 shrink-0 rounded-full", item.isRead ? "bg-transparent" : "bg-ink")} />
                  <span className="min-w-0">
                    <span className="block text-sm font-semibold text-text group-hover:underline">
                      {item.title}
                      {!item.isRead ? <span className="sr-only"> ({t("unread")})</span> : null}
                    </span>
                    <span className="block text-[0.8125rem] text-text-muted">{item.message}</span>
                    <span className="block font-mono text-[0.6875rem] text-text-faint">{formatDateTime(item.createdAt)}</span>
                  </span>
                </button>
                <div className="flex shrink-0 items-center gap-1">
                  {!item.isRead ? (
                    <button
                      type="button"
                      title={t("markRead")}
                      aria-label={t("markReadItem", { title: item.title })}
                      onClick={() =>
                        void act(() =>
                          clientApiRequest(`Notifications/me/${encodeURIComponent(item.notificationId)}/read`, { method: "PUT" }),
                        )
                      }
                      className="inline-flex size-8 items-center justify-center rounded-md text-text-faint hover:bg-ink-wash hover:text-text"
                    >
                      <Icon name="done" className="text-[18px]" />
                    </button>
                  ) : null}
                  <button
                    type="button"
                    title={t("remove")}
                    aria-label={t("removeItem", { title: item.title })}
                    onClick={() => void remove(item)}
                    className="inline-flex size-8 items-center justify-center rounded-md text-text-faint hover:bg-status-urgent-surface hover:text-red-ink"
                  >
                    <Icon name="delete" className="text-[18px]" />
                  </button>
                </div>
              </li>
            ))}
          </ul>
          <Pager {...paged} className="border-t border-rule px-space-lg py-space-sm" />
        </>
      )}
      {confirmDialog}
    </section>
  );
}
