import { getTranslations, setRequestLocale } from "next-intl/server";
import {
  ButtonLink,
  DataTable,
  Notice,
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

export default async function MyCorrectionsPage({
  params,
}: PageProps<"/[locale]/me/corrections">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("student.correction");
  const tl = await getTranslations("advisor.lists");
  const { corrections } = await apiGet<{ corrections: CorrectionRequest[] }>(
    "/api/corrections",
  );
  const mine = corrections.filter((item) => item.StudentCode === "STU-2024-001");

  return (
    <>
      <PageHeader
        title={t("title")}
        description={t("rightBody")}
        actions={
          <ButtonLink href="/me/corrections/new" size="sm" icon="add">
            {t("submit")}
          </ButtonLink>
        }
      />

      <Notice tone="sealed" icon="lock_clock" title={t("nonOverwriting")}>
        {t("nonOverwritingBody")}
      </Notice>

      <Panel>
        <DataTable className="min-w-[860px]">
          <thead>
            <Tr className="hover:bg-transparent">
              <Th>{tl("colRequest")}</Th>
              <Th>{tl("colTarget")}</Th>
              <Th>{tl("colDisputed")}</Th>
              <Th>{tl("colProposed")}</Th>
              <Th>{tl("colState")}</Th>
              <Th>{t("assignedReviewer")}</Th>
            </Tr>
          </thead>
          <tbody>
            {mine.map((correction) => (
              <Tr key={correction.CorrectionId}>
                <Td>
                  <p className="font-code-audit text-code-audit text-on-surface">
                    {correction.CorrectionCode}
                  </p>
                  <p className="text-on-surface-variant">{correction.SubmittedAt}</p>
                </Td>
                <Td>{correction.TargetLabel}</Td>
                <Td>{correction.DisputedValue}</Td>
                <Td>{correction.ProposedValue}</Td>
                <Td>
                  <StatusChip
                    tone={stateTone[correction.State]}
                    icon={correction.State === "ADDENDUM_ADDED" ? "post_add" : "hourglass_top"}
                    label={correction.State.replace("_", " ")}
                  />
                  {correction.ReviewerOutcome ? (
                    <p className="mt-1 text-on-surface-variant">{correction.ReviewerOutcome}</p>
                  ) : null}
                </Td>
                <Td>{correction.ReviewerName ?? "—"}</Td>
              </Tr>
            ))}
          </tbody>
        </DataTable>
      </Panel>
    </>
  );
}
