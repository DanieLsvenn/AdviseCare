import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import { Brand } from "./brand";
import { SidebarNav, type NavItem } from "./sidebar-nav";
import { TopBar } from "./top-bar";
import { Icon } from "@/components/ui/icon";
import { RoutePath } from "@/components/ui/route-path";

/**
 * Bố cục 3 vùng theo Design Rule §8:
 *  - Sidebar 256px cố định (staff) / 224px (student, rút gọn)
 *  - Utility header 56px
 *  - Canvas chính co giãn; inspector pane 352px do từng trang tự gắn ở ≥1280px
 * Dưới 1024px sidebar chuyển thành dải ngang cuộn được, không có drawer ẩn —
 * điều hướng luôn nhìn thấy được là yêu cầu vận hành của hệ thống này.
 */
export function AppShell({
  navItems,
  navSectionLabel,
  brandSubtitle,
  title,
  contextLabel,
  purposeLabel,
  accountName,
  accountRole,
  accountInitials,
  accountStatus,
  footerNote,
  hasAlert,
  children,
  variant = "staff",
}: {
  navItems: NavItem[];
  navSectionLabel: string;
  brandSubtitle: string;
  title: ReactNode;
  contextLabel?: string;
  purposeLabel?: string;
  accountName: string;
  accountRole: string;
  accountInitials: string;
  accountStatus?: string;
  footerNote?: { icon: string; title: string; caption: string };
  hasAlert?: boolean;
  children: ReactNode;
  variant?: "staff" | "student";
}) {
  const sidebarWidth = variant === "staff" ? "lg:w-sidebar-width" : "lg:w-56";

  return (
    <div className="flex min-h-screen flex-col lg:flex-row">
      <aside
        className={cn(
          "flex shrink-0 flex-col border-b border-outline-variant/30 bg-surface-container-lowest lg:h-screen lg:sticky lg:top-0 lg:border-b-0 lg:border-r",
          sidebarWidth,
        )}
      >
        <div className="flex h-header-height items-center border-b border-outline-variant/30 px-space-base">
          <Brand subtitle={brandSubtitle} compact={variant === "student"} />
        </div>

        <div className="hidden border-b border-outline-variant/20 bg-surface-container-low/50 px-space-base py-space-sm lg:block">
          <div className="flex items-center justify-between text-on-surface-variant">
            <span className="font-caption text-caption font-semibold uppercase tracking-wider">
              {navSectionLabel}
            </span>
            <span
              aria-hidden
              className="size-2 rounded-full bg-secondary"
              title="Live system"
            />
          </div>
        </div>

        <div className="flex-1 overflow-x-auto lg:overflow-x-visible">
          <div className="min-w-max lg:min-w-0">
            <SidebarNav items={navItems} />
          </div>
        </div>

        {footerNote ? (
          <div className="hidden items-center gap-space-sm border-t border-outline-variant/30 px-space-base py-space-md lg:flex">
            <Icon
              name={footerNote.icon}
              className="text-[20px] text-on-surface-variant"
            />
            <div className="min-w-0">
              <p className="truncate font-label-md text-label-md font-semibold text-on-surface">
                {footerNote.title}
              </p>
              <p className="truncate font-caption text-caption text-on-surface-variant">
                {footerNote.caption}
              </p>
            </div>
          </div>
        ) : null}
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <TopBar
          title={title}
          contextLabel={contextLabel}
          purposeLabel={purposeLabel}
          accountName={accountName}
          accountRole={accountStatus ?? accountRole}
          accountInitials={accountInitials}
          hasAlert={hasAlert}
        />
        <main className="flex-1 px-gutter-mobile py-space-lg lg:px-gutter-desktop">
          <div className="mx-auto flex w-full max-w-[1400px] flex-col gap-space-lg">
            <RoutePath />
            {children}
          </div>
        </main>
      </div>
    </div>
  );
}

/** Inspector pane 352px, chỉ hiện từ 1280px trở lên (Design Rule §Layout Model 4). */
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
