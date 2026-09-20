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
import type { AccessRequest } from "@/lib/types";

const scopeMatrix = [
  {
    attribute: "Housing liaison & relocation status (ATTR-RES-004)",
    requested: true,
    availability: "available" as const,
    decision: "Eligible for a 48-hour ephemeral token",
  },
  {
    attribute: "Financial circumstances (ATTR-FIN-001)",
    requested: false,
    availability: "destroyed" as const,
    decision: "Erased under ERA-2026-0141 — cannot be restored by any grant",
  },
  {
    attribute: "Health & clinical wellbeing notes (ATTR-MED-009)",
    requested: false,
    availability: "outOfScope" as const,
    decision: "Categorically ineligible for academic disclosure",
  },
];

/** D06 — Emergency access review. */
export default async function EmergencyReviewPage({
  params,
}: PageProps<"/[locale]/steward/emergency-reviews">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("steward.emergency");
  const ts = await getTranslations("steward");
  const tc = await getTranslations("common");

  const { requests } = await apiGet<{ requests: AccessRequest[] }>(
    "/api/access-requests?type=EMERGENCY_ACCESS",
  );
  const pending = requests.find((request) => request.State === "PENDING") ?? requests[0];

  return (
    <>
      <PageHeader
        eyebrow={[ts("console"), "Emergency requests", "D06"]}
        title={`${t("title")} — ${pending.RequestCode}`}
        badge={
          <StatusChip tone="attention" icon="pending_actions" label={t("pendingAdjudication")} />
        }
        actions={
          <div className="flex flex-wrap gap-space-sm">
            <ButtonLink href="/steward/policies" variant="secondary" size="sm" icon="arrow_back">
              {t("backToQueue")}
            </ButtonLink>
            <ButtonLink
              href="/steward/access-records"
              variant="secondary"
              size="sm"
              icon="policy"
            >
              {t("viewRecords")}
            </ButtonLink>
          </div>
        }
      />

      <Notice tone="sealed" icon="verified_user" title={t("boundaryTitle")}>
        <p>{t("boundaryBody")}</p>
        <p className="mt-space-sm font-code-audit text-code-audit">
          Decree 13 / policy SEC-09 · article 14 §2
        </p>
      </Notice>

      <Notice tone="urgent" icon="lock_clock" title={t("supremacyTitle")}>
        <p>{t("supremacyBody")}</p>
        <p className="mt-space-sm">
          <StatusChip tone="urgent" icon="gavel" label={t("supremacyBadge")} />
        </p>
      </Notice>

      <InspectorLayout
        inspector={
          <>
            <Panel>
              <PanelHeader
                icon="inbox"
                title={t("queue")}
                subtitle={t("requisitions", { count: requests.length })}
              />
              <PanelBody className="space-y-space-sm">
                {requests.map((request) => (
                  <div
                    key={request.AccessRequestId}
                    className={
                      request.AccessRequestId === pending.AccessRequestId
                        ? "rounded-md border border-secondary bg-secondary-container/20 px-space-base py-space-md"
                        : "rounded-md border border-outline-variant/40 px-space-base py-space-md"
                    }
                  >
                    <div className="flex flex-wrap items-center justify-between gap-space-sm">
                      <p className="font-code-audit text-code-audit text-on-surface">
                        {request.RequestCode}
                      </p>
                      <StatusChip
                        tone={
                          request.State === "APPROVED"
                            ? "ontrack"
                            : request.State === "REJECTED"
                              ? "urgent"
                              : "attention"
                        }
                        size="sm"
                        label={request.State}
                      />
                    </div>
                    <p className="mt-1 font-label-md text-label-md font-semibold text-on-surface">
                      {request.StudentName}
                    </p>
                    <p className="font-body-sm text-body-sm text-on-surface-variant">
                      {request.StudentCode} · {tc("advisor")}: {request.RequesterName}
                    </p>
                  </div>
                ))}
              </PanelBody>
            </Panel>

            <Panel>
              <PanelHeader icon="verified" title={t("ledgerIntegrity")} />
              <PanelBody>
                <p className="font-body-sm text-body-sm text-on-surface-variant">
                  {t("ledgerIntegrityBody")}
                </p>
              </PanelBody>
            </Panel>

            <Panel>
              <PanelHeader icon="gavel" title={tc("decision")} />
              <PanelBody className="space-y-space-base">
                <Field label={t("decisionRationale")} required>
                  <TextArea
                    rows={4}
                    defaultValue="Proportionate and minimal: only the housing liaison attribute is required to reschedule a compulsory practical. A 48-hour ephemeral token is granted; all other attributes stay outside scope."
                  />
                </Field>
                <Button icon="key" className="w-full">
                  {t("grantWindow")}
                </Button>
                <Button variant="secondary" icon="block" className="w-full">
                  {t("reject")}
                </Button>
              </PanelBody>
            </Panel>
          </>
        }
      >
        <Panel>
          <PanelHeader
            icon="badge"
            title={t("credentials")}
            actions={
              <StatusChip tone="sealed" icon="tag" label={`${t("auditNonce")}: 0x99A3F…B712`} />
            }
          />
          <PanelBody className="space-y-space-base">
            <KeyValueGrid columns={4}>
              <KeyValue
                label={t("dataSubject")}
                value={`${pending.StudentName} · ${pending.StudentCode}`}
              />
              <KeyValue
                label={t("requester")}
                value={`${pending.RequesterName} (UID ADV-COMP-09)`}
              />
              <KeyValue
                label={t("statutoryBasis")}
                value="Pastoral emergency · PURP-EMERG-COORD-01"
              />
              <KeyValue label={t("relationship")} value="Assigned advisor · CASE-001" />
            </KeyValueGrid>

            <div>
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <h2 className="font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
                  {t("justification")}
                </h2>
                <span className="font-code-audit text-code-audit text-on-surface-variant">
                  {t("filed")}: {pending.RaisedAt}
                </span>
              </div>
              <blockquote className="mt-space-sm border-l-2 border-l-outline-variant pl-space-base font-body-base text-body-base text-on-surface">
                {pending.Justification}
              </blockquote>
            </div>
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader
            icon="schema"
            title={t("matrixTitle")}
            subtitle={t("matrixSubtitle")}
            actions={<StatusChip tone="sealed" icon="security" label={t("zeroTrust")} />}
          />
          <DataTable className="min-w-[760px]">
            <thead>
              <Tr className="hover:bg-transparent">
                <Th>{t("colAttribute")}</Th>
                <Th>{t("colRequested")}</Th>
                <Th>{t("colAvailability")}</Th>
                <Th>{t("colDecision")}</Th>
              </Tr>
            </thead>
            <tbody>
              {scopeMatrix.map((row) => (
                <Tr key={row.attribute}>
                  <Td>{row.attribute}</Td>
                  <Td>
                    <StatusChip
                      tone={row.requested ? "referral" : "sealed"}
                      icon={row.requested ? "check" : "remove"}
                      label={row.requested ? tc("yes") : tc("no")}
                    />
                  </Td>
                  <Td>
                    <StatusChip
                      tone={
                        row.availability === "available"
                          ? "ontrack"
                          : row.availability === "destroyed"
                            ? "urgent"
                            : "sealed"
                      }
                      icon={
                        row.availability === "available"
                          ? "key"
                          : row.availability === "destroyed"
                            ? "delete_forever"
                            : "block"
                      }
                      label={
                        row.availability === "available"
                          ? t("available")
                          : row.availability === "destroyed"
                            ? t("permanentlyUnavailable")
                            : t("outOfScope")
                      }
                    />
                  </Td>
                  <Td>{row.decision}</Td>
                </Tr>
              ))}
            </tbody>
          </DataTable>
          <PanelFooter>
            <span className="flex items-center gap-1.5">
              <Icon name="lock_clock" className="text-[16px]" />
              {t("supremacyBody")}
            </span>
          </PanelFooter>
        </Panel>
      </InspectorLayout>
    </>
  );
}
