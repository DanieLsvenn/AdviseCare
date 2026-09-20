import { getTranslations, setRequestLocale } from "next-intl/server";
import {
  DataTable,
  PageHeader,
  Panel,
  StatusChip,
  Td,
  Th,
  Tr,
} from "@/components/ui";
import { Link } from "@/i18n/navigation";
import { apiGet } from "@/lib/api";
import type { AdvisingCase, CasePriority } from "@/lib/types";

const priorityTone: Record<CasePriority, "ontrack" | "attention" | "urgent" | "sealed"> = {
  LOW: "sealed",
  NORMAL: "ontrack",
  HIGH: "attention",
  URGENT: "urgent",
};

export default async function CasesPage({ params }: PageProps<"/[locale]/advisor/cases">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.lists");
  const { cases } = await apiGet<{ cases: AdvisingCase[] }>("/api/cases");

  return (
    <>
      <PageHeader title={t("casesTitle")} description={t("casesSubtitle")} />
      <Panel>
        <DataTable className="min-w-[840px]">
          <thead>
            <Tr className="hover:bg-transparent">
              <Th>{t("colCase")}</Th>
              <Th>{t("colStudent")}</Th>
              <Th>{t("colStage")}</Th>
              <Th>{t("colPriority")}</Th>
              <Th>{t("colOpened")}</Th>
              <Th align="right">{t("colActions")}</Th>
            </Tr>
          </thead>
          <tbody>
            {cases.map((item) => (
              <Tr key={item.CaseId}>
                <Td>
                  <Link
                    href={`/advisor/cases/${item.CaseId}`}
                    className="font-label-md text-label-md font-semibold text-on-surface hover:text-primary"
                  >
                    {item.CaseNumber}
                  </Link>
                  <p className="mt-0.5 text-on-surface-variant">{item.Title}</p>
                </Td>
                <Td>
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    {item.StudentName}
                  </p>
                  <p className="font-code-audit text-code-audit text-on-surface-variant">
                    {item.StudentCode}
                  </p>
                </Td>
                <Td>
                  <StatusChip
                    tone={item.ClosedAt ? "sealed" : "referral"}
                    icon={item.ClosedAt ? "lock" : "conversion_path"}
                    label={item.CurrentState}
                  />
                </Td>
                <Td>
                  <StatusChip
                    tone={priorityTone[item.Priority]}
                    icon="flag"
                    label={item.Priority}
                  />
                </Td>
                <Td>
                  <span className="font-code-audit text-code-audit text-on-surface-variant">
                    {item.OpenedAt}
                  </span>
                </Td>
                <Td align="right">{item.OpenActions}</Td>
              </Tr>
            ))}
          </tbody>
        </DataTable>
      </Panel>
    </>
  );
}
