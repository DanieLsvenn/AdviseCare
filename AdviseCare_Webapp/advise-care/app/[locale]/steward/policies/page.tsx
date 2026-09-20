import { getTranslations, setRequestLocale } from "next-intl/server";
import {
  Button,
  ButtonLink,
  DataTable,
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
import type { AccessPolicy, ConsentPurpose } from "@/lib/types";

/** D02 — Access policies. */
export default async function PoliciesPage({
  params,
}: PageProps<"/[locale]/steward/policies">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("steward.policies");
  const ts = await getTranslations("steward");
  const tc = await getTranslations("common");

  const { policies, purposes } = await apiGet<{
    policies: AccessPolicy[];
    purposes: ConsentPurpose[];
  }>("/api/consent/policies");

  const purposeLabel = (code: string) =>
    purposes.find((purpose) => purpose.Code === code)?.Label ?? code;

  const permits = policies.filter((policy) => policy.Effect === "PERMIT");
  const denies = policies.filter((policy) => policy.Effect === "DENY");
  const highlighted = permits[1] ?? permits[0];

  return (
    <>
      <PageHeader
        eyebrow={[ts("console"), "Privacy subsystem (D02)"]}
        title={t("title")}
        description={t("subtitle")}
        meta={
          <div className="flex flex-wrap items-center gap-space-sm">
            <StatusChip tone="sealed" icon="lock" label={t("zeroBypass")} />
            <StatusChip tone="sealed" icon="gavel" label="Decree 13/2023 compliant" />
          </div>
        }
        actions={
          <div className="flex flex-wrap gap-space-sm">
            <Button variant="secondary" size="sm" icon="history">
              {t("ledger")}
            </Button>
            <Button size="sm" icon="add_moderator">
              {t("create")}
            </Button>
          </div>
        }
      />

      <Panel>
        <PanelHeader
          icon="rule"
          title={`${t("directory")} (${policies.length})`}
          subtitle={t("directoryCount", { active: permits.length, invariant: denies.length })}
        />
        <DataTable className="min-w-[980px]">
          <thead>
            <Tr className="hover:bg-transparent">
              <Th>{t("colPolicy")}</Th>
              <Th>{t("colRole")}</Th>
              <Th>{t("colPurpose")}</Th>
              <Th>{t("colAttribute")}</Th>
              <Th>{t("colConditions")}</Th>
              <Th>{t("colEffect")}</Th>
            </Tr>
          </thead>
          <tbody>
            {policies.map((policy) => (
              <Tr key={policy.PolicyId}>
                <Td>
                  <p className="font-code-audit text-code-audit text-on-surface">
                    POLICY-{String(policy.PolicyId).padStart(3, "0")}
                  </p>
                  <p className="text-on-surface-variant">v{policy.Version}.0</p>
                </Td>
                <Td>{policy.RoleLabel}</Td>
                <Td>{purposeLabel(policy.PurposeCode)}</Td>
                <Td>
                  <p>{policy.AttributeLabel}</p>
                  {policy.AttributeCode ? (
                    <p className="font-code-audit text-code-audit text-on-surface-variant">
                      {policy.AttributeCode}
                    </p>
                  ) : null}
                </Td>
                <Td>
                  <ul className="space-y-space-xs">
                    <li>
                      {tc("requiresConsent")}: {policy.RequiresConsent ? tc("yes") : tc("no")}
                    </li>
                    <li>
                      {tc("requiresApproval")}: {policy.RequiresApproval ? tc("yes") : tc("no")}
                    </li>
                  </ul>
                </Td>
                <Td>
                  <StatusChip
                    tone={policy.Effect === "PERMIT" ? "ontrack" : "urgent"}
                    icon={policy.Effect === "PERMIT" ? "check_circle" : "cancel"}
                    label={
                      policy.Effect === "PERMIT"
                        ? t("evaluationAllowed")
                        : t("evaluationDenied")
                    }
                  />
                </Td>
              </Tr>
            ))}
          </tbody>
        </DataTable>
      </Panel>

      <div className="grid gap-space-md xl:grid-cols-2">
        {highlighted ? (
          <Panel>
            <PanelHeader
              icon="verified_user"
              title={`${highlighted.RoleLabel} — ${highlighted.AttributeLabel}`}
              subtitle={`POLICY-${String(highlighted.PolicyId).padStart(3, "0")} · effective 01-Sep-2026 00:00 ICT`}
              actions={<StatusChip tone="ontrack" icon="bolt" label="Active" />}
            />
            <PanelBody>
              <KeyValueGrid columns={2}>
                <KeyValue label={t("principalRole")} value={highlighted.RoleLabel} />
                <KeyValue label={t("attributeToken")} value={highlighted.AttributeCode ?? "—"} />
                <KeyValue label={t("requiredState")} value={t("explicitConsent")} />
                <KeyValue label={t("bindingInvariant")} value="Direct advisee roster" />
              </KeyValueGrid>
              <p className="mt-space-base font-body-sm text-body-sm text-on-surface-variant">
                All three bindings must hold simultaneously. A single “can view sensitive data”
                boolean cannot express this, which is precisely the gap this registry closes.
              </p>
            </PanelBody>
            <PanelFooter>
              <span className="font-code-audit text-code-audit">
                {t("maxExp")}: 180 days
              </span>
              <StatusChip tone="ontrack" icon="check_circle" label={t("evaluationAllowed")} />
            </PanelFooter>
          </Panel>
        ) : null}

        <Panel className="border-status-urgent-border">
          <PanelHeader
            icon="shield"
            title={t("superuserTitle")}
            subtitle="POLICY-ADMIN-WILDCARD-DENY · v1.0"
            actions={<StatusChip tone="urgent" icon="lock" label={t("constitutional")} />}
          />
          <PanelBody>
            <p className="font-body-sm text-body-sm text-on-surface-variant">
              {t("superuserBody")}
            </p>
            <p className="mt-space-base">
              <StatusChip tone="urgent" icon="block" label={t("superuserBlocked")} />
            </p>
            <p className="mt-space-sm font-code-audit text-code-audit text-on-surface-variant">
              Decree 13 Art. 8 invariant · perpetual, non-delegable
            </p>
          </PanelBody>
        </Panel>
      </div>

      <Notice
        tone="sealed"
        icon="monitoring"
        title={t("inspectAudits")}
        actions={
          <ButtonLink href="/steward/access-records" size="sm" iconAfter="arrow_forward">
            {t("inspectAudits")}
          </ButtonLink>
        }
      >
        {t("inspectAuditsBody")}
      </Notice>
    </>
  );
}
