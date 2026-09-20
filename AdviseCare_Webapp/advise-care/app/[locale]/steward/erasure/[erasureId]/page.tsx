import { notFound } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { InspectorLayout } from "@/components/layout/app-shell";
import {
  Button,
  ButtonLink,
  DataTable,
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
  StatusChip,
  Td,
  Th,
  TextArea,
  Tr,
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type { ErasureExecution } from "@/lib/types";

const dependencies = [
  {
    purpose: "PURP-ACAD-01 (academic advising)",
    role: "Academic Advisor",
    grant: "Withdrawn (10:00 ICT)",
    resolution: "Immediate eradication cleared",
    tone: "ontrack" as const,
  },
  {
    purpose: "PURP-WELF-02 (welfare support)",
    role: "Student Welfare Officer",
    grant: "Never granted (NULL)",
    resolution: "No dependency lock",
    tone: "sealed" as const,
  },
  {
    purpose: "PURP-DISAB-03 (disability support)",
    role: "Accommodation Officer",
    grant: "Never granted (NULL)",
    resolution: "No dependency lock",
    tone: "sealed" as const,
  },
];

/** D05 — Erasure verification. */
export default async function ErasureVerificationPage({
  params,
}: PageProps<"/[locale]/steward/erasure/[erasureId]">) {
  const { locale, erasureId } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("steward.verification");
  const te = await getTranslations("steward.erasure");
  const tc = await getTranslations("common");

  const data = await apiGet<{ erasure: ErasureExecution }>(
    `/api/erasures/${erasureId}`,
  ).catch(() => null);
  if (!data) notFound();
  const { erasure } = data;

  const checks = [
    { icon: "bolt", badge: "Enforced", title: t("severedTitle"), body: t("severedBody") },
    { icon: "fence", badge: "Cleared", title: t("isolationTitle"), body: t("isolationBody") },
    {
      icon: "history_edu",
      badge: "Preserved",
      title: t("auditabilityTitle"),
      body: t("auditabilityBody"),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow={["Privacy subsystem", `Erasure verification (D05)`]}
        title={`${t("title")}: ${erasure.AttributeLabel}`}
        badge={
          <StatusChip tone="attention" icon="hourglass_top" label={t("pendingReview")} />
        }
        description={t("intro")}
        actions={
          <div className="flex flex-wrap gap-space-sm">
            <ButtonLink href="/steward/erasure" variant="secondary" size="sm" icon="arrow_back">
              {t("backToQueue")}
            </ButtonLink>
            <ButtonLink
              href="/steward/access-records"
              variant="secondary"
              size="sm"
              icon="receipt_long"
            >
              {t("viewRecords")}
            </ButtonLink>
          </div>
        }
      />

      <div className="grid gap-space-md lg:grid-cols-3">
        {checks.map((check) => (
          <Panel key={check.title} className="p-space-base">
            <div className="flex flex-wrap items-center justify-between gap-space-sm">
              <p className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
                <Icon name={check.icon} className="text-[18px] text-primary" />
                {check.title}
              </p>
              <StatusChip tone="ontrack" icon="check" size="sm" label={check.badge} />
            </div>
            <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
              {check.body}
            </p>
          </Panel>
        ))}
      </div>

      <InspectorLayout
        inspector={
          <Panel>
            <PanelHeader
              icon="how_to_reg"
              title={t("attestationTitle")}
              subtitle={t("attestationBody")}
            />
            <PanelBody className="space-y-space-base">
              <Field label={tc("decision")}>
                <TextArea
                  rows={4}
                  defaultValue="Preconditions verified: access severed at the gateway, no secondary purpose grant, advising references preserved. Proceeding with key destruction and verification seal."
                />
              </Field>
              <Button icon="verified" className="w-full">
                {t("executeErasure")}
              </Button>
              <Button variant="secondary" icon="pause" className="w-full">
                {t("quarantine")}
              </Button>
            </PanelBody>
            <PanelFooter>
              <span className="font-code-audit text-code-audit">
                Ledger block #491,209
              </span>
              <StatusChip tone="sealed" icon="lock" label={t("zeroKeyExposure")} />
            </PanelFooter>
          </Panel>
        }
      >
        <Panel>
          <PanelHeader
            icon="description"
            title={t("jobSpec")}
            subtitle={erasure.ErasureCode}
            actions={
              <StatusChip
                tone={erasure.Status === "VERIFIED" ? "ontrack" : "attention"}
                icon="autorenew"
                label={erasure.Status}
              />
            }
          />
          <PanelBody className="space-y-space-base">
            <KeyValueGrid columns={3}>
              <KeyValue label={t("subject")} value={erasure.StudentCode} mono />
              <KeyValue
                label={t("withdrawalRef")}
                value={`CG-${erasure.TriggeredByGrantId} · receipt RCPT-WDR-89012`}
              />
              <KeyValue label={t("withdrawalTimestamp")} value={erasure.RequestedAt} mono />
              <KeyValue label={t("attributeCategory")} value={erasure.AttributeLabel} />
              <KeyValue label={tc("purpose")} value={erasure.PurposeLabel} />
              <KeyValue
                label={te("colVerification")}
                value={erasure.VerifiedAt ?? tc("pending")}
              />
            </KeyValueGrid>

            <ul className="space-y-space-sm">
              <li className="flex flex-wrap items-start justify-between gap-space-md rounded-md border border-status-urgent-border bg-status-urgent-surface/25 px-space-base py-space-md">
                <div className="min-w-0">
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    {t("shardRef")}
                  </p>
                  <p className="mt-1 break-all font-code-audit text-code-audit text-on-surface-variant">
                    {erasure.KeyRef}
                  </p>
                </div>
                <StatusChip tone="urgent" icon="delete_forever" label={t("targetForShredding")} />
              </li>
              <li className="flex flex-wrap items-start justify-between gap-space-md rounded-md border border-outline-variant/40 px-space-base py-space-md">
                <div className="min-w-0">
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    {t("snapshotRef")}
                  </p>
                  <p className="mt-1 break-all font-code-audit text-code-audit text-on-surface-variant">
                    bitemporal-snap::rec-fin-tier-2024a
                  </p>
                </div>
                <StatusChip tone="attention" icon="key_off" label={t("keySevered")} />
              </li>
              <li className="flex flex-wrap items-start justify-between gap-space-md rounded-md border border-status-ontrack-border bg-status-ontrack-surface/25 px-space-base py-space-md">
                <div className="min-w-0">
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    {t("retainedEvidence")}
                  </p>
                  <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
                    CASE-001 / NOTE-001 / ATTEND-EV-001
                  </p>
                </div>
                <StatusChip tone="ontrack" icon="lock" label={t("preservedUnmodified")} />
              </li>
            </ul>
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader
            icon="account_tree"
            title={t("dependencyTitle")}
            subtitle={t("dependencySubtitle")}
            actions={<StatusChip tone="ontrack" icon="check_circle" label={t("noConflicts")} />}
          />
          <DataTable className="min-w-[720px]">
            <thead>
              <Tr className="hover:bg-transparent">
                <Th>{t("colPurpose")}</Th>
                <Th>{t("colRole")}</Th>
                <Th>{t("colGrant")}</Th>
                <Th>{t("colResolution")}</Th>
              </Tr>
            </thead>
            <tbody>
              {dependencies.map((row) => (
                <Tr key={row.purpose}>
                  <Td>{row.purpose}</Td>
                  <Td>{row.role}</Td>
                  <Td>{row.grant}</Td>
                  <Td>
                    <StatusChip
                      tone={row.tone}
                      icon={row.tone === "ontrack" ? "check_circle" : "remove"}
                      label={row.resolution}
                    />
                  </Td>
                </Tr>
              ))}
            </tbody>
          </DataTable>
          <PanelBody>
            <Notice tone="attention" icon="gavel" title={t("invariantTitle")}>
              {t("invariantBody")}
            </Notice>
          </PanelBody>
        </Panel>
      </InspectorLayout>
    </>
  );
}
