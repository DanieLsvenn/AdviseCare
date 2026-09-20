"use client";

import { useLocale } from "next-intl";
import { cn } from "@/lib/cn";
import { Link, usePathname } from "@/i18n/navigation";
import { routing } from "@/i18n/routing";

const labels: Record<string, string> = { en: "EN", vi: "VI" };

/**
 * Design Rule §4.7 — bộ chuyển ngôn ngữ luôn hiện ở header, mọi màn hình.
 * Là segmented toggle: lựa chọn đang bật có nền trắng + viền + chữ teal đậm.
 */
export function LanguageSwitcher({ className }: { className?: string }) {
  const pathname = usePathname();
  const active = useLocale();

  return (
    <div
      className={cn(
        "inline-flex items-center rounded border border-outline-variant/60 bg-surface-container-low p-0.5",
        className,
      )}
      role="group"
      aria-label="Language"
    >
      {routing.locales.map((locale) => (
        <Link
          key={locale}
          href={pathname}
          locale={locale}
          aria-current={locale === active ? "true" : undefined}
          className={cn(
            "rounded px-space-sm py-0.5 font-label-sm text-label-sm transition-colors",
            locale === active
              ? "border border-outline-variant/60 bg-surface-container-lowest font-bold text-primary"
              : "border border-transparent text-on-surface-variant hover:text-on-surface",
          )}
        >
          {labels[locale] ?? locale.toUpperCase()}
        </Link>
      ))}
    </div>
  );
}
