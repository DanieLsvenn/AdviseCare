import { getTranslations, setRequestLocale } from "next-intl/server";
import {
  AuditFooter,
  Avatar,
  Button,
  ButtonLink,
  Icon,
  ImmutableRecord,
  KeyValue,
  KeyValueGrid,
  Notice,
  Panel,
  PanelBody,
  PanelFooter,
  PanelHeader,
  StatusChip,
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type {
  AccessRequest,
  ConsentGrant,
  LedgerEntry,
  ModuleRecord,
  Student,
} from "@/lib/types";

/** S01 — My profile (student self-advocacy view). */
export default async function StudentProfilePage({
  params,
}: PageProps<"/[locale]/me/profile">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("student.profile");
  const tc = await getTranslations("common");

  const [detail, ledger, requests] = await Promise.all([
    apiGet<{ student: Student; modules: ModuleRecord[]; grants: ConsentGrant[] }>(
      "/api/students/1",
    ),
    apiGet<{ entries: LedgerEntry[] }>("/api/ledger?studentId=1"),
    apiGet<{ requests: AccessRequest[] }>("/api/access-requests"),
  ]);

  const pending = requests.requests.filter(
    (request) => request.StudentId === 1 && request.State === "PENDING",
  );
  const studentVisible = ledger.entries.filter(
    (entry) => entry.Visibility === "STUDENT_VISIBLE",
  );
  const activeGrants = detail.grants.filter((grant) => grant.State !== "WITHDRAWN");

  const agencyActions = [
    { href: "/me/consent", icon: "tune", label: t("consentScopes"), code: "S02" },
    { href: "/me/access-history", icon: "history", label: t("accessLog"), code: "S06" },
    { href: "/me/corrections", icon: "build", label: t("fixRecord"), code: "S07" },
    { href: "/me/export", icon: "download", label: t("officialExport"), code: "S08" },
    { href: "/me/events", icon: "calendar_month", label: t("sessions"), code: "S09" },
  ] as const;

  return (
    <>
      {pending.length > 0 ? (
        <Notice
          tone="referral"
          icon="shield_person"
          title={`${t("noticeTitle")} · ${pending[0].RequestCode}`}
          actions={
            <div className="flex flex-wrap gap-space-sm">
              <Button variant="tertiary" size="sm">
                {t("dismiss")}
              </Button>
              <ButtonLink
                href={`/me/consent/${pending[0].AccessRequestId}`}
                size="sm"
                iconAfter="arrow_forward"
              >
                {t("reviewRequest")} ({pending[0].RequestCode})
              </ButtonLink>
            </div>
          }
        >
          {t("noticeBody", { count: pending.length })}
        </Notice>
      ) : null}

      <Panel>
        <PanelBody>
          <div className="flex flex-wrap items-start gap-space-lg">
            <Avatar initials={detail.student.Initials} className="size-16 text-headline-md" />
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-space-sm">
                <h1 className="font-display-sm text-display-sm text-on-surface">
                  {detail.student.FullName}
                </h1>
                <span className="rounded border border-outline-variant/50 bg-surface-container-low px-space-sm py-0.5 font-code-audit text-code-audit text-on-surface-variant">
                  DEMO-001
                </span>
                <span className="rounded border border-outline-variant/50 bg-surface-container-low px-space-sm py-0.5 font-code-audit text-code-audit text-on-surface-variant">
                  {detail.student.StudentCode}
                </span>
              </div>
              <div className="mt-space-sm flex flex-wrap items-center gap-space-sm">
                <StatusChip tone="ontrack" icon="check_circle" label="Active enrolment" />
                <StatusChip tone="referral" icon="rule" label={detail.student.EnrolmentTrack} />
              </div>
              <p className="mt-space-sm font-body-base text-body-base text-on-surface-variant">
                {detail.student.FacultyName} · {detail.student.ProgrammeName}
              </p>
              <p className="font-body-sm text-body-sm text-on-surface-variant">
                Year {detail.student.YearOfStudy} ({detail.student.CohortLabel}) · Term 1 — 2024/2025
              </p>
              <ul className="mt-space-base space-y-space-xs font-body-sm text-body-sm text-on-surface-variant">
                {[
                  { icon: "verified_user", label: t("identityVerified") },
                  { icon: "lock", label: t("privacyStandard") },
                  { icon: "sync", label: `${t("registrySynced")}: today 08:30 ICT` },
                ].map((item) => (
                  <li key={item.label} className="flex items-center gap-1.5">
                    <Icon name={item.icon} className="text-[14px]" />
                    {item.label}
                  </li>
                ))}
              </ul>
            </div>

            <div className="w-full max-w-sm rounded-md border border-outline-variant/40 bg-surface-container-low/40 p-space-base">
              <p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                {t("agencyActions")}
              </p>
              <ul className="mt-space-sm grid gap-space-xs sm:grid-cols-2">
                {agencyActions.map((action) => (
                  <li key={action.href}>
                    <ButtonLink
                      href={action.href}
                      variant="secondary"
                      size="sm"
                      icon={action.icon}
                      className="w-full justify-start"
                    >
                      {action.label}
                    </ButtonLink>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </PanelBody>
      </Panel>

      <Notice tone="sealed" icon="balance" title={t("milestoneTitle")}>
        <p>{t("milestoneBody")}</p>
        <p className="mt-space-sm font-code-audit text-code-audit">ISO-27701 autonomous</p>
      </Notice>

      <div className="grid gap-space-lg xl:grid-cols-2">
        <Panel>
          <PanelHeader
            icon="school"
            title={t("academicTitle")}
            subtitle="DOMAIN-REF: SIS-ACAD-DEGREE-03"
            actions={<StatusChip tone="ontrack" icon="verified" label={t("verifiedStanding")} />}
          />
          <PanelBody className="space-y-space-base">
            <div className="grid gap-space-md sm:grid-cols-2">
              <div className="rounded-md border border-outline-variant/40 px-space-base py-space-md">
                <p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                  {t("gpa")}
                </p>
                <p className="mt-1 font-display-sm text-display-sm text-on-surface">
                  2.74 <span className="font-body-sm text-body-sm text-on-surface-variant">/ 4.00</span>
                </p>
                <p className="font-body-sm text-body-sm text-on-surface-variant">
                  Standard academic standing
                </p>
              </div>
              <div className="rounded-md border border-outline-variant/40 px-space-base py-space-md">
                <p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                  {t("credits")}
                </p>
                <p className="mt-1 font-display-sm text-display-sm text-on-surface">
                  68 <span className="font-body-sm text-body-sm text-on-surface-variant">/ 120 ECTS</span>
                </p>
                <span className="mt-space-sm block h-1.5 w-full overflow-hidden rounded-full bg-surface-container-high">
                  <span className="block h-full w-[57%] bg-primary" />
                </span>
              </div>
            </div>

            <div>
              <h3 className="font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
                {t("currentModules")}
              </h3>
              <ul className="mt-space-sm space-y-space-sm">
                {detail.modules.map((module) => (
                  <li
                    key={module.CourseCode}
                    className="rounded-md border border-outline-variant/40 px-space-base py-space-md"
                  >
                    <div className="flex flex-wrap items-start justify-between gap-space-sm">
                      <div className="min-w-0">
                        <p className="font-code-audit text-code-audit text-on-surface-variant">
                          {module.CourseCode}
                        </p>
                        <p className="font-label-md text-label-md font-semibold text-on-surface">
                          {module.CourseName}
                        </p>
                      </div>
                      <StatusChip tone={module.Tone} label={module.StatusLabel} />
                    </div>
                    <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                      {module.Detail}
                    </p>
                    <div className="mt-space-sm flex flex-wrap items-center justify-between gap-space-sm border-t border-outline-variant/25 pt-space-sm">
                      <span className="font-body-sm text-body-sm text-on-surface-variant">
                        {t("facultyLead")}: {module.FacultyLead}
                      </span>
                      <span className="font-body-sm text-body-sm text-primary">
                        {t("viewSyllabus")}
                      </span>
                    </div>
                  </li>
                ))}
              </ul>
            </div>
          </PanelBody>
          <PanelFooter>
            <span>Source: Student Information System (SIS v3.2) · synced today 08:30 ICT</span>
            <StatusChip tone="ontrack" icon="check_circle" label="100% verified complete" />
          </PanelFooter>
        </Panel>

        <Panel>
          <PanelHeader
            icon="how_to_reg"
            title={t("attendanceTitle")}
            subtitle="SENSOR-FEED: CAM-GATE-09-VERIFIED"
            actions={<StatusChip tone="ontrack" icon="verified" label="90.0% verified regular" />}
          />
          <PanelBody className="space-y-space-base">
            <div className="flex flex-wrap items-center justify-between gap-space-base rounded-md border border-outline-variant/40 px-space-base py-space-md">
              <div>
                <p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                  {t("labRatio")}
                </p>
                <p className="mt-1 font-display-sm text-display-sm text-on-surface">90.0%</p>
                <p className="font-body-sm text-body-sm text-on-surface-variant">
                  {t("sessionsValidated", { present: 18, total: 20 })}
                </p>
              </div>
              <div
                aria-hidden
                className="flex size-16 items-center justify-center rounded-full border-4 border-secondary font-label-md text-label-md font-semibold text-primary"
              >
                90%
              </div>
            </div>

            <Notice tone="ontrack" icon="check_circle" title={t("correctionRatified")}>
              A manual tap desynchronisation on the 2 Sep CS301 lab was resolved by addendum
              #CORR-20260907 on 7 Sep 2026, ratified by the Faculty Registrar. The session is
              counted as attended and the original record is preserved.
            </Notice>

            <div className="flex flex-wrap gap-space-sm">
              <ButtonLink href="/me/access-history" variant="secondary" size="sm" icon="insights">
                {t("viewCalculation")}
              </ButtonLink>
              <ButtonLink href="/me/corrections/new" variant="secondary" size="sm" icon="gavel">
                {t("contest")}
              </ButtonLink>
            </div>
            <p className="font-code-audit text-code-audit text-on-surface-variant">
              Source: Campus turnstile RFID · reconciled 7 Sep, 11:15 ICT · GATE-ID-09
            </p>
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader
            icon="laptop_mac"
            title={t("participationTitle")}
            subtitle="VLE-TELEMETRY: CANVAS-LMS-2024"
            actions={<StatusChip tone="ontrack" icon="wifi" label="Active weekly engagement" />}
          />
          <PanelBody className="space-y-space-base">
            <KeyValueGrid columns={3}>
              <KeyValue label={t("vleStatus")} value="Active today — last login 14:15 ICT" />
              <KeyValue label={t("forumContributions")} value="4 threads (past 30 days)" />
              <KeyValue label={t("onTimeSubmissions")} value="100% — 5 of 5 milestones" />
            </KeyValueGrid>
            <Notice tone="sealed" icon="shield" title={t("telemetryTitle")}>
              {t("telemetryBody")}
            </Notice>
          </PanelBody>
          <PanelFooter>
            <span>Source: Canvas VLE telemetry engine · synced 2 hours ago</span>
            <span className="font-code-audit text-code-audit">Audit: VLE-HASH-883</span>
          </PanelFooter>
        </Panel>

        <Panel>
          <PanelHeader
            icon="shield_person"
            title={t("circumstancesTitle")}
            subtitle="PRIVACY-GATEWAY: CONS-SCOPES-V1"
            actions={
              <ButtonLink href="/me/consent" variant="secondary" size="sm" icon="tune">
                {t("manageScopes")}
              </ButtonLink>
            }
          />
          <PanelBody className="space-y-space-base">
            <Notice tone="sealed" icon="policy" title={t("agencyRule")}>
              {t("agencyRuleBody")}
            </Notice>

            {activeGrants.map((grant) => (
              <div
                key={grant.ConsentGrantId}
                className="border-l-[3px] border-l-secondary border-y border-r border-outline-variant/40 px-space-base py-space-md"
              >
                <div className="flex flex-wrap items-center justify-between gap-space-sm">
                  <p className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
                    <Icon name="lock" className="text-[16px]" />
                    {grant.ScopeLabel}
                  </p>
                  <StatusChip
                    tone={grant.State === "GRANTED" ? "ontrack" : "referral"}
                    icon={grant.State === "GRANTED" ? "check_circle" : "hourglass_top"}
                    label={grant.State === "GRANTED" ? tc("granted") : t("pendingDecision")}
                  />
                </div>
                <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
                  {grant.AttributeCodes.join(", ")}
                </p>
                <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                  {t("sharedWith")}:{" "}
                  <strong className="font-semibold text-on-surface">
                    {grant.SharedWith.join(" & ")}
                  </strong>
                </p>
                <div className="mt-space-sm flex flex-wrap items-center justify-between gap-space-sm">
                  <span className="font-body-sm text-body-sm text-on-surface-variant">
                    {t("validUntil")}: {grant.ExpiresAt ?? "—"}
                  </span>
                  <ButtonLink
                    href={`/me/consent/${grant.ConsentGrantId}`}
                    variant="tertiary"
                    size="sm"
                  >
                    {t("revokeOrUpdate")}
                  </ButtonLink>
                </div>
              </div>
            ))}

            <div className="rounded-md border border-outline-variant/40 bg-surface-container-low/40 px-space-base py-space-md">
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <p className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
                  <Icon name="medical_services" className="text-[16px]" />
                  Medical & clinical records
                </p>
                <StatusChip tone="sealed" icon="do_not_disturb_on" label={t("notShared")} />
              </div>
              <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                University medical and counselling records are private by default and have not been
                authorised for academic advising staff.
              </p>
            </div>
          </PanelBody>
          <PanelFooter>
            <span>Governed by Vietnam Personal Data Protection Decree (PDPD 13/2023)</span>
            <StatusChip tone="ontrack" icon="lock" label={`${activeGrants.length} shared scopes`} />
          </PanelFooter>
        </Panel>
      </div>

      <Panel>
        <PanelHeader
          icon="history_edu"
          title={t("ledgerTitle")}
          subtitle={t("ledgerSubtitle")}
          actions={
            <ButtonLink href="/me/corrections/new" variant="secondary" size="sm" icon="edit_note">
              {t("requestAddendum")}
            </ButtonLink>
          }
        />
        <PanelBody className="space-y-space-md">
          <Notice tone="sealed" icon="lock" title={tc("appendOnlyTitle")}>
            {tc("appendOnlyBody")}
          </Notice>
          {studentVisible.map((entry) => (
            <ImmutableRecord
              key={entry.LedgerEntryId}
              title={entry.Title}
              typeLabel={entry.EntryCode}
              author={`${entry.AuthorName} (${entry.AuthorRoleLabel})`}
              timestamp={entry.RecordedAt}
              hash={entry.ContentHash}
              supersedes={entry.SupersedesEntryCode}
              footer={
                <ButtonLink href="/me/corrections/new" variant="tertiary" size="sm">
                  {t("fileComment")}
                </ButtonLink>
              }
            >
              <p>{entry.BodyText}</p>
            </ImmutableRecord>
          ))}
        </PanelBody>
      </Panel>

      <AuditFooter
        message={tc("auditMessage")}
        sessionHash="#STU-2024-001"
        timestamp="2026-09-20 08:30 ICT"
      />
    </>
  );
}
