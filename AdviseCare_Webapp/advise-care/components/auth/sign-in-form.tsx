"use client";

import { useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { clientApiPost } from "@/lib/client-api";
import { ApiError } from "@/lib/api-contract";
import { landingPathForRoles } from "@/lib/roles";

interface LoginResponse {
  roles: string[];
}

export function SignInForm({ locale }: { locale: string }) {
  const t = useTranslations("auth");
  const [identifier, setIdentifier] = useState("");
  const [password, setPassword] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setPending(true);
    try {
      const result = await clientApiPost<LoginResponse>(
        "Auth/login",
        {
          identifier: identifier.trim(),
          password,
          deviceInfo:
            typeof navigator === "undefined" ? "" : navigator.userAgent,
        },
        { redirectOnUnauthorized: false },
      );
      window.location.assign(
        `/${locale}${landingPathForRoles(result.roles)}`,
      );
    } catch (cause) {
      setError(messageForError(cause, t));
    } finally {
      setPending(false);
    }
  }

  return (
    <form className="mt-space-xl space-y-space-md" onSubmit={submit}>
      <label className="block">
        <span className="mb-space-xs block font-label-md text-label-md font-medium text-on-surface">
          {t("identifier")}
        </span>
        <input
          autoComplete="username"
          className="w-full rounded border border-outline-variant bg-surface-container-lowest px-space-md py-2.5 font-body-base text-body-base text-on-surface focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
          maxLength={255}
          name="identifier"
          onChange={(event) => setIdentifier(event.target.value)}
          required
          value={identifier}
        />
      </label>

      <label className="block">
        <span className="mb-space-xs block font-label-md text-label-md font-medium text-on-surface">
          {t("password")}
        </span>
        <input
          autoComplete="current-password"
          className="w-full rounded border border-outline-variant bg-surface-container-lowest px-space-md py-2.5 font-body-base text-body-base text-on-surface focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
          minLength={6}
          name="password"
          onChange={(event) => setPassword(event.target.value)}
          required
          type="password"
          value={password}
        />
      </label>

      {error ? (
        <p
          aria-live="polite"
          className="rounded border border-status-urgent-border bg-status-urgent-surface px-space-md py-space-sm font-body-sm text-body-sm text-status-urgent-text"
          role="alert"
        >
          {error}
        </p>
      ) : null}

      <button
        className="flex h-10 w-full items-center justify-center rounded border border-transparent bg-primary-container px-space-md font-label-md text-label-md font-semibold text-on-primary hover:bg-primary disabled:cursor-not-allowed disabled:opacity-60"
        disabled={pending}
        type="submit"
      >
        {pending ? t("signingIn") : t("signInButton")}
      </button>
    </form>
  );
}

function messageForError(
  error: unknown,
  translate: (
    key:
      | "networkError"
      | "validationError"
      | "invalidCredentials"
      | "accountUnavailable"
      | "notFound"
      | "conflictError"
      | "requestError",
  ) => string,
): string {
  if (!(error instanceof ApiError)) return translate("networkError");

  switch (error.status) {
    case 400:
      return `${translate("validationError")} ${error.message}`;
    case 401:
      return translate("invalidCredentials");
    case 403:
      return translate("accountUnavailable");
    case 404:
      return translate("notFound");
    case 409:
      return `${translate("conflictError")} ${error.message}`;
    case 502:
    case 503:
    case 504:
      return translate("networkError");
    default:
      return `${translate("requestError")} ${error.message}`;
  }
}
