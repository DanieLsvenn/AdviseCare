import { notFound } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { InspectorLayout } from "@/components/layout/app-shell";
import {
  Avatar,
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
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type { CorrectionRequest, LedgerEntry } from "@/lib/types";

const attestations = [
  {
    icon: "description",
    label: "Primary document",
    title: "REF-EVID-20260902-CS301.pdf",
    meta: "1.4 MB · SHA-256 valid",
    detail:
      "Physical roster signed in ink by the teaching assistant, showing entry at 09:58 ICT.",
    confirmation: "Inspect document",
  },
  {
    icon: "build_circle",
    label: "Facilities hardware audit",
    title: "Ticket #FAC-8821",
    meta: "Confirmed hardware drop",
    detail:
      "CAM-GATE-09 turnstile sensor coil drop confirmed for 2 Sep, 09:30–11:15 ICT. Repaired 7 Sep.",
    confirmation: "Verified by Campus Facilities",
  },
  {
    icon: "assignment_ind",
    label: "Faculty attestation",
    title: "Dr. Tran Minh Khoa — course lead, CS301",
    meta: "Instructor signature on file",
    detail:
      "Digital counter-attestation issued on 6 Sep 2026 verifying lab assignment submission during the slot.",
    confirmation: "Signature on file",
  },
];

/** A11 — Correction review (advisor determination). */
export default async function CorrectionReviewPage({
  params,
}: PageProps<"/[locale]/advisor/corrections/[correctionId]">) {
  const { locale, correctionId } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.correction");
  const tc = await getTranslations("common");

  const data = await apiGet<{
    correction: CorrectionRequest;
    addendum: LedgerEntry | null;
  }>(`/api/corrections/${correctionId}`).catch(() => null);

  if (!data) notFound();
  const { correction } = data;

  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-space-md">
        <div>
          <h1 className="font-headline-md text-headline-md text-on-surface">{t("title")}</h1>
          <p className="mt-space-xs font-code-audit text-code-audit text-on-surface-variant">
            {correction.CorrectionCode}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-space-sm">
          <StatusChip tone="attention" icon="pending_actions" label={t("pending")} />
          <StatusChip
            tone="sealed"
            icon="schedule"
            label={t("sla", { submitted: correction.SubmittedAt })}
          />
        </div>
      </div>

      <Notice tone="sealed" icon="policy" title={t("protocolTitle")}>
        <p>{t("protocolBody")}</p>
        <p className="mt-space-sm flex flex-wrap items-center gap-space-sm">
          <StatusChip tone="sealed" icon="lock_clock" label={t("protocolBadge")} />
          <span className="font-code-audit text-code-audit">
            Decree 13/2023 · Institutional governance rule 4.4
          </span>
        </p>
      </Notice>

      <Panel>
        <PanelBody>
          <div className="flex flex-wrap items-center gap-space-base">
            <Avatar initials="NA" className="size-12" />
            <div className="min-w-0 flex-1">
              <p className="font-headline-sm text-headline-sm text-on-surface">
                {correction.StudentName}
              </p>
              <p className="font-code-audit text-code-audit text-on-surface-variant">
                {correction.StudentCode}
              </p>
            </div>
            <KeyValueGrid columns={2} className="w-full sm:w-auto">
              <KeyValue label={t("disputedRange")} value="Term weeks 01–03 (Fall 2026)" />
              <KeyValue label={t("citation")} value="Academic policy §4.3b" />
            </KeyValueGrid>
          </div>
        </PanelBody>
      </Panel>

      <InspectorLayout
        inspector={
          <Panel>
            <PanelHeader
              icon="gavel"
              title={t("determination")}
              subtitle={t("authority")}
            />
            <PanelBody className="space-y-space-base">
              <Notice tone="sealed" icon="info" title={t("guaranteeTitle")}>
                {t("guaranteeBody")}
              </Notice>
              <Field label={t("explanation")} required>
                <TextArea
                  rows={5}
                  defaultValue="Hardware fault corroborated by three independent attestations. The reconciled fact is appended; the original 85.0% record and the decision it informed remain in the ledger."
                />
              </Field>
              <div className="flex flex-col gap-space-sm">
                <Button icon="add_task">{t("approve")}</Button>
                <Button variant="secondary" icon="block">
                  {t("reject")}
                </Button>
              </div>
            </PanelBody>
            <PanelFooter>
              <span className="font-code-audit text-code-audit">
                Target block: #BLK-20260907-Reconciled-004
              </span>
            </PanelFooter>
          </Panel>
        }
      >
        <Panel>
          <PanelHeader
            icon="compare_arrows"
            title={t("deltaTitle")}
            subtitle={t("deltaSubtitle")}
          />
          <PanelBody>
            <div className="grid gap-space-md lg:grid-cols-2">
              <section className="rounded-md border border-outline-variant/40">
                <header className="flex items-center justify-between gap-space-sm border-b border-outline-variant/30 bg-surface-container-low/50 px-space-base py-space-md">
                  <p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                    {t("archived")}
                  </p>
                  <StatusChip tone="sealed" icon="history" label={t("originalFact")} />
                </header>
                <div className="p-space-base">
                  <p className="font-display-sm text-display-sm text-on-surface">
                    {correction.DisputedValue === "ABSENT" ? "85.0%" : correction.DisputedValue}
                  </p>
                  <StatusChip
                    tone="attention"
                    icon="warning"
                    label="Attendance deficiency trigger"
                    className="mt-space-sm"
                  />
                  <p className="mt-space-md font-body-sm text-body-sm text-on-surface-variant">
                    One unexcused lab absence recorded during CS301 on Wednesday 2 Sep 2026,
                    10:00–12:00 ICT.
                  </p>
                  <KeyValueGrid columns={1} className="mt-space-base">
                    <KeyValue label={t("hardwareSource")} value="CAM-GATE-09 turnstile" />
                    <KeyValue label={t("ingestionTimestamp")} value="05-Sep-2026 09:15 ICT" mono />
                    <KeyValue label={t("auditBatch")} value="#SYNC-7741" mono />
                    <KeyValue
                      label={t("recordState")}
                      value={<StatusChip tone="sealed" icon="lock" label="IMMUTABLE_LOG" />}
                    />
                  </KeyValueGrid>
                </div>
              </section>

              <section className="rounded-md border border-outline-variant/40">
                <header className="flex items-center justify-between gap-space-sm border-b border-outline-variant/30 bg-surface-container-low/50 px-space-base py-space-md">
                  <p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                    {t("proposed")}
                  </p>
                  <StatusChip tone="referral" icon="edit_calendar" label={t("proposedFact")} />
                </header>
                <div className="p-space-base">
                  <p className="font-display-sm text-display-sm text-on-surface">
                    {correction.ProposedValue === "PRESENT" ? "90.0%" : correction.ProposedValue}
                  </p>
                  <StatusChip
                    tone="ontrack"
                    icon="check_circle"
                    label="Regular standing reinstated"
                    className="mt-space-sm"
                  />
                  <p className="mt-space-md font-body-sm text-body-sm text-on-surface-variant">
                    Correction of the CS301 unexcused absence to confirmed in-person attendance via
                    the secondary manual attendance ledger.
                  </p>
                  <KeyValueGrid columns={1} className="mt-space-base">
                    <KeyValue label={t("groundsCategory")} value="Hardware / reader fault" />
                    <KeyValue label={t("targetedSession")} value="02-Sep-2026 10:00–12:00" mono />
                    <KeyValue label={t("requestedOn")} value={correction.SubmittedAt} mono />
                    <KeyValue label={t("channel")} value="Student portal" />
                  </KeyValueGrid>
                </div>
              </section>
            </div>
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader
            icon="record_voice_over"
            title={t("declaration")}
            actions={
              <StatusChip tone="sealed" icon="key" label={`${t("digitalSig")}: SHA256:7e91d…00a1`} />
            }
          />
          <PanelBody>
            <blockquote className="border-l-2 border-l-outline-variant pl-space-base font-body-base text-body-base text-on-surface">
              {correction.StudentStatement}
            </blockquote>
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader
            icon="verified"
            title={t("evidenceChain")}
            actions={
              <StatusChip
                tone="ontrack"
                icon="done_all"
                label={t("attestationsConfirmed", { confirmed: 3, total: 3 })}
              />
            }
          />
          <PanelBody>
            <ul className="space-y-space-sm">
              {attestations.map((item) => (
                <li
                  key={item.title}
                  className="flex flex-wrap items-start gap-space-md rounded-md border border-outline-variant/40 px-space-base py-space-md"
                >
                  <Icon name={item.icon} className="mt-0.5 text-[20px] text-primary" />
                  <div className="min-w-0 flex-1">
                    <p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                      {item.label}
                    </p>
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      {item.title}
                    </p>
                    <p className="font-code-audit text-code-audit text-on-surface-variant">
                      {item.meta}
                    </p>
                    <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                      {item.detail}
                    </p>
                  </div>
                  <StatusChip tone="ontrack" icon="check" label={item.confirmation} />
                </li>
              ))}
            </ul>
          </PanelBody>
          <PanelFooter>
            <ButtonLink href="/advisor/caseload" variant="tertiary" size="sm" icon="arrow_back">
              {tc("back")}
            </ButtonLink>
            <ButtonLink
              href="/advisor/students/1"
              variant="tertiary"
              size="sm"
              icon="account_circle"
            >
              {t("inspectDocument")}
            </ButtonLink>
          </PanelFooter>
        </Panel>
      </InspectorLayout>
    </>
  );
}
