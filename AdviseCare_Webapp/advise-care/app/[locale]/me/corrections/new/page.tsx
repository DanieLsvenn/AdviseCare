import { getTranslations, setRequestLocale } from "next-intl/server";
import { InspectorLayout } from "@/components/layout/app-shell";
import {
  Button,
  ButtonLink,
  Field,
  Icon,
  KeyValue,
  KeyValueGrid,
  Notice,
  PageHeader,
  Panel,
  PanelBody,
  PanelFooter,
  PanelHeader,
  Select,
  StatusChip,
  TextArea,
  TextInput,
} from "@/components/ui";

const statement =
  "I was physically present at the CS301 Advanced Algorithms laboratory on Wednesday 2 September from 10:00 to 12:00. The card reader at turnstile CAM-GATE-09 malfunctioned and flashed yellow. The lab teaching assistant confirmed my presence on the manual lab sheet.";

/** S07 — Request a correction. */
export default async function RequestCorrectionPage({
  params,
}: PageProps<"/[locale]/me/corrections/new">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("student.correction");
  const tc = await getTranslations("common");

  const principles = [
    { icon: "lock_clock", title: t("nonOverwriting"), body: t("nonOverwritingBody") },
    { icon: "verified", title: t("verification"), body: t("verificationBody") },
    { icon: "shield_lock", title: t("genericNotifications"), body: t("genericNotificationsBody") },
  ];

  return (
    <>
      <PageHeader
        eyebrow={["Privacy governance", "S07 · Rectification"]}
        title={t("title")}
        description={t("rightBody")}
        meta={
          <div className="flex flex-wrap items-center gap-space-sm">
            <StatusChip tone="sealed" icon="schedule" label="TIMELOCK: 06-SEP-2026 14:30 ICT" />
            <StatusChip tone="sealed" icon="link" label="#BLK-20260906-8104" />
          </div>
        }
      />

      <Notice tone="sealed" icon="gavel" title={t("rightTitle")}>
        Vietnam PDPD Decree 13/2023 & institutional standards.
      </Notice>

      <div className="grid gap-space-md lg:grid-cols-3">
        {principles.map((principle) => (
          <Panel key={principle.title} className="p-space-base">
            <p className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
              <Icon name={principle.icon} className="text-[18px] text-primary" />
              {principle.title}
            </p>
            <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
              {principle.body}
            </p>
          </Panel>
        ))}
      </div>

      <InspectorLayout
        inspector={
          <Panel>
            <PanelHeader
              icon="receipt_long"
              title={t("receipt")}
              subtitle="REQ-CORR-20260906-04"
              actions={
                <StatusChip tone="attention" icon="hourglass_top" label={t("pendingReview")} />
              }
            />
            <PanelBody>
              <KeyValueGrid columns={1}>
                <KeyValue label={t("submittedAt")} value="06-Sep-2026 14:30:18 ICT" mono />
                <KeyValue label={t("assignedReviewer")} value="Demo Advisor 01 (Computing)" />
                <KeyValue
                  label={t("slaResolution")}
                  value="Within 2 institutional working days"
                />
                <KeyValue
                  label={t("profileImpact")}
                  value={`${t("profileImpactValue")} — 85.0% held`}
                />
              </KeyValueGrid>
              <div className="mt-space-base rounded-md border border-outline-variant/40 bg-surface-container-low/50 p-space-md">
                <p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                  {t("notificationPreview")}
                </p>
                <p className="mt-space-sm font-code-audit text-code-audit text-on-surface">
                  [SYS-DISPATCH #NOTIF-8104] Correction request logged
                  #REQ-CORR-20260906-04
                </p>
              </div>
            </PanelBody>
          </Panel>
        }
      >
        <Panel>
          <PanelHeader
            icon="rate_review"
            title={t("title")}
            actions={<StatusChip tone="sealed" icon="description" label="FORM-VER-2.4" />}
          />
          <PanelBody className="space-y-space-lg">
            <section>
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <h2 className="font-headline-sm text-headline-sm text-on-surface">
                  {t("targetStream")}
                </h2>
                <StatusChip tone="attention" icon="warning" label="Active trigger alert" />
              </div>
              <KeyValueGrid columns={2} className="mt-space-md">
                <KeyValue
                  label={t("selectedMetric")}
                  value="In-person attendance telemetry — term weeks 01–03"
                />
                <KeyValue
                  label={t("currentValue")}
                  value="85.0% overall — CS301 lab unexcused absence on 02-Sep-2026"
                />
                <KeyValue
                  label={t("ingestionSource")}
                  value="CAM-GATE-09 (biometric turnstile) · last synced 05-Sep-2026 09:15 ICT"
                />
                <KeyValue
                  label={t("observationWindow")}
                  value="28 Aug 2026 – 04 Sep 2026 · audit batch #SYNC-7741"
                />
              </KeyValueGrid>
            </section>

            <div className="grid gap-space-base sm:grid-cols-2">
              <Field label={t("proposedValue")} hint={t("proposedHint")} required>
                <TextInput defaultValue="90.0% — verified present" />
              </Field>
              <Field label={t("reasonCategory")} hint={t("reasonHint")} required>
                <Select defaultValue="hardware" className="w-full">
                  <option value="hardware">{t("reasonHardware")}</option>
                  <option value="medical">{t("reasonMedical")}</option>
                  <option value="event">{t("reasonEvent")}</option>
                  <option value="clerical">{t("reasonClerical")}</option>
                </Select>
              </Field>
            </div>

            <div>
              <Field label={t("statement")} hint={t("statementHint")} required>
                <TextArea rows={5} defaultValue={statement} maxLength={1000} />
              </Field>
              <p className="mt-space-xs text-right font-code-audit text-code-audit text-on-surface-variant">
                {statement.length} / 1000
              </p>
            </div>

            <section>
              <h2 className="font-headline-sm text-headline-sm text-on-surface">{t("evidence")}</h2>
              <ul className="mt-space-md space-y-space-sm">
                <li className="flex flex-wrap items-center justify-between gap-space-md rounded-md border border-outline-variant/40 px-space-base py-space-md">
                  <div className="flex min-w-0 items-center gap-space-md">
                    <Icon name="description" className="text-[20px] text-primary" />
                    <div className="min-w-0">
                      <p className="truncate font-label-md text-label-md font-semibold text-on-surface">
                        REF-EVID-20260902-CS301-LABSHEET.pdf
                      </p>
                      <p className="font-code-audit text-code-audit text-on-surface-variant">
                        Signed physical lab attendance slip · 1.4 MB · SHA-256 verified
                      </p>
                    </div>
                  </div>
                  <StatusChip tone="ontrack" icon="verified" label="Verified" />
                </li>
                <li className="flex flex-wrap items-center justify-between gap-space-md rounded-md border border-outline-variant/40 px-space-base py-space-md">
                  <div className="flex min-w-0 items-center gap-space-md">
                    <Icon name="confirmation_number" className="text-[20px] text-primary" />
                    <div className="min-w-0">
                      <p className="font-label-md text-label-md font-semibold text-on-surface">
                        FACILITIES-TKT-8821
                      </p>
                      <p className="font-body-sm text-body-sm text-on-surface-variant">
                        Reported hardware fault on CAM-GATE-09
                      </p>
                    </div>
                  </div>
                  <StatusChip tone="ontrack" icon="sync" label="Synced with Campus IT" />
                </li>
              </ul>
              <div className="mt-space-md rounded-md border border-dashed border-outline-variant px-space-base py-space-lg text-center">
                <Icon name="upload_file" className="text-[24px] text-on-surface-variant" />
                <p className="mt-space-sm font-label-md text-label-md text-on-surface">
                  {t("attach")}
                </p>
                <p className="font-body-sm text-body-sm text-on-surface-variant">
                  {t("attachHint")}
                </p>
              </div>
            </section>

            <Notice tone="attention" icon="info" title={t("governanceTitle")}>
              {t("governanceBody")}
            </Notice>
          </PanelBody>
          <PanelFooter>
            <ButtonLink href="/me/profile" variant="tertiary" size="sm">
              {tc("cancel")}
            </ButtonLink>
            <div className="flex flex-wrap gap-space-sm">
              <Button variant="secondary" size="sm" icon="save">
                {t("saveDraft")}
              </Button>
              <Button size="sm" icon="send">
                {t("submit")}
              </Button>
            </div>
          </PanelFooter>
        </Panel>
      </InspectorLayout>
    </>
  );
}
