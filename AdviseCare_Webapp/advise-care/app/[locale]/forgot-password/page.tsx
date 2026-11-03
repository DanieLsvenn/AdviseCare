import { getTranslations, setRequestLocale } from "next-intl/server";
import { AuthFrame } from "@/components/auth/auth-frame";
import { ForgotPasswordForm } from "@/components/auth/forgot-password-form";
import { Link } from "@/i18n/navigation";

export default async function ForgotPasswordPage({
  params,
}: PageProps<"/[locale]/forgot-password">) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("auth");

  return (
    <AuthFrame title={t("forgotTitle")} description={t("forgotSubtitle")}>
      <ForgotPasswordForm />
      <Link
        className="mt-space-lg inline-flex items-center gap-1 text-sm font-semibold text-ink hover:underline"
        href="/sign-in"
      >
        ← {t("backToSignIn")}
      </Link>
    </AuthFrame>
  );
}
