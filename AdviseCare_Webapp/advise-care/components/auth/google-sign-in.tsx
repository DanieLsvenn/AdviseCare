"use client";

import { useEffect, useRef, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ApiError } from "@/lib/api-contract";
import { clientApiPost } from "@/lib/client-api";
import { landingPathForRoles } from "@/lib/roles";

interface GoogleCredentialResponse {
  credential?: string;
}

interface GoogleIdentity {
  accounts: {
    id: {
      initialize: (options: {
        client_id: string;
        callback: (response: GoogleCredentialResponse) => void;
        ux_mode?: "popup";
      }) => void;
      renderButton: (
        element: HTMLElement,
        options: Record<string, string | number>,
      ) => void;
    };
  };
}

declare global {
  interface Window {
    google?: GoogleIdentity;
  }
}

const SCRIPT_SRC = "https://accounts.google.com/gsi/client";
const clientId = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID ?? "";

function loadScript(): Promise<void> {
  if (window.google?.accounts) return Promise.resolve();
  const existing = document.querySelector<HTMLScriptElement>(`script[src="${SCRIPT_SRC}"]`);
  return new Promise((resolve, reject) => {
    const script = existing ?? document.createElement("script");
    script.addEventListener("load", () => resolve(), { once: true });
    script.addEventListener("error", () => reject(new Error("gsi")), { once: true });
    if (!existing) {
      script.src = SCRIPT_SRC;
      script.async = true;
      document.head.appendChild(script);
    }
  });
}

/**
 * "Sign in with Google" (SRS: every account belongs to a personal Google
 * account). Sends Google's ID token to Auth/google, or to Auth/link-google
 * together with the account-linking token from the email. Renders nothing
 * when NEXT_PUBLIC_GOOGLE_CLIENT_ID is not configured.
 */
export function GoogleSignIn({ linkToken }: { linkToken?: string }) {
  const t = useTranslations("auth");
  const locale = useLocale();
  const target = useRef<HTMLDivElement>(null);
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);

  useEffect(() => {
    if (!clientId) return;
    let active = true;
    loadScript()
      .then(() => {
        if (!active || !target.current || !window.google) return;
        window.google.accounts.id.initialize({
          client_id: clientId,
          ux_mode: "popup",
          callback: async ({ credential }) => {
            if (!credential) return;
            setPending(true);
            setError("");
            try {
              const deviceInfo = navigator.userAgent;
              const result = await clientApiPost<{ roles: string[] }>(
                linkToken ? "Auth/link-google" : "Auth/google",
                linkToken
                  ? { token: linkToken, idToken: credential, deviceInfo }
                  : { idToken: credential, deviceInfo },
                { redirectOnUnauthorized: false },
              );
              window.location.assign(`/${locale}${landingPathForRoles(result.roles)}`);
            } catch (cause) {
              setPending(false);
              setError(
                cause instanceof ApiError && cause.status === 401
                  ? t(linkToken ? "linkRefused" : "googleNotLinked")
                  : cause instanceof ApiError && cause.status < 500 && cause.message
                    ? cause.message
                    : t("networkError"),
              );
            }
          },
        });
        window.google.accounts.id.renderButton(target.current, {
          type: "standard",
          theme: "outline",
          size: "large",
          text: linkToken ? "continue_with" : "signin_with",
          shape: "rectangular",
          width: 384,
          locale,
        });
      })
      .catch(() => {
        if (active) setError(t("googleUnavailable"));
      });
    return () => {
      active = false;
    };
  }, [linkToken, locale, t]);

  if (!clientId) {
    return linkToken ? (
      <p className="rounded-lg border border-status-attention-border bg-status-attention-surface/60 p-space-md text-sm text-status-attention-text">
        {t("googleNotConfigured")}
      </p>
    ) : null;
  }

  return (
    <div className="flex flex-col gap-space-sm">
      <div ref={target} aria-busy={pending || undefined} className="min-h-11" />
      {error ? (
        <p role="alert" className="text-sm text-red-ink">
          {error}
        </p>
      ) : null}
    </div>
  );
}
