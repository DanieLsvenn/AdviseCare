"use client";

import { useLocale, useTranslations } from "next-intl";
import { cn } from "@/lib/cn";
import { Link, usePathname } from "@/i18n/navigation";
import { routing } from "@/i18n/routing";

const labels: Record<string, string> = { en: "EN", vi: "VI" };

/**
 * The EN | VI toggle is visible on every screen (brand commitment).
 * Segmented control: the active language is a white chip in ink.
 */
export function LanguageSwitcher({
  className,
  tone = "paper",
}: {
  className?: string;
  tone?: "paper" | "cover";
}) {
  const pathname = usePathname();
  const active = useLocale();
  const t = useTranslations("common");

  return (
    <div
      className={cn(
        "inline-flex h-8 items-center rounded-md border p-0.5",
        tone === "cover" ? "border-cover-line bg-cover-raised" : "border-rule bg-paper-tint",
        className,
      )}
      role="group"
      aria-label={t("language")}
    >
      {routing.locales.map((locale) => (
        <Link
          key={locale}
          href={pathname}
          locale={locale}
          aria-current={locale === active ? "true" : undefined}
          className={cn(
            "flex h-6 items-center rounded px-2 text-[0.75rem] font-semibold tracking-wide transition-colors",
            locale === active
              ? "bg-paper text-ink shadow-[0_0_0_1px_var(--color-rule)]"
              : tone === "cover"
                ? "text-cover-ink hover:text-white"
                : "text-text-faint hover:text-text",
          )}
        >
          {labels[locale] ?? locale.toUpperCase()}
        </Link>
      ))}
    </div>
  );
}
