import { getTranslations } from "next-intl/server";
import { AppShell } from "@/components/layout/app-shell";
import { ActiveNavTitle } from "@/components/layout/active-nav-title";
import type { NavItem } from "@/components/layout/sidebar-nav";
import { apiGet } from "@/lib/api";
import type { Account } from "@/lib/types";

export default async function StudentLayout({ children }: LayoutProps<"/[locale]/me">) {
  const t = await getTranslations("nav");
  const tc = await getTranslations("common");
  const session = await apiGet<{ student: Account }>("/api/session");

  const navItems: NavItem[] = [
    { href: "/me/profile", label: t("student.profile"), icon: "account_circle" },
    { href: "/me/consent", label: t("student.consent"), icon: "verified_user", badge: "dot" },
    { href: "/me/access-history", label: t("student.accessHistory"), icon: "history" },
    { href: "/me/corrections", label: t("student.corrections"), icon: "edit_note" },
    { href: "/me/export", label: t("student.export"), icon: "download" },
    { href: "/me/events", label: t("student.events"), icon: "calendar_month" },
  ];

  return (
    <AppShell
      variant="student"
      navItems={navItems}
      navSectionLabel={t("sectionStudent")}
      brandSubtitle={tc("studentPortal")}
      title={<ActiveNavTitle items={navItems} fallback={t("student.profile")} />}
      contextLabel="Academic Year 2024–2025"
      purposeLabel="Autonomous privacy active"
      accountName={session.student.FullName}
      accountRole="DEMO-001"
      accountInitials={session.student.Initials}
      hasAlert
      footerNote={{
        icon: "lock",
        title: t("studentFooterTitle"),
        caption: t("studentFooterCaption"),
      }}
    >
      {children}
    </AppShell>
  );
}
