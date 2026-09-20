import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import { Avatar } from "@/components/ui/avatar";
import { Icon } from "@/components/ui/icon";
import { LanguageSwitcher } from "@/components/ui/language-switcher";
import { StatusChip } from "@/components/ui/status-chip";

/** Global utility header — 56px, chứa danh tính, ngữ cảnh mục đích và EN|VI. */
export function TopBar({
  title,
  contextLabel,
  purposeLabel,
  accountName,
  accountRole,
  accountInitials,
  hasAlert,
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
  className?: string;
  extra?: ReactNode;
}) {
  return (
    <header
      className={cn(
        "sticky top-0 z-40 flex h-header-height items-center gap-space-md border-b border-outline-variant/30 bg-surface-container-lowest px-space-lg",
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
        <span className="relative flex size-8 items-center justify-center rounded text-on-surface-variant">
          <Icon name="notifications" className="text-[20px]" />
          {hasAlert ? (
            <span
              aria-label="Unread notifications"
              role="status"
              className="absolute right-1.5 top-1.5 size-1.5 rounded-full bg-status-urgent-text"
            />
          ) : null}
        </span>

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
      </div>
    </header>
  );
}
