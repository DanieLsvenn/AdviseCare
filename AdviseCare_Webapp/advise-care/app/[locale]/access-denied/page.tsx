import { getTranslations, setRequestLocale } from "next-intl/server";
import { PageHeader } from "@/components/ui/page-header";
import { ButtonLink, Panel } from "@/components/ui";

export default async function AccessDeniedPage({
  params,
}: PageProps<"/[locale]/access-denied">) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("auth");

  return (
    <main className="mx-auto flex min-h-screen max-w-3xl flex-col justify-center gap-space-lg px-gutter-mobile py-space-2xl">
      <PageHeader title={t("accessDenied")} />
      <Panel className="p-space-xl">
        <p className="font-body-base text-body-base text-on-surface-variant">
          {t("accessDenied")}
        </p>
        <div className="mt-space-lg">
          <ButtonLink href="/sign-in" variant="secondary">
            {t("backToSignIn")}
          </ButtonLink>
        </div>
      </Panel>
    </main>
  );
}
