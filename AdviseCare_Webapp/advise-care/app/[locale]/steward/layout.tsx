import { getTranslations } from "next-intl/server";
import { AppShell } from "@/components/layout/app-shell";
import { ActiveNavTitle } from "@/components/layout/active-nav-title";
import type { NavItem } from "@/components/layout/sidebar-nav";
import { getInitials, requireRole } from "@/lib/auth";
import { localizeRole } from "@/lib/roles";

export const dynamic = "force-dynamic";

export default async function StewardLayout({
  children,
  params,
}: LayoutProps<"/[locale]/steward">) {
  const { locale } = await params;
  const t = await getTranslations("nav");
  const user = await requireRole(locale, ["Data Protection Steward"]);
  const navItems: NavItem[] = [
    { href: "/steward/purposes", label: t("steward.purposes"), icon: "checklist" },
    { href: "/steward/policies", label: t("steward.policies"), icon: "policy" },
    {
      href: "/steward/attributes",
      label: t("steward.attributes"),
      icon: "dataset",
    },
    {
      href: "/steward/consents",
      label: t("steward.consents"),
      icon: "history",
    },
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
    },
    { href: "/steward/keys", label: t("steward.keys"), icon: "key" },
  ];

  return (
    <AppShell
      navItems={navItems}
      navSectionLabel={t("sectionSteward")}
      brandSubtitle="AdviseCare data protection"
      title={
        <ActiveNavTitle items={navItems} fallback={t("steward.purposes")} />
      }
      accountName={user.fullName}
      accountRole={user.roles.map((role) => localizeRole(role, t)).join(", ")}
      accountInitials={getInitials(user.fullName)}
      roles={user.roles}
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
