import { getTranslations, setRequestLocale } from "next-intl/server";
import { InspectorLayout } from "@/components/layout/app-shell";
import {
  Button,
  ButtonLink,
  Field,
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
  TextArea,
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type { ConsentPurpose, SensitiveAttributeType } from "@/lib/types";

/** D01 — Consent purposes registry. */
export default async function PurposesPage({
  params,
}: PageProps<"/[locale]/steward/purposes">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("steward.purposes");
  const ts = await getTranslations("steward");
  const tc = await getTranslations("common");

  const { purposes, attributes } = await apiGet<{
    purposes: ConsentPurpose[];
    attributes: SensitiveAttributeType[];
  }>("/api/consent/purposes");

  const focus = purposes[0];
  const focusAttributes = attributes.filter((attribute) =>
    focus.AttributeCodes.includes(attribute.Code),
  );
  const excluded = attributes.filter(
    (attribute) =>
      attribute.IsSensitive && !focus.AttributeCodes.includes(attribute.Code),
  );

  return (
    <>
      <PageHeader
        eyebrow={[ts("console"), ts("federation")]}
        title={t("title")}
        description={t("subtitle")}
        meta={<StatusChip tone="sealed" icon="verified_user" label={ts("statutoryAudit")} />}
        actions={
          <Button size="sm" icon="add">
            {t("create")}
          </Button>
        }
      />

      <Notice tone="sealed" icon="layers" title={t("versionIsolation")}>
        {t("versionIsolationBody")}
      </Notice>

      <div className="grid gap-space-md xl:grid-cols-3">
        {purposes.map((purpose) => (
          <Panel key={purpose.PurposeId}>
            <PanelHeader
              icon="policy"
              title={purpose.Label}
              subtitle={`${purpose.Code} · v${purpose.Version}.0`}
              actions={
                <StatusChip tone="ontrack" icon="check_circle" label={t("inProduction")} />
              }
            />
            <PanelBody>
              <p className="font-body-sm text-body-sm text-on-surface-variant">
                {purpose.Description}
              </p>
              <KeyValueGrid columns={1} className="mt-space-base">
                <KeyValue label={t("role")} value={purpose.LegalBasis} />
                <KeyValue
                  label={t("attributes")}
                  value={`${purpose.AttributeCodes.length} scoped (unbundled)`}
                />
                <KeyValue
                  label="Retention"
                  value={`${purpose.DefaultRetentionDays} days`}
                />
              </KeyValueGrid>
            </PanelBody>
            <PanelFooter>
              <span className="font-code-audit text-code-audit">
                {purpose.GrantsActive} active grants
              </span>
              <StatusChip tone="sealed" icon="tag" label={`v${purpose.Version}`} />
            </PanelFooter>
          </Panel>
        ))}
      </div>

      <InspectorLayout
        inspector={
          <>
            <Notice tone="attention" icon="gavel" title={t("antiExpansionTitle")}>
              {t("antiExpansionBody")}
            </Notice>
            <Panel>
              <PanelHeader
                icon="translate"
                title={t("bilingualEditor")}
                subtitle={t("bilingualCaption")}
                actions={
                  <StatusChip
                    tone="ontrack"
                    icon="sync"
                    label={`${t("draftStatus")}: ${t("syncedWith")} v${focus.Version}`}
                  />
                }
              />
              <PanelBody className="space-y-space-base">
                <Field label={t("englishNotice")} hint={t("primaryLanguage")}>
                  <TextArea
                    rows={4}
                    defaultValue="Allows designated academic advisors to coordinate academic pacing, tutoring referrals and study adjustments during your degree. Consenting to financial circumstances coordinates fee relief without disclosing private bank statements."
                  />
                </Field>
                <Field label={t("vietnameseNotice")}>
                  <TextArea
                    rows={4}
                    defaultValue="Cho phép cố vấn học tập được chỉ định điều phối tiến độ học tập, giới thiệu phụ đạo và điều chỉnh kế hoạch học trong quá trình học của bạn. Việc đồng ý chia sẻ hoàn cảnh tài chính giúp điều phối miễn giảm học phí mà không tiết lộ sao kê ngân hàng."
                  />
                </Field>
              </PanelBody>
              <PanelFooter>
                <span>{tc("noResults") ? null : null}</span>
                <Button size="sm" icon="save">
                  {t("createVersion")}
                </Button>
              </PanelFooter>
            </Panel>
          </>
        }
      >
        <Panel>
          <PanelHeader
            icon="description"
            title={`${focus.Label} — ${t("detailTitle")}`}
            subtitle={`${focus.Code} · enforced across the student consent screens`}
            actions={<StatusChip tone="sealed" icon="lock" label={t("sealed")} />}
          />
          <PanelBody className="space-y-space-lg">
            <KeyValueGrid columns={3}>
              <KeyValue label={t("publishedVersion")} value={`Version ${focus.Version} (signed)`} />
              <KeyValue label={t("effectiveDate")} value="01 September 2026, 00:00 ICT" mono />
              <KeyValue label={t("boundRole")} value="Academic Advisor (Faculty of Computing)" />
            </KeyValueGrid>
            <p className="font-body-sm text-body-sm text-on-surface-variant">
              {t("boundRoleBody")}
            </p>

            <section>
              <h2 className="font-headline-sm text-headline-sm text-on-surface">
                {t("duration")}
              </h2>
              <KeyValueGrid columns={3} className="mt-space-md">
                <KeyValue
                  label={t("maxTerm")}
                  value={`Current academic term (max ${focus.DefaultRetentionDays} days)`}
                />
                <KeyValue
                  label={t("autoRevocation")}
                  value="Automatic key revocation on term completion"
                />
                <KeyValue label={t("gracePeriod")} value={t("gracePeriodValue")} />
              </KeyValueGrid>
            </section>

            <section>
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <h2 className="font-headline-sm text-headline-sm text-on-surface">
                  {t("coveredTypes")}
                </h2>
                <StatusChip tone="sealed" icon="schema" label={t("schemaBounds")} />
              </div>
              <ul className="mt-space-md space-y-space-sm">
                {focusAttributes.map((attribute) => (
                  <li
                    key={attribute.Code}
                    className="rounded-md border border-status-ontrack-border bg-status-ontrack-surface/25 px-space-base py-space-md"
                  >
                    <div className="flex flex-wrap items-center justify-between gap-space-sm">
                      <p className="font-label-md text-label-md font-semibold text-on-surface">
                        {attribute.Label}
                      </p>
                      <StatusChip tone="ontrack" icon="check_circle" label={t("eligible")} />
                    </div>
                    <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
                      {attribute.Code} · {attribute.Category}
                      {attribute.IsEncryptedAtRest ? " · encrypted at rest" : ""}
                    </p>
                  </li>
                ))}
              </ul>
            </section>

            <section>
              <h2 className="font-headline-sm text-headline-sm text-on-surface">
                {t("excludedTitle")}
              </h2>
              <ul className="mt-space-md space-y-space-sm">
                {excluded.map((attribute) => (
                  <li
                    key={attribute.Code}
                    className="flex flex-wrap items-center justify-between gap-space-sm rounded-md border border-outline-variant/50 bg-surface-container-low/50 px-space-base py-space-md"
                  >
                    <div className="min-w-0">
                      <p className="font-label-md text-label-md font-semibold text-on-surface">
                        {attribute.Label}
                      </p>
                      <p className="font-code-audit text-code-audit text-on-surface-variant">
                        {attribute.Code}
                      </p>
                    </div>
                    <StatusChip
                      tone={attribute.Category === "HEALTH" ? "urgent" : "sealed"}
                      icon={attribute.Category === "HEALTH" ? "block" : "swap_horiz"}
                      label={
                        attribute.Category === "HEALTH"
                          ? t("prohibited")
                          : t("assignedElsewhere")
                      }
                    />
                  </li>
                ))}
              </ul>
            </section>
          </PanelBody>
          <PanelFooter>
            <span className="flex items-center gap-1.5">
              <Icon name="verified" className="text-[16px]" />
              {t("auditVerified")} · HASH 994a…cf21
            </span>
            <ButtonLink href="/steward/policies" size="sm" iconAfter="arrow_forward">
              {t("openPolicies")}
            </ButtonLink>
          </PanelFooter>
        </Panel>
      </InspectorLayout>
    </>
  );
}
