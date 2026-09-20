import { getTranslations, setRequestLocale } from "next-intl/server";
import {
  ButtonLink,
  DataTable,
  PageHeader,
  Panel,
  StatusChip,
  Td,
  Th,
  Tr,
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type { CorrectionRequest } from "@/lib/types";

const stateTone = {
  SUBMITTED: "referral",
  UNDER_REVIEW: "attention",
  ADDENDUM_ADDED: "ontrack",
  DECLINED: "sealed",
} as const;

export default async function CorrectionsPage({
  params,
}: PageProps<"/[locale]/advisor/corrections">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.lists");
  const { corrections } = await apiGet<{ corrections: CorrectionRequest[] }>(
    "/api/corrections",
  );

  return (
    <>
      <PageHeader title={t("correctionsTitle")} description={t("correctionsSubtitle")} />
      <Panel>
        <DataTable className="min-w-[960px]">
          <thead>
            <Tr className="hover:bg-transparent">
              <Th>{t("colRequest")}</Th>
              <Th>{t("colStudent")}</Th>
              <Th>{t("colTarget")}</Th>
              <Th>{t("colDisputed")}</Th>
              <Th>{t("colProposed")}</Th>
              <Th>{t("colState")}</Th>
              <Th align="right">{t("review")}</Th>
            </Tr>
          </thead>
          <tbody>
            {corrections.map((correction) => (
              <Tr key={correction.CorrectionId}>
                <Td>
                  <p className="font-code-audit text-code-audit text-on-surface">
                    {correction.CorrectionCode}
                  </p>
                  <p className="text-on-surface-variant">{correction.SubmittedAt}</p>
                </Td>
                <Td>
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    {correction.StudentName}
                  </p>
                  <p className="font-code-audit text-code-audit text-on-surface-variant">
                    {correction.StudentCode}
                  </p>
                </Td>
                <Td>{correction.TargetLabel}</Td>
                <Td>{correction.DisputedValue}</Td>
                <Td>{correction.ProposedValue}</Td>
                <Td>
                  <StatusChip
                    tone={stateTone[correction.State]}
                    icon={
                      correction.State === "ADDENDUM_ADDED"
                        ? "post_add"
                        : correction.State === "DECLINED"
                          ? "block"
                          : "hourglass_top"
                    }
                    label={correction.State.replace("_", " ")}
                  />
                </Td>
                <Td align="right">
                  <ButtonLink
                    href={`/advisor/corrections/${correction.CorrectionCode}`}
                    variant="secondary"
                    size="sm"
                  >
                    {t("review")}
                  </ButtonLink>
                </Td>
              </Tr>
            ))}
          </tbody>
        </DataTable>
      </Panel>
    </>
  );
}
