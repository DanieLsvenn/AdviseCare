import { getTranslations } from "next-intl/server";
import { AppShell } from "@/components/layout/app-shell";
import { ActiveNavTitle } from "@/components/layout/active-nav-title";
import type { NavItem } from "@/components/layout/sidebar-nav";
import { getInitials, requireRole } from "@/lib/auth";
import { localizeRole } from "@/lib/roles";

export const dynamic = "force-dynamic";

export default async function AdminLayout({
  children,
  params,
}: LayoutProps<"/[locale]/admin">) {
  const { locale } = await params;
  const t = await getTranslations("nav");
  const user = await requireRole(locale, ["System Administrator"]);
  const navItems: NavItem[] = [
    { href: "/admin/m01", label: t("administrator.accounts"), icon: "manage_accounts" },
    { href: "/admin/m02", label: t("administrator.faculties"), icon: "account_tree" },
    { href: "/admin/m03", label: t("administrator.sources"), icon: "upload_file" },
    { href: "/admin/m04", label: t("administrator.reference"), icon: "category" },
    { href: "/admin/m05", label: t("administrator.health"), icon: "history" },
  ];

  return (
    <AppShell
      navItems={navItems}
      navSectionLabel={t("sectionAdministration")}
      brandSubtitle="AdviseCare administration"
      title={
        <ActiveNavTitle
          items={navItems}
          fallback={t("administrator.accounts")}
        />
      }
      accountName={user.fullName}
      accountRole={user.roles.map((role) => localizeRole(role, t)).join(", ")}
      accountInitials={getInitials(user.fullName)}
      roles={user.roles}
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
