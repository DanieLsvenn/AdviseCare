"use client";

import { useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { clientApiPost } from "@/lib/client-api";
import { ApiError } from "@/lib/api-contract";
import { landingPathForRoles } from "@/lib/roles";
import { Button } from "@/components/ui/button";
import { controlClass } from "@/components/ui/field";
import { Notice } from "@/components/ui/notice";

interface LoginResponse {
  roles: string[];
}

export function SignInForm({ locale }: { locale: string }) {
  const t = useTranslations("auth");
  const [identifier, setIdentifier] = useState("");
  const [password, setPassword] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const [fieldErrors, setFieldErrors] = useState<{ identifier?: string; password?: string }>({});

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    const nextErrors = {
      identifier: identifier.trim() ? undefined : t("identifierRequired"),
      password: !password
        ? t("passwordRequired")
        : password.length < 6
          ? t("passwordRule")
          : undefined,
    };
    setFieldErrors(nextErrors);
    if (nextErrors.identifier || nextErrors.password) {
      document.getElementById(nextErrors.identifier ? "identifier" : "password")?.focus();
      return;
    }
    setPending(true);
    try {
      const result = await clientApiPost<LoginResponse>(
        "Auth/login",
        {
          identifier: identifier.trim(),
          password,
          deviceInfo: typeof navigator === "undefined" ? "" : navigator.userAgent,
        },
        { redirectOnUnauthorized: false },
      );
      window.location.assign(`/${locale}${landingPathForRoles(result.roles)}`);
    } catch (cause) {
      setError(messageForError(cause, t));
      setPending(false);
    }
  }

  return (
    <form className="flex flex-col gap-space-base" noValidate onSubmit={submit}>
      <div>
        <label htmlFor="identifier" className="mb-1.5 block text-sm font-semibold text-text">
          {t("identifier")}
        </label>
        <input
          autoComplete="username"
          className={`${controlClass} h-11 w-full text-[0.9375rem]`}
          id="identifier"
          aria-invalid={fieldErrors.identifier ? true : undefined}
          aria-describedby={fieldErrors.identifier ? "identifier-error" : undefined}
          maxLength={255}
          name="identifier"
          onChange={(event) => setIdentifier(event.target.value)}
          required
          value={identifier}
        />
        {fieldErrors.identifier ? (
          <p id="identifier-error" className="mt-1 text-[0.8125rem] text-red-ink">
            {fieldErrors.identifier}
          </p>
        ) : null}
      </div>

      <div>
        <div className="mb-1.5 flex items-baseline justify-between">
          <label htmlFor="password" className="text-sm font-semibold text-text">
            {t("password")}
          </label>
          <Link className="text-[0.8125rem] font-semibold text-ink hover:underline" href="/forgot-password">
            {t("forgotPassword")}
          </Link>
        </div>
        <input
          autoComplete="current-password"
          className={`${controlClass} h-11 w-full text-[0.9375rem]`}
          id="password"
          aria-invalid={fieldErrors.password ? true : undefined}
          aria-describedby={fieldErrors.password ? "password-error" : undefined}
          minLength={6}
          name="password"
          onChange={(event) => setPassword(event.target.value)}
          required
          type="password"
          value={password}
        />
        {fieldErrors.password ? (
          <p id="password-error" className="mt-1 text-[0.8125rem] text-red-ink">
            {fieldErrors.password}
          </p>
        ) : null}
      </div>

      {error ? <Notice tone="urgent" icon="error" title={error} /> : null}

      <Button className="mt-space-xs w-full" loading={pending} size="lg" type="submit">
        {pending ? t("signingIn") : t("signInButton")}
      </Button>
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
