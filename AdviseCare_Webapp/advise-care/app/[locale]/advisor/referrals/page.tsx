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
import type { Referral, SupportService } from "@/lib/types";

export default async function ReferralsPage({
  params,
}: PageProps<"/[locale]/advisor/referrals">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.lists");
  const { referrals, services } = await apiGet<{
    referrals: Referral[];
    services: SupportService[];
  }>("/api/services");

  return (
    <>
      <PageHeader
        title={t("referralsTitle")}
        description={t("referralsSubtitle")}
        actions={
          <ButtonLink href="/advisor/referrals/new" size="sm" icon="add">
            {t("newReferral")}
          </ButtonLink>
        }
      />

      <Panel>
        <DataTable className="min-w-[760px]">
          <thead>
            <Tr className="hover:bg-transparent">
              <Th>{t("colReferral")}</Th>
              <Th>{t("colService")}</Th>
              <Th>{t("colStudent")}</Th>
              <Th>{t("colConsent")}</Th>
              <Th>{t("colState")}</Th>
            </Tr>
          </thead>
          <tbody>
            {referrals.map((referral) => (
              <Tr key={referral.ReferralId}>
                <Td>
                  <p className="font-code-audit text-code-audit text-on-surface">
                    {referral.ReferralCode}
                  </p>
                  <p className="text-on-surface-variant">{referral.RaisedAt}</p>
                </Td>
                <Td>{referral.ServiceLabel}</Td>
                <Td>
                  <span className="font-code-audit text-code-audit">{referral.StudentCode}</span>
                </Td>
                <Td>
                  <StatusChip tone="ontrack" icon="handshake" label={referral.ConsentGrantRef} />
                </Td>
                <Td>
                  <StatusChip
                    tone={referral.State === "CLOSED" ? "sealed" : "referral"}
                    icon="arrow_outward"
                    label={referral.State.replace("_", " ")}
                  />
                </Td>
              </Tr>
            ))}
          </tbody>
        </DataTable>
      </Panel>

      <Panel>
        <DataTable className="min-w-[640px]">
          <thead>
            <Tr className="hover:bg-transparent">
              <Th>{t("colService")}</Th>
              <Th>Category</Th>
              <Th>Description</Th>
            </Tr>
          </thead>
          <tbody>
            {services.map((service) => (
              <Tr key={service.ServiceId}>
                <Td>
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    {service.Label}
                  </p>
                  <p className="font-code-audit text-code-audit text-on-surface-variant">
                    {service.Code}
                  </p>
                </Td>
                <Td>
                  <StatusChip
                    tone={service.IsExternal ? "attention" : "sealed"}
                    icon={service.IsExternal ? "public" : "apartment"}
                    label={service.Category}
                  />
                </Td>
                <Td>{service.Description}</Td>
              </Tr>
            ))}
          </tbody>
        </DataTable>
      </Panel>
    </>
  );
}
