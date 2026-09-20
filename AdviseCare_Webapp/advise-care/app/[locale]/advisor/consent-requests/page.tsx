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
import type { AccessRequest, AccessRequestState } from "@/lib/types";

const stateTone: Record<AccessRequestState, "ontrack" | "attention" | "urgent" | "sealed"> = {
  PENDING: "attention",
  APPROVED: "ontrack",
  REJECTED: "urgent",
  EXPIRED: "sealed",
};

export default async function ConsentRequestsPage({
  params,
}: PageProps<"/[locale]/advisor/consent-requests">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.lists");
  const tcr = await getTranslations("advisor.consentRequest");
  const { requests } = await apiGet<{ requests: AccessRequest[] }>("/api/access-requests");

  return (
    <>
      <PageHeader
        title={t("consentTitle")}
        description={t("consentSubtitle")}
        actions={
          <ButtonLink href="/advisor/consent-requests/new" size="sm" icon="add">
            {t("newRequest")}
          </ButtonLink>
        }
      />

      <Notice tone="sealed" icon="policy" title={tcr("protocolTitle")}>
        {tcr("protocolBody")}
      </Notice>

      <Panel>
        <DataTable className="min-w-[900px]">
          <thead>
            <Tr className="hover:bg-transparent">
              <Th>{t("colRequest")}</Th>
              <Th>{t("colStudent")}</Th>
              <Th>{t("colType")}</Th>
              <Th>{t("colAttributes")}</Th>
              <Th>{t("colState")}</Th>
              <Th>{t("colJustification")}</Th>
            </Tr>
          </thead>
          <tbody>
            {requests.map((request) => (
              <Tr key={request.AccessRequestId}>
                <Td>
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    {request.RequestCode}
                  </p>
                  <p className="font-code-audit text-code-audit text-on-surface-variant">
                    {request.RaisedAt}
                  </p>
                </Td>
                <Td>
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    {request.StudentName}
                  </p>
                  <p className="font-code-audit text-code-audit text-on-surface-variant">
                    {request.StudentCode}
                  </p>
                </Td>
                <Td>{request.RequestType.replace("_", " ").toLowerCase()}</Td>
                <Td>
                  <ul className="space-y-space-xs">
                    {request.AttributeLabels.map((label) => (
                      <li key={label}>{label}</li>
                    ))}
                  </ul>
                </Td>
                <Td>
                  <StatusChip
                    tone={stateTone[request.State]}
                    icon={
                      request.State === "APPROVED"
                        ? "check_circle"
                        : request.State === "REJECTED"
                          ? "cancel"
                          : "hourglass_top"
                    }
                    label={request.State}
                  />
                </Td>
                <Td className="max-w-[340px]">{request.Justification}</Td>
              </Tr>
            ))}
          </tbody>
        </DataTable>
      </Panel>
    </>
  );
}
