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
import type { AccessRequest } from "@/lib/types";

export default async function EmergencyAccessPage({
  params,
}: PageProps<"/[locale]/advisor/emergency-access">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.lists");
  const te = await getTranslations("advisor.emergency");
  const { requests } = await apiGet<{ requests: AccessRequest[] }>(
    "/api/access-requests?type=EMERGENCY_ACCESS",
  );

  return (
    <>
      <PageHeader
        title={t("emergencyTitle")}
        description={t("emergencySubtitle")}
        actions={
          <div className="flex flex-wrap gap-space-sm">
            <ButtonLink href="/advisor/emergency-access/active" variant="secondary" size="sm" icon="timelapse">
              {t("openActive")}
            </ButtonLink>
            <ButtonLink href="/advisor/emergency-access/request" size="sm" icon="add">
              {t("newEmergency")}
            </ButtonLink>
          </div>
        }
      />

      <Notice tone="attention" icon="gavel" title={te("protocolTitle")}>
        {te("protocolBody")}
      </Notice>

      <Panel>
        <DataTable className="min-w-[900px]">
          <thead>
            <Tr className="hover:bg-transparent">
              <Th>{t("colRequest")}</Th>
              <Th>{t("colStudent")}</Th>
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
                <Td>{request.AttributeLabels.join(", ")}</Td>
                <Td>
                  <StatusChip
                    tone={
                      request.State === "APPROVED"
                        ? "ontrack"
                        : request.State === "REJECTED"
                          ? "urgent"
                          : "attention"
                    }
                    icon={
                      request.State === "APPROVED"
                        ? "check_circle"
                        : request.State === "REJECTED"
                          ? "cancel"
                          : "hourglass_top"
                    }
                    label={request.State}
                  />
                  {request.DecidedBy ? (
                    <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
                      {request.DecidedBy} · {request.DecidedAt}
                    </p>
                  ) : null}
                </Td>
                <Td className="max-w-[320px]">{request.Justification}</Td>
              </Tr>
            ))}
          </tbody>
        </DataTable>
      </Panel>
    </>
  );
}
