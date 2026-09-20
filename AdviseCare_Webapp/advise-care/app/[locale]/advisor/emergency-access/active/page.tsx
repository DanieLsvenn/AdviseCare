import { getTranslations, setRequestLocale } from "next-intl/server";
import { EvidenceSection } from "@/components/advisor/evidence-section";
import {
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
import type { Student } from "@/lib/types";

/**
 * A14 — Time-limited access.
 * Khác A02 ở đúng một chỗ: mục hoàn cảnh hiện đúng một thuộc tính, kèm đồng hồ
 * đếm ngược và mã quyết định của cán bộ duyệt. Hết hạn thì nó biến mất.
 */
export default async function TimeLimitedAccessPage({
  params,
}: PageProps<"/[locale]/advisor/emergency-access/active">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.timeLimited");
  const tp = await getTranslations("advisor.profile");
  const tc = await getTranslations("common");

  const detail = await apiGet<{ student: Student; sections: ProfileSection[] }>(
    "/api/students/1",
  );
  const visibleSections = detail.sections.filter(
    (section) => section.id !== "circumstance",
  );

  return (
    <>
      <Notice tone="sealed" icon="security">
        Time-bounded ephemeral access token (CERT-EMERG-20260908-01) active under ledger block
        #441,893. Statutory Decree 13 and ISO/IEC 27001 certified.
      </Notice>

      <Panel className="border-secondary">
        <PanelHeader
          icon="verified"
          title={t("bannerTitle")}
          subtitle="CERT-EMERG-20260908-01"
          actions={
            <StatusChip
              tone="attention"
              icon="timer"
              label={t("expires", {
                timestamp: "09-Sep-2026 14:28:40 ICT",
                remaining: "23h 54m",
              })}
            />
          }
        />
        <PanelBody className="space-y-space-base">
          <KeyValueGrid columns={2}>
            <KeyValue
              label={t("approvedPurpose")}
              value="Statutory emergency pastoral coordination & academic rescheduling (PURP-EMERG-COORD-01)"
            />
            <KeyValue
              label={t("allowedCategory")}
              value="ATTR-RES-004 — housing liaison & relocation status only"
            />
            <KeyValue
              label={t("approvingSteward")}
              value="Dr. Le Thao Nguyen (DPO-STEWARD-01) · Data Protection & Governance Directorate"
            />
            <KeyValue label={t("requisitionRef")} value="REQ-20260908-01" mono />
          </KeyValueGrid>

          <Notice tone="attention" icon="policy" title={t("invariantTitle")}>
            {t("invariantBody")}
          </Notice>

          <div className="flex flex-wrap gap-space-sm">
            <ButtonLink
              href="/steward/emergency-reviews"
              variant="secondary"
              size="sm"
              iconAfter="arrow_forward"
            >
              {t("viewDecision")}
            </ButtonLink>
            <ButtonLink
              href="/advisor/emergency-access/request"
              variant="tertiary"
              size="sm"
              icon="assignment"
            >
              {t("viewRequest")}
            </ButtonLink>
          </div>
        </PanelBody>
      </Panel>

      <Panel>
        <PanelBody>
          <div className="flex flex-wrap items-start gap-space-base">
            <Avatar initials={detail.student.Initials} className="size-14 text-headline-sm" />
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-space-sm">
                <h1 className="font-display-sm text-display-sm text-on-surface">
                  {detail.student.FullName}
                </h1>
                <Icon name="alarm_on" className="text-[20px] text-status-attention-text" />
                <StatusChip tone="attention" icon="timelapse" label={t("exceptionActive")} />
              </div>
              <p className="mt-space-sm font-body-base text-body-base text-on-surface-variant">
                {detail.student.ProgrammeName} · Year {detail.student.YearOfStudy}, term week 03 of 16
              </p>
              <KeyValueGrid columns={3} className="mt-space-base">
                <KeyValue label={tp("assignedAdvisor")} value="Demo Advisor 01" />
                <KeyValue
                  label={tc("purpose")}
                  value="Academic advising + emergency relocation"
                />
                <KeyValue label={tc("lastSynced")} value="08-Sep-2026 14:28:40 ICT" mono />
              </KeyValueGrid>
            </div>
            <div className="flex flex-col gap-space-sm">
              <ButtonLink href="/advisor/students/1" size="sm" icon="arrow_back">
                {tp("backToCaseload")}
              </ButtonLink>
              <ButtonLink
                href="/advisor/students/1/historical"
                variant="secondary"
                size="sm"
                icon="history"
              >
                {tp("viewFullHistory")}
              </ButtonLink>
            </div>
          </div>
        </PanelBody>
      </Panel>

      {visibleSections.map((section) => (
        <EvidenceSection key={section.id} section={section} />
      ))}

      <Panel className="border-secondary">
        <PanelHeader
          icon="admin_panel_settings"
          title={t("enclave")}
          actions={<StatusChip tone="ontrack" icon="vpn_key" label={t("enclaveBadge")} />}
        />
        <PanelBody className="space-y-space-base">
          <div className="rounded-md border border-status-ontrack-border bg-status-ontrack-surface/30 px-space-base py-space-md">
            <div className="flex flex-wrap items-center justify-between gap-space-sm">
              <p className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
                <Icon name="home_work" className="text-[18px]" />
                ATTR-RES-004 — housing liaison & relocation status
              </p>
              <StatusChip tone="ontrack" icon="lock_open" label="Active exception (REQ-20260908-01)" />
            </div>
            <KeyValueGrid columns={1} className="mt-space-md">
              <KeyValue
                label={t("housingCase")}
                value="EXT-HOUS-2026-8819 (municipal student tenancy assistance)"
              />
              <KeyValue label={t("liaison")} value="Mai Van Hai — senior tenancy officer" />
              <KeyValue
                label={t("scheduleConflict")}
                value="Mandatory relocation intake on Wednesday 09-Sep-2026, 09:30–12:00 ICT, clashing with the CS301 laboratory practical exam."
              />
            </KeyValueGrid>
          </div>
          <p className="font-body-sm text-body-sm text-on-surface-variant">
            {t("exceptionWindow", { remaining: "23h 54m" })} — {tc("unavailableExplainer")}
          </p>
        </PanelBody>
      </Panel>
    </>
  );
}
