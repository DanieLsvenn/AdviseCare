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
import type { ErasureExecution, ErasureStatus } from "@/lib/types";

const statusTone: Record<ErasureStatus, "ontrack" | "attention" | "urgent" | "referral"> = {
  VERIFIED: "ontrack",
  EXECUTED: "referral",
  PENDING: "attention",
  FAILED: "urgent",
};

/** D04 — Erasure queue. */
export default async function ErasureQueuePage({
  params,
}: PageProps<"/[locale]/steward/erasure">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("steward.erasure");
  const ts = await getTranslations("steward");

  const { erasures } = await apiGet<{ erasures: ErasureExecution[] }>("/api/erasures");
  const count = (status: ErasureStatus) =>
    erasures.filter((item) => item.Status === status).length;

  const principles = [
    { icon: "inventory_2", title: t("retentionTitle"), body: t("retentionBody") },
    { icon: "bolt", title: t("zeroLagTitle"), body: t("zeroLagBody") },
    { icon: "gpp_maybe", title: t("failedBarTitle"), body: t("failedBarBody") },
  ];

  return (
    <>
      <PageHeader
        eyebrow={[ts("console"), "Erasure queue (D04)"]}
        title={t("title")}
        description={t("subtitle")}
        meta={<StatusChip tone="sealed" icon="conveyor_belt" label={t("pipeline")} />}
        actions={
          <div className="flex flex-wrap gap-space-sm">
            <Button variant="secondary" size="sm" icon="sync">
              {t("syncStatus")}
            </Button>
            <Button size="sm" icon="verified">
              {t("runAudit")}
            </Button>
          </div>
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

      <Notice tone="sealed" icon="visibility_off">
        {t("metadataOnly")}
      </Notice>

      <Panel>
        <div className="flex flex-wrap items-center gap-space-md border-b border-outline-variant/30 px-space-lg py-space-base">
          {[
            { label: `${t("filterAll")} (${erasures.length})`, active: true },
            { label: `${t("filterPending")} (${count("PENDING")})` },
            { label: `${t("filterProcessing")} (${count("EXECUTED")})` },
            { label: `${t("filterVerified")} (${count("VERIFIED")})` },
            { label: `${t("filterFailed")} (${count("FAILED")})` },
          ].map((chip) => (
            <span
              key={chip.label}
              className={
                chip.active
                  ? "rounded border border-secondary bg-secondary-container/25 px-space-md py-1 font-label-md text-label-md font-semibold text-primary"
                  : "rounded border border-outline-variant/50 px-space-md py-1 font-label-md text-label-md text-on-surface-variant"
              }
            >
              {chip.label}
            </span>
          ))}
          <Select aria-label={t("colAttribute")} defaultValue="all" className="ml-auto w-auto">
            <option value="all">{t("colAttribute")}: all</option>
            <option value="fin">ATTR-FIN-001</option>
            <option value="res">ATTR-RES-004</option>
          </Select>
        </div>

        <DataTable className="min-w-[1040px]">
          <thead>
            <Tr className="hover:bg-transparent">
              <Th>{t("colJob")}</Th>
              <Th>{t("colAttribute")}</Th>
              <Th>{t("colPurpose")}</Th>
              <Th>{t("colWithdrawal")}</Th>
              <Th>{t("colAccessBlock")}</Th>
              <Th>{t("colErasure")}</Th>
              <Th align="right">{t("colVerification")}</Th>
            </Tr>
          </thead>
          <tbody>
            {erasures.map((job) => (
              <Tr key={job.ErasureId}>
                <Td>
                  <p className="font-code-audit text-code-audit text-on-surface">
                    {job.ErasureCode}
                  </p>
                  <p className="text-on-surface-variant">{job.StudentCode}</p>
                  <p className="font-code-audit text-code-audit text-on-surface-variant">
                    CG-{job.TriggeredByGrantId}
                  </p>
                </Td>
                <Td>{job.AttributeLabel}</Td>
                <Td>{job.PurposeLabel}</Td>
                <Td>
                  <span className="font-code-audit text-code-audit text-on-surface-variant">
                    {job.RequestedAt}
                  </span>
                </Td>
                <Td>
                  <StatusChip tone="ontrack" icon="lock" label={t("blocked")} />
                </Td>
                <Td>
                  <StatusChip
                    tone={statusTone[job.Status]}
                    icon={
                      job.Status === "VERIFIED"
                        ? "verified"
                        : job.Status === "FAILED"
                          ? "error"
                          : "autorenew"
                    }
                    label={job.Status}
                  />
                  <p className="mt-1 text-on-surface-variant">
                    {job.ValuesAffected} values affected
                  </p>
                </Td>
                <Td align="right">
                  <ButtonLink
                    href={`/steward/erasure/${job.ErasureCode}`}
                    variant="secondary"
                    size="sm"
                  >
                    {t("verify")}
                  </ButtonLink>
                </Td>
              </Tr>
            ))}
          </tbody>
        </DataTable>
      </Panel>

      <p className="font-body-sm text-body-sm text-on-surface-variant">{t("drilldownHint")}</p>
    </>
  );
}
