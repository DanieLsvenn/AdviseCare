import { getTranslations, setRequestLocale } from "next-intl/server";
import { InspectorLayout } from "@/components/layout/app-shell";
import {
  Button,
  ButtonLink,
  Eyebrow,
  Icon,
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
import type { AdvisingCase, LedgerEntry } from "@/lib/types";

const observations =
  "In-person consultation held at Advisor Office 402 with Nguyen Van An following the CS301 midterm deficit (42/100) and the recent lab attendance correction. The advisee stated difficulty managing concurrent software architecture project deadlines alongside CS301 lab assignments, confirmed that attendance was reconciled on 7 Sep after the reader sensor failure on 2 Sep, and expressed anxiety about continuous assessment pacing.";

const commitments = [
  {
    text: "Advisee to schedule twice-weekly peer-tutoring sessions via the Faculty Peer Learning Centre for CS301 algorithms.",
    owner: "Student",
  },
  {
    text: "Advisor to initiate a formal referral check with the Academic Skills Hub.",
    owner: "Advisor",
  },
  {
    text: "Weekly milestone check-in scheduled for 14 Sep 2026 at 10:00 ICT.",
    owner: "Joint action",
  },
];

/** A06 — Review & commit advising note (shown in its retryable-failure state). */
export default async function ReviewNotePage({
  params,
}: PageProps<"/[locale]/advisor/cases/[caseId]/notes/review">) {
  const { locale, caseId } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.review");
  const tc = await getTranslations("common");

  const data = await apiGet<{ case: AdvisingCase; entries: LedgerEntry[] }>(
    `/api/cases/${caseId}`,
  );
  const predecessor = data.entries.at(-1);

  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-space-md">
        <div>
          <Eyebrow
            items={["Caseload", data.case.CaseNumber, `${t("title")} (NOTE-002)`]}
          />
          <p className="mt-space-xs font-body-sm text-body-sm text-on-surface-variant">
            {t("state")}
          </p>
        </div>
        <StatusChip tone="sealed" icon="verified" label="Ledger node: VN-HCM-NODE-04" />
      </div>

      <Notice tone="sealed" icon="gavel" title={t("preCommitTitle")}>
        <p>{t("preCommitBody")}</p>
        <p className="mt-space-sm font-code-audit text-code-audit">
          {t("preCommitSpec")}: HE-GOV-2026-IMMUTABLE
        </p>
      </Notice>

      <Notice
        tone="urgent"
        icon="error"
        title={t("failureTitle")}
        actions={
          <div className="flex flex-wrap gap-space-sm">
            <Button size="sm" icon="refresh">
              {t("retry")}
            </Button>
            <ButtonLink
              href={`/advisor/cases/${caseId}/notes/new`}
              variant="secondary"
              size="sm"
            >
              {t("returnToDraft")}
            </ButtonLink>
          </div>
        }
      >
        <p>{t("failureBody")}</p>
        <p className="mt-space-sm font-code-audit text-code-audit">ERR_LEDGER_TIMEOUT_504</p>
      </Notice>

      <InspectorLayout
        inspector={
          <>
            <Panel>
              <PanelHeader
                icon="archive"
                title={t("snapshotTitle")}
                subtitle={`${t("snapshotCaption")} · 07-SEP-2026 15:00 ICT`}
                actions={<StatusChip tone="sealed" icon="ac_unit" label={t("frozen")} />}
              />
              <PanelBody className="space-y-space-base">
                <KeyValueGrid columns={1}>
                  <KeyValue
                    label="Attendance telemetry"
                    value="90.0% reconciled — 18 / 20 recorded sessions (threshold ≥ 80.0%)"
                  />
                  <KeyValue label="Calculation engine" value="Attendance v1" />
                  <KeyValue label="Synced & reconciled" value="07-SEP-2026 11:15 ICT" mono />
                  <KeyValue label="CS301 midterm deficit" value="42.0% — trigger flag active" />
                  <KeyValue label="Cumulative GPA" value="2.74 / 4.00" />
                </KeyValueGrid>
                <div className="rounded-md border border-outline-variant/40 bg-surface-container-low/50 p-space-md">
                  <StatusChip tone="sealed" icon="security" label="Firewalled" />
                  <p className="mt-space-sm font-code-audit text-code-audit text-on-surface">
                    ATTR-FIN-001 · scope CONS-001
                  </p>
                  <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                    The token references validated financial assistance without exposing any
                    monetary amount. Unconsented attributes remain sealed.
                  </p>
                </div>
              </PanelBody>
            </Panel>

            <Panel>
              <PanelHeader
                icon="history"
                title={t("predecessor")}
                actions={<StatusChip tone="ontrack" icon="done_all" label={t("hashMatched")} />}
              />
              <PanelBody>
                <KeyValueGrid columns={1}>
                  <KeyValue
                    label={predecessor?.EntryCode ?? "NOTE-001"}
                    value={predecessor?.RecordedAt ?? "—"}
                    mono
                  />
                  <KeyValue
                    label="Preceding digest"
                    value={`${predecessor?.ContentHash.slice(0, 8)}…${predecessor?.ContentHash.slice(-8)}`}
                    mono
                  />
                  <KeyValue label="Baseline attendance" value="85.0% (prior) → 90.0% (current)" />
                </KeyValueGrid>
              </PanelBody>
            </Panel>

            <Panel>
              <PanelHeader
                icon="enhanced_encryption"
                title={t("sealingTarget")}
                actions={<StatusChip tone="attention" icon="pending" label={t("unsignedTarget")} />}
              />
              <PanelBody>
                <p className="break-all font-code-audit text-code-audit text-on-surface-variant">
                  e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
                </p>
              </PanelBody>
            </Panel>
          </>
        }
      >
        <Panel>
          <PanelHeader
            icon="description"
            title={t("documentTitle")}
            subtitle="NOTE-002 — formal advising consultation"
            actions={
              <StatusChip tone="ontrack" icon="verified_user" label={t("humanJudgement")} />
            }
          />
          <PanelBody className="space-y-space-lg">
            <KeyValueGrid columns={4}>
              <KeyValue label="Signatory origin" value="Demo Advisor 01 — Faculty of Computing" />
              <KeyValue label="Effective timestamp" value="07-SEP-2026 15:00 ICT" mono />
              <KeyValue
                label={t("targetAdvisee")}
                value={`${data.case.StudentName} · ${data.case.CaseNumber}`}
              />
              <KeyValue
                label={t("classification")}
                value="Statutory tier 1 — 5-year academic retention"
              />
            </KeyValueGrid>

            <section>
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <h2 className="flex items-center gap-space-sm font-headline-sm text-headline-sm text-on-surface">
                  <Icon name="record_voice_over" className="text-[20px] text-primary" />
                  {t("section1")}
                </h2>
                <StatusChip tone="sealed" icon="lock" label={t("readOnlyVerbatim")} />
              </div>
              <p className="mt-space-md rounded-md border border-outline-variant/40 bg-surface-container-low/40 p-space-base font-body-base text-body-base text-on-surface">
                {observations}
              </p>
            </section>

            <section>
              <div className="flex flex-wrap items-center justify-between gap-space-sm">
                <h2 className="flex items-center gap-space-sm font-headline-sm text-headline-sm text-on-surface">
                  <Icon name="checklist" className="text-[20px] text-primary" />
                  {t("section2")}
                </h2>
                <StatusChip
                  tone="referral"
                  icon="task_alt"
                  label={t("bindingCommitments", { count: commitments.length })}
                />
              </div>
              <ol className="mt-space-md space-y-space-sm">
                {commitments.map((commitment, index) => (
                  <li
                    key={commitment.text}
                    className="flex gap-space-md rounded-md border border-outline-variant/40 px-space-base py-space-md"
                  >
                    <span className="flex size-6 shrink-0 items-center justify-center rounded bg-surface-container-high font-label-sm text-label-sm text-primary">
                      {index + 1}
                    </span>
                    <div className="min-w-0 flex-1">
                      <p className="font-body-base text-body-base text-on-surface">
                        {commitment.text}
                      </p>
                      <p className="mt-1 font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                        {tc("owner")}: {commitment.owner}
                      </p>
                    </div>
                  </li>
                ))}
              </ol>
            </section>

            <section>
              <h2 className="flex items-center gap-space-sm font-headline-sm text-headline-sm text-on-surface">
                <Icon name="policy" className="text-[20px] text-primary" />
                {t("section3")}
              </h2>
              <KeyValueGrid columns={3} className="mt-space-md">
                <KeyValue
                  label={t("actionResponsibility")}
                  value="Student (Nguyen Van An) & Advisor (Demo Advisor 01)"
                />
                <KeyValue label={t("targetReview")} value="14/09/2026 · 10:00 ICT" mono />
                <KeyValue
                  label={t("decisionIntegrity")}
                  value={<StatusChip tone="ontrack" icon="how_to_reg" label={t("noAi")} />}
                />
              </KeyValueGrid>
            </section>

            <Notice tone="sealed" icon="lock">
              {t("immutabilityNotice")}
            </Notice>
          </PanelBody>
          <PanelFooter>
            <div>
              <ButtonLink
                href={`/advisor/cases/${caseId}/notes/new`}
                variant="tertiary"
                size="sm"
                icon="edit_note"
              >
                {t("backToDraft")}
              </ButtonLink>
              <p className="mt-1 font-body-sm text-body-sm">{t("draftUnlocked")}</p>
            </div>
            <Button size="sm" icon="fingerprint">
              {t("commit")}
            </Button>
          </PanelFooter>
        </Panel>
      </InspectorLayout>

      <p className="text-center font-code-audit text-code-audit text-on-surface-variant">
        AdviseCare Operational Governance Engine v4.12.0 · Faculty of Computing Academic
        Records Authority · {t("sessionCertified")}: Demo Advisor 01 (PKI #ADVISOR-01-HCM-2026)
      </p>
    </>
  );
}
