import { getTranslations } from "next-intl/server";
import { AppShell } from "@/components/layout/app-shell";
import type { NavItem } from "@/components/layout/sidebar-nav";
import { getInitials, requireRole } from "@/lib/auth";
import { localizeRole } from "@/lib/roles";

export const dynamic = "force-dynamic";

export default async function AdvisorLayout({
  children,
  params,
}: LayoutProps<"/[locale]/advisor">) {
  const { locale } = await params;
  const t = await getTranslations("nav");
  const user = await requireRole(locale, ["Academic Advisor", "Wellbeing Advisor"]);
  const navItems: NavItem[] = [
    { href: "/advisor/caseload", label: t("advisor.myCases"), icon: "folder_open", match: ["/advisor/cases", "/advisor/students"] },
    { href: "/advisor/referrals", label: t("advisor.referrals"), icon: "forward_to_inbox" },
    { href: "/advisor/consent-requests", label: t("advisor.consentRequests"), icon: "rule" },
    { href: "/advisor/corrections", label: t("advisor.corrections"), icon: "edit_note" },
    { href: "/advisor/emergency-access", label: t("advisor.emergencyAccess"), icon: "emergency" },
  ];

  return (
    <AppShell
      navItems={navItems}
      navSectionLabel={t("sectionAdvisor")}
      accountName={user.fullName}
      accountRole={user.roles.map((role) => localizeRole(role, t)).join(", ")}
      accountInitials={getInitials(user.fullName)}
      roles={user.roles}
    >
      {children}
    </AppShell>
  );
}
