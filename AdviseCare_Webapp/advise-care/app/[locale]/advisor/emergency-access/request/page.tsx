import { getTranslations, setRequestLocale } from "next-intl/server";
import { InspectorLayout } from "@/components/layout/app-shell";
import {
  Button,
  ButtonLink,
  Checkbox,
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
} from "@/components/ui";

const justification =
  "The student presented an urgent schedule conflict with an external agency appointment for temporary tenancy relocation. Temporary access to the housing case reference and liaison milestone is required strictly to adjust the examination deadline and the CS301 lab schedule.";

const scopeRows = [
  {
    code: "ATTR-RES-004",
    title: "Housing liaison & relocation status",
    state: "inScope" as const,
    tone: "ontrack" as const,
    icon: "check_circle",
    detail:
      "Neutral operational status: verifying the emergency tenancy relocation timetable in order to reschedule compulsory CS301 laboratory practicals and coordinate a commuter shuttle pass.",
  },
  {
    code: "ATTR-FIN-001",
    title: "Financial circumstances & bursary ledger",
    state: "excluded" as const,
    tone: "sealed" as const,
    icon: "remove_circle_outline",
    detail:
      "Financial hardship data is not essential for laboratory timetable accommodation. Excluded by the advisor's minimality assertion.",
  },
  {
    code: "ATTR-MED-009",
    title: "Health & clinical wellbeing notes",
    state: "firewalled" as const,
    tone: "urgent" as const,
    icon: "block",
    detail:
      "Health Centre records are restricted to licensed clinical practitioners under article 8. Ineligible for academic disclosure under any emergency scenario.",
  },
];

/** A13 — Emergency access request. */
export default async function EmergencyAccessRequestPage({
  params,
}: PageProps<"/[locale]/advisor/emergency-access/request">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.emergency");
  const tc = await getTranslations("common");

  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-space-md">
        <div className="flex items-center gap-space-sm">
          <Icon name="emergency_home" className="text-[22px] text-status-urgent-text" />
          <h1 className="font-headline-md text-headline-md text-on-surface">{t("title")}</h1>
        </div>
        <ButtonLink href="/advisor/students/1" variant="tertiary" size="sm" icon="arrow_back">
          {tc("cancel")}
        </ButtonLink>
      </div>

      <Notice tone="attention" icon="gavel" title={t("protocolTitle")}>
        <p>{t("protocolBody")}</p>
        <p className="mt-space-sm flex flex-wrap items-center gap-space-sm">
          <StatusChip tone="attention" icon="lock_clock" label={t("protocolBadge")} />
          <StatusChip tone="ontrack" icon="groups" label={t("stewardReady")} />
          <span className="font-code-audit text-code-audit">Decree 13 · policy SEC-09</span>
        </p>
      </Notice>

      <Notice tone="urgent" icon="shield_lock" title={t("currentState")}>
        <p className="font-code-audit text-code-audit">{t("httpState")}</p>
        <p className="mt-space-sm">
          <strong className="font-semibold text-on-surface">{t("ruleEnforcement")}:</strong> Rule 3 —
          attribute isolation & purpose boundary
        </p>
        <p className="mt-space-sm">{t("ruleBody")}</p>
      </Notice>

      <InspectorLayout
        inspector={
          <>
            <Notice tone="ontrack" icon="mark_email_read" title={t("lodgedTitle")}>
              <p>{t("pendingBody")}</p>
              <p className="mt-space-sm font-code-audit text-code-audit">
                {t("lodgedRef")}: EMRG-REQ-2026-089 · sha256:7f83b165…948bc · 08-Sep-2026 14:02:11 ICT
              </p>
              <p className="mt-space-sm">
                <StatusChip tone="attention" icon="hourglass_top" label={t("pendingReview")} />
              </p>
              <p className="mt-space-sm">{t("zeroLeak")}</p>
            </Notice>

            <Panel>
              <PanelHeader
                icon="verified"
                title={t("dispatchTitle")}
                subtitle="Block #441,892"
              />
              <PanelBody>
                <KeyValueGrid columns={1}>
                  <KeyValue
                    label={t("requesterIdentity")}
                    value="Demo Advisor 01 (UID ADV-COMP-09)"
                  />
                  <KeyValue label={tc("faculty")} value="Faculty of Computing" />
                  <KeyValue label={t("targetSubject")} value="STU-2024-001 (DEMO-001)" mono />
                  <KeyValue label={t("requestedScope")} value="ATTR-RES-004 (housing status only)" />
                  <KeyValue label={t("stewardPool")} value="GRP-STEWARD-SEC-09" mono />
                  <KeyValue
                    label={t("immediateDecryption")}
                    value={<StatusChip tone="urgent" icon="lock" label={t("blocked")} />}
                  />
                </KeyValueGrid>
              </PanelBody>
              <PanelFooter>
                <ButtonLink
                  href="/advisor/emergency-access/active"
                  variant="tertiary"
                  size="sm"
                  iconAfter="arrow_forward"
                >
                  {tc("viewAll")}
                </ButtonLink>
              </PanelFooter>
            </Panel>
          </>
        }
      >
        <Panel>
          <PanelHeader
            icon="assignment"
            title={t("requisition")}
            subtitle={t("requisitionCaption")}
            actions={<StatusChip tone="sealed" icon="history" label="FORM-A13-REV4" />}
          />
          <PanelBody className="space-y-space-lg">
            <KeyValueGrid columns={2}>
              <KeyValue
                label={t("targetRecord")}
                value="Nguyen Van An · STU-2024-001 · B.Sc. Computing (Yr 3)"
              />
              <KeyValue label={t("linkedCase")} value="CASE-001 (advisory & timetable friction)" />
            </KeyValueGrid>

            <div className="rounded-md border border-outline-variant/40 bg-surface-container-low/40 px-space-base py-space-md">
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <p className="font-label-md text-label-md font-semibold text-on-surface">
                  {t("permittedPurpose")}
                </p>
                <span className="font-code-audit text-code-audit text-on-surface-variant">
                  {t("immutableCode")}: PURP-EMERG-COORD-01
                </span>
              </div>
              <p className="mt-space-sm flex items-start gap-1.5 font-body-sm text-body-sm text-on-surface">
                <Icon name="verified_user" className="mt-0.5 text-[16px] text-primary" />
                Statutory emergency pastoral coordination & academic rescheduling
              </p>
              <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                {t("purposeBody")}
              </p>
            </div>

            <section>
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <h2 className="font-headline-sm text-headline-sm text-on-surface">
                  {t("attributeScope")}
                </h2>
                <StatusChip tone="sealed" icon="compress" label={t("strictMinimality")} />
              </div>
              <ul className="mt-space-md space-y-space-sm">
                {scopeRows.map((row) => (
                  <li
                    key={row.code}
                    className="flex flex-wrap items-start justify-between gap-space-md rounded-md border border-outline-variant/40 px-space-base py-space-md"
                  >
                    <div className="min-w-0 flex-1">
                      <p className="font-label-md text-label-md font-semibold text-on-surface">
                        {row.title}
                      </p>
                      <p className="font-code-audit text-code-audit text-on-surface-variant">
                        {row.code}
                      </p>
                      <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                        {row.detail}
                      </p>
                    </div>
                    <StatusChip tone={row.tone} icon={row.icon} label={t(row.state)} />
                  </li>
                ))}
              </ul>
            </section>

            <section>
              <p className="font-label-md text-label-md font-medium text-on-surface">
                {t("duration")} *
              </p>
              <div className="mt-space-sm grid gap-space-sm sm:grid-cols-3">
                {[
                  { id: "24", label: t("duration24"), active: false },
                  { id: "48", label: t("duration48"), active: true },
                  { id: "72", label: t("duration72"), active: false },
                ].map((option) => (
                  <label
                    key={option.id}
                    className={
                      option.active
                        ? "flex cursor-pointer items-start gap-space-sm rounded-md border border-secondary bg-secondary-container/20 px-space-md py-space-sm"
                        : "flex cursor-pointer items-start gap-space-sm rounded-md border border-outline-variant/40 px-space-md py-space-sm"
                    }
                  >
                    <input
                      type="radio"
                      name="duration"
                      defaultChecked={option.active}
                      className="mt-1 accent-[#0f766e]"
                    />
                    <span className="font-body-sm text-body-sm text-on-surface">{option.label}</span>
                  </label>
                ))}
              </div>
              <p className="mt-space-sm font-code-audit text-code-audit text-on-surface-variant">
                {t("validityBounds")}: 08-Sep-2026 14:00 ICT → 10-Sep-2026 14:00 ICT
              </p>
            </section>

            <div>
              <Field label={t("justification")} required>
                <TextArea rows={4} defaultValue={justification} maxLength={1000} />
              </Field>
              <div className="mt-space-xs flex flex-wrap items-center justify-between gap-space-sm">
                <StatusChip tone="sealed" icon="gavel" label="FERPA / statutory record" />
                <span className="font-code-audit text-code-audit text-on-surface-variant">
                  {justification.length} / 1000
                </span>
              </div>
              <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                {t("archiveNotice")}
              </p>
            </div>

            <label className="flex items-start gap-space-md rounded-md border border-outline-variant/40 bg-surface-container-low/40 px-space-base py-space-md">
              <Checkbox defaultChecked className="mt-1" />
              <span className="font-body-sm text-body-sm text-on-surface-variant">
                {t("acknowledgement")}
              </span>
            </label>
          </PanelBody>
          <PanelFooter>
            <ButtonLink href="/advisor/students/1" variant="tertiary" size="sm">
              {tc("cancel")}
            </ButtonLink>
            <Button size="sm" icon="send">
              {t("submit")}
            </Button>
          </PanelFooter>
        </Panel>
      </InspectorLayout>
    </>
  );
}
