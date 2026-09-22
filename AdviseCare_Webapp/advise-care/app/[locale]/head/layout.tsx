import { AppShell } from "@/components/layout/app-shell";
import { ActiveNavTitle } from "@/components/layout/active-nav-title";
import type { NavItem } from "@/components/layout/sidebar-nav";
import { getTranslations } from "next-intl/server";

export default async function HeadLayout({
  children,
}: LayoutProps<"/[locale]/head">) {
  const t = await getTranslations("nav");
  const navItems: NavItem[] = [
    {
      href: "/head/programme-overview",
      label: t("head.overview"),
      icon: "analytics",
    },
    {
      href: "/head/advisor-workload",
      label: t("head.workload"),
      icon: "balance",
    },
    {
      href: "/head/assignment-changes",
      label: t("head.assignments"),
      icon: "assignment_ind",
      badge: "dot",
    },
    { href: "/head/reports", label: t("head.reports"), icon: "description" },
  ];

  return (
    <AppShell
      navItems={navItems}
      navSectionLabel="Strategic governance"
      brandSubtitle="Programme performance & strategic advising oversight"
      title={<ActiveNavTitle items={navItems} fallback={t("head.overview")} />}
      contextLabel="Fall 2026"
      purposeLabel="Purpose: cohort performance & strategic advising oversight"
      accountName="Prof. Vu Thi Kim Oanh"
      accountRole="Programme Head · Computing"
      accountInitials="VO"
      hasAlert
      footerNote={{
        icon: "account_balance",
        title: "Institutional registry",
        caption: "Audit log immutable · v4.18.0-rel",
      }}
    >
      {children}
    </AppShell>
  );
}
