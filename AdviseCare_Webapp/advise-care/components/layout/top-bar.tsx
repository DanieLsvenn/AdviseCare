import type { ReactNode } from "react";
import { getTranslations } from "next-intl/server";
import { cn } from "@/lib/cn";
import { Avatar } from "@/components/ui/avatar";
import { Icon } from "@/components/ui/icon";
import { LanguageSwitcher } from "@/components/ui/language-switcher";
import { StatusChip } from "@/components/ui/status-chip";
import { SignOutButton } from "@/components/auth/sign-out-button";
import { Link } from "@/i18n/navigation";
import { localizeRole, roleHomePath } from "@/lib/roles";

export async function TopBar({
  title,
  contextLabel,
  purposeLabel,
  accountName,
  accountRole,
  accountInitials,
  hasAlert,
  roles = [],
  className,
  extra,
}: {
  title: ReactNode;
  contextLabel?: string;
  purposeLabel?: string;
  accountName: string;
  accountRole: string;
  accountInitials: string;
  hasAlert?: boolean;
  roles?: string[];
  className?: string;
  extra?: ReactNode;
}) {
  const tn = await getTranslations("nav");
  const tc = await getTranslations("common");
  const workspaces = roles.flatMap((role) => {
    const href = roleHomePath(role);
    return href ? [{ role, href }] : [];
  });

  return (
    <header
      className={cn(
        "sticky top-0 z-40 flex min-h-header-height flex-wrap items-center gap-space-md border-b border-outline-variant/30 bg-surface-container-lowest px-space-lg py-space-xs",
        className,
      )}
    >
      <h1 className="truncate font-headline-md text-headline-md text-on-surface">
        {title}
      </h1>

      {contextLabel ? (
        <>
          <span aria-hidden className="h-5 w-px bg-outline-variant/50" />
          <span className="hidden rounded border border-outline-variant/50 bg-surface-container-low px-space-md py-1 font-label-md text-label-md text-on-surface-variant sm:inline-flex">
            {contextLabel}
          </span>
        </>
      ) : null}

      {purposeLabel ? (
        <StatusChip
          tone="sealed"
          icon="verified_user"
          label={purposeLabel}
          className="hidden lg:inline-flex"
        />
      ) : null}

      <div className="ml-auto flex items-center gap-space-md">
        {extra}
        <Link
          aria-label={tc("notifications")}
          className="relative flex size-8 items-center justify-center rounded text-on-surface-variant hover:bg-surface-container-high hover:text-on-surface"
          href="/notifications"
        >
          <Icon name="notifications" className="text-[20px]" />
          {hasAlert ? (
            <span className="absolute right-1 top-1 size-2 rounded-full bg-status-urgent-text" />
          ) : null}
        </Link>
        <Link
          aria-label={tc("settings")}
          className="flex size-8 items-center justify-center rounded text-on-surface-variant hover:bg-surface-container-high hover:text-on-surface"
          href="/settings"
        >
          <Icon name="settings" className="text-[20px]" />
        </Link>

        <LanguageSwitcher />

        <span aria-hidden className="h-6 w-px bg-outline-variant/50" />

        <span className="flex items-center gap-space-sm">
          <span className="hidden text-right md:block">
            <span className="block font-label-md text-label-md font-semibold leading-tight text-on-surface">
              {accountName}
            </span>
            <span className="block font-caption text-caption leading-tight text-on-surface-variant">
              {accountRole}
            </span>
          </span>
          <Avatar initials={accountInitials} className="size-8" />
        </span>
        {workspaces.length > 1 ? (
          <details className="relative">
            <summary className="cursor-pointer list-none rounded border border-outline-variant/50 px-space-sm py-1 font-caption text-caption text-on-surface-variant hover:bg-surface-container-low">
              {tc("workspaces")}
            </summary>
            <nav
              aria-label={tc("workspaces")}
              className="absolute right-0 top-full z-50 mt-1 min-w-56 rounded border border-outline-variant bg-surface-container-lowest p-space-xs shadow-lg"
            >
              {workspaces.map(({ role, href }) => (
                <Link
                  className="block rounded px-space-sm py-space-sm font-body-sm text-body-sm text-on-surface hover:bg-surface-container-low"
                  href={href}
                  key={role}
                >
                  {localizeRole(role, tn)}
                </Link>
              ))}
            </nav>
          </details>
        ) : null}
        <SignOutButton />
      </div>
    </header>
  );
}
