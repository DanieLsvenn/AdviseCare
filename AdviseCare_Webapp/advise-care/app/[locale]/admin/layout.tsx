import { getTranslations } from "next-intl/server";
import { AppShell } from "@/components/layout/app-shell";
import { ActiveNavTitle } from "@/components/layout/active-nav-title";
import type { NavItem } from "@/components/layout/sidebar-nav";
import { apiGet } from "@/lib/api";
import type { Account, AcademicTerm } from "@/lib/types";

export default async function AdminLayout({ children }: LayoutProps<"/[locale]/admin">) {
  const t = await getTranslations("nav");
  const session = await apiGet<{ administrator: Account; term: AcademicTerm }>("/api/session");
  const navItems: NavItem[] = [
    { href: "/admin/m01", label: t("administrator.accounts"), icon: "manage_accounts" },
    { href: "/admin/m02", label: t("administrator.faculties"), icon: "account_tree" },
    { href: "/admin/m03", label: t("administrator.sources"), icon: "hub" },
    { href: "/admin/m04", label: t("administrator.reference"), icon: "menu_book" },
    { href: "/admin/m05", label: t("administrator.health"), icon: "monitoring" },
    { href: "/admin/m06", label: t("administrator.restore"), icon: "restore" },
    { href: "/admin/m07", label: t("administrator.indicators"), icon: "tune" },
  ];

  return (
    <AppShell
      navItems={navItems}
      navSectionLabel={t("sectionAdministration")}
      brandSubtitle="Institution-wide"
      title={<ActiveNavTitle items={navItems} fallback={t("administrator.accounts")} />}
      contextLabel={session.term.Label}
      purposeLabel="System administration scope"
      accountName={session.administrator.FullName}
      accountRole={session.administrator.RoleLabel}
      accountInitials={session.administrator.Initials}
      hasAlert
      footerNote={{
        icon: "admin_panel_settings",
        title: t("administratorFooterTitle"),
        caption: t("administratorFooterCaption"),
      }}
    >
      {children}
    </AppShell>
  );
}