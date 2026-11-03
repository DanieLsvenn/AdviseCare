"use client";

import { useEffect, useRef, type ReactNode } from "react";
import { cn } from "@/lib/cn";
import { Icon } from "./icon";

/**
 * Right-hand sheet for creating a record or working on a selected one, so the
 * list stays visible behind it. Built on <dialog>: focus trap, Esc to close and
 * the top layer come from the platform.
 */
export function Drawer({
  open,
  onClose,
  title,
  description,
  closeLabel,
  footer,
  width = "md",
  children,
}: {
  open: boolean;
  onClose: () => void;
  title: ReactNode;
  description?: ReactNode;
  closeLabel: string;
  footer?: ReactNode;
  width?: "md" | "lg";
  children: ReactNode;
}) {
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={ref}
      onCancel={(event) => {
        event.preventDefault();
        onClose();
      }}
      onClick={(event) => {
        if (event.target === ref.current) onClose();
      }}
      className={cn(
        "fixed inset-y-0 right-0 left-auto m-0 h-dvh max-h-dvh w-full max-w-full bg-transparent p-0 open:animate-sheet-in",
        width === "md" ? "sm:max-w-[34rem]" : "sm:max-w-[46rem]",
      )}
    >
      {open ? (
        <div className="flex h-full flex-col border-l border-rule bg-paper shadow-sheet">
          <header className="flex items-start gap-space-md border-b border-rule px-space-lg py-space-base">
            <div className="min-w-0 flex-1">
              <h2 className="text-lg font-semibold leading-6 text-text">{title}</h2>
              {description ? (
                <div className="mt-0.5 text-sm text-text-muted">{description}</div>
              ) : null}
            </div>
            <button
              type="button"
              onClick={onClose}
              aria-label={closeLabel}
              className="-mr-1.5 flex size-8 shrink-0 items-center justify-center rounded-md text-text-muted transition-colors hover:bg-ink-wash hover:text-text"
            >
              <Icon name="close" className="text-[20px]" />
            </button>
          </header>
          <div className="min-h-0 flex-1 overflow-y-auto px-space-lg py-space-lg">
            {children}
          </div>
          {footer ? (
            <footer className="flex flex-wrap items-center justify-end gap-space-sm border-t border-rule px-space-lg py-space-md">
              {footer}
            </footer>
          ) : null}
        </div>
      ) : null}
    </dialog>
  );
}
