import { getTranslations } from "next-intl/server";
import { AppShell } from "@/components/layout/app-shell";
import type { NavItem } from "@/components/layout/sidebar-nav";
import { getInitials, requireRole } from "@/lib/auth";
import { localizeRole } from "@/lib/roles";

export const dynamic = "force-dynamic";

export default async function StudentLayout({
  children,
  params,
}: LayoutProps<"/[locale]/me">) {
  const { locale } = await params;
  const t = await getTranslations("nav");
  const user = await requireRole(locale, ["Student"]);
  const navItems: NavItem[] = [
    { href: "/me/cases", label: t("student.cases"), icon: "folder_open", match: ["/me/support-request"] },
    { href: "/me/consent", label: t("student.consent"), icon: "verified_user" },
    { href: "/me/access-history", label: t("student.accessHistory"), icon: "history" },
    { href: "/me/corrections", label: t("student.corrections"), icon: "edit_note" },
    { href: "/me/profile", label: t("student.profile"), icon: "account_circle" },
    { href: "/me/export", label: t("student.export"), icon: "download" },
  ];

  return (
    <AppShell
      variant="student"
      navItems={navItems}
      navSectionLabel={t("sectionStudent")}
      accountName={user.fullName}
      accountRole={user.roles.map((role) => localizeRole(role, t)).join(", ")}
      accountInitials={getInitials(user.fullName)}
      roles={user.roles}
    >
      {children}
    </AppShell>
  );
}
