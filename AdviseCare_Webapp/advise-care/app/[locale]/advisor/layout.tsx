import { getTranslations } from "next-intl/server";
import { AppShell } from "@/components/layout/app-shell";
import { ActiveNavTitle } from "@/components/layout/active-nav-title";
import type { NavItem } from "@/components/layout/sidebar-nav";
import { apiGet } from "@/lib/api";
import type { Account, AcademicTerm } from "@/lib/types";

export default async function AdvisorLayout({ children }: LayoutProps<"/[locale]/advisor">) {
  const t = await getTranslations("nav");
  const tc = await getTranslations("common");
  const session = await apiGet<{ advisor: Account; term: AcademicTerm }>("/api/session");

  const navItems: NavItem[] = [
    { href: "/advisor/caseload", label: t("advisor.caseload"), icon: "view_list" },
    { href: "/advisor/cases", label: t("advisor.cases"), icon: "folder_open" },
    {
      href: "/advisor/consent-requests",
      label: t("advisor.consentRequests"),
      icon: "rule",
      badge: "dot",
    },
    { href: "/advisor/corrections", label: t("advisor.corrections"), icon: "edit_note" },
    { href: "/advisor/referrals", label: t("advisor.referrals"), icon: "share" },
    {
      href: "/advisor/emergency-access",
      label: t("advisor.emergencyAccess"),
      icon: "emergency",
    },
  ];

  return (
    <AppShell
      navItems={navItems}
      navSectionLabel={t("sectionAdvisor")}
      brandSubtitle={tc("facultySubtitle")}
      title={<ActiveNavTitle items={navItems} fallback={t("advisor.caseload")} />}
      contextLabel={session.term.Label}
      purposeLabel={`${tc("activePurpose")}: ${tc("academicAdvising")}`}
      accountName={session.advisor.FullName}
      accountRole={session.advisor.RoleLabel}
      accountInitials={session.advisor.Initials}
      hasAlert
      footerNote={{
        icon: "account_circle",
        title: t("advisorFooterTitle"),
        caption: t("advisorFooterCaption"),
      }}
    >
      {children}
    </AppShell>
  );
}
