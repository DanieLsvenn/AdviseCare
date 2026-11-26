"use client";

import { useCallback, useId, useRef, useState, type ReactNode } from "react";
import { cn } from "@/lib/cn";
import { Button } from "./button";
import { controlClass } from "./field";
import { Icon } from "./icon";

interface PromptRequest {
  title: string;
  body?: ReactNode;
  label: string;
  placeholder?: string;
  hint?: string;
  /** The text must not be empty (e.g. a decline or dismiss reason). */
  required?: boolean;
  maxLength?: number;
  /** Text already in the box (e.g. the note being changed). */
  initialValue?: string;
  confirmLabel: string;
  cancelLabel: string;
  destructive?: boolean;
  icon?: string;
}

/**
 * Like useConfirm, but asks for a short text with the decision (a reason or a
 * note). Resolves with the trimmed text, or null when cancelled.
 */
export function usePrompt() {
  const ref = useRef<HTMLDialogElement>(null);
  const resolver = useRef<((value: string | null) => void) | null>(null);
  const [request, setRequest] = useState<PromptRequest | null>(null);
  const [text, setText] = useState("");
  const fieldId = useId();

  const prompt = useCallback((next: PromptRequest) => {
    setRequest(next);
    setText(next.initialValue ?? "");
    requestAnimationFrame(() => ref.current?.showModal());
    return new Promise<string | null>((resolve) => {
      resolver.current = resolve;
    });
  }, []);

  function settle(value: string | null) {
    ref.current?.close();
    resolver.current?.(value);
    resolver.current = null;
  }

  const trimmed = text.trim();
  const element = (
    <dialog
      ref={ref}
      onCancel={(event) => {
        event.preventDefault();
        settle(null);
      }}
      className="m-auto w-[calc(100%-2rem)] max-w-md rounded-xl border border-rule bg-paper p-0 shadow-popover open:animate-fade-in"
      aria-labelledby={`${fieldId}-title`}
    >
      {request ? (
        <form
          className="p-space-lg"
          onSubmit={(event) => {
            event.preventDefault();
            if (request.required && !trimmed) return;
            settle(trimmed);
          }}
        >
          <div className="flex items-start gap-space-md">
            <span
              aria-hidden
              className={
                request.destructive
                  ? "flex size-9 shrink-0 items-center justify-center rounded-full bg-status-urgent-surface text-red-ink"
                  : "flex size-9 shrink-0 items-center justify-center rounded-full bg-ink-wash text-ink"
              }
            >
              <Icon name={request.icon ?? (request.destructive ? "warning" : "edit_note")} className="text-[20px]" />
            </span>
            <div className="min-w-0 flex-1">
              <h2 id={`${fieldId}-title`} className="text-base font-semibold text-text">
                {request.title}
              </h2>
              {request.body ? <div className="mt-1 text-sm leading-6 text-text-muted">{request.body}</div> : null}
              <label htmlFor={fieldId} className="mt-space-md mb-1 block text-[0.8125rem] font-semibold text-text">
                {request.label}
                {request.required ? <span aria-hidden className="ml-0.5 text-red-ink">*</span> : null}
              </label>
              <textarea
                id={fieldId}
                autoFocus
                required={request.required}
                maxLength={request.maxLength ?? 1000}
                placeholder={request.placeholder}
                className={cn(controlClass, "min-h-20 w-full py-2 text-sm text-text")}
                value={text}
                onChange={(event) => setText(event.target.value)}
              />
              {request.hint ? <p className="mt-1 text-[0.75rem] text-text-faint">{request.hint}</p> : null}
            </div>
          </div>
          <div className="mt-space-lg flex justify-end gap-space-sm">
            <Button type="button" variant="secondary" onClick={() => settle(null)}>
              {request.cancelLabel}
            </Button>
            <Button type="submit" variant={request.destructive ? "destructive" : "primary"} disabled={request.required && !trimmed}>
              {request.confirmLabel}
            </Button>
          </div>
        </form>
      ) : null}
    </dialog>
  );

  return { prompt, element };
}
