import { getTranslations, setRequestLocale } from "next-intl/server";
import { Brand } from "@/components/layout/brand";
import { ForgotPasswordForm } from "@/components/auth/forgot-password-form";
import { LanguageSwitcher, Panel } from "@/components/ui";
import { Link } from "@/i18n/navigation";

export default async function ForgotPasswordPage({
  params,
}: PageProps<"/[locale]/forgot-password">) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("auth");

  return (
    <div className="flex min-h-screen flex-col">
      <header className="flex h-header-height items-center justify-between border-b border-outline-variant/30 bg-surface-container-lowest px-gutter-mobile lg:px-gutter-desktop">
        <Brand subtitle="Academic advising & student welfare" />
        <LanguageSwitcher />
      </header>
      <main className="flex flex-1 justify-center px-gutter-mobile py-space-2xl">
        <Panel className="h-fit w-full max-w-xl p-space-xl">
          <h1 className="font-display-sm text-display-sm text-on-surface">
            {t("forgotTitle")}
          </h1>
          <p className="mt-space-sm font-body-base text-body-base text-on-surface-variant">
            {t("forgotSubtitle")}
          </p>
          <ForgotPasswordForm />
          <Link
            className="mt-space-lg inline-flex font-label-md text-label-md text-primary underline-offset-2 hover:underline"
            href="/sign-in"
          >
            {t("backToSignIn")}
          </Link>
        </Panel>
      </main>
    </div>
  );
}
