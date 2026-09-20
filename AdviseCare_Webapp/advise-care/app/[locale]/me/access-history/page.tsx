import { getTranslations, setRequestLocale } from "next-intl/server";
import {
  Button,
  ButtonLink,
  DataTable,
  Icon,
  Notice,
  PageHeader,
  Panel,
  Select,
  StatusChip,
  Td,
  Th,
  Tr,
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type { AccessLogEntry } from "@/lib/types";

/** S06 — My access history & audit log. */
export default async function AccessHistoryPage({
  params,
}: PageProps<"/[locale]/me/access-history">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("student.access");
  const tc = await getTranslations("common");

  const { entries } = await apiGet<{ entries: AccessLogEntry[] }>(
    "/api/access-log?studentCode=STU-2024-001",
  );

  const allowed = entries.filter((entry) => entry.Decision === "PERMIT").length;
  const denied = entries.length - allowed;

  const principles = [
    { icon: "visibility", title: t("zeroHidden"), body: t("zeroHiddenBody") },
    { icon: "policy", title: t("purposeBound"), body: t("purposeBoundBody") },
    { icon: "shield_lock", title: t("instantSeverance"), body: t("instantSeveranceBody") },
  ];

  return (
    <>
      <PageHeader
        eyebrow={[t("protocol"), "PDPD Decree 13/2023 Art. 9 · ISO-27701"]}
        title={t("title")}
        description={t("intro")}
        meta={<StatusChip tone="sealed" icon="link" label="LEDGER SYNCED #BLK-20260908-8842" />}
        actions={
          <ButtonLink href="/me/export" size="sm" iconAfter="arrow_forward">
            {t("exportLink")}
          </ButtonLink>
        }
      />

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

      <Panel>
        <div className="flex flex-wrap items-center gap-space-md border-b border-outline-variant/30 px-space-lg py-space-base">
          <Select aria-label={t("filterRange")} defaultValue="30" className="w-auto">
            <option value="30">{t("range30")}</option>
            <option value="7">{t("range7")}</option>
            <option value="90">{t("range90")}</option>
            <option value="year">{t("rangeYear")}</option>
          </Select>
          <Select aria-label={t("filterPurpose")} defaultValue="all" className="w-auto">
            <option value="all">{t("allPurposes")}</option>
            <option value="academic">{tc("academicAdvising")}</option>
            <option value="welfare">{tc("welfareSupport")}</option>
          </Select>
          <Select aria-label={t("filterResult")} defaultValue="all" className="w-auto">
            <option value="all">{t("allResults")}</option>
            <option value="allowed">{`${t("allowedOnly")} (${allowed})`}</option>
            <option value="denied">{`${t("deniedOnly")} (${denied})`}</option>
          </Select>
          <Button variant="secondary" size="sm" icon="restart_alt">
            {t("resetFilters")}
          </Button>
          <p className="ml-auto font-body-sm text-body-sm text-on-surface-variant">
            {t("showing", { count: entries.length, studentCode: "STU-2024-001" })}
          </p>
        </div>

        <DataTable className="min-w-[900px]">
          <thead>
            <Tr className="hover:bg-transparent">
              <Th>{t("colActor")}</Th>
              <Th>{t("colTimestamp")}</Th>
              <Th>{t("colCategory")}</Th>
              <Th>{t("colResult")}</Th>
              <Th>{t("colAudit")}</Th>
            </Tr>
          </thead>
          <tbody>
            {entries.map((entry) => (
              <Tr key={entry.AccessLogId}>
                <Td>
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    {entry.AccountName}
                  </p>
                  <p className="text-on-surface-variant">{entry.AccountRoleLabel}</p>
                </Td>
                <Td>
                  <span className="font-code-audit text-code-audit text-on-surface">
                    {entry.OccurredAt}
                  </span>
                </Td>
                <Td>
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    {entry.AttributeLabel}
                  </p>
                  <p className="text-on-surface-variant">
                    {tc("purpose")}: {entry.PurposeLabel}
                  </p>
                </Td>
                <Td>
                  <StatusChip
                    tone={entry.Decision === "PERMIT" ? "ontrack" : "urgent"}
                    icon={entry.Decision === "PERMIT" ? "check_circle" : "block"}
                    label={entry.Decision === "PERMIT" ? t("allowed") : t("denied")}
                  />
                  <p className="mt-1 text-on-surface-variant">{entry.DecisionReason}</p>
                </Td>
                <Td>
                  <span className="font-code-audit text-code-audit text-on-surface-variant">
                    {entry.PolicyRef}
                    {entry.ConsentGrantRef ? ` · ${entry.ConsentGrantRef}` : ""}
                  </span>
                </Td>
              </Tr>
            ))}
          </tbody>
        </DataTable>
      </Panel>

      <Notice tone="sealed" icon="gpp_good">
        {tc("unavailableExplainer")}
      </Notice>
    </>
  );
}
