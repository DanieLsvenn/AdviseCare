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
  Panel,
  PanelBody,
  PanelFooter,
  PanelHeader,
  StatusChip,
  TextArea,
  TextInput,
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type { SensitiveAttributeType } from "@/lib/types";

const justification =
  "The advisee indicated during the in-person consultation on 7 Sep 2026 (NOTE-002) that living arrangement instability and continuous assessment fee pressure are affecting coursework completion for CS301. Cross-departmental welfare alignment is requested to evaluate emergency accommodation and hardship bursary eligibility.";

const requestedAttributes = [
  {
    code: "ATTR-RES-004",
    title: "Housing support need",
    category: "Residential & living circumstances",
    description:
      "Verification of active temporary accommodation disruption and urgent off-campus tenancy displacement.",
    boundary: "Housing officer consultation only. Hidden from academic instructors.",
    selected: true,
  },
  {
    code: "ATTR-FIN-001",
    title: "Financial circumstances",
    category: "Economic hardship & bursary",
    description:
      "Student self-declaration and fee support assessment eligibility for Fall 2026 emergency funding.",
    boundary: "Emergency hardship committee review only. Raw banking telemetry is not requested.",
    selected: true,
  },
  {
    code: "ATTR-MED-099",
    title: "Medical / clinical records",
    category: "Protected health category",
    description: "Clinical notes, diagnosis records or disability medical certificates.",
    boundary: "Excluded from this request — not relevant to immediate housing relief.",
    selected: false,
  },
];

/** A08 — Consent scope request composer. */
export default async function ConsentRequestPage({
  params,
}: PageProps<"/[locale]/advisor/consent-requests/new">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.consentRequest");
  const tc = await getTranslations("common");

  await apiGet<{ attributes: SensitiveAttributeType[] }>("/api/consent/grants");

  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-space-md">
        <h1 className="font-headline-md text-headline-md text-on-surface">{t("title")}</h1>
        <div className="flex flex-wrap items-center gap-space-sm">
          <StatusChip tone="referral" icon="pending_actions" label="Staging: REQ-001" />
          <StatusChip tone="sealed" icon="lock" label={`${t("protocolLevel")}: ${t("granular")}`} />
        </div>
      </div>

      <Notice tone="sealed" icon="policy" title={t("protocolTitle")}>
        <p>{t("protocolBody")}</p>
        <p className="mt-space-sm flex items-start gap-1.5">
          <Icon name="notifications_active" className="mt-0.5 text-[14px]" />
          <span>{t("zeroDisclosure")}</span>
        </p>
        <p className="mt-space-sm font-code-audit text-code-audit">SEC-CONS-2026 · ISO 27701 aligned</p>
      </Notice>

      <InspectorLayout
        inspector={
          <>
            <Notice tone="ontrack" icon="check_circle" title={t("dispatchedTitle")}>
              {t("dispatchedBody")}
            </Notice>

            <Panel>
              <PanelHeader icon="account_tree" title={t("pipeline")} />
              <PanelBody>
                <ol className="space-y-space-sm">
                  {[
                    { icon: "edit_note", label: t("pipelineAdvisor"), ref: "REQ-001" },
                    { icon: "how_to_reg", label: t("pipelineStudent"), ref: "Grant / partial / veto" },
                    { icon: "key", label: t("pipelineRecipient"), ref: "Scoped token" },
                  ].map((step, index) => (
                    <li
                      key={step.label}
                      className="flex items-start gap-space-md rounded-md border border-outline-variant/40 px-space-md py-space-sm"
                    >
                      <span className="flex size-6 shrink-0 items-center justify-center rounded bg-surface-container-high font-label-sm text-label-sm text-primary">
                        {index + 1}
                      </span>
                      <div className="min-w-0">
                        <p className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
                          <Icon name={step.icon} className="text-[16px]" />
                          {step.label}
                        </p>
                        <p className="font-code-audit text-code-audit text-on-surface-variant">
                          {step.ref}
                        </p>
                      </div>
                    </li>
                  ))}
                </ol>
              </PanelBody>
            </Panel>

            <Panel>
              <PanelBody className="space-y-space-md">
                {[
                  { icon: "shield", title: t("separationTitle"), body: t("separationBody") },
                  { icon: "verified_user", title: t("affirmationTitle"), body: t("affirmationBody") },
                  { icon: "rule", title: t("splitTitle"), body: t("splitBody") },
                  { icon: "cancel", title: t("revocationTitle"), body: t("revocationBody") },
                ].map((item) => (
                  <div key={item.title} className="flex items-start gap-space-md">
                    <Icon name={item.icon} className="mt-0.5 text-[18px] text-primary" />
                    <div className="min-w-0">
                      <p className="font-label-md text-label-md font-semibold text-on-surface">
                        {item.title}
                      </p>
                      <p className="mt-0.5 font-body-sm text-body-sm text-on-surface-variant">
                        {item.body}
                      </p>
                    </div>
                  </div>
                ))}
              </PanelBody>
              <PanelFooter>
                <span>Charter baseline</span>
                <span className="font-code-audit text-code-audit">
                  University Data Protection Mandate §14.8
                </span>
              </PanelFooter>
            </Panel>
          </>
        }
      >
        <Panel>
          <PanelHeader
            icon="rule"
            title={t("staging")}
            subtitle={t("stagingSubtitle")}
            actions={<StatusChip tone="sealed" icon="tag" label={`${t("stagingId")}: REQ-001`} />}
          />
          <PanelBody className="space-y-space-lg">
            <KeyValueGrid columns={3}>
              <KeyValue label={t("targetAdvisee")} value="Nguyen Van An · STU-2024-001" />
              <KeyValue label={t("requestingAdvisor")} value="Demo Advisor 01 · Computing" />
              <KeyValue label={t("timestamp")} value="07-SEP-2026 15:30 ICT" mono />
            </KeyValueGrid>

            <section>
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <h2 className="font-headline-sm text-headline-sm text-on-surface">{t("step1")}</h2>
                <StatusChip tone="sealed" icon="fence" label={t("step1Hint")} />
              </div>
              <div className="mt-space-md grid gap-space-sm sm:grid-cols-2">
                {[
                  {
                    icon: "volunteer_activism",
                    title: tc("welfareSupport"),
                    caption: "Emergency living, financial and emotional support services.",
                    detail:
                      "Circumstance disclosure is bound strictly to welfare coordination; it is not merged into general academic records.",
                    active: true,
                  },
                  {
                    icon: "school",
                    title: tc("academicAdvising"),
                    caption: "Timetable adjustment and faculty exam dispensation only.",
                    detail:
                      "Separate authorisation path. Excludes sensitive living hardship disclosures.",
                    active: false,
                  },
                ].map((option) => (
                  <label
                    key={option.title}
                    className={
                      option.active
                        ? "flex cursor-pointer gap-space-md rounded-md border border-secondary bg-secondary-container/20 px-space-base py-space-md"
                        : "flex cursor-pointer gap-space-md rounded-md border border-outline-variant/40 px-space-base py-space-md"
                    }
                  >
                    <input
                      type="radio"
                      name="purpose"
                      defaultChecked={option.active}
                      className="mt-1 accent-[#0f766e]"
                    />
                    <span className="min-w-0">
                      <span className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
                        <Icon name={option.icon} className="text-[16px]" />
                        {option.title}
                      </span>
                      <span className="mt-0.5 block font-body-sm text-body-sm text-on-surface-variant">
                        {option.caption}
                      </span>
                      <span className="mt-space-sm block font-body-sm text-body-sm text-on-surface-variant">
                        {option.detail}
                      </span>
                    </span>
                  </label>
                ))}
              </div>
            </section>

            <section>
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <h2 className="font-headline-sm text-headline-sm text-on-surface">{t("step2")}</h2>
                <StatusChip tone="referral" icon="how_to_vote" label={t("step2Hint")} />
              </div>
              <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                {t("antiBlanket")}
              </p>
              <ul className="mt-space-md space-y-space-sm">
                {requestedAttributes.map((attribute) => (
                  <li
                    key={attribute.code}
                    className="rounded-md border border-outline-variant/40 px-space-base py-space-md"
                  >
                    <div className="flex flex-wrap items-start justify-between gap-space-md">
                      <div className="min-w-0 flex-1">
                        <p className="font-label-md text-label-md font-semibold text-on-surface">
                          {attribute.title}
                        </p>
                        <p className="font-code-audit text-code-audit text-on-surface-variant">
                          {attribute.code} · {attribute.category}
                        </p>
                        <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                          {attribute.description}
                        </p>
                        <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                          <strong className="font-semibold text-on-surface">
                            {t("scopeBoundary")}:
                          </strong>{" "}
                          {attribute.boundary}
                        </p>
                      </div>
                      <StatusChip
                        tone={attribute.selected ? "ontrack" : "sealed"}
                        icon={attribute.selected ? "check_circle" : "remove_circle_outline"}
                        label={attribute.selected ? t("selected") : t("omitted")}
                      />
                    </div>
                  </li>
                ))}
              </ul>
            </section>

            <div className="grid gap-space-base sm:grid-cols-2">
              <section>
                <div className="flex flex-wrap items-center justify-between gap-space-sm">
                  <h2 className="font-headline-sm text-headline-sm text-on-surface">{t("step3")}</h2>
                  <StatusChip tone="sealed" icon="badge" label={t("step3Hint")} />
                </div>
                <div className="mt-space-md rounded-md border border-outline-variant/40 px-space-base py-space-md">
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    Student Welfare Officer
                  </p>
                  <p className="mt-0.5 font-body-sm text-body-sm text-on-surface-variant">
                    Student Welfare & Inclusion Services — recipients hold signed non-disclosure
                    agreements under the institutional privacy framework §4.2.
                  </p>
                </div>
              </section>

              <section>
                <div className="flex flex-wrap items-center justify-between gap-space-sm">
                  <h2 className="font-headline-sm text-headline-sm text-on-surface">{t("step4")}</h2>
                  <StatusChip tone="attention" icon="event_busy" label={t("step4Hint")} />
                </div>
                <div className="mt-space-md">
                  <Field label={tc("expiresAt")}>
                    <TextInput type="date" defaultValue="2026-12-31" />
                  </Field>
                  <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                    End of the Fall 2026 term (115 calendar days). The token auto-terminates on
                    that date and the student may revoke it at any time before then.
                  </p>
                </div>
              </section>
            </div>

            <section>
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <h2 className="font-headline-sm text-headline-sm text-on-surface">{t("step5")}</h2>
                <StatusChip tone="referral" icon="visibility" label={t("step5Hint")} />
              </div>
              <div className="mt-space-md space-y-space-base">
                <Field label={t("statedReason")}>
                  <TextInput defaultValue="Coordinate requested student support" />
                </Field>
                <Field label={tc("details")} required>
                  <TextArea rows={5} defaultValue={justification} maxLength={1000} />
                </Field>
                <div className="flex flex-wrap items-center justify-between gap-space-sm">
                  <StatusChip tone="ontrack" icon="verified" label={t("linkedNote")} />
                  <span className="font-code-audit text-code-audit text-on-surface-variant">
                    {justification.length} / 1000
                  </span>
                </div>
              </div>
            </section>
          </PanelBody>
          <PanelFooter>
            <span className="flex items-center gap-1.5">
              <Icon name="info" className="text-[16px]" />
              {t("dispatchNotice")}
            </span>
            <div className="flex flex-wrap gap-space-sm">
              <ButtonLink href="/advisor/cases/1" variant="tertiary" size="sm">
                {tc("cancel")}
              </ButtonLink>
              <Button size="sm" icon="send">
                {t("submit")} (REQ-001)
              </Button>
            </div>
          </PanelFooter>
        </Panel>
      </InspectorLayout>
    </>
  );
}
