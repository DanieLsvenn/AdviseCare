import type { ReactNode } from "react";
import { getTranslations } from "next-intl/server";
import { Brand } from "@/components/layout/brand";
import { LanguageSwitcher } from "@/components/ui/language-switcher";

/**
 * Frame for the signed-out pages (sign in, link account, reset password).
 * Left: the contact book's cover with what AdviseCare is for, in three plain
 * lines. Right: the page itself on white paper. No imagery.
 */
export async function AuthFrame({
  title,
  description,
  children,
}: {
  title: string;
  description?: ReactNode;
  children: ReactNode;
}) {
  const t = await getTranslations("auth");
  const lines = [t("coverLine1"), t("coverLine2"), t("coverLine3")];

  return (
    <div className="grid min-h-screen grid-rows-[auto_1fr] lg:grid-rows-1 lg:grid-cols-[minmax(0,26rem)_minmax(0,1fr)] xl:grid-cols-[minmax(0,30rem)_minmax(0,1fr)]">
      <aside className="flex flex-col justify-between bg-cover px-gutter-mobile py-space-base text-white lg:px-space-2xl lg:py-space-2xl">
        <Brand subtitle={t("coverSubtitle")} />
        <div className="hidden lg:block">
          <p className="max-w-[22ch] text-[1.75rem] font-semibold leading-9 tracking-[-0.015em]">
            {t("coverStatement")}
          </p>
          <ol className="mt-space-xl divide-y divide-cover-line border-y border-cover-line">
            {lines.map((line, index) => (
              <li key={line} className="grid grid-cols-[2rem_minmax(0,1fr)] py-space-md text-[0.9375rem] leading-6 text-cover-ink">
                <span className="font-mono text-[0.8125rem] text-white/70">{index + 1}</span>
                <span>{line}</span>
              </li>
            ))}
          </ol>
        </div>
        <p className="hidden text-[0.8125rem] text-cover-ink lg:block">{t("legalLine")}</p>
      </aside>

      <main className="flex flex-col bg-paper">
        <div className="flex justify-end px-gutter-mobile py-space-md lg:px-space-xl">
          <LanguageSwitcher />
        </div>
        <div className="flex flex-1 items-start justify-center px-gutter-mobile pb-space-2xl pt-space-lg sm:items-center lg:px-space-xl">
          <div className="w-full max-w-[24rem]">
            <h1 className="text-2xl font-semibold leading-8 tracking-[-0.01em] text-text">{title}</h1>
            {description ? (
              <p className="mt-1.5 text-[0.9375rem] leading-6 text-text-muted">{description}</p>
            ) : null}
            <div className="mt-space-xl">{children}</div>
          </div>
        </div>
      </main>
    </div>
  );
}
