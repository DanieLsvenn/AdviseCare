import { notFound } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { CasePipeline } from "@/components/advisor/case-pipeline";
import { InspectorLayout } from "@/components/layout/app-shell";
import {
  ButtonLink,
  CitationList,
  Icon,
  ImmutableRecord,
  KeyValue,
  KeyValueGrid,
  Notice,
  Panel,
  PanelBody,
  PanelFooter,
  PanelHeader,
  StatusChip,
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type {
  AdvisingCase,
  AgreedAction,
  CaseStateTransition,
  LedgerEntry,
  Referral,
} from "@/lib/types";

/** A04 — Advising case (CASE-001). */
export default async function CaseDetailPage({
  params,
}: PageProps<"/[locale]/advisor/cases/[caseId]">) {
  const { locale, caseId } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.case");
  const tc = await getTranslations("common");

  const data = await apiGet<{
    case: AdvisingCase;
    transitions: CaseStateTransition[];
    entries: LedgerEntry[];
    actions: AgreedAction[];
    referrals: Referral[];
  }>(`/api/cases/${caseId}`).catch(() => null);

  if (!data) notFound();
  const { case: advisingCase, transitions, entries, actions } = data;
  const openAction = actions.find((action) => action.Status === "OPEN");

  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-space-md">
        <div className="flex items-center gap-space-sm">
          <h1 className="font-headline-md text-headline-md text-on-surface">
            {advisingCase.CaseNumber}
          </h1>
          <StatusChip tone="ontrack" icon="folder_open" label={t("activeCase")} />
        </div>
        <div className="flex flex-wrap items-center gap-space-sm">
          <StatusChip tone="sealed" icon="lock" label={t("secureSession")} />
          <span className="font-code-audit text-code-audit text-on-surface-variant">
            7 Sep 2026, 14:30 ICT
          </span>
        </div>
      </div>

      <Notice tone="sealed" icon="verified" title={t("protocolTitle")}>
        <p>{t("protocolBody")}</p>
        <p className="mt-space-sm font-code-audit text-code-audit">HE-GOV-2026.4</p>
      </Notice>

      <Panel>
        <PanelHeader
          icon="folder_special"
          title={advisingCase.Title}
          subtitle="Intervention plan triggered by the CS301 midterm assessment deficit and consecutive laboratory absences."
          actions={
            <div className="flex flex-wrap gap-space-sm">
              <StatusChip tone="sealed" icon="school" label={advisingCase.StudentCode} />
              <StatusChip
                tone={advisingCase.Priority === "URGENT" ? "urgent" : "attention"}
                icon="priority_high"
                label={`${t("stage")}: ${advisingCase.CurrentState}`}
              />
            </div>
          }
        />
        <PanelBody>
          <KeyValueGrid columns={4}>
            <KeyValue
              label={t("advisee")}
              value={`${advisingCase.StudentName} (${advisingCase.StudentCode})`}
            />
            <KeyValue label={t("caseOwner")} value={advisingCase.AdvisorName} />
            <KeyValue label={t("lifecycle")} value={`${tc("open")}: ${advisingCase.OpenedAt}`} />
            <KeyValue
              label={t("consentedContext")}
              value={
                <StatusChip tone="ontrack" icon="shield_person" label="CG-5001 (financial read)" />
              }
            />
          </KeyValueGrid>
        </PanelBody>
        <PanelFooter>
          <div className="flex flex-wrap gap-space-sm">
            <ButtonLink href={`/advisor/cases/${advisingCase.CaseId}/notes/new`} size="sm" icon="add_circle">
              {t("newNote")}
            </ButtonLink>
            <ButtonLink
              href={`/advisor/referrals/new?case=${advisingCase.CaseId}`}
              variant="secondary"
              size="sm"
              icon="forward"
            >
              {t("newReferral")}
            </ButtonLink>
            <ButtonLink
              href={`/advisor/cases/${advisingCase.CaseId}/decision`}
              variant="secondary"
              size="sm"
              icon="rule"
            >
              {t("advanceStage")}
            </ButtonLink>
          </div>
          <ButtonLink
            href={`/advisor/students/${advisingCase.StudentId}/historical`}
            variant="tertiary"
            size="sm"
            icon="fact_check"
          >
            {t("auditLog")}
          </ButtonLink>
        </PanelFooter>
      </Panel>

      <Panel>
        <PanelHeader
          icon="conversion_path"
          title={t("pipeline")}
          subtitle={`${t("lastStageChange")}: ${transitions.at(-1)?.OccurredAt ?? "—"}`}
          actions={
            <StatusChip
              tone="sealed"
              icon="linear_scale"
              label={t("stepOf", { current: 2, total: 5 })}
            />
          }
        />
        <PanelBody>
          <CasePipeline current={advisingCase.CurrentState} />
        </PanelBody>
      </Panel>

      <InspectorLayout
        inspector={
          <>
            <Panel>
              <PanelHeader icon="assignment_turned_in" title={t("agreedAction")} />
              <PanelBody>
                {openAction ? (
                  <>
                    <p className="font-body-base text-body-base text-on-surface">
                      {openAction.Description}
                    </p>
                    <KeyValueGrid columns={1} className="mt-space-base">
                      <KeyValue label={tc("owner")} value={openAction.OwnerName} />
                      <KeyValue label={tc("dueDate")} value={openAction.DueDate} mono />
                    </KeyValueGrid>
                  </>
                ) : (
                  <Notice tone="attention" icon="event_busy" title={t("emptyActionTitle")}>
                    {t("emptyActionBody")}
                  </Notice>
                )}
              </PanelBody>
            </Panel>

            <Panel>
              <PanelHeader
                icon="edit_note"
                title={t("draftsTitle")}
                subtitle={t("draftsCaption")}
                actions={<StatusChip tone="referral" icon="cloud_off" label={t("draftsBadge")} />}
              />
              <PanelBody>
                <p className="font-label-md text-label-md font-semibold text-on-surface">
                  Draft note for the 7 Sep consultation session
                </p>
                <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
                  Auto-saved 14:22 ICT
                </p>
                <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                  Detailed discussion regarding the CS301 midterm grade review and a
                  recommendation for one-to-one tutoring sessions…
                </p>
                <div className="mt-space-base flex flex-wrap gap-space-sm">
                  <ButtonLink
                    href={`/advisor/cases/${advisingCase.CaseId}/notes/new`}
                    size="sm"
                    icon="edit"
                  >
                    {t("continueEditing")}
                  </ButtonLink>
                  <ButtonLink href={`/advisor/cases/${advisingCase.CaseId}`} variant="tertiary" size="sm">
                    {t("discardDraft")}
                  </ButtonLink>
                </div>
              </PanelBody>
            </Panel>
          </>
        }
      >
        <Panel>
          <PanelHeader
            icon="history_edu"
            title={t("ledgerTitle")}
            subtitle={tc("appendOnlyBody")}
            actions={
              <StatusChip
                tone="sealed"
                icon="lock"
                label={t("eventsRecorded", { count: entries.length })}
              />
            }
          />
          <PanelBody className="space-y-space-md">
            {entries.map((entry) => (
              <ImmutableRecord
                key={entry.LedgerEntryId}
                title={entry.Title}
                typeLabel={`${entry.EntryCode} · ${entry.TypeLabel}`}
                author={`${entry.AuthorName} (${entry.AuthorRoleLabel})`}
                timestamp={entry.RecordedAt}
                hash={entry.ContentHash}
                supersedes={entry.SupersedesEntryCode}
                footer={
                  <ButtonLink
                    href={`/advisor/cases/${advisingCase.CaseId}/notes/new`}
                    variant="tertiary"
                    size="sm"
                    icon="post_add"
                  >
                    {t("addAddendum")}
                  </ButtonLink>
                }
              >
                <p>{entry.BodyText}</p>
                {entry.Citations.length > 0 ? (
                  <div className="mt-space-md">
                    <p className="font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
                      {tc("evidence")}
                    </p>
                    <CitationList citations={entry.Citations} className="mt-space-sm" />
                  </div>
                ) : null}
                {entry.LocationLabel ? (
                  <p className="mt-space-md flex items-center gap-1.5 font-body-sm text-body-sm text-on-surface-variant">
                    <Icon name="place" className="text-[14px]" />
                    {entry.LocationLabel}
                  </p>
                ) : null}
              </ImmutableRecord>
            ))}
          </PanelBody>
          <PanelFooter>
            <ButtonLink
              href={`/advisor/students/${advisingCase.StudentId}`}
              variant="tertiary"
              size="sm"
              iconAfter="open_in_new"
            >
              {t("viewProfile")}
            </ButtonLink>
            <ButtonLink
              href="/advisor/consent-requests"
              variant="tertiary"
              size="sm"
              iconAfter="arrow_forward"
            >
              {t("inspectConsent")}
            </ButtonLink>
          </PanelFooter>
        </Panel>
      </InspectorLayout>
    </>
  );
}
