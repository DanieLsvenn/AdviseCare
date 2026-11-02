"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { clientApiPost } from "@/lib/client-api";
import { ApiError } from "@/lib/api-contract";
import { Button } from "@/components/ui/button";

export function SignOutButton() {
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
    <div className="flex flex-col items-end gap-1">
      <Button
        aria-label={t("signOut")}
        disabled={pending}
        onClick={signOut}
        size="sm"
        variant="tertiary"
      >
        {pending ? t("signingOut") : t("signOut")}
      </Button>
      {error ? (
        <span aria-live="polite" className="text-status-urgent-text" role="alert">
          {error}
        </span>
      ) : null}
    </div>
  );
}
