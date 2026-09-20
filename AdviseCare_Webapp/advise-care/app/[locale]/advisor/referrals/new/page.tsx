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
  Panel,
  PanelBody,
  PanelFooter,
  PanelHeader,
  Select,
  StatusChip,
  TextArea,
  TextInput,
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type { SupportService } from "@/lib/types";

const assistance =
  "Urgent accommodation liaison and university residential welfare support following the CS301 laboratory commute disruption. Student consent verified under the housing domain.";

/** A12 — New welfare referral. */
export default async function NewReferralPage({
  params,
}: PageProps<"/[locale]/advisor/referrals/new">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.referral");
  const tc = await getTranslations("common");

  await apiGet<{ services: SupportService[] }>("/api/services");

  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-space-md">
        <div>
          <h1 className="font-headline-md text-headline-md text-on-surface">{t("title")}</h1>
          <p className="mt-space-xs font-body-sm text-body-sm text-on-surface-variant">
            Nguyen Van An (DEMO-001 / STU-2024-001) · Demo Advisor 01 · 07-Sep-2026
          </p>
        </div>
        <StatusChip tone="attention" icon="timer" label={t("sla")} />
      </div>

      <Notice tone="sealed" icon="shield" title={t("protocolTitle")}>
        <p>{t("protocolBody")}</p>
        <p className="mt-space-sm font-code-audit text-code-audit">
          Decree 13/2023 · Institutional rule 3.2
        </p>
      </Notice>

      <InspectorLayout
        inspector={
          <Panel>
            <PanelHeader
              icon="assignment_turned_in"
              title={t("summary")}
              subtitle={t("summaryCaption")}
              actions={<StatusChip tone="referral" icon="pending" label="REF-001" />}
            />
            <PanelBody>
              <KeyValueGrid columns={1}>
                <KeyValue label={t("reference")} value="REF-001 (allocated on commit)" mono />
                <KeyValue
                  label={tc("student")}
                  value="Nguyen Van An (DEMO-001) · STU-2024-001"
                />
                <KeyValue
                  label={t("receivingService")}
                  value="Student Welfare Office — designated duty caseworker"
                />
                <KeyValue
                  label={tc("status")}
                  value="Housing & living stability · priority 2 (within 48 hours)"
                />
                <KeyValue label={t("milestone")} value="14/09/2026" mono />
              </KeyValueGrid>
              <div className="mt-space-base border-t border-outline-variant/30 pt-space-md">
                <p className="font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
                  {t("transmittedScope")}
                </p>
                <ul className="mt-space-sm space-y-space-xs font-body-sm text-body-sm">
                  <li className="flex items-center gap-1.5 text-status-ontrack-text">
                    <Icon name="check" className="text-[16px]" />
                    ATTR-RES-004 (housing need)
                  </li>
                  <li className="flex items-center gap-1.5 text-on-surface-variant">
                    <Icon name="remove" className="text-[16px]" />
                    ATTR-FIN-001 (omitted — no consent)
                  </li>
                </ul>
              </div>
            </PanelBody>
            <PanelFooter>
              <Button size="sm" icon="outbox">
                {t("commit")} (REF-001)
              </Button>
            </PanelFooter>
          </Panel>
        }
      >
        <Panel>
          <PanelHeader
            icon="forward_to_inbox"
            title={t("destination")}
            actions={<StatusChip tone="sealed" icon="description" label={t("destinationBadge")} />}
          />
          <PanelBody className="space-y-space-lg">
            <div className="rounded-md border border-outline-variant/40 bg-surface-container-low/40 px-space-base py-space-md">
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                  {t("designatedUnit")} · INST-DEST-WELF
                </p>
                <StatusChip tone="ontrack" icon="domain" label={t("preRouted")} />
              </div>
              <p className="mt-space-sm font-label-md text-label-md font-semibold text-on-surface">
                Student Welfare Office / designated Student Welfare Officer
              </p>
              <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
                {t("routingKey")}: welfare-triage@university.edu.vn
              </p>
            </div>

            <Field label={t("reasonCategory")} required>
              <Select defaultValue="housing" className="w-full">
                <option value="housing">
                  Housing & living stability friction (impacting lab attendance)
                </option>
                <option value="health">Health & chronic impairment accommodations</option>
                <option value="psychosocial">Psychosocial wellbeing & resilience intake</option>
                <option value="dependent">Dependent care / family hardship liaison</option>
                <option value="transport">Non-crisis transportation & commuter hardship</option>
              </Select>
            </Field>

            <div>
              <p className="font-label-md text-label-md font-medium text-on-surface">
                {t("priority")} *
              </p>
              <div className="mt-space-sm grid gap-space-sm sm:grid-cols-3">
                {[
                  { id: "p3", label: t("priority3"), active: false },
                  { id: "p2", label: t("priority2"), active: true },
                  { id: "p1", label: t("priority1"), active: false },
                ].map((option) => (
                  <label
                    key={option.id}
                    className={
                      option.active
                        ? "flex cursor-pointer items-start gap-space-sm rounded-md border border-secondary bg-secondary-container/20 px-space-md py-space-sm"
                        : "flex cursor-pointer items-start gap-space-sm rounded-md border border-outline-variant/40 px-space-md py-space-sm"
                    }
                  >
                    <input
                      type="radio"
                      name="priority"
                      defaultChecked={option.active}
                      className="mt-1 accent-[#0f766e]"
                    />
                    <span className="font-body-sm text-body-sm text-on-surface">{option.label}</span>
                  </label>
                ))}
              </div>
              <Notice tone="referral" icon="info" title={t("triageRationale")} className="mt-space-md">
                {t("triageRationaleBody")}
              </Notice>
            </div>

            <div className="grid gap-space-base sm:grid-cols-2">
              <Field label={t("milestone")} required>
                <TextInput type="date" defaultValue="2026-09-14" />
              </Field>
              <Field label={t("signatory")}>
                <TextInput defaultValue="Demo Advisor 01 (ADV-COMP-01)" readOnly />
              </Field>
            </div>

            <div>
              <Field label={t("assistance")} required>
                <TextArea rows={4} defaultValue={assistance} maxLength={1000} />
              </Field>
              <div className="mt-space-xs flex flex-wrap items-center justify-between gap-space-sm">
                <StatusChip tone="ontrack" icon="verified" label={t("clearOfDiagnostic")} />
                <span className="font-code-audit text-code-audit text-on-surface-variant">
                  {assistance.length} / 1000
                </span>
              </div>
              <p className="mt-space-sm flex items-start gap-1.5 font-body-sm text-body-sm text-on-surface-variant">
                <Icon name="lock" className="mt-0.5 text-[14px]" />
                {t("assistanceNotice")}
              </p>
            </div>
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader
            icon="privacy_tip"
            title={t("minimumNecessary")}
            subtitle="Target purpose: PURP-WELF-02 (welfare support)"
            actions={<StatusChip tone="sealed" icon="compress" label={t("dataMinimization")} />}
          />
          <PanelBody className="space-y-space-md">
            <p className="font-body-sm text-body-sm text-on-surface-variant">
              {t("minimumNecessaryBody")}
            </p>

            <div className="rounded-md border border-status-ontrack-border bg-status-ontrack-surface/30 px-space-base py-space-md">
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <p className="font-label-md text-label-md font-semibold text-on-surface">
                  Housing support need · ATTR-RES-004
                </p>
                <StatusChip tone="ontrack" icon="verified" label={t("consentedIncluded")} />
              </div>
              <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                Active consent granted (CONS-082, executed digitally on 2 Sep 2026). Included in the
                referral payload bundle.
              </p>
              <p className="mt-space-sm font-code-audit text-code-audit text-on-surface-variant">
                {t("payloadSize")}: 1.4 KB · residence_status: &quot;commute_disrupted&quot; |
                lease_end: &quot;30-Sep-2026&quot; | lab_distance_km: 26.4
              </p>
            </div>

            <div className="rounded-md border border-outline-variant/50 bg-surface-container-low/50 px-space-base py-space-md">
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <p className="font-label-md text-label-md font-semibold text-on-surface">
                  Financial circumstances & tuition ledger · ATTR-FIN-001
                </p>
                <StatusChip tone="sealed" icon="block" label={t("quarantined")} />
              </div>
              <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                {t("quarantineBody")}
              </p>
              <p className="mt-space-sm font-code-audit text-code-audit text-on-surface-variant">
                {t("payloadSize")}: 0 KB transferred
              </p>
            </div>

            <Notice
              tone="referral"
              icon="rule"
              actions={
                <ButtonLink
                  href="/advisor/consent-requests/new"
                  variant="secondary"
                  size="sm"
                  iconAfter="arrow_outward"
                >
                  {t("requestConsent")}
                </ButtonLink>
              }
            >
              {t("draftHint")}
            </Notice>
          </PanelBody>
        </Panel>
      </InspectorLayout>
    </>
  );
}
