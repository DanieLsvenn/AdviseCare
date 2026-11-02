import { getTranslations } from "next-intl/server";
import { AppShell } from "@/components/layout/app-shell";
import { ActiveNavTitle } from "@/components/layout/active-nav-title";
import type { NavItem } from "@/components/layout/sidebar-nav";
import { getInitials, requireRole } from "@/lib/auth";
import { localizeRole } from "@/lib/roles";

export const dynamic = "force-dynamic";

export default async function HeadLayout({
  children,
  params,
}: LayoutProps<"/[locale]/head">) {
  const { locale } = await params;
  const t = await getTranslations("nav");
  const user = await requireRole(locale, ["Programme Head"]);
  const navItems: NavItem[] = [
    {
      href: "/head",
      label: t("programmeHead.matching"),
      icon: "assignment",
    },
    {
      href: "/head/advisor-workload",
      label: t("programmeHead.workload"),
      icon: "balance",
    },
    {
      href: "/head/assignment-changes",
      label: t("programmeHead.assignments"),
      icon: "assignment_ind",
    },
    {
      href: "/head/reports",
      label: t("programmeHead.reports"),
      icon: "description",
    },
  ];

  return (
    <AppShell
      navItems={navItems}
      navSectionLabel={t("sectionHead")}
      brandSubtitle="AdviseCare programme governance"
      title={
        <ActiveNavTitle
          items={navItems}
          fallback={t("programmeHead.matching")}
        />
      }
      accountName={user.fullName}
      accountRole={user.roles.map((role) => localizeRole(role, t)).join(", ")}
      accountInitials={getInitials(user.fullName)}
      roles={user.roles}
      footerNote={{
        icon: "account_balance",
        title: t("headFooterTitle"),
        caption: t("headFooterCaption"),
      }}
    >
      {children}
    </AppShell>
  );
}
