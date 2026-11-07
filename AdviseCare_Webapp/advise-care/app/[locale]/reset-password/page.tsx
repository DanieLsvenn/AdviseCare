import { getTranslations, setRequestLocale } from "next-intl/server";
import { AuthFrame } from "@/components/auth/auth-frame";
import { ResetPasswordForm } from "@/components/auth/reset-password-form";
import { Notice } from "@/components/ui/notice";
import { Link } from "@/i18n/navigation";

/** Opened from the password-reset email: /reset-password?token=… */
export default async function ResetPasswordPage({
  params,
  searchParams,
}: PageProps<"/[locale]/reset-password">) {
  const { locale } = await params;
  const { token } = await searchParams;
  setRequestLocale(locale);
  const t = await getTranslations("auth");
  const value = typeof token === "string" ? token : "";

  return (
    <AuthFrame title={t("resetTitle")} description={t("resetSubtitle")}>
      {value ? (
        <ResetPasswordForm locale={locale} token={value} />
      ) : (
        <Notice tone="attention" title={t("linkMissingToken")} />
      )}
      <Link
        className="mt-space-lg inline-flex items-center gap-1 text-sm font-semibold text-ink hover:underline"
        href="/sign-in"
      >
        ← {t("backToSignIn")}
      </Link>
    </AuthFrame>
  );
}
