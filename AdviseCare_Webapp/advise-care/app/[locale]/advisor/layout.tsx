import { getTranslations } from "next-intl/server";
import { AppShell } from "@/components/layout/app-shell";
import { ActiveNavTitle } from "@/components/layout/active-nav-title";
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
  const user = await requireRole(locale, [
    "Academic Advisor",
    "Wellbeing Advisor",
  ]);
  const navItems: NavItem[] = [
    { href: "/advisor/caseload", label: t("advisor.caseload"), icon: "view_list" },
    { href: "/advisor/cases", label: t("advisor.cases"), icon: "folder_open" },
    {
      href: "/advisor/consent-requests",
      label: t("advisor.consentRequests"),
      icon: "rule",
    },
    {
      href: "/advisor/corrections",
      label: t("advisor.corrections"),
      icon: "edit_note",
    },
    {
      href: "/advisor/referrals",
      label: t("advisor.referrals"),
      icon: "share",
    },
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
      brandSubtitle="AdviseCare"
      title={
        <ActiveNavTitle items={navItems} fallback={t("advisor.caseload")} />
      }
      accountName={user.fullName}
      accountRole={user.roles.map((role) => localizeRole(role, t)).join(", ")}
      accountInitials={getInitials(user.fullName)}
      roles={user.roles}
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
