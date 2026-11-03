import { getTranslations } from "next-intl/server";
import { AppShell } from "@/components/layout/app-shell";
import type { NavItem } from "@/components/layout/sidebar-nav";
import { getInitials, requireRole } from "@/lib/auth";
import { localizeRole } from "@/lib/roles";

export const dynamic = "force-dynamic";

export default async function WelfareLayout({
  children,
  params,
}: LayoutProps<"/[locale]/welfare">) {
  const { locale } = await params;
  const t = await getTranslations("nav");
  const user = await requireRole(locale, ["Student Welfare Officer"]);
  const navItems: NavItem[] = [
    { href: "/welfare/referrals", label: t("welfare.referrals"), icon: "inbox" },
    { href: "/welfare/cases", label: t("welfare.cases"), icon: "payments" },
    { href: "/welfare/external-referrals", label: t("welfare.externalReferrals"), icon: "outbound" },
    { href: "/welfare/external-services", label: t("welfare.externalServices"), icon: "store" },
  ];

  return (
    <AppShell
      navItems={navItems}
      navSectionLabel={t("sectionWelfare")}
      accountName={user.fullName}
      accountRole={user.roles.map((role) => localizeRole(role, t)).join(", ")}
      accountInitials={getInitials(user.fullName)}
      roles={user.roles}
    >
      {children}
    </AppShell>
  );
}
