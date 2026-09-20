import { getTranslations, setRequestLocale } from "next-intl/server";
import {
  ButtonLink,
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

/** S04 — Withdraw financial-circumstances consent. */
export default async function WithdrawConsentPage({
  params,
}: PageProps<"/[locale]/me/consent/[grantId]/withdraw">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("student.withdraw");
  const tc = await getTranslations("common");

  const rows = [
    {
      item: "Financial circumstances",
      context: "Under CONS-001 (academic advising)",
      current: "Granted (active)",
      effect: "Immediate access cut-off; enters the erasure pipeline",
      post: tc("withdrawn"),
      tone: "urgent" as const,
    },
    {
      item: "Other active consent choices",
      context: "Other purposes & pending requests",
      current: "Configured as set",
      effect: "Stay exactly as they are — zero cross-impact",
      post: "Unchanged",
      tone: "ontrack" as const,
    },
    {
      item: "Past advising consultation notes",
      context: "NOTE-001, NOTE-002 in the immutable ledger",
      current: "Committed & sealed",
      effect: "Entries remain intact; referenced circumstance details cannot be reopened",
      post: "Preserved",
      tone: "sealed" as const,
    },
    {
      item: "University account & course standing",
      context: "Enrolment, VLE, exams, student services",
      current: "Active / enrolled",
      effect: "Full unconditional access; zero academic or administrative penalty",
      post: "Protected",
      tone: "ontrack" as const,
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow={["Privacy governance", "S04 · Consent revocation"]}
        title={t("title")}
        badge={<StatusChip tone="sealed" icon="verified" label={t("sovereignRight")} />}
      />

      <Panel>
        <PanelBody>
          <KeyValueGrid columns={3}>
            <KeyValue
              label={t("targetScope")}
              value="Purpose: academic advising · CONS-001"
            />
            <KeyValue
              label={t("authorisedRole")}
              value="Academic Advisor (Faculty of Computing)"
            />
            <KeyValue
              label={t("attributeToWithdraw")}
              value={
                <span className="flex items-center gap-1.5">
                  <Icon name="payments" className="text-[16px]" />
                  Financial circumstances (ATTR-FIN-001)
                </span>
              }
            />
          </KeyValueGrid>
          <p className="mt-space-md font-code-audit text-code-audit text-on-surface-variant">
            {t("valueStatus")}
          </p>
        </PanelBody>
      </Panel>

      <Notice tone="attention" icon="info" title={t("noticeTitle")}>
        {t("noticeBody")}
      </Notice>

      <Notice tone="sealed" icon="lock_reset" title={t("safeguardTitle")}>
        {t("safeguardBody")}
      </Notice>

      <Panel>
        <PanelHeader
          icon="schema"
          title={t("affectedScope")}
          subtitle="Evaluation for Nguyen Van An (STU-2024-001)"
        />
        <DataTable className="min-w-[880px]">
          <thead>
            <Tr className="hover:bg-transparent">
              <Th>{t("colItem")}</Th>
              <Th>{t("colCurrent")}</Th>
              <Th>{t("colEffect")}</Th>
              <Th>{t("colPost")}</Th>
            </Tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <Tr key={row.item}>
                <Td>
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    {row.item}
                  </p>
                  <p className="text-on-surface-variant">{row.context}</p>
                </Td>
                <Td>{row.current}</Td>
                <Td>{row.effect}</Td>
                <Td>
                  <StatusChip
                    tone={row.tone}
                    icon={
                      row.tone === "urgent"
                        ? "cancel"
                        : row.tone === "sealed"
                          ? "lock"
                          : "check_circle"
                    }
                    label={row.post}
                  />
                </Td>
              </Tr>
            ))}
          </tbody>
        </DataTable>
        <PanelFooter>
          <span className="flex items-center gap-1.5">
            <Icon name="check_circle" className="text-[16px] text-status-ontrack-text" />
            {t("isolationOk")}
          </span>
          <span className="font-code-audit text-code-audit">STU-ISOLATION-OK</span>
        </PanelFooter>
      </Panel>

      <Notice tone="sealed" icon="policy" title={t("guaranteeTitle")}>
        {t("guaranteeBody")}
      </Notice>

      <Panel>
        <PanelFooter>
          <span className="flex items-center gap-1.5">
            <Icon name="arrow_back" className="text-[16px]" />
            {t("cancelNote")}
          </span>
          <div className="flex flex-wrap gap-space-sm">
            <ButtonLink href="/me/consent" variant="secondary" size="sm" icon="close">
              {t("keepConsent")}
            </ButtonLink>
            <ButtonLink
              href="/me/consent/receipts/RCPT-WDR-89012"
              size="sm"
              icon="cancel_presentation"
            >
              {t("proceed")}
            </ButtonLink>
          </div>
        </PanelFooter>
      </Panel>
    </>
  );
}
