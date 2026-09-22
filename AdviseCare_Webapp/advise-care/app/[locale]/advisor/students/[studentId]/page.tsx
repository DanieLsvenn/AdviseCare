import { notFound } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { EvidenceSection } from "@/components/advisor/evidence-section";
import { InspectorLayout } from "@/components/layout/app-shell";
import {
  AuditFooter,
  Avatar,
  ButtonLink,
  Icon,
  KeyValue,
  KeyValueGrid,
  Notice,
  Panel,
  PanelBody,
  PanelHeader,
  StatusChip,
} from "@/components/ui";
import { Link } from "@/i18n/navigation";
import { apiGet } from "@/lib/api";
import type { LedgerBlock, ProfileSection } from "@/lib/mock/profile";
import type { AdvisingCase, ConsentGrant, Student } from "@/lib/types";

/** A02 — Student profile (evidence view under the active purpose). */
export default async function StudentProfilePage({
  params,
}: PageProps<"/[locale]/advisor/students/[studentId]">) {
  const { locale, studentId } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.profile");
  const tc = await getTranslations("common");

  const [detail, caseData] = await Promise.all([
    apiGet<{
      student: Student;
      sections: ProfileSection[];
      ledgerBlocks: LedgerBlock[];
      grants: ConsentGrant[];
    }>(`/api/students/${studentId}`).catch(() => null),
    apiGet<{ cases: AdvisingCase[] }>("/api/cases"),
  ]);

  if (!detail) notFound();

  const { student, sections, ledgerBlocks } = detail;
  const activeCase = caseData.cases.find(
    (item) => item.StudentId === student.StudentId && item.ClosedAt === null,
  );

  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-space-md">
        <Link
          href="/advisor/caseload"
          className="flex items-center gap-1.5 font-label-md text-label-md text-on-surface-variant hover:text-on-surface"
        >
          <Icon name="arrow_back" className="text-[18px]" />
          {t("backToCaseload")}
        </Link>
        <div className="flex flex-wrap items-center gap-space-sm">
          <StatusChip
            tone="sealed"
            icon="schedule"
            label="7 Sep 2026, 14:15 ICT"
          />
          <StatusChip
            tone="ontrack"
            icon="check_circle"
            label={t("currentRecord")}
          />
          <StatusChip
            tone="sealed"
            icon="verified_user"
            label={`${tc("activePurpose")}: ${tc("academicAdvising")}`}
          />
        </div>
      </div>

      <Panel>
        <PanelBody className="space-y-space-base">
          <div className="flex flex-wrap items-start gap-space-base">
            <Avatar
              initials={student.Initials}
              className="size-14 text-headline-sm"
            />
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-space-sm">
                <h1 className="font-display-sm text-display-sm text-on-surface">
                  {student.FullName}
                </h1>
                <span className="rounded border border-outline-variant/50 bg-surface-container-low px-space-sm py-0.5 font-code-audit text-code-audit text-on-surface-variant">
                  DEMO-001
                </span>
                <span className="rounded border border-outline-variant/50 bg-surface-container-low px-space-sm py-0.5 font-code-audit text-code-audit text-on-surface-variant">
                  {student.StudentCode}
                </span>
              </div>
              <p className="mt-space-sm font-body-base text-body-base text-on-surface-variant">
                {student.ProgrammeName} · Year {student.YearOfStudy}, Fall 2026
              </p>
              <KeyValueGrid columns={3} className="mt-space-base">
                <KeyValue
                  label={t("assignedAdvisor")}
                  value="Demo Advisor 01 (Computing)"
                />
                <KeyValue
                  label={t("institutionalStatus")}
                  value={
                    <span className="flex items-center gap-1.5">
                      <Icon
                        name="warning"
                        className="text-[16px] text-status-attention-text"
                      />
                      Registered / pre-withdrawal review
                    </span>
                  }
                />
                <KeyValue label={t("termWeek")} value="03 of 16" />
              </KeyValueGrid>
            </div>
            <div className="flex flex-col gap-space-sm">
              {activeCase ? (
                <ButtonLink
                  href={`/advisor/cases/${activeCase.CaseId}`}
                  icon="folder_open"
                  size="sm"
                >
                  {t("openCase")} ({activeCase.CaseNumber})
                </ButtonLink>
              ) : null}
              <ButtonLink
                href={`/advisor/referrals/new?student=${student.StudentId}`}
                variant="secondary"
                size="sm"
                icon="arrow_outward"
              >
                {t("newReferral")}
              </ButtonLink>
              <ButtonLink
                href={`/advisor/students/${student.StudentId}/historical`}
                variant="secondary"
                size="sm"
                icon="history"
              >
                {t("viewHistory")}
              </ButtonLink>
            </div>
          </div>
        </PanelBody>
      </Panel>

      <Notice tone="sealed" icon="lock" title={t("auditProtocolTitle")}>
        <p>{t("auditProtocolBody", { scope: "#AUD-2026-FOC-9042" })}</p>
        <p className="mt-space-sm font-code-audit text-code-audit">
          ISO/IEC 27001 certified advisory store
        </p>
      </Notice>

      <InspectorLayout
        inspector={
          <>
            {activeCase ? (
              <Panel>
                <PanelHeader
                  icon="folder_special"
                  title={t("activeCase")}
                  subtitle={activeCase.Title}
                />
                <PanelBody>
                  <KeyValueGrid columns={1}>
                    <KeyValue label={t("opened")} value={activeCase.OpenedAt} />
                    <KeyValue
                      label={t("stage")}
                      value={activeCase.CurrentState}
                    />
                    <KeyValue
                      label={t("urgency")}
                      value={
                        <StatusChip
                          tone={
                            activeCase.Priority === "URGENT"
                              ? "urgent"
                              : "attention"
                          }
                          icon="flag"
                          label={activeCase.Priority}
                        />
                      }
                    />
                    <KeyValue
                      label={t("assignedLead")}
                      value={activeCase.AdvisorName}
                    />
                  </KeyValueGrid>
                  <ButtonLink
                    href={`/advisor/cases/${activeCase.CaseId}`}
                    icon="folder_open"
                    size="sm"
                    className="mt-space-base w-full"
                  >
                    {t("openCase")}
                  </ButtonLink>
                </PanelBody>
              </Panel>
            ) : null}

            <Panel>
              <PanelHeader
                icon="lock"
                title={tc("appendOnlyTitle")}
                subtitle={tc("appendOnlyBody")}
              />
            </Panel>
          </>
        }
      >
        <div className="flex flex-col gap-space-lg">
          {sections.map((section) => (
            <EvidenceSection key={section.id} section={section} />
          ))}

          <Panel>
            <PanelHeader
              icon="history_edu"
              title={t("ledgerTitle")}
              subtitle={t("ledgerSubtitle")}
              actions={
                <StatusChip
                  tone="sealed"
                  icon="lock"
                  label={t("appendOnlyChip")}
                />
              }
            />
            <PanelBody className="space-y-space-md">
              {ledgerBlocks.map((block) => (
                <article
                  key={block.blockRef}
                  className="border-l-[3px] border-l-outline-variant border-y border-r border-outline-variant/40 px-space-base py-space-md"
                >
                  <div className="flex flex-wrap items-center gap-space-sm">
                    <Icon
                      name={block.icon}
                      className="text-[18px] text-on-surface-variant"
                    />
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      {block.typeLabel}
                    </p>
                    <time className="ml-auto font-code-audit text-code-audit text-on-surface-variant">
                      {block.timestamp}
                    </time>
                  </div>
                  <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                    {tc("author")}: {block.author}
                  </p>
                  <p className="mt-space-sm font-body-base text-body-base text-on-surface">
                    {block.body}
                  </p>
                  <div className="mt-space-md flex flex-wrap items-center justify-between gap-space-sm border-t border-outline-variant/25 pt-space-sm">
                    <span className="flex items-center gap-1.5 font-body-sm text-body-sm text-on-surface-variant">
                      <Icon name="verified" className="text-[14px]" />
                      {block.sealNote}
                    </span>
                    <span className="font-code-audit text-code-audit text-on-surface-variant">
                      Block: {block.blockRef}
                    </span>
                  </div>
                </article>
              ))}
            </PanelBody>
            <div className="flex flex-wrap items-center justify-between gap-space-sm border-t border-outline-variant/30 px-space-lg py-space-md">
              <ButtonLink
                href="/advisor/caseload"
                variant="tertiary"
                size="sm"
                icon="arrow_back"
              >
                {t("backToCaseload")}
              </ButtonLink>
              <div className="flex flex-wrap gap-space-sm">
                <ButtonLink
                  href={`/advisor/cases/${activeCase?.CaseId ?? 1}/notes/new`}
                  variant="secondary"
                  size="sm"
                  icon="post_add"
                >
                  {t("logSupersedingNote")}
                </ButtonLink>
                <ButtonLink
                  href={`/advisor/students/${student.StudentId}/historical`}
                  size="sm"
                  icon="manage_search"
                >
                  {t("viewFullHistory")}
                </ButtonLink>
              </div>
            </div>
          </Panel>
        </div>
      </InspectorLayout>

      <AuditFooter
        message={tc("auditMessage")}
        sessionHash="#AUD-2026-FOC-9042"
        timestamp="2026-09-07 14:15 ICT"
      />
    </>
  );
}
