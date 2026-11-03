"use client";

import { useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { ApiError } from "@/lib/api-contract";
import { clientApiPost } from "@/lib/client-api";
import { Button } from "@/components/ui/button";
import { controlClass } from "@/components/ui/field";
import { Notice } from "@/components/ui/notice";

export function ResetPasswordForm({ locale, token }: { locale: string; token: string }) {
  const t = useTranslations("auth");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const mismatch = confirm.length > 0 && confirm !== password;

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (password.length < 6) {
      setError(t("passwordRule"));
      document.getElementById("new-password")?.focus();
      return;
    }
    if (mismatch || !confirm) {
      setError(t("passwordMismatch"));
      document.getElementById("confirm-password")?.focus();
      return;
    }
    setPending(true);
    setError("");
    try {
      await clientApiPost(
        "Auth/reset-password",
        { token, newPassword: password, confirmPassword: confirm },
        { redirectOnUnauthorized: false },
      );
      // Every session is signed out by the reset; go back to sign in.
      window.location.assign(`/${locale}/sign-in?reset=1`);
    } catch (cause) {
      setPending(false);
      setError(
        cause instanceof ApiError && cause.status < 500
          ? cause.message || t("resetLinkInvalid")
          : t("networkError"),
      );
    }
  }

  return (
    <form className="flex flex-col gap-space-base" noValidate onSubmit={submit}>
      <div>
        <label htmlFor="new-password" className="mb-1.5 block text-sm font-semibold text-text">
          {t("newPassword")}
        </label>
        <input
          autoComplete="new-password"
          className={`${controlClass} h-11 w-full text-[0.9375rem]`}
          id="new-password"
          minLength={6}
          onChange={(event) => setPassword(event.target.value)}
          required
          type="password"
          value={password}
        />
        <p className="mt-1 text-[0.8125rem] text-text-faint">{t("passwordRule")}</p>
      </div>
      <div>
        <label htmlFor="confirm-password" className="mb-1.5 block text-sm font-semibold text-text">
          {t("confirmPassword")}
        </label>
        <input
          aria-invalid={mismatch || undefined}
          aria-describedby={mismatch ? "confirm-error" : undefined}
          autoComplete="new-password"
          className={`${controlClass} h-11 w-full text-[0.9375rem]`}
          id="confirm-password"
          minLength={6}
          onChange={(event) => setConfirm(event.target.value)}
          required
          type="password"
          value={confirm}
        />
        {mismatch ? (
          <p id="confirm-error" className="mt-1 text-[0.8125rem] text-red-ink">
            {t("passwordMismatch")}
          </p>
        ) : null}
      </div>
      {error ? <Notice tone="urgent" icon="error" title={error} /> : null}
      <Button className="w-full" disabled={mismatch} loading={pending} size="lg" type="submit">
        {t("savePassword")}
      </Button>
    </form>
  );
}
