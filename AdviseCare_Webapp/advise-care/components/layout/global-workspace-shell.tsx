import type { ReactNode } from "react";
import { getTranslations } from "next-intl/server";
import { AppShell } from "@/components/layout/app-shell";
import { ActiveNavTitle } from "@/components/layout/active-nav-title";
import type { NavItem } from "@/components/layout/sidebar-nav";
import { getInitials, requireUser } from "@/lib/auth";
import { localizeRole } from "@/lib/roles";

export async function GlobalWorkspaceShell({
  locale,
  children,
}: {
  locale: string;
  children: ReactNode;
}) {
  const [user, common, nav] = await Promise.all([
    requireUser(locale),
    getTranslations("common"),
    getTranslations("nav"),
  ]);
  const navItems: NavItem[] = [
    { href: "/", label: common("workspaces"), icon: "dashboard" },
    {
      href: "/notifications",
      label: common("notifications"),
      icon: "notifications",
    },
    { href: "/settings", label: common("settings"), icon: "settings" },
  ];

  return (
    <AppShell
      navItems={navItems}
      navSectionLabel={common("workspaces")}
      brandSubtitle={common("workspaces")}
      title={
        <ActiveNavTitle
          items={navItems}
          fallback={common("workspaces")}
        />
      }
      accountName={user.fullName}
      accountRole={user.roles
        .map((role) => localizeRole(role, nav))
        .join(", ")}
      accountInitials={getInitials(user.fullName)}
      roles={user.roles}
    >
      {children}
    </AppShell>
  );
}
