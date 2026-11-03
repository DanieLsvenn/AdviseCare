"use client";

import { cn } from "@/lib/cn";
import { Link, usePathname } from "@/i18n/navigation";
import { Icon } from "@/components/ui/icon";

export interface NavItem {
  href: string;
  label: string;
  icon: string;
  /** A count only on a queue that needs action. */
  badge?: "dot" | number;
  /** Extra paths that should light this item up (e.g. detail pages). */
  match?: string[];
}

export function activeNavItem(items: NavItem[], pathname: string): NavItem | undefined {
  // Score each item by the longest of its paths that the current path falls under,
  // so "/head/reports" beats a "/head" fallback on another item.
  const score = (path: string) =>
    pathname === path || pathname.startsWith(`${path}/`) ? path.length : -1;
  let best: NavItem | undefined;
  let bestScore = -1;
  for (const item of items) {
    const itemScore = Math.max(score(item.href), ...(item.match ?? []).map(score));
    if (itemScore > bestScore) {
      best = item;
      bestScore = itemScore;
    }
  }
  return best;
}

/** Vertical navigation on the blue cover (staff workspaces). */
export function SidebarNav({ items, label }: { items: NavItem[]; label?: string }) {
  const pathname = usePathname();
  const active = activeNavItem(items, pathname);

  return (
    <nav aria-label={label} className="flex gap-0.5 px-space-sm py-space-sm lg:flex-col">
      {items.map((item) => {
        const isActive = item === active;
        return (
          <Link
            key={item.href}
            href={item.href}
            aria-current={isActive ? "page" : undefined}
            className={cn(
              "group flex min-h-9 shrink-0 items-center gap-2.5 rounded-md px-2.5 py-1.5 text-[0.9375rem] leading-5 transition-colors duration-150",
              isActive
                ? "bg-cover-raised font-semibold text-white shadow-[inset_0_0_0_1px_var(--color-cover-line)]"
                : "text-cover-ink hover:bg-cover-raised/70 hover:text-white",
            )}
          >
            <Icon
              name={item.icon}
              filled={isActive}
              className={cn("text-[20px]", isActive ? "text-white" : "text-cover-ink/80 group-hover:text-white")}
            />
            <span className="min-w-0 flex-1 whitespace-nowrap lg:whitespace-normal">
              {item.label}
            </span>
            {item.badge === "dot" ? (
              <span aria-hidden className="size-2 shrink-0 rounded-full bg-[#ffb4a9]" />
            ) : typeof item.badge === "number" && item.badge > 0 ? (
              <span className="rounded-full bg-white px-1.5 font-mono text-[0.75rem] font-medium leading-5 text-cover">
                {item.badge}
              </span>
            ) : null}
          </Link>
        );
      })}
    </nav>
  );
}

/**
 * Horizontal navigation for the student's simpler top bar. The few places a
 * student goes at a moment of need stay visible; account housekeeping folds
 * into "More" so nothing is cut off on a phone.
 */
export function TopNav({
  items,
  more = [],
  moreLabel = "More",
  label,
}: {
  items: NavItem[];
  more?: NavItem[];
  moreLabel?: string;
  label?: string;
}) {
  const pathname = usePathname();
  const active = activeNavItem([...items, ...more], pathname);
  const moreActive = more.includes(active as NavItem);

  return (
    <nav aria-label={label} className="-mb-px flex gap-0.5 overflow-x-auto [scrollbar-width:none] sm:gap-1 sm:overflow-visible">
      {items.map((item) => {
        const isActive = item === active;
        return (
          <Link
            key={item.href}
            href={item.href}
            aria-current={isActive ? "page" : undefined}
            className={cn(
              "flex h-11 shrink-0 items-center gap-2 border-b-2 px-2.5 text-[0.9375rem] transition-colors duration-150 sm:px-3",
              isActive
                ? "border-white font-semibold text-white"
                : "border-transparent text-cover-ink hover:text-white",
            )}
          >
            <span aria-hidden className="hidden sm:inline-flex">
              <Icon name={item.icon} filled={isActive} className="text-[19px]" />
            </span>
            {item.label}
          </Link>
        );
      })}
      {more.length ? (
        <details className="group relative shrink-0">
          <summary
            className={cn(
              "flex h-11 cursor-pointer list-none items-center gap-1 border-b-2 px-2.5 text-[0.9375rem] transition-colors duration-150 sm:px-3",
              moreActive
                ? "border-white font-semibold text-white"
                : "border-transparent text-cover-ink hover:text-white",
            )}
          >
            {moreActive ? active?.label : moreLabel}
            <Icon name="expand_more" className="text-[19px] transition-transform group-open:rotate-180" />
          </summary>
          <div className="fixed right-3 top-[6.25rem] z-50 min-w-56 rounded-lg border border-rule bg-paper p-1 shadow-popover sm:absolute sm:right-0 sm:top-full sm:mt-1">
            {more.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                aria-current={item === active ? "page" : undefined}
                className={cn(
                  "flex min-h-11 items-center gap-2.5 rounded-md px-3 text-[0.9375rem] text-text hover:bg-ink-wash",
                  item === active && "font-semibold text-ink",
                )}
              >
                <Icon name={item.icon} className="text-[19px] text-text-muted" />
                {item.label}
              </Link>
            ))}
          </div>
        </details>
      ) : null}
    </nav>
  );
}

/** "Section / Page" trail in the utility bar, from the same nav items. */
export function NavTrail({
  items,
  section,
  fallback,
}: {
  items: NavItem[];
  section: string;
  fallback?: string;
}) {
  const pathname = usePathname();
  const active = activeNavItem(items, pathname);
  const current = active?.label ?? fallback;

  return (
    <nav aria-label="Breadcrumb" className="min-w-0">
      <ol className="flex min-w-0 items-center gap-1.5 text-sm">
        <li className="hidden shrink-0 text-text-faint sm:block">{section}</li>
        {current ? (
          <>
            <li aria-hidden className="hidden text-rule-strong sm:block">
              /
            </li>
            <li aria-current="page" className="truncate font-semibold text-text">
              {current}
            </li>
          </>
        ) : null}
      </ol>
    </nav>
  );
}
