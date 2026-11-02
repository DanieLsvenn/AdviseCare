"use client";

import { useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { ApiError } from "@/lib/api-contract";
import { clientApiPost } from "@/lib/client-api";

export function ForgotPasswordForm() {
  const t = useTranslations("auth");
  const [email, setEmail] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setError("");
    try {
      await clientApiPost("Auth/forgot-password", { email: email.trim() }, {
        redirectOnUnauthorized: false,
      });
      setSubmitted(true);
    } catch (cause) {
      setError(
        cause instanceof ApiError && cause.status >= 500
          ? t("networkError")
          : t("resetFailed"),
      );
    } finally {
      setPending(false);
    }
  }

  if (submitted) {
    return (
      <p
        aria-live="polite"
        className="mt-space-lg rounded border border-status-ontrack-border bg-status-ontrack-surface px-space-md py-space-sm font-body-sm text-body-sm text-status-ontrack-text"
        role="status"
      >
        {t("resetSubmitted")}
      </p>
    );
  }

  return (
    <form className="mt-space-xl space-y-space-md" onSubmit={submit}>
      <label className="block">
        <span className="mb-space-xs block font-label-md text-label-md font-medium text-on-surface">
          {t("identifier")}
        </span>
        <input
          autoComplete="email"
          className="w-full rounded border border-outline-variant bg-surface-container-lowest px-space-md py-2.5 font-body-base text-body-base text-on-surface focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
          maxLength={255}
          name="email"
          onChange={(event) => setEmail(event.target.value)}
          required
          type="email"
          value={email}
        />
      </label>
      {error ? (
        <p aria-live="polite" className="text-status-urgent-text" role="alert">
          {error}
        </p>
      ) : null}
      <button
        className="flex h-10 w-full items-center justify-center rounded bg-primary-container px-space-md font-label-md text-label-md font-semibold text-on-primary disabled:opacity-60"
        disabled={pending}
        type="submit"
      >
        {pending ? t("signingIn") : t("sendResetLink")}
      </button>
    </form>
  );
}
