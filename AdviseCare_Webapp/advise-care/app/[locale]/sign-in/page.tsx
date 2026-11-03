import { getTranslations, setRequestLocale } from "next-intl/server";
import { AuthFrame } from "@/components/auth/auth-frame";
import { GoogleSignIn } from "@/components/auth/google-sign-in";
import { SignInForm } from "@/components/auth/sign-in-form";
import { Notice } from "@/components/ui/notice";

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
            : query.reset
              ? t("resetDone")
              : "";
  const hasGoogle = Boolean(process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID);

  return (
    <AuthFrame title={t("title")} description={t("subtitle")}>
      {notice ? (
        <Notice
          className="mb-space-lg"
          tone={query.reason ? "attention" : "info"}
          icon={query.reason ? "info" : "check_circle"}
          title={notice}
        />
      ) : null}
      {hasGoogle ? (
        <>
          <GoogleSignIn />
          <div className="my-space-lg flex items-center gap-space-md text-[0.8125rem] text-text-faint">
            <span className="h-px flex-1 bg-rule" />
            {t("orWithPassword")}
            <span className="h-px flex-1 bg-rule" />
          </div>
        </>
      ) : null}
      <SignInForm locale={locale} />
      <p className="mt-space-xl border-t border-rule pt-space-md text-[0.8125rem] leading-5 text-text-faint">
        {t("firstTimeHint")}
      </p>
    </AuthFrame>
  );
}
