import { getTranslations } from "next-intl/server";
import { AppShell } from "@/components/layout/app-shell";
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
    { href: "/head/matching", label: t("head.overview"), icon: "assignment_ind", match: ["/head"] },
    { href: "/head/assignment-changes", label: t("head.assignments"), icon: "swap_horiz" },
    { href: "/head/cases", label: t("head.allCases"), icon: "folder_open" },
    { href: "/head/advisor-workload", label: t("head.workload"), icon: "balance" },
    { href: "/head/reports", label: t("head.reports"), icon: "description" },
  ];

  return (
    <AppShell
      navItems={navItems}
      navSectionLabel={t("sectionHead")}
      accountName={user.fullName}
      accountRole={user.roles.map((role) => localizeRole(role, t)).join(", ")}
      accountInitials={getInitials(user.fullName)}
      roles={user.roles}
    >
      {children}
    </AppShell>
  );
}
