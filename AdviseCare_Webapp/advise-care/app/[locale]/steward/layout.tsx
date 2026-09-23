import { getTranslations } from "next-intl/server";
import { AppShell } from "@/components/layout/app-shell";
import { ActiveNavTitle } from "@/components/layout/active-nav-title";
import type { NavItem } from "@/components/layout/sidebar-nav";
import { apiGet } from "@/lib/api";
import type { Account, AcademicTerm } from "@/lib/types";

export default async function StewardLayout({
  children,
}: LayoutProps<"/[locale]/steward">) {
  const t = await getTranslations("nav");
  const session = await apiGet<{ steward: Account; term: AcademicTerm }>(
    "/api/session",
  );

  const navItems: NavItem[] = [
    {
      href: "/steward/purposes",
      label: t("steward.purposes"),
      icon: "checklist",
    },
    { href: "/steward/policies", label: t("steward.policies"), icon: "policy" },
    {
      href: "/steward/access-records",
      label: t("steward.accessRecords"),
      icon: "receipt_long",
    },
    {
      href: "/steward/erasure",
      label: t("steward.erasure"),
      icon: "delete_sweep",
    },
    {
      href: "/steward/emergency-reviews",
      label: t("steward.emergencyReviews"),
      icon: "gavel",
      badge: "dot",
    },
  ];

  return (
    <AppShell
      navItems={navItems}
      navSectionLabel={t("sectionSteward")}
      brandSubtitle="Institution-wide"
      title={
        <ActiveNavTitle items={navItems} fallback={t("steward.purposes")} />
      }
      contextLabel={session.term.Label}
      purposeLabel="Steward scope: metadata only"
      accountName={session.steward.FullName}
      accountRole={session.steward.RoleLabel}
      accountInitials={session.steward.Initials}
      hasAlert
      footerNote={{
        icon: "shield_person",
        title: t("stewardFooterTitle"),
        caption: t("stewardFooterCaption"),
      }}
    >
      {children}
    </AppShell>
  );
}
