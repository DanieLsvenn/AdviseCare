import { AppShell } from "@/components/layout/app-shell";
import { ActiveNavTitle } from "@/components/layout/active-nav-title";
import type { NavItem } from "@/components/layout/sidebar-nav";
import { apiGet } from "@/lib/api";
import type { Account, AcademicTerm } from "@/lib/types";

const navItems: NavItem[] = [
  {
    href: "/welfare/referrals",
    label: "Referrals (W01)",
    icon: "inbox",
    badge: 3,
  },
  {
    href: "/welfare/cases/REF-001",
    label: "Follow-up (W03 / W04)",
    icon: "checklist",
  },
  {
    href: "/welfare/statistics",
    label: "Statistics (W05)",
    icon: "query_stats",
  },
];

export default async function WelfareLayout({
  children,
}: LayoutProps<"/[locale]/welfare">) {
  const session = await apiGet<{ welfareOfficer: Account; term: AcademicTerm }>(
    "/api/session",
  );

  return (
    <AppShell
      navItems={navItems}
      navSectionLabel="Welfare Console"
      brandSubtitle="Academic Advising & Student Welfare"
      title={<ActiveNavTitle items={navItems} fallback="Referrals (W01)" />}
      contextLabel={session.term.Label}
      purposeLabel="PURPOSE: WELFARE SUPPORT (PURP-WELF-02)"
      accountName={session.welfareOfficer.FullName}
      accountRole={session.welfareOfficer.RoleLabel}
      accountInitials={session.welfareOfficer.Initials}
      accountStatus="Confidential & restricted"
      hasAlert
      footerNote={{
        icon: "verified_user",
        title: "Decree 13 / Invariant enforced",
        caption: "Consent-bound welfare workspace",
      }}
    >
      <div className="border border-primary/20 bg-primary-fixed/20 px-space-lg py-space-md">
        <div className="flex flex-wrap items-start gap-space-sm">
          <span className="material-symbols-outlined text-[20px] text-primary">
            policy
          </span>
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-space-sm">
              <p className="font-label-sm text-label-sm font-semibold uppercase tracking-wider text-primary">
                Statutory purpose segregation notice
              </p>
              <span className="font-code-audit text-code-audit bg-surface-container-high px-space-xs py-0.5 text-on-surface-variant">
                PURP-WELF-02
              </span>
              <span className="font-code-audit text-code-audit text-primary">
                CONS-082 active
              </span>
            </div>
            <p className="mt-space-xs max-w-5xl font-body-sm text-body-sm text-on-surface-variant">
              Welfare casework displays only attributes explicitly consented for
              welfare support. Academic diagnostics, detailed tuition records,
              and unconsented circumstances remain unavailable and zero-copied.
            </p>
          </div>
          <span className="font-code-audit text-code-audit text-on-surface-variant">
            AUDITED ENCLAVE
          </span>
        </div>
      </div>
      {children}
    </AppShell>
  );
}
