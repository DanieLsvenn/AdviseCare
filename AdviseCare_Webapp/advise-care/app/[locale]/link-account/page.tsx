import { getTranslations, setRequestLocale } from "next-intl/server";
import { AuthFrame } from "@/components/auth/auth-frame";
import { GoogleSignIn } from "@/components/auth/google-sign-in";
import { Notice } from "@/components/ui/notice";

/**
 * Opened from the account-linking email: /link-account?token=…
 * The student or staff member signs in with their personal Google account
 * once; the API checks the token (7 days) and that the emails match.
 */
export default async function LinkAccountPage({
  params,
  searchParams,
}: PageProps<"/[locale]/link-account">) {
  const { locale } = await params;
  const { token } = await searchParams;
  setRequestLocale(locale);
  const t = await getTranslations("auth");
  const value = typeof token === "string" ? token : "";

  return (
    <AuthFrame title={t("linkTitle")} description={t("linkSubtitle")}>
      {value ? (
        <>
          <ol className="mb-space-lg flex flex-col gap-space-sm text-sm leading-6 text-text-muted">
            <li>1. {t("linkStep1")}</li>
            <li>2. {t("linkStep2")}</li>
          </ol>
          <GoogleSignIn linkToken={value} />
        </>
      ) : (
        <Notice tone="attention" title={t("linkMissingToken")} />
      )}
    </AuthFrame>
  );
}
