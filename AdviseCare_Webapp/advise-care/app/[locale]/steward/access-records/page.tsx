import { getTranslations, setRequestLocale } from "next-intl/server";
import { InspectorLayout } from "@/components/layout/app-shell";
import {
  Button,
  DataTable,
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
  Tr,
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type { AccessLogEntry } from "@/lib/types";

const predicates = [
  { expression: "Subject.Role == 'AcademicAdvisor'", pass: true },
  { expression: "Subject.AssignedAdvisee == Target.StudentId", pass: true },
  { expression: "Request.Purpose == 'AcademicAdvising'", pass: true },
  { expression: "ConsentGrant.Status == 'ACTIVE'", pass: false },
];

/** D03 — Access & processing records. */
export default async function AccessRecordsPage({
  params,
}: PageProps<"/[locale]/steward/access-records">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("steward.records");
  const ts = await getTranslations("steward");
  const tc = await getTranslations("common");

  const { entries } = await apiGet<{ entries: AccessLogEntry[] }>("/api/access-log");
  const denied = entries.find((entry) => entry.Decision === "DENY");

  return (
    <>
      <PageHeader
        eyebrow={[ts("console"), "D03"]}
        title={t("title")}
        description={t("subtitle")}
        meta={<StatusChip tone="sealed" icon="verified_user" label={ts("statutoryAudit")} />}
      />

      <InspectorLayout
        inspector={
          <>
            {denied ? (
              <Panel className="border-status-urgent-border">
                <PanelHeader
                  icon="lock"
                  title={t("inspection")}
                  subtitle={`EVT-${denied.AccessLogId} · ${denied.OccurredAt}`}
                  actions={<StatusChip tone="urgent" icon="block" label={tc("deny")} />}
                />
                <PanelBody className="space-y-space-base">
                  <KeyValueGrid columns={1}>
                    <KeyValue
                      label={t("deterministicUnix")}
                      value="1788865334.821 (08-Sep 10:02:14 ICT)"
                      mono
                    />
                    <KeyValue
                      label={t("cryptographicSeal")}
                      value="SHA-256 7f8a931c…3b401"
                      mono
                    />
                    <KeyValue label={t("enforcedPolicy")} value={denied.PolicyRef} />
                    <KeyValue label={t("requestor")} value={denied.AccountName} />
                    <KeyValue label={t("targetSubject")} value={denied.StudentCode} mono />
                    <KeyValue label={t("targetAttribute")} value={denied.AttributeLabel} />
                    <KeyValue label={t("declaredPurpose")} value={denied.PurposeLabel} />
                  </KeyValueGrid>

                  <div>
                    <p className="font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
                      {t("predicateEvaluation")}
                    </p>
                    <ul className="mt-space-sm space-y-space-xs">
                      {predicates.map((predicate) => (
                        <li
                          key={predicate.expression}
                          className="flex items-center justify-between gap-space-sm rounded border border-outline-variant/40 px-space-md py-space-sm"
                        >
                          <code className="min-w-0 truncate font-code-audit text-code-audit text-on-surface">
                            {predicate.expression}
                          </code>
                          <StatusChip
                            tone={predicate.pass ? "ontrack" : "urgent"}
                            icon={predicate.pass ? "check" : "close"}
                            size="sm"
                            label={predicate.pass ? t("pass") : t("fail")}
                          />
                        </li>
                      ))}
                    </ul>
                  </div>

                  <div className="rounded-md border border-outline-variant/40 bg-surface-container-low/50 p-space-md">
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      {t("revocationCorrelation")}
                    </p>
                    <KeyValueGrid columns={1} className="mt-space-sm">
                      <KeyValue
                        label={t("consentReference")}
                        value="CONS-001 (revoked 10:00 ICT)"
                      />
                      <KeyValue label={t("revocationReceipt")} value="RCPT-WDR-89012" mono />
                      <KeyValue
                        label={t("correlationToken")}
                        value="CORR-SEC-20260908-994102"
                        mono
                      />
                    </KeyValueGrid>
                    <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                      {t("correlationBody")}
                    </p>
                  </div>

                  <Notice tone="sealed" icon="verified_user" title={t("zeroPayloadTitle")}>
                    {t("zeroPayloadBody")}
                  </Notice>
                </PanelBody>
                <PanelFooter>
                  <span>{t("readOnly")}</span>
                  <StatusChip tone="ontrack" icon="verified" label={t("valid")} />
                </PanelFooter>
              </Panel>
            ) : null}

            <Panel>
              <PanelHeader icon="download_for_offline" title={t("exportTitle")} />
              <PanelBody className="space-y-space-base">
                <KeyValueGrid columns={1}>
                  <KeyValue label={t("dateRange")} value="01-Sep 00:00 – 08-Sep 23:59 ICT" />
                  <KeyValue label={t("matchingScope")} value="1,428 processing events" />
                  <KeyValue label={t("actorFilters")} value="All roles" />
                  <KeyValue
                    label={t("outcomeFilter")}
                    value={`${tc("permit")} (1,382) & ${tc("deny")} (46)`}
                  />
                </KeyValueGrid>

                <Notice tone="sealed" icon="verified_user" title={t("exportGuarantee")}>
                  {t("exportGuaranteeBody")}
                </Notice>

                <div className="rounded-md border border-outline-variant/40 bg-surface-container-low/50 p-space-md">
                  <div className="flex flex-wrap items-center justify-between gap-space-sm">
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      {t("packageReady")}
                    </p>
                    <StatusChip tone="referral" icon="inventory_2" label={t("staged")} />
                  </div>
                  <p className="mt-space-sm font-code-audit text-code-audit text-on-surface-variant">
                    {t("archive")}: ARCHIVE-EXP-20260908-419.json.gz
                  </p>
                  <p className="font-code-audit text-code-audit text-on-surface-variant">
                    {t("digest")}: sha256:2d184cfb92…8839ef
                  </p>
                </div>

                <Notice tone="attention" icon="warning" title={t("interruptedTitle")}>
                  <p>{t("interruptedBody")}</p>
                  <p className="mt-space-sm">
                    <Button size="sm" icon="refresh">
                      {t("retry")}
                    </Button>
                  </p>
                </Notice>
              </PanelBody>
              <PanelFooter>
                <Button size="sm" icon="download">
                  {t("download")}
                </Button>
              </PanelFooter>
            </Panel>
          </>
        }
      >
        <Panel>
          <PanelHeader
            icon="receipt_long"
            title={t("title")}
            actions={
              <StatusChip tone="sealed" icon="list" label={`${entries.length} events`} />
            }
          />
          <DataTable className="min-w-[900px]">
            <thead>
              <Tr className="hover:bg-transparent">
                <Th>{t("colEvent")}</Th>
                <Th>{t("colActor")}</Th>
                <Th>{t("colTarget")}</Th>
                <Th>{t("colDecision")}</Th>
                <Th>{t("colPolicy")}</Th>
              </Tr>
            </thead>
            <tbody>
              {entries.map((entry) => (
                <Tr key={entry.AccessLogId} highlighted={entry.Decision === "DENY"}>
                  <Td>
                    <p className="font-code-audit text-code-audit text-on-surface">
                      EVT-{entry.AccessLogId}
                    </p>
                    <p className="text-on-surface-variant">{entry.OccurredAt}</p>
                  </Td>
                  <Td>
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      {entry.AccountName}
                    </p>
                    <p className="text-on-surface-variant">{entry.AccountRoleLabel}</p>
                  </Td>
                  <Td>
                    <p className="font-code-audit text-code-audit text-on-surface">
                      {entry.StudentCode}
                    </p>
                    <p className="text-on-surface-variant">{entry.AttributeLabel}</p>
                  </Td>
                  <Td>
                    <StatusChip
                      tone={entry.Decision === "PERMIT" ? "ontrack" : "urgent"}
                      icon={entry.Decision === "PERMIT" ? "check_circle" : "block"}
                      label={entry.Decision === "PERMIT" ? tc("permit") : tc("deny")}
                    />
                    <p className="mt-1 text-on-surface-variant">{entry.DecisionReason}</p>
                  </Td>
                  <Td>
                    <span className="font-code-audit text-code-audit text-on-surface-variant">
                      {entry.PolicyRef}
                    </span>
                  </Td>
                </Tr>
              ))}
            </tbody>
          </DataTable>
          <PanelFooter>
            <span className="flex items-center gap-1.5">
              <Icon name="lock" className="text-[16px]" />
              {t("readOnly")} — no edit or delete primitives exist on this ledger.
            </span>
          </PanelFooter>
        </Panel>
      </InspectorLayout>
    </>
  );
}
