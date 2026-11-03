import type { ReactNode } from "react";
import { getTranslations } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { Avatar } from "@/components/ui/avatar";
import { Icon } from "@/components/ui/icon";
import { LanguageSwitcher } from "@/components/ui/language-switcher";
import { SignOutButton } from "@/components/auth/sign-out-button";
import { localizeRole, roleHomePath } from "@/lib/roles";
import { Brand } from "./brand";
import { NavTrail, SidebarNav, TopNav, type NavItem } from "./sidebar-nav";

const skipLink = (label: string) => (
  <a
    href="#main"
    className="sr-only z-[60] rounded-md bg-paper px-3 py-2 text-sm font-semibold text-ink shadow-popover focus:not-sr-only focus:fixed focus:left-3 focus:top-3"
  >
    {label}
  </a>
);

/**
 * Two shells, one book.
 *  - Staff: the blue cover runs down the left as the sidebar (240px); the
 *    page is white paper on the desk to its right. Below 1024px the cover
 *    becomes a top band with a scrollable row of destinations.
 *  - Student: the cover is a top bar with the few places a student goes,
 *    and the page is a single, calmer column.
 */
export async function AppShell({
  navItems,
  moreNavItems,
  navSectionLabel,
  title,
  accountName,
  accountRole,
  accountInitials,
  roles = [],
  hasAlert,
  children,
  variant = "staff",
}: {
  navItems: NavItem[];
  /** Student shell: secondary destinations folded under "More". */
  moreNavItems?: NavItem[];
  navSectionLabel: string;
  /** @deprecated the product name is fixed */
  brandSubtitle?: string;
  /** Fallback label for the breadcrumb when no nav item matches. */
  title?: ReactNode;
  contextLabel?: string;
  purposeLabel?: string;
  accountName: string;
  accountRole: string;
  accountInitials: string;
  accountStatus?: string;
  roles?: string[];
  /** @deprecated replaced by the account row */
  footerNote?: { icon: string; title: string; caption: string };
  hasAlert?: boolean;
  children: ReactNode;
  variant?: "staff" | "student";
}) {
  const [tc, tn] = await Promise.all([
    getTranslations("common"),
    getTranslations("nav"),
  ]);
  const workspaces = roles.flatMap((role) => {
    const href = roleHomePath(role);
    return href ? [{ role, href }] : [];
  });
  const fallback = typeof title === "string" ? title : undefined;

  const iconLink = (tone: "paper" | "cover") =>
    tone === "cover"
      ? "relative flex size-9 items-center justify-center rounded-md text-cover-ink transition-colors hover:bg-cover-raised hover:text-white"
      : "relative flex size-9 items-center justify-center rounded-md text-text-muted transition-colors hover:bg-ink-wash hover:text-text";
  const utilities = (tone: "paper" | "cover") => (
    <div className="flex items-center gap-1">
      <Link aria-label={tc("notifications")} className={iconLink(tone)} href="/notifications">
        <Icon name="notifications" className="text-[21px]" />
        {hasAlert ? (
          <span className="absolute right-2 top-2 size-2 rounded-full bg-red-ink ring-2 ring-paper" />
        ) : null}
      </Link>
      <Link aria-label={tc("settings")} className={iconLink(tone)} href="/settings">
        <Icon name="settings" className="text-[21px]" />
      </Link>
      <LanguageSwitcher className="ml-1" tone={tone} />
    </div>
  );

  if (variant === "student") {
    return (
      <div className="flex min-h-screen flex-col">
        {skipLink(tc("skipToContent"))}
        <header className="sticky top-0 z-40 bg-cover text-white">
          <div className="mx-auto flex h-14 w-full max-w-[72rem] items-center gap-space-lg px-gutter-mobile lg:px-gutter-desktop">
            <Link href="/me/profile" className="rounded-md focus-visible:outline-white">
              <Brand />
            </Link>
            <div className="ml-auto flex items-center gap-space-md">
              {utilities("cover")}
              <span aria-hidden className="mx-1 hidden h-6 w-px bg-cover-line sm:block" />
              <span className="hidden items-center gap-2.5 sm:flex">
                <span className="text-right leading-tight">
                  <span className="block text-sm font-semibold">{accountName}</span>
                  <span className="block text-[0.75rem] text-cover-ink">{accountRole}</span>
                </span>
                <Avatar initials={accountInitials} tone="cover" />
              </span>
              <SignOutButton tone="cover" compact />
            </div>
          </div>
          <div className="border-t border-cover-line">
            <div className="mx-auto w-full max-w-[72rem] px-gutter-mobile lg:px-gutter-desktop">
              <TopNav items={navItems} more={moreNavItems} moreLabel={tn("more")} label={navSectionLabel} />
            </div>
          </div>
        </header>
        <main id="main" tabIndex={-1} className="flex-1 px-gutter-mobile py-space-xl outline-none lg:px-gutter-desktop">
          <div className="mx-auto flex w-full max-w-[60rem] flex-col gap-space-lg">
            {children}
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen flex-col lg:flex-row">
      {skipLink(tc("skipToContent"))}
      <aside className="flex shrink-0 flex-col bg-cover text-white lg:sticky lg:top-0 lg:h-screen lg:w-sidebar-width">
        <div className="flex h-14 items-center gap-space-sm px-space-base">
          <Link href={workspaces[0]?.href ?? "/"} className="min-w-0 rounded-md focus-visible:outline-white">
            <Brand />
          </Link>
          {/* Phones: the utilities live on the cover so there is one bar, not two. */}
          <div className="ml-auto flex items-center gap-1 lg:hidden">
            {utilities("cover")}
            <SignOutButton tone="cover" compact />
          </div>
        </div>

        <div className="hidden px-space-base pb-space-sm lg:block">
          {workspaces.length > 1 ? (
            <details className="group relative">
              <summary className="flex cursor-pointer list-none items-center justify-between gap-2 rounded-md border border-cover-line px-2.5 py-1.5 text-sm text-cover-ink hover:text-white">
                <span className="truncate">{navSectionLabel}</span>
                <Icon name="unfold_more" className="text-[18px]" />
              </summary>
              <nav
                aria-label={tc("workspaces")}
                className="absolute inset-x-0 top-full z-50 mt-1 rounded-lg border border-rule bg-paper p-1 shadow-popover"
              >
                {workspaces.map(({ role, href }) => (
                  <Link
                    className="block rounded-md px-2.5 py-2 text-sm text-text hover:bg-ink-wash"
                    href={href}
                    key={role}
                  >
                    {localizeRole(role, tn)}
                  </Link>
                ))}
              </nav>
            </details>
          ) : (
            <p className="px-0.5 text-[0.8125rem] text-cover-ink">{navSectionLabel}</p>
          )}
        </div>

        <div className="overflow-x-auto border-t border-cover-line lg:flex-1 lg:overflow-y-auto lg:overflow-x-visible lg:border-t-0">
          <SidebarNav items={navItems} label={navSectionLabel} />
        </div>

        <div className="hidden items-center gap-2.5 border-t border-cover-line px-space-base py-space-md lg:flex">
          <Avatar initials={accountInitials} tone="cover" />
          <div className="min-w-0 flex-1 leading-tight">
            <p className="truncate text-sm font-semibold">{accountName}</p>
            <p className="truncate text-[0.75rem] text-cover-ink">{accountRole}</p>
          </div>
          <SignOutButton tone="cover" compact />
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-30 hidden h-14 items-center gap-space-md border-b border-rule bg-paper/95 px-gutter-mobile text-text backdrop-blur-sm lg:flex lg:px-gutter-desktop">
          <NavTrail items={navItems} section={navSectionLabel} fallback={fallback} />
          <div className="ml-auto flex items-center gap-space-sm">
            {utilities("paper")}
          </div>
        </header>
        <main id="main" tabIndex={-1} className="flex-1 px-gutter-mobile py-space-xl outline-none lg:px-gutter-desktop">
          <div className="mx-auto flex w-full max-w-[90rem] flex-col gap-space-lg">
            {children}
          </div>
        </main>
      </div>
    </div>
  );
}

/** Detail pages: main column with a 352px side column from 1280px. */
export function InspectorLayout({
  children,
  inspector,
}: {
  children: ReactNode;
  inspector: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-space-lg xl:flex-row xl:items-start">
      <div className="min-w-0 flex-1">{children}</div>
      <aside className="flex w-full flex-col gap-space-lg xl:w-meta-pane-width xl:shrink-0">
        {inspector}
      </aside>
    </div>
  );
}
