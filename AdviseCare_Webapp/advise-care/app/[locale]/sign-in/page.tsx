import { getTranslations, setRequestLocale } from "next-intl/server";
import { Brand } from "@/components/layout/brand";
import {
  ButtonLink,
  Icon,
  KeyValue,
  LanguageSwitcher,
  Notice,
  Panel,
  StatusChip,
} from "@/components/ui";
import { Link } from "@/i18n/navigation";

/** C01 — University sign-in. */
export default async function SignInPage({
  params,
}: PageProps<"/[locale]/sign-in">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("auth");
  const tc = await getTranslations("common");

  const demoRoles = [
    {
      href: "/advisor/caseload",
      icon: "supervisor_account",
      title: t("roleAdvisor"),
      caption: t("roleAdvisorCaption"),
    },
    {
      href: "/me/profile",
      icon: "person",
      title: t("roleStudent"),
      caption: t("roleStudentCaption"),
    },
    {
      href: "/steward/purposes",
      icon: "policy",
      title: t("roleSteward"),
      caption: t("roleStewardCaption"),
    },
    {
      href: "/admin/m01",
      icon: "admin_panel_settings",
      title: "System Administrator",
      caption: "Institutional root trust • M01–M07 administration console",
    },
    {
      href: "/welfare/referrals",
      icon: "health_and_safety",
      title: "Student Welfare Officer",
      caption: "Demo Welfare Officer 01 • Confidential casework console",
    },
    {
      href: "/head",
      icon: "account_balance",
      title: t("roleHead"),
      caption: t("roleHeadCaption"),
    },
  ] as const;

  return (
    <div className="flex min-h-screen flex-col">
      <header className="flex h-header-height items-center justify-between border-b border-outline-variant/30 bg-surface-container-lowest px-gutter-mobile lg:px-gutter-desktop">
        <Brand subtitle={tc("facultySubtitle")} />
        <LanguageSwitcher />
      </header>

      <main className="flex flex-1 items-start justify-center px-gutter-mobile py-space-2xl lg:px-gutter-desktop">
        <div className="grid w-full max-w-5xl gap-space-lg lg:grid-cols-[minmax(0,1fr)_minmax(0,22rem)] lg:items-start">
          <Panel className="p-space-xl">
            <h1 className="font-display-sm text-display-sm text-on-surface">
              {t("title")}
            </h1>
            <p className="mt-space-sm max-w-xl font-body-base text-body-base text-on-surface-variant">
              {t("subtitle")}
            </p>

            <div className="mt-space-xl rounded-md border border-outline-variant/40 bg-surface-container-low/50 p-space-lg">
              <ButtonLink
                href="/advisor/caseload"
                icon="shield_person"
                className="w-full"
              >
                {t("ssoButton")}
              </ButtonLink>
              <p className="mt-space-md font-body-sm text-body-sm text-on-surface-variant">
                {t("ssoHint")}
              </p>
              <dl className="mt-space-base border-t border-outline-variant/30 pt-space-md">
                <KeyValue
                  label={t("identityProvider")}
                  value={t("identityProviderValue")}
                />
              </dl>
            </div>

            <div className="mt-space-xl">
              <div className="flex items-center gap-space-md">
                <span className="h-px flex-1 bg-outline-variant/40" />
                <span className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                  {t("orDivider")}
                </span>
                <span className="h-px flex-1 bg-outline-variant/40" />
              </div>

              <p className="mt-space-md font-body-sm text-body-sm text-on-surface-variant">
                {t("demoHint")}
              </p>

              <ul className="mt-space-base space-y-space-sm">
                {demoRoles.map((role) => (
                  <li key={role.href}>
                    <Link
                      href={role.href}
                      className="flex items-center gap-space-md rounded-md border border-outline-variant/40 bg-surface-container-lowest px-space-base py-space-md transition-colors hover:border-outline-variant hover:bg-surface-container-low"
                    >
                      <span className="flex size-9 shrink-0 items-center justify-center rounded bg-surface-container-high text-primary">
                        <Icon name={role.icon} className="text-[20px]" />
                      </span>
                      <span className="min-w-0 flex-1">
                        <span className="block font-label-md text-label-md font-semibold text-on-surface">
                          {role.title}
                        </span>
                        <span className="block font-body-sm text-body-sm text-on-surface-variant">
                          {role.caption}
                        </span>
                      </span>
                      <Icon
                        name="arrow_forward"
                        className="text-[18px] text-on-surface-variant"
                      />
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          </Panel>

          <div className="flex flex-col gap-space-lg">
            <Notice tone="sealed" icon="gpp_good" title={t("noticeTitle")}>
              {t("noticeBody")}
            </Notice>
            <Panel className="p-space-lg">
              <StatusChip tone="sealed" icon="gavel" label="PDPD 13/2023" />
              <p className="mt-space-md font-body-sm text-body-sm text-on-surface-variant">
                {t("legalLine")}
              </p>
            </Panel>
          </div>
        </div>
      </main>
    </div>
  );
}
