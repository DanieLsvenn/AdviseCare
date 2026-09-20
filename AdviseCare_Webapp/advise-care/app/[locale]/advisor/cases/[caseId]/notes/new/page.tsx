import { getTranslations, setRequestLocale } from "next-intl/server";
import { InspectorLayout } from "@/components/layout/app-shell";
import {
  Button,
  ButtonLink,
  Eyebrow,
  Field,
  Icon,
  KeyValue,
  KeyValueGrid,
  Notice,
  Panel,
  PanelBody,
  PanelFooter,
  PanelHeader,
  Select,
  StatusChip,
  TextArea,
  TextInput,
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type { AdvisingCase, LedgerEntry } from "@/lib/types";

const draftObservations =
  "In-person consultation held at Advisor Office 402 with Nguyen Van An following the CS301 midterm deficit (42/100) and the recent lab attendance correction. The advisee stated difficulty managing concurrent software architecture project deadlines alongside CS301 lab assignments, confirmed that attendance was reconciled on 7 Sep after the reader sensor failure on 2 Sep, and expressed anxiety about continuous assessment pacing.";

const draftActions =
  "1. Advisee to schedule twice-weekly peer-tutoring sessions via the Faculty Peer Learning Centre for CS301 algorithms.\n2. Advisor to initiate a formal referral check with the Academic Skills Hub.\n3. Weekly milestone check-in scheduled for 14 Sep 2026 at 10:00 ICT.";

/** A05 — Draft advising note. */
export default async function DraftNotePage({
  params,
}: PageProps<"/[locale]/advisor/cases/[caseId]/notes/new">) {
  const { locale, caseId } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.note");
  const tc = await getTranslations("common");

  const data = await apiGet<{ case: AdvisingCase; entries: LedgerEntry[] }>(
    `/api/cases/${caseId}`,
  );
  const predecessor = data.entries.at(-1);

  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-space-md">
        <div>
          <Eyebrow items={[data.case.CaseNumber, `${t("draftTitle")} (NOTE-002)`]} />
          <p className="mt-space-xs font-body-sm text-body-sm text-on-surface-variant">
            {t("draftSavedAt", { time: "15:02 ICT", edits: 3 })}
          </p>
        </div>
        <StatusChip tone="referral" icon="save" label={t("draftBadge")} />
      </div>

      <Notice tone="attention" icon="lock_clock" title={t("localOnlyTitle")}>
        <p>{t("localOnlyBody")}</p>
        <p className="mt-space-sm">
          <StatusChip tone="attention" icon="pending" label={t("localOnlyBadge")} />
        </p>
      </Notice>

      <InspectorLayout
        inspector={
          <>
            <Panel>
              <PanelHeader
                icon="dataset"
                title={t("telemetrySnapshot")}
                subtitle="07-SEP-2026 15:00 ICT"
              />
              <PanelBody className="space-y-space-base">
                <div>
                  <div className="flex items-center justify-between gap-space-sm">
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      {t("attendanceTelemetry")}
                    </p>
                    <StatusChip tone="ontrack" icon="check_circle" label="90.0% verified" />
                  </div>
                  <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                    Source: biometric RFID turnstile and room sensor telemetry. Correction
                    reconciled 7 Sep 11:15 ICT for the 2 Sep lab latency (#ATT-V1-2026-0907-1400).
                  </p>
                </div>
                <div className="border-t border-outline-variant/30 pt-space-md">
                  <div className="flex items-center justify-between gap-space-sm">
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      {t("sisStanding")}
                    </p>
                    <StatusChip tone="sealed" icon="sync" label="v3.2 synced 08:30" />
                  </div>
                  <KeyValueGrid columns={1} className="mt-space-sm">
                    <KeyValue label="CS301 midterm examination" value="42.0% (deficit)" />
                    <KeyValue label="CS304 software architecture" value="Pending release" />
                    <KeyValue label="Cumulative programme GPA" value="2.74 / 4.00" />
                  </KeyValueGrid>
                </div>
              </PanelBody>
            </Panel>

            <Panel>
              <PanelHeader
                icon="shield_person"
                title={t("protectedBoundary")}
                actions={<StatusChip tone="ontrack" icon="vpn_key" label="ATTR-FIN-001" />}
              />
              <PanelBody>
                <p className="font-body-sm text-body-sm text-on-surface-variant">
                  {t("protectedBody")}
                </p>
                <p className="mt-space-sm font-code-audit text-code-audit text-on-surface-variant">
                  Scope: CONS-001 · valid to 15 Oct 2026
                </p>
              </PanelBody>
            </Panel>

            <Panel>
              <PanelHeader
                icon="history_edu"
                title={t("precedingRecords")}
                actions={<StatusChip tone="sealed" icon="lock" label="1 immutable" />}
              />
              <PanelBody>
                {predecessor ? (
                  <>
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      {predecessor.EntryCode} · {predecessor.RecordedAt}
                    </p>
                    <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                      {predecessor.BodyText.slice(0, 180)}…
                    </p>
                    <p className="mt-space-sm font-code-audit text-code-audit text-on-surface-variant">
                      HASH: {predecessor.ContentHash.slice(0, 8)}… (sealed)
                    </p>
                  </>
                ) : null}
                <p className="mt-space-md font-body-sm text-body-sm text-on-surface-variant">
                  {t("appendNotice")}
                </p>
              </PanelBody>
            </Panel>
          </>
        }
      >
        <Panel>
          <PanelHeader
            icon="edit_note"
            title="NOTE-002"
            subtitle={`${data.case.StudentName} (${data.case.StudentCode}) · ${data.case.CaseNumber} · 7 Sep 2026, 15:00 ICT`}
            actions={
              <StatusChip tone="sealed" icon="verified_user" label="HE-GOV-2026 verified" />
            }
          />
          <PanelBody className="space-y-space-lg">
            <KeyValueGrid columns={2}>
              <KeyValue
                label={t("signatoryOrigin")}
                value="Demo Advisor 01 — Academic Advisor, Computing"
              />
              <KeyValue label={t("effectiveConsultation")} value="07-SEP-2026 15:00 ICT" mono />
            </KeyValueGrid>

            <Field label={t("category")} hint={t("categoryHint")} required>
              <Select defaultValue="formal" className="w-full">
                <option value="formal">{t("categoryFormal")}</option>
                <option value="progress">{t("categoryProgress")}</option>
                <option value="welfare">{t("categoryWelfare")}</option>
                <option value="withdrawal">{t("categoryPreWithdrawal")}</option>
              </Select>
            </Field>

            <div>
              <Field label={t("observations")} hint={t("observationsHint")} required>
                <TextArea rows={7} defaultValue={draftObservations} maxLength={2000} />
              </Field>
              <div className="mt-space-xs flex flex-wrap items-center justify-between gap-space-sm">
                <p className="flex items-center gap-1.5 font-body-sm text-body-sm text-on-surface-variant">
                  <Icon name="verified" className="text-[14px]" />
                  {t("observationsCriteria")}
                </p>
                <span className="font-code-audit text-code-audit text-on-surface-variant">
                  {draftObservations.length} / 2000
                </span>
              </div>
            </div>

            <div>
              <Field label={t("actionPlan")} hint={t("actionPlanHint")} required>
                <TextArea rows={5} defaultValue={draftActions} />
              </Field>
              <p className="mt-space-xs font-code-audit text-code-audit text-on-surface-variant">
                {t("actionItems", { count: 3 })}
              </p>
            </div>

            <div className="grid gap-space-base sm:grid-cols-2">
              <Field label={t("owner")} required>
                <Select defaultValue="both" className="w-full">
                  <option value="both">{t("ownerBoth")}</option>
                  <option value="advisor">{t("ownerAdvisor")}</option>
                  <option value="student">{t("ownerStudent")}</option>
                </Select>
              </Field>
              <Field label={t("reviewDate")} required>
                <TextInput type="date" defaultValue="2026-09-14" />
              </Field>
            </div>

            <Notice tone="sealed" icon="psychology_alt" title={t("integrityTitle")}>
              {t("integrityBody")}
            </Notice>
          </PanelBody>
          <PanelFooter>
            <div className="flex flex-wrap items-center gap-space-sm">
              <Button variant="secondary" size="sm" icon="save">
                {t("saveDraft")}
              </Button>
              <span className="font-code-audit text-code-audit">
                Draft saved locally at 15:02 ICT
              </span>
            </div>
            <div className="flex flex-wrap gap-space-sm">
              <ButtonLink href={`/advisor/cases/${caseId}`} variant="tertiary" size="sm">
                {tc("cancel")}
              </ButtonLink>
              <ButtonLink
                href={`/advisor/cases/${caseId}/notes/review`}
                size="sm"
                iconAfter="arrow_forward"
              >
                {t("reviewAndCommit")}
              </ButtonLink>
            </div>
          </PanelFooter>
        </Panel>
      </InspectorLayout>
    </>
  );
}
