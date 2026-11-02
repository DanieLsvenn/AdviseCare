import { getTranslations, setRequestLocale } from "next-intl/server";
import { SignInForm } from "@/components/auth/sign-in-form";
import { Brand } from "@/components/layout/brand";
import { LanguageSwitcher, Notice, Panel } from "@/components/ui";
import { Link } from "@/i18n/navigation";

export default async function SignInPage({
  params,
  searchParams,
}: PageProps<"/[locale]/sign-in">) {
  const { locale } = await params;
  const query = await searchParams;
  setRequestLocale(locale);

  const t = await getTranslations("auth");
  const notice = query.logoutUnconfirmed
    ? t("logoutUnconfirmed")
    : query.signedOut
      ? t("signedOut")
      : query.reason === "expired"
        ? t("expiredSession")
        : query.reason === "denied"
          ? t("accessDenied")
          : query.reason === "account"
            ? t("accountUnavailable")
          : "";

  return (
    <div className="flex min-h-screen flex-col">
      <header className="flex h-header-height items-center justify-between border-b border-outline-variant/30 bg-surface-container-lowest px-gutter-mobile lg:px-gutter-desktop">
        <Brand subtitle="Academic advising & student welfare" />
        <LanguageSwitcher />
      </header>

      <main className="flex flex-1 items-start justify-center px-gutter-mobile py-space-2xl lg:px-gutter-desktop">
        <div className="grid w-full max-w-5xl gap-space-lg lg:grid-cols-[minmax(0,1fr)_minmax(0,22rem)] lg:items-start">
          <Panel className="p-space-xl">
            <h1 className="font-display-sm text-display-sm text-on-surface">
              {t("title")}
            </h1>
            <p className="mt-space-sm max-w-xl font-body-base text-body-base text-on-surface-variant">
              {t("subtitle")}
            </p>
            {notice ? (
              <Notice className="mt-space-lg" title={notice} />
            ) : null}
            <SignInForm locale={locale} />
            <Link
              className="mt-space-md inline-flex font-label-md text-label-md text-primary underline-offset-2 hover:underline"
              href="/forgot-password"
            >
              {t("forgotPassword")}
            </Link>
            <p className="mt-space-lg font-body-sm text-body-sm text-on-surface-variant">
              {t("passwordHint")}
            </p>
          </Panel>

          <div className="flex flex-col gap-space-lg">
            <Notice tone="sealed" icon="gpp_good" title={t("noticeTitle")}>
              {t("noticeBody")}
            </Notice>
            <Panel className="p-space-lg">
              <p className="font-body-sm text-body-sm text-on-surface-variant">
                {t("legalLine")}
              </p>
            </Panel>
          </div>
        </div>
      </main>
    </div>
  );
}
