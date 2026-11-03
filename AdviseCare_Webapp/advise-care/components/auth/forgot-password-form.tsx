"use client";

import { useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { ApiError } from "@/lib/api-contract";
import { clientApiPost } from "@/lib/client-api";
import { Button } from "@/components/ui/button";
import { controlClass } from "@/components/ui/field";
import { Notice } from "@/components/ui/notice";

export function ForgotPasswordForm() {
  const t = useTranslations("auth");
  const [email, setEmail] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!/^\S+@\S+\.\S+$/.test(email.trim())) {
      setError(t("emailInvalid"));
      document.getElementById("email")?.focus();
      return;
    }
    setPending(true);
    setError("");
    try {
      await clientApiPost("Auth/forgot-password", { email: email.trim() }, {
        redirectOnUnauthorized: false,
      });
      setSubmitted(true);
    } catch (cause) {
      setError(
        cause instanceof ApiError && cause.status >= 500 ? t("networkError") : t("resetFailed"),
      );
    } finally {
      setPending(false);
    }
  }

  if (submitted) {
    return <Notice tone="ontrack" icon="mark_email_read" title={t("resetSubmitted")} />;
  }

  return (
    <form className="flex flex-col gap-space-base" noValidate onSubmit={submit}>
      <div>
        <label htmlFor="email" className="mb-1.5 block text-sm font-semibold text-text">
          {t("email")}
        </label>
        <input
          autoComplete="email"
          className={`${controlClass} h-11 w-full text-[0.9375rem]`}
          id="email"
          maxLength={255}
          name="email"
          onChange={(event) => setEmail(event.target.value)}
          required
          type="email"
          value={email}
        />
      </div>
      {error ? <Notice tone="urgent" icon="error" title={error} /> : null}
      <Button className="w-full" loading={pending} size="lg" type="submit">
        {t("sendResetLink")}
      </Button>
    </form>
  );
}
