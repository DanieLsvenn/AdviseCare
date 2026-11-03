"use client";

import { useCallback, useRef, useState, type ReactNode } from "react";
import { Button } from "./button";
import { Icon } from "./icon";

interface ConfirmRequest {
  title: string;
  body?: ReactNode;
  confirmLabel: string;
  cancelLabel: string;
  destructive?: boolean;
}

/**
 * Replaces window.confirm for actions that change a record for someone else
 * (assigning, reassigning, reopening). Returns a promise and an element to
 * render once.
 */
export function useConfirm() {
  const ref = useRef<HTMLDialogElement>(null);
  const resolver = useRef<((value: boolean) => void) | null>(null);
  const [request, setRequest] = useState<ConfirmRequest | null>(null);

  const confirm = useCallback((next: ConfirmRequest) => {
    setRequest(next);
    requestAnimationFrame(() => ref.current?.showModal());
    return new Promise<boolean>((resolve) => {
      resolver.current = resolve;
    });
  }, []);

  function settle(value: boolean) {
    ref.current?.close();
    resolver.current?.(value);
    resolver.current = null;
  }

  const element = (
    <dialog
      ref={ref}
      onCancel={(event) => {
        event.preventDefault();
        settle(false);
      }}
      className="m-auto w-[calc(100%-2rem)] max-w-md rounded-xl border border-rule bg-paper p-0 shadow-popover open:animate-fade-in"
      aria-labelledby="confirm-title"
    >
      {request ? (
        <div className="p-space-lg">
          <div className="flex items-start gap-space-md">
            <span
              aria-hidden
              className={
                request.destructive
                  ? "flex size-9 shrink-0 items-center justify-center rounded-full bg-status-urgent-surface text-red-ink"
                  : "flex size-9 shrink-0 items-center justify-center rounded-full bg-ink-wash text-ink"
              }
            >
              <Icon name={request.destructive ? "warning" : "help"} className="text-[20px]" />
            </span>
            <div className="min-w-0">
              <h2 id="confirm-title" className="text-base font-semibold text-text">
                {request.title}
              </h2>
              {request.body ? (
                <div className="mt-1 text-sm leading-6 text-text-muted">{request.body}</div>
              ) : null}
            </div>
          </div>
          <div className="mt-space-lg flex justify-end gap-space-sm">
            <Button variant="secondary" onClick={() => settle(false)}>
              {request.cancelLabel}
            </Button>
            <Button
              autoFocus
              variant={request.destructive ? "destructive" : "primary"}
              onClick={() => settle(true)}
            >
              {request.confirmLabel}
            </Button>
          </div>
        </div>
      ) : null}
    </dialog>
  );

  return { confirm, element };
}
