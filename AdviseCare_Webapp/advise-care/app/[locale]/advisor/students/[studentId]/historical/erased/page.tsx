import { getTranslations, setRequestLocale } from "next-intl/server";
import {
  Button,
  ButtonLink,
  Icon,
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
import type { Student } from "@/lib/types";

/**
 * A10 — Historical record after cryptographic erasure.
 * Điểm mấu chốt: hàng bản ghi, tham chiếu chứng cứ và quyết định vẫn nguyên;
 * chỉ phần payload được bảo vệ là không còn đọc được. Vết kiểm toán không thủng lỗ.
 */
export default async function ErasedHistoryPage({
  params,
}: PageProps<"/[locale]/advisor/students/[studentId]/historical/erased">) {
  const { locale, studentId } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.erasure");
  const td = await getTranslations("advisor.decisionTime");
  const tc = await getTranslations("common");

  const detail = await apiGet<{ student: Student }>(`/api/students/${studentId}`);

  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-space-md">
        <div className="flex items-center gap-space-sm">
          <Icon name="history" className="text-[22px] text-primary" />
          <h1 className="font-headline-md text-headline-md text-on-surface">{t("title")}</h1>
        </div>
        <StatusChip tone="sealed" icon="tag" label="#BLK-20260908-8814 (#491,209)" />
      </div>

      <Notice tone="attention" icon="policy" title={td("directiveTitle")}>
        {td("directiveBody")}
      </Notice>

      <Panel>
        <PanelBody>
          <KeyValueGrid columns={4}>
            <KeyValue label={td("inspector")} value="Demo Advisor 01 — Faculty of Computing" />
            <KeyValue
              label={tc("student")}
              value={`${detail.student.FullName} · ${detail.student.StudentCode}`}
            />
            <KeyValue label={td("targetCase")} value="CASE-001 (early intervention)" />
            <KeyValue label={tc("purpose")} value="Academic advising (PURP-ACAD-01)" />
          </KeyValueGrid>
        </PanelBody>
      </Panel>

      <Panel>
        <PanelHeader
          icon="compare_arrows"
          title={td("deltaTitle")}
          subtitle="Specification HE-GOV-2026 / subpart 4.1"
          actions={<StatusChip tone="sealed" icon="lock" label={td("deltaBadge")} />}
        />
        <PanelBody>
          <div className="grid gap-space-md lg:grid-cols-2">
            <section className="rounded-md border border-outline-variant/40">
              <header className="flex flex-wrap items-center justify-between gap-space-sm border-b border-outline-variant/30 bg-surface-container-low/50 px-space-base py-space-md">
                <p className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
                  <Icon name="history_toggle_off" className="text-[18px]" />
                  {td("snapshotTitle")}
                </p>
                <StatusChip tone="sealed" icon="lock" label="03 Sep 2026" />
              </header>
              <div className="space-y-space-md p-space-base">
                <div>
                  <div className="flex flex-wrap items-center justify-between gap-space-sm">
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      Attendance telemetry (snapshot v1)
                    </p>
                    <StatusChip tone="attention" icon="warning" label="85.0% — trigger alert" />
                  </div>
                  <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
                    #ATT-V1-2026-0903-HIST · SHA-256 4f8b91…a12c
                  </p>
                </div>

                <div className="rounded-md border border-status-urgent-border bg-status-urgent-surface/40 px-space-base py-space-md">
                  <div className="flex flex-wrap items-center justify-between gap-space-sm">
                    <p className="font-label-md text-label-md font-semibold text-status-urgent-text">
                      {t("erasedTitle")}
                    </p>
                    <StatusChip tone="urgent" icon="delete_forever" label={t("erasedLabel")} />
                  </div>
                  <p className="mt-space-sm font-body-sm text-body-sm text-on-surface">
                    {t("erasedBody")}
                  </p>
                  <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                    {t("erasedDetail")}
                  </p>
                  <p className="mt-space-sm font-code-audit text-code-audit text-on-surface-variant">
                    {t("attestation")}: CERT-ERASE-20260908 · {t("steward")}: Dr. Le Thao Nguyen
                    (10:05 ICT)
                  </p>
                </div>

                <div className="border-t border-outline-variant/25 pt-space-md">
                  <div className="flex flex-wrap items-center justify-between gap-space-sm">
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      SIS academic records v3.2
                    </p>
                    <StatusChip tone="urgent" icon="priority_high" label="Midterm deficit 42.0%" />
                  </div>
                  <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                    Cumulative GPA 2.74. The academic evidence that justified the intervention is
                    untouched by the erasure.
                  </p>
                </div>
              </div>
            </section>

            <section className="rounded-md border border-outline-variant/40">
              <header className="flex flex-wrap items-center justify-between gap-space-sm border-b border-outline-variant/30 bg-surface-container-low/50 px-space-base py-space-md">
                <p className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
                  <Icon name="fact_check" className="text-[18px]" />
                  {td("currentTitle")}
                </p>
                <StatusChip tone="ontrack" icon="refresh" label="08 Sep 2026" />
              </header>
              <div className="space-y-space-md p-space-base">
                <p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                  Status recorded after hardware calibration (7 Sep) and consent erasure
                  (08-SEP-2026 10:00 ICT)
                </p>
                <div>
                  <div className="flex flex-wrap items-center justify-between gap-space-sm">
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      Attendance telemetry (reconciled v2)
                    </p>
                    <StatusChip tone="ontrack" icon="check_circle" label="90.0% — regular standing" />
                  </div>
                  <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
                    #CORR-20260907-004 · reconciled 07-SEP-2026 11:15 ICT
                  </p>
                </div>
                <Notice tone="sealed" icon="verified_user" title={td("nonOverwritingTitle")}>
                  {td("nonOverwritingBody")}
                </Notice>
                <div className="border-t border-outline-variant/25 pt-space-md">
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    Consent registry
                  </p>
                  <KeyValueGrid columns={1} className="mt-space-sm">
                    <KeyValue label="Grant" value="CG-4990 — state WITHDRAWN" />
                    <KeyValue label="Withdrawal receipt" value="RCPT-WDR-89012" mono />
                    <KeyValue label="Ciphertext hash (retained)" value="d1f8207b…5b3c" mono />
                  </KeyValueGrid>
                  <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                    The hash survives as proof that a value once existed, without revealing it.
                    Every foreign key pointing at that value stays valid.
                  </p>
                </div>
              </div>
            </section>
          </div>
        </PanelBody>
        <PanelFooter>
          <div className="flex flex-wrap gap-space-sm">
            <ButtonLink
              href={`/advisor/students/${studentId}/current`}
              variant="tertiary"
              size="sm"
              icon="arrow_back"
            >
              {t("returnCurrent")}
            </ButtonLink>
            <ButtonLink href="/advisor/cases/1" variant="tertiary" size="sm" icon="folder">
              {td("backToCase")}
            </ButtonLink>
          </div>
          <div className="flex flex-wrap items-center gap-space-sm">
            <StatusChip tone="ontrack" icon="verified_user" label={td("pkiReady")} />
            <Button size="sm" icon="picture_as_pdf">
              {t("exportPack")}
            </Button>
          </div>
        </PanelFooter>
      </Panel>
    </>
  );
}
