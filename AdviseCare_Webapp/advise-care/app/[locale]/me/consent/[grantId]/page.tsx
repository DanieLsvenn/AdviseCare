import { getTranslations, setRequestLocale } from "next-intl/server";
import { InspectorLayout } from "@/components/layout/app-shell";
import {
  Button,
  ButtonLink,
  Checkbox,
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
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type { AccessRequest } from "@/lib/types";

const matrix = [
  {
    code: "ATTR-RES-004",
    title: "Housing-support need",
    category: "Residential & living circumstances",
    state: "consented" as const,
    tone: "ontrack" as const,
    icon: "check_circle",
    checked: true,
    scopeLabel: "disclosedScope" as const,
    scopeBody:
      "Verification of active temporary accommodation disruption and urgent off-campus tenancy displacement vouchers. Hidden from classroom instructors and examiners.",
    footnote: "Expires 31-DEC-2026 · subject to the real-time audit ledger",
  },
  {
    code: "ATTR-FIN-001",
    title: "Financial circumstances",
    category: "Economic hardship & bursary",
    state: "withheldLabel" as const,
    tone: "sealed" as const,
    icon: "do_not_disturb_on",
    checked: false,
    scopeLabel: "effectOfWithholding" as const,
    scopeBody:
      "Hardship tier self-declaration and fee support assessment records remain protected. Welfare officers cannot pre-fill or assess emergency subsistence bursary disbursements.",
    footnote: null,
  },
  {
    code: "ATTR-MED-099",
    title: "Medical / clinical records",
    category: "Special health category",
    state: "excluded" as const,
    tone: "urgent" as const,
    icon: "block",
    checked: false,
    scopeLabel: "disclosedScope" as const,
    scopeBody:
      "Omitted from the request scope by institutional privacy protocol. Welfare officers are categorically unauthorised to request clinical notes or diagnoses through this channel.",
    footnote: null,
  },
];

/** S03 — Review consent scope (student decision workbench). */
export default async function ReviewScopePage({
  params,
}: PageProps<"/[locale]/me/consent/[grantId]">) {
  const { locale, grantId } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("student.scope");
  const ts = await getTranslations("student.consent");
  const tc = await getTranslations("common");

  const { requests } = await apiGet<{ requests: AccessRequest[] }>("/api/access-requests");
  const request =
    requests.find((item) => String(item.AccessRequestId) === grantId) ??
    requests.find((item) => item.State === "PENDING")!;

  return (
    <>
      <PageHeader
        eyebrow={[t("workbench"), t("granularEval")]}
        title={t("title")}
        description={t("intro")}
        meta={
          <div className="flex flex-wrap items-center gap-space-sm">
            <StatusChip tone="sealed" icon="verified_user" label="PKI transaction seal ready" />
            <span className="font-code-audit text-code-audit text-on-surface-variant">
              REF: {request.RequestCode}::FALL26
            </span>
          </div>
        }
      />

      <Notice tone="sealed" icon="lock" title={t("zeroCoercion")}>
        Vietnam PDPD Decree 13/2023 compliant · article 9 unbundled student sovereignty.
      </Notice>

      <InspectorLayout
        inspector={
          <Panel className="border-secondary">
            <PanelHeader icon="chat" title={t("summaryTitle")} />
            <PanelBody className="space-y-space-base">
              <p className="font-body-base text-body-base text-on-surface">{t("summaryBody")}</p>
              <KeyValueGrid columns={1}>
                <KeyValue
                  label={t("resultingStatus")}
                  value={<StatusChip tone="ontrack" icon="pie_chart" label={t("grantedInPart")} />}
                />
                <KeyValue label={t("validity")} value="Today → 31 Dec 2026" />
                <KeyValue label={t("revocation")} value={t("revocationValue")} />
              </KeyValueGrid>
              <Notice tone="sealed" icon="verified">
                {t("immutableNote")}
              </Notice>
              <Button icon="save" className="w-full">
                {t("execute")}
              </Button>
            </PanelBody>
          </Panel>
        }
      >
        <Panel>
          <PanelHeader
            icon="mail"
            title={`${ts("reviewRequest")} — ${request.RequestCode}`}
            subtitle={request.PurposeLabel}
            actions={
              <StatusChip tone="attention" icon="pending_actions" label={ts("pendingAffirmation")} />
            }
          />
          <PanelBody className="space-y-space-base">
            <KeyValueGrid columns={2}>
              <KeyValue
                label={t("requestedBy")}
                value={`${request.RequesterName} — ${request.RequesterRoleLabel}`}
              />
              <KeyValue label={t("recipientRole")} value="Student Welfare Officer (RBAC level 3)" />
              <KeyValue label={t("requestedExpiry")} value={request.ExpiresAt ?? "—"} />
              <KeyValue
                label={t("associatedFile")}
                value={`${request.StudentName} (${request.StudentCode}) · ref NOTE-002`}
              />
            </KeyValueGrid>
            <p className="font-body-sm text-body-sm text-on-surface-variant">{t("rbacNote")}</p>
            <div>
              <h3 className="font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
                {t("statedPurpose")}
              </h3>
              <blockquote className="mt-space-sm border-l-2 border-l-outline-variant pl-space-base font-body-base text-body-base text-on-surface">
                {request.Justification}
              </blockquote>
            </div>
            <Notice tone="sealed" icon="shield" title={t("nonRetaliationTitle")}>
              {t("nonRetaliationBody")}
            </Notice>
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader
            icon="tune"
            title={t("matrixTitle")}
            subtitle={t("matrixSubtitle")}
            actions={<StatusChip tone="ontrack" icon="check_box" label={t("antiBlanket")} />}
          />
          <PanelBody className="space-y-space-md">
            <Notice tone="referral" icon="info">
              {t("antiBlanketBody")}
            </Notice>
            {matrix.map((row) => (
              <div
                key={row.code}
                className={
                  row.checked
                    ? "rounded-md border border-status-ontrack-border bg-status-ontrack-surface/25 px-space-base py-space-md"
                    : "rounded-md border border-outline-variant/40 px-space-base py-space-md"
                }
              >
                <div className="flex flex-wrap items-start justify-between gap-space-md">
                  <label className="flex min-w-0 flex-1 items-start gap-space-md">
                    <Checkbox
                      defaultChecked={row.checked}
                      disabled={row.state === "excluded"}
                      className="mt-1"
                    />
                    <span className="min-w-0">
                      <span className="block font-label-md text-label-md font-semibold text-on-surface">
                        {row.title}
                      </span>
                      <span className="block font-code-audit text-code-audit text-on-surface-variant">
                        {row.code} · {row.category}
                      </span>
                    </span>
                  </label>
                  <StatusChip tone={row.tone} icon={row.icon} label={t(row.state)} />
                </div>
                <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                  <strong className="font-semibold text-on-surface">{t(row.scopeLabel)}:</strong>{" "}
                  {row.scopeBody}
                </p>
                <p className="mt-space-sm flex items-center gap-1.5 font-code-audit text-code-audit text-on-surface-variant">
                  <Icon name={row.footnote ? "lock_clock" : "shield"} className="text-[14px]" />
                  {row.footnote ?? t("defaultState")}
                </p>
              </div>
            ))}
          </PanelBody>
          <PanelFooter>
            <ButtonLink href="/me/consent" variant="tertiary" size="sm" icon="arrow_back">
              {tc("back")}
            </ButtonLink>
            <Button size="sm" icon="save">
              {t("execute")}
            </Button>
          </PanelFooter>
        </Panel>
      </InspectorLayout>
    </>
  );
}
