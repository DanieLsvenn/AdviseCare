"use client";

import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";
import { useTranslations } from "next-intl";
import { clientApiGet } from "@/lib/client-api";
import { cn } from "@/lib/cn";
import { controlClass } from "@/components/ui/field";
import type { WorkspaceOption, WorkspaceOptionSource } from "./api-workspace";
import { extractOptions } from "./workspace-helpers";

const MIN_CHARS = 2;
const RESULT_LIMIT = 20;
const DEBOUNCE_MS = 300;

interface SearchOutcome {
  term: string;
  options: WorkspaceOption[];
  truncated: boolean;
  failed: boolean;
}

/**
 * Lets people find a record (a student, an account) by typing part of its name
 * instead of pasting an ID. The chosen record's ID is what the form submits.
 */
export function SearchPicker({
  id,
  source,
  value,
  onChange,
  placeholder,
  required,
  invalid,
  describedBy,
  compact,
}: {
  id: string;
  source: WorkspaceOptionSource;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  required?: boolean;
  invalid?: boolean;
  describedBy?: string;
  compact?: boolean;
}) {
  const t = useTranslations("workspace");
  const listId = useId();
  const inputRef = useRef<HTMLInputElement>(null);
  const [term, setTerm] = useState("");
  const [selected, setSelected] = useState<WorkspaceOption | null>(null);
  const [open, setOpen] = useState(false);
  const [outcome, setOutcome] = useState<SearchOutcome | null>(null);
  const trimmed = term.trim();
  const canSearch = trimmed.length >= MIN_CHARS;

  // The form cleared the value (after saving): forget the previous choice too.
  if (!value && selected) {
    setSelected(null);
    setTerm("");
    setOutcome(null);
  }

  useEffect(() => {
    if (!canSearch) return;
    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      void Promise.all(
        (source.searchKeys ?? []).map((key) =>
          clientApiGet<unknown>(source.path, {
            signal: controller.signal,
            query: { ...source.query, [key]: trimmed },
          }),
        ),
      )
        .then((payloads) => {
          const merged = new Map<string, WorkspaceOption>();
          for (const payload of payloads) {
            for (const option of extractOptions(payload, source) ?? []) {
              if (!merged.has(option.value)) merged.set(option.value, option);
            }
          }
          const all = [...merged.values()];
          setOutcome({
            term: trimmed,
            options: all.slice(0, RESULT_LIMIT),
            truncated: all.length > RESULT_LIMIT,
            failed: false,
          });
        })
        .catch(() => {
          if (controller.signal.aborted) return;
          setOutcome({ term: trimmed, options: [], truncated: false, failed: true });
        });
    }, DEBOUNCE_MS);
    return () => {
      window.clearTimeout(timer);
      controller.abort();
    };
  }, [canSearch, trimmed, source]);

  useEffect(() => {
    // Typed text is not a choice: block submitting until a result is picked.
    inputRef.current?.setCustomValidity(required && !value ? t("pickFromList") : "");
  }, [required, value, t]);

  const settled = canSearch && outcome?.term === trimmed ? outcome : null;
  const showList = open && canSearch;
  const size = compact ? "h-8 text-[0.8125rem]" : "h-10";

  function choose(option: WorkspaceOption) {
    setSelected(option);
    setOpen(false);
    onChange(option.value);
  }

  function clear() {
    setSelected(null);
    setTerm("");
    setOutcome(null);
    onChange("");
  }

  function onInputKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "Enter") {
      // Enter must not submit the surrounding form.
      event.preventDefault();
      if (settled?.options.length === 1) choose(settled.options[0]);
    } else if (event.key === "Escape") {
      setOpen(false);
    } else if (event.key === "ArrowDown") {
      event.preventDefault();
      setOpen(true);
      event.currentTarget.parentElement?.querySelector<HTMLButtonElement>("[role=option]")?.focus();
    }
  }

  function onListKeyDown(event: KeyboardEvent<HTMLUListElement>) {
    if (event.key !== "ArrowDown" && event.key !== "ArrowUp" && event.key !== "Escape") return;
    event.preventDefault();
    if (event.key === "Escape") {
      setOpen(false);
      inputRef.current?.focus();
      return;
    }
    const options = [...event.currentTarget.querySelectorAll<HTMLButtonElement>("[role=option]")];
    const index = options.indexOf(document.activeElement as HTMLButtonElement);
    const next = event.key === "ArrowDown" ? index + 1 : index - 1;
    if (next < 0) inputRef.current?.focus();
    else options[Math.min(next, options.length - 1)]?.focus();
  }

  if (value) {
    return (
      <div
        className={cn(
          "flex items-center justify-between gap-3 rounded-md border border-rule-strong bg-paper px-3",
          compact ? "min-h-8 text-[0.8125rem]" : "min-h-10 text-sm",
        )}
      >
        <span className="min-w-0 truncate text-text">{selected?.label ?? value}</span>
        <button
          type="button"
          className="shrink-0 font-semibold text-ink hover:underline focus-visible:underline focus-visible:outline-none"
          onClick={clear}
        >
          {t("changeSelection")}
        </button>
      </div>
    );
  }

  return (
    <div
      className="relative"
      onBlur={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget)) setOpen(false);
      }}
    >
      <input
        ref={inputRef}
        id={id}
        type="text"
        role="combobox"
        aria-autocomplete="list"
        aria-controls={listId}
        aria-expanded={showList}
        aria-describedby={describedBy}
        aria-invalid={invalid ? true : undefined}
        autoComplete="off"
        className={cn(controlClass, "w-full", size)}
        placeholder={placeholder ?? t("typeToSearch")}
        value={term}
        onChange={(event) => {
          setTerm(event.target.value);
          setOpen(true);
        }}
        onFocus={() => setOpen(true)}
        onKeyDown={onInputKeyDown}
      />
      {showList ? (
        <ul
          id={listId}
          role="listbox"
          onKeyDown={onListKeyDown}
          className="absolute left-0 right-0 z-20 mt-1 max-h-64 overflow-y-auto rounded-md border border-rule-strong bg-paper shadow-lg"
        >
          {settled === null ? (
            <li role="presentation" className="px-3 py-2 text-sm text-text-faint">
              {t("searching")}
            </li>
          ) : settled.failed ? (
            <li role="presentation" className="px-3 py-2 text-sm text-red-ink">
              {t("optionsUnavailable")}
            </li>
          ) : settled.options.length === 0 ? (
            <li role="presentation" className="px-3 py-2 text-sm text-text-faint">
              {t("noMatches")}
            </li>
          ) : (
            <>
              {settled.options.map((option) => (
                <li key={option.value} role="presentation">
                  <button
                    type="button"
                    role="option"
                    aria-selected={false}
                    className="block w-full px-3 py-2 text-left text-sm hover:bg-paper-tint focus-visible:bg-paper-tint focus-visible:outline-none"
                    onClick={() => choose(option)}
                  >
                    <span className="block font-semibold text-text">{option.label}</span>
                    {option.detail ? (
                      <span className="block text-[0.8125rem] text-text-muted">{option.detail}</span>
                    ) : null}
                  </button>
                </li>
              ))}
              {settled.truncated ? (
                <li role="presentation" className="border-t border-rule px-3 py-2 text-[0.8125rem] text-text-faint">
                  {t("refineSearch")}
                </li>
              ) : null}
            </>
          )}
        </ul>
      ) : null}
    </div>
  );
}
