import { getTranslations, setRequestLocale } from "next-intl/server";
import { InspectorLayout } from "@/components/layout/app-shell";
import {
  Button,
  ButtonLink,
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
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type { ConsentGrant, ErasureExecution } from "@/lib/types";

/** S05 — Consent withdrawal & erasure dispatch receipt. */
export default async function WithdrawalReceiptPage({
  params,
}: PageProps<"/[locale]/me/consent/receipts/[receiptId]">) {
  const { locale, receiptId } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("student.receipt");
  const tc = await getTranslations("common");

  const [{ grants }, { erasures }] = await Promise.all([
    apiGet<{ grants: ConsentGrant[] }>("/api/consent/grants?studentId=1"),
    apiGet<{ erasures: ErasureExecution[] }>("/api/erasures"),
  ]);

  const otherScopes = grants.filter((grant) => grant.State !== "WITHDRAWN");
  const erasure = erasures.find((item) => item.StudentCode === "STU-2024-001");

  const steps = [
    { label: t("step1"), done: true },
    { label: t("step2"), done: false, active: true },
    { label: t("step3"), done: false },
  ];

  return (
    <>
      <PageHeader
        eyebrow={["Consent governance", "S05 · Withdrawal receipt"]}
        title={t("title")}
        badge={<StatusChip tone="urgent" icon="gpp_bad" label={t("withdrawn")} />}
        description={t("body")}
        meta={
          <div className="flex flex-wrap items-center gap-space-sm">
            <StatusChip tone="sealed" icon="tag" label="LEDGER-EVENT #WDR-20260908-01" />
            <span className="font-code-audit text-code-audit text-on-surface-variant">
              {t("recorded")}: 8 September 2026, 10:00 ICT
            </span>
          </div>
        }
      />

      <Notice tone="urgent" icon="block" title={t("enforcementTitle")}>
        {t("enforcementBody")}
      </Notice>

      <InspectorLayout
        inspector={
          <>
            <Panel>
              <PanelHeader
                icon="delete_sweep"
                title={t("erasureProcessing")}
                subtitle={`JOB #${erasure?.ErasureCode ?? "ERZ-20260908-04"}`}
              />
              <PanelBody className="space-y-space-base">
                <div>
                  <p className="font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
                    {t("lifecycle")}
                  </p>
                  <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                    {t("pipelineStep", { current: 2, total: 3 })}
                  </p>
                  <span className="mt-space-sm block h-1.5 w-full overflow-hidden rounded-full bg-surface-container-high">
                    <span className="block h-full w-[66%] bg-secondary" />
                  </span>
                  <ol className="mt-space-md space-y-space-xs">
                    {steps.map((step) => (
                      <li
                        key={step.label}
                        className="flex items-center gap-1.5 font-body-sm text-body-sm text-on-surface-variant"
                      >
                        <Icon
                          name={
                            step.done
                              ? "check_circle"
                              : step.active
                                ? "autorenew"
                                : "radio_button_unchecked"
                          }
                          className={
                            step.done
                              ? "text-[16px] text-status-ontrack-text"
                              : "text-[16px] text-on-surface-variant"
                          }
                        />
                        {step.label}
                      </li>
                    ))}
                  </ol>
                </div>
                <KeyValueGrid columns={1}>
                  <KeyValue label={t("trackingRef")} value="TRK-PURGE-99420" mono />
                  <KeyValue label={t("workerNode")} value="crypto-shred-sg3" mono />
                  <KeyValue label={t("targetSchema")} value="consented_support_attrs" mono />
                </KeyValueGrid>
                <Notice tone="urgent" icon="warning" title={t("irrevocableTitle")}>
                  {t("irrevocableBody")}
                </Notice>
              </PanelBody>
              <PanelFooter>
                <span>{t("milestonePreview")}</span>
                <span className="font-code-audit text-code-audit">T + 5 min</span>
              </PanelFooter>
            </Panel>

            <Notice tone="ontrack" icon="schedule" title="T + 5 minutes">
              <p>{t("milestoneBody")}</p>
              <p className="mt-space-sm font-code-audit text-code-audit">
                RECEIPT_DIGEST: sha256:d8a2…41ef
              </p>
            </Notice>
          </>
        }
      >
        <Panel>
          <PanelHeader
            icon="receipt_long"
            title={t("withdrawnScope")}
            subtitle="ID: CONS-001"
            actions={
              <div className="flex flex-wrap items-center gap-space-sm">
                <StatusChip tone="sealed" icon="lock" label={t("sealed")} />
                <span className="font-code-audit text-code-audit text-on-surface-variant">
                  {t("receiptRef")}: {receiptId}
                </span>
              </div>
            }
          />
          <PanelBody className="space-y-space-base">
            <KeyValueGrid columns={2}>
              <KeyValue
                label={t("targetPurpose")}
                value="Academic advising — Faculty of Computing & Systems"
              />
              <KeyValue label={t("priorRecipient")} value="Academic Advisor (Demo Advisor 01)" />
            </KeyValueGrid>

            <div className="rounded-md border border-status-urgent-border bg-status-urgent-surface/30 px-space-base py-space-md">
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <p className="font-label-md text-label-md font-semibold text-on-surface">
                  Financial circumstances · ATTR-FIN-001
                </p>
                <StatusChip tone="urgent" icon="link_off" label={t("accessSevered")} />
              </div>
              <p className="mt-space-sm font-code-audit text-code-audit text-on-surface-variant">
                {t("valueState")}
              </p>
            </div>

            <Notice tone="sealed" icon="history_edu" title={t("ledgerIntegrity")}>
              {t("ledgerIntegrityBody")}
            </Notice>
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader
            icon="checklist"
            title={t("otherScopes")}
            subtitle={t("otherScopesBody")}
            actions={
              <StatusChip
                tone="sealed"
                icon="layers"
                label={`${otherScopes.length} distinct purposes`}
              />
            }
          />
          <PanelBody className="space-y-space-sm">
            {otherScopes.map((grant) => (
              <div
                key={grant.ConsentGrantId}
                className="rounded-md border border-outline-variant/40 px-space-base py-space-md"
              >
                <div className="flex flex-wrap items-center justify-between gap-space-sm">
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    {tc("purpose")}: {grant.PurposeLabel}
                  </p>
                  <StatusChip
                    tone={grant.State === "GRANTED" ? "ontrack" : "referral"}
                    icon={grant.State === "GRANTED" ? "check_circle" : "hourglass_top"}
                    label={grant.State === "GRANTED" ? tc("granted") : tc("narrowed")}
                  />
                </div>
                <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                  {grant.ScopeLabel} — {grant.SharedWith.join(", ")}
                </p>
                <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
                  {tc("expiresAt")}: {grant.ExpiresAt ?? "—"}
                </p>
              </div>
            ))}
          </PanelBody>
          <PanelFooter>
            <div className="flex flex-wrap gap-space-sm">
              <ButtonLink href="/me/consent" variant="tertiary" size="sm" icon="arrow_back">
                {t("viewHistory")}
              </ButtonLink>
              <ButtonLink href="/me/profile" variant="tertiary" size="sm" icon="person">
                {t("viewRecord")}
              </ButtonLink>
            </div>
            <Button size="sm" icon="download">
              {t("downloadReceipt")}
            </Button>
          </PanelFooter>
        </Panel>
      </InspectorLayout>

      <p className="text-center font-code-audit text-code-audit text-on-surface-variant">
        AdviseCare autonomous privacy gateway · STU-2024-001 (DEMO-001) · session seal 0x8F14…E290
      </p>
    </>
  );
}
