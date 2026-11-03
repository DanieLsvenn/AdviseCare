"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { clientApiPost } from "@/lib/client-api";
import { ApiError } from "@/lib/api-contract";
import { cn } from "@/lib/cn";
import { Button } from "@/components/ui/button";
import { Icon } from "@/components/ui/icon";

export function SignOutButton({
  compact,
  tone = "paper",
}: {
  /** Icon-only button (sidebar account row, student top bar). */
  compact?: boolean;
  tone?: "paper" | "cover";
}) {
  const t = useTranslations("common");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");

  async function signOut() {
    setPending(true);
    setError("");
    try {
      await clientApiPost("Auth/logout", undefined, {
        redirectOnUnauthorized: false,
      });
      const locale = document.documentElement.lang || "en";
      window.location.assign(`/${locale}/sign-in?signedOut=1`);
    } catch (cause) {
      if (cause instanceof ApiError && cause.status >= 500) {
        const locale = document.documentElement.lang || "en";
        window.location.assign(`/${locale}/sign-in?logoutUnconfirmed=1`);
        return;
      }
      setError(t("logoutFailed"));
      setPending(false);
    }
  }

  return (
    <div className="relative flex flex-col items-end">
      {compact ? (
        <button
          type="button"
          aria-label={pending ? t("signingOut") : t("signOut")}
          title={t("signOut")}
          disabled={pending}
          onClick={signOut}
          className={cn(
            "flex size-9 items-center justify-center rounded-md transition-colors disabled:opacity-60",
            tone === "cover"
              ? "text-cover-ink hover:bg-cover-raised hover:text-white"
              : "text-text-muted hover:bg-ink-wash hover:text-text",
          )}
        >
          <Icon
            name={pending ? "progress_activity" : "logout"}
            className={cn("text-[20px]", pending && "animate-spin")}
          />
        </button>
      ) : (
        <Button
          disabled={pending}
          icon="logout"
          onClick={signOut}
          size="sm"
          variant="tertiary"
        >
          {pending ? t("signingOut") : t("signOut")}
        </Button>
      )}
      {error ? (
        <span
          aria-live="polite"
          className="absolute right-0 top-full z-50 mt-1 w-56 rounded-md border border-status-urgent-border bg-paper p-2 text-[0.8125rem] text-red-ink shadow-popover"
          role="alert"
        >
          {error}
        </span>
      ) : null}
    </div>
  );
}
