"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { clientApiGet, clientApiRequest } from "@/lib/client-api";
import { formatDateTime } from "@/lib/format";
import { getInitials } from "@/lib/initials";
import { apiMessage, takeOnRequests, type TakeOnRequestRecord } from "@/components/take-on/take-on-api";
import { Avatar } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import { useConfirm } from "@/components/ui/confirm-dialog";
import { Drawer } from "@/components/ui/drawer";
import { Notice } from "@/components/ui/notice";
import { usePrompt } from "@/components/ui/prompt-dialog";
import { EmptyState, ErrorState, SkeletonRows } from "@/components/ui/states";

export interface TakeOnTarget {
  kind: "case" | "flag";
  id: string;
  studentName: string;
  /** What is asked for, e.g. the category or the rule name. */
  title: string;
}

/**
 * The Programme Head's "Requests" pop-up of one unassigned case or flag: the
 * advisors' pending take-on requests, each with Approve (the case is assigned
 * to that advisor, or a case is opened from the flag) and Decline (reason
 * required; the request leaves the list).
 */
export function TakeOnRequestsDrawer({
  target,
  onClose,
  onChanged,
}: {
  target: TakeOnTarget | null;
  onClose: () => void;
  /** Called after a decision so the list behind can refresh its counts. */
  onChanged: (resolved: boolean) => void;
}) {
  const t = useTranslations("ui.takeOn");
  const tu = useTranslations("ui");
  const tw = useTranslations("workspace");
  const { confirm, element: confirmDialog } = useConfirm();
  const { prompt, element: promptDialog } = usePrompt();
  const [requests, setRequests] = useState<TakeOnRequestRecord[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [actionError, setActionError] = useState("");
  const [busy, setBusy] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!target) return;
    setLoading(true);
    setError("");
    try {
      const query = target.kind === "case" ? { caseId: target.id, status: "pending" } : { indicatorFlagId: target.id, status: "pending" };
      setRequests(takeOnRequests(await clientApiGet<unknown>("TakeOnRequests/search", { query })));
    } catch {
      setError(tw("apiErrorNetwork"));
    } finally {
      setLoading(false);
    }
  }, [target, tw]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setActionError("");
      void load();
    }, 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  async function approve(request: TakeOnRequestRecord) {
    if (!target) return;
    const ok = await confirm({
      title: t("approveTitle", { advisor: request.advisorFullName }),
      body: target.kind === "case" ? t("approveCaseBody", { student: target.studentName }) : t("approveFlagBody", { student: target.studentName }),
      confirmLabel: t("approve"),
      cancelLabel: tu("cancel"),
      icon: "how_to_reg",
    });
    if (!ok) return;
    setBusy(request.takeOnRequestId);
    setActionError("");
    try {
      await clientApiRequest(`TakeOnRequests/${encodeURIComponent(request.takeOnRequestId)}/approve`, { method: "PUT" });
      onChanged(true);
      onClose();
    } catch (cause) {
      setActionError(apiMessage(cause, tw("apiErrorNetwork")));
    } finally {
      setBusy(null);
    }
  }

  async function decline(request: TakeOnRequestRecord) {
    const reason = await prompt({
      title: t("declineTitle", { advisor: request.advisorFullName }),
      body: t("declineBody"),
      label: t("declineReason"),
      required: true,
      confirmLabel: t("decline"),
      cancelLabel: tu("cancel"),
      destructive: true,
      icon: "block",
    });
    if (reason === null) return;
    setBusy(request.takeOnRequestId);
    setActionError("");
    try {
      await clientApiRequest(`TakeOnRequests/${encodeURIComponent(request.takeOnRequestId)}/decline`, {
        method: "PUT",
        body: { declineReason: reason },
      });
      setRequests((current) => current.filter((item) => item.takeOnRequestId !== request.takeOnRequestId));
      onChanged(false);
    } catch (cause) {
      setActionError(apiMessage(cause, tw("apiErrorNetwork")));
    } finally {
      setBusy(null);
    }
  }

  return (
    <>
      <Drawer
        open={target !== null}
        onClose={onClose}
        title={t("requestsTitle", { student: target?.studentName ?? "" })}
        description={target?.title}
        closeLabel={tu("close")}
      >
        <div className="flex flex-col gap-space-md">
          {actionError ? <Notice tone="urgent" icon="error" title={actionError} /> : null}
          {error ? (
            <ErrorState title={error} action={<Button size="sm" variant="secondary" onClick={() => void load()}>{tu("retry")}</Button>} />
          ) : loading ? (
            <SkeletonRows rows={3} columns={2} label={tw("loading")} />
          ) : requests.length === 0 ? (
            <EmptyState icon="inbox" title={t("noRequestsTitle")}>
              {t("noRequestsBody")}
            </EmptyState>
          ) : (
            <ul className="divide-y divide-rule rounded-lg border border-rule">
              {requests.map((request) => (
                <li key={request.takeOnRequestId} className="flex flex-col gap-space-sm px-space-base py-space-md">
                  <div className="flex items-start gap-space-md">
                    <Avatar initials={getInitials(request.advisorFullName)} />
                    <div className="min-w-0 flex-1">
                      <p className="font-semibold text-text">{request.advisorFullName}</p>
                      <p className="truncate text-[0.8125rem] text-text-faint">
                        {request.advisorEmail} · {t("openCases", { count: request.advisorOpenCaseCount })}
                      </p>
                      <p className="mt-0.5 font-mono text-[0.75rem] text-text-faint">{formatDateTime(request.createdAt)}</p>
                    </div>
                  </div>
                  {request.note ? (
                    <p className="whitespace-pre-line rounded-md bg-paper-tint px-space-md py-space-sm text-sm text-text">{request.note}</p>
                  ) : null}
                  <div className="flex flex-wrap justify-end gap-space-sm">
                    <Button size="sm" variant="secondary" icon="block" disabled={busy !== null} onClick={() => void decline(request)}>
                      {t("decline")}
                    </Button>
                    <Button size="sm" icon="how_to_reg" loading={busy === request.takeOnRequestId} disabled={busy !== null} onClick={() => void approve(request)}>
                      {t("approve")}
                    </Button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </Drawer>
      {confirmDialog}
      {promptDialog}
    </>
  );
}

/** "Requests" button with a small red dot while advisors' requests are waiting. */
export function RequestsButton({ count, onClick, label }: { count: number; onClick: () => void; label: string }) {
  return (
    <Button size="sm" variant="secondary" icon="group_add" onClick={onClick} className="relative" aria-label={count ? `${label} (${count})` : label}>
      {label}
      {count ? (
        <span aria-hidden className="absolute -right-1 -top-1 flex size-4 items-center justify-center rounded-full bg-red-ink font-mono text-[0.625rem] font-semibold text-white ring-2 ring-paper">
          {count}
        </span>
      ) : null}
    </Button>
  );
}
