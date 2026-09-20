import { getTranslations, setRequestLocale } from "next-intl/server";
import { EvidenceSection } from "@/components/advisor/evidence-section";
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
import { apiGet } from "@/lib/api";
import type { ProfileSection } from "@/lib/mock/profile";
import type { ConsentGrant, Student } from "@/lib/types";

/**
 * A09 — Current profile after the student withdrew consent.
 * Cùng một hồ sơ, nhưng mục hoàn cảnh biến mất hoàn toàn thay vì hiện giá trị
 * bị che: đó là điểm khác biệt giữa "ẩn" và "không có quyền".
 */
export default async function PostWithdrawalProfilePage({
  params,
}: PageProps<"/[locale]/advisor/students/[studentId]/current">) {
  const { locale, studentId } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.postWithdrawal");
  const tp = await getTranslations("advisor.profile");
  const tc = await getTranslations("common");

  const detail = await apiGet<{
    student: Student;
    sections: ProfileSection[];
    grants: ConsentGrant[];
  }>(`/api/students/${studentId}`);

  const visibleSections = detail.sections.filter(
    (section) => section.id !== "circumstance",
  );
  const withdrawnGrant = detail.grants.find((grant) => grant.State === "WITHDRAWN");

  return (
    <>
      <Notice tone="sealed" icon="verified" title={tp("auditProtocolTitle")}>
        <p>{tp("auditProtocolBody", { scope: "#AUD-2026-FOC-9042" })}</p>
        <p className="mt-space-sm">
          <StatusChip tone="ontrack" icon="cloud_done" label={t("ledgerSynchronized")} />
        </p>
      </Notice>

      <Notice
        tone="urgent"
        icon="gpp_bad"
        title={t("stateTitle")}
        actions={
          <ButtonLink
            href={`/advisor/students/${studentId}/historical/erased`}
            size="sm"
            iconAfter="arrow_forward"
          >
            {t("verifyReceipts")}
          </ButtonLink>
        }
      >
        {t("stateBody")}
      </Notice>

      <Panel>
        <PanelBody>
          <div className="flex flex-wrap items-start gap-space-base">
            <Avatar initials={detail.student.Initials} className="size-14 text-headline-sm" />
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-space-sm">
                <h1 className="font-display-sm text-display-sm text-on-surface">
                  {detail.student.FullName}
                </h1>
                <Icon name="lock_reset" className="text-[20px] text-status-urgent-text" />
                <span className="font-code-audit text-code-audit text-on-surface-variant">
                  {detail.student.StudentCode} · {t("formerly")} DEMO-001
                </span>
                <StatusChip tone="attention" icon="rule" label="Pre-withdrawal review" />
              </div>
              <p className="mt-space-sm font-body-base text-body-base text-on-surface-variant">
                {detail.student.ProgrammeName} · Year {detail.student.YearOfStudy}, term week 03 of 16
              </p>
              <KeyValueGrid columns={3} className="mt-space-base">
                <KeyValue label={tp("assignedAdvisor")} value="Demo Advisor 01" />
                <KeyValue label={tc("purpose")} value={tc("academicAdvising")} />
                <KeyValue label={tc("lastSynced")} value="08-Sep-2026 10:06:14 ICT" mono />
              </KeyValueGrid>
            </div>
            <div className="flex flex-col gap-space-sm">
              <ButtonLink href="/advisor/cases/1" size="sm" icon="folder_open">
                {tp("openCase")} (CASE-001)
              </ButtonLink>
              <ButtonLink
                href={`/advisor/referrals/new?student=${studentId}`}
                variant="secondary"
                size="sm"
                icon="person_add"
              >
                {tp("newReferral")}
              </ButtonLink>
              <ButtonLink
                href={`/advisor/students/${studentId}/historical/erased`}
                variant="secondary"
                size="sm"
                icon="history"
              >
                {tp("viewHistory")}
              </ButtonLink>
            </div>
          </div>
        </PanelBody>
      </Panel>

      {visibleSections.map((section) => (
        <EvidenceSection key={section.id} section={section} />
      ))}

      <Panel>
        <PanelHeader
          icon="no_encryption_gmailerrorred"
          title={t("sectionCircumstances")}
          actions={<StatusChip tone="sealed" icon="cancel" label={tc("unavailableForPurpose")} />}
        />
        <PanelBody className="space-y-space-base">
          <Notice tone="sealed" icon="lock" title={t("noScopeTitle")}>
            {t("noScopeBody")}
          </Notice>
          <div>
            <h3 className="font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
              {t("whatRemains")}
            </h3>
            <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
              {t("whatRemainsBody")}
            </p>
            {withdrawnGrant ? (
              <KeyValueGrid columns={3} className="mt-space-base">
                <KeyValue
                  label={tc("purpose")}
                  value={withdrawnGrant.PurposeLabel}
                />
                <KeyValue
                  label={tc("status")}
                  value={<StatusChip tone="sealed" icon="block" label={tc("withdrawn")} />}
                />
                <KeyValue
                  label="Evidence hash"
                  value={`${withdrawnGrant.EvidenceHash.slice(0, 10)}…`}
                  mono
                />
              </KeyValueGrid>
            ) : null}
          </div>
        </PanelBody>
      </Panel>

      <AuditFooter
        message={tc("auditMessage")}
        sessionHash="#AUD-2026-FOC-9042"
        timestamp="2026-09-08 10:06 ICT"
      />
    </>
  );
}
