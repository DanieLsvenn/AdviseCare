import { getTranslations, setRequestLocale } from "next-intl/server";
import {
  Button,
  ButtonLink,
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
import type { LedgerEntry, Student } from "@/lib/types";

/** A07 — Record at decision time (bitemporal reconstruction). */
export default async function DecisionTimePage({
  params,
}: PageProps<"/[locale]/advisor/students/[studentId]/historical">) {
  const { locale, studentId } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.decisionTime");
  const tc = await getTranslations("common");

  const detail = await apiGet<{ student: Student }>(`/api/students/${studentId}`);
  const ledger = await apiGet<{ entries: LedgerEntry[] }>(
    `/api/ledger?studentId=${studentId}&asOf=2026-09-07 15:00:00 ICT`,
  );
  const sealedEntry = ledger.entries.at(-1);

  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-space-md">
        <div className="flex items-center gap-space-sm">
          <Icon name="history" className="text-[22px] text-primary" />
          <h1 className="font-headline-md text-headline-md text-on-surface">{t("title")}</h1>
        </div>
        <div className="flex flex-wrap items-center gap-space-sm">
          <StatusChip tone="sealed" icon="schedule" label={`${t("viewingSession")}: 07-SEP-2026 15:15 ICT`} />
          <StatusChip tone="sealed" icon="fingerprint" label="#AUD-HIST-2026-0903-NOTE001" />
        </div>
      </div>

      <Notice tone="attention" icon="policy" title={t("directiveTitle")}>
        <p>{t("directiveBody")}</p>
        <p className="mt-space-sm font-code-audit text-code-audit uppercase">
          Audit governance directive HE-GOV-2026
        </p>
      </Notice>

      <Panel>
        <PanelBody>
          <KeyValueGrid columns={4}>
            <KeyValue label={t("inspector")} value="Demo Advisor 01 — Faculty of Computing" />
            <KeyValue
              label={tc("student")}
              value={`${detail.student.FullName} · ${detail.student.StudentCode}`}
            />
            <KeyValue label={t("targetCase")} value="CASE-001 (early intervention)" />
            <KeyValue label={t("ledgerBlock")} value="#BLK-20260903-8812" mono />
          </KeyValueGrid>
        </PanelBody>
      </Panel>

      <Panel>
        <PanelHeader
          icon="hourglass_top"
          title={t("reconstructionTitle")}
          subtitle={t("reconstructionSubtitle")}
        />
        <PanelBody className="space-y-space-base">
          <div className="grid gap-space-md lg:grid-cols-2">
            <div className="rounded-md border border-outline-variant/40 bg-surface-container-low/40 p-space-base">
              <p className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
                <Icon name="event_available" className="text-[18px] text-primary" />
                {t("axisA")}
              </p>
              <p className="mt-space-sm font-display-sm text-display-sm text-on-surface">
                03-SEP-2026 10:00 ICT
              </p>
              <p className="font-code-audit text-code-audit text-on-surface-variant">
                REALITY_TS: 1788404400
              </p>
              <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                {t("axisABody")}
              </p>
            </div>
            <div className="rounded-md border border-outline-variant/40 bg-surface-container-low/40 p-space-base">
              <p className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
                <Icon name="database" className="text-[18px] text-primary" />
                {t("axisB")}
              </p>
              <p className="mt-space-sm font-display-sm text-display-sm text-on-surface">
                03-SEP-2026 10:00 ICT
              </p>
              <p className="font-code-audit text-code-audit text-on-surface-variant">
                TX_COMMITTED: 1788404414
              </p>
              <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                {t("axisBBody")}
              </p>
            </div>
          </div>

          <Notice tone="sealed" icon="gavel" title={t("necessityTitle")}>
            {t("necessityBody")}
          </Notice>
        </PanelBody>
      </Panel>

      <Panel>
        <PanelHeader
          icon="compare_arrows"
          title={t("deltaTitle")}
          subtitle="Specification HE-GOV-2026 / subpart 4.1"
          actions={<StatusChip tone="sealed" icon="lock" label={t("deltaBadge")} />}
        />
        <PanelBody>
          <div className="grid gap-space-md lg:grid-cols-2">
            <section className="rounded-md border border-outline-variant/40">
              <header className="flex flex-wrap items-center justify-between gap-space-sm border-b border-outline-variant/30 bg-surface-container-low/50 px-space-base py-space-md">
                <p className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
                  <Icon name="history_toggle_off" className="text-[18px]" />
                  {t("snapshotTitle")}
                </p>
                <StatusChip tone="sealed" icon="lock" label="03 Sep 2026" />
              </header>
              <div className="space-y-space-md p-space-base">
                <p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                  {t("snapshotCaption", { timestamp: "03-SEP-2026 10:00 ICT" })}
                </p>
                <div>
                  <div className="flex flex-wrap items-center justify-between gap-space-sm">
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      Attendance telemetry (snapshot v1)
                    </p>
                    <StatusChip tone="attention" icon="warning" label="85.0% — trigger alert" />
                  </div>
                  <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                    Unexcused absence logged for the 2 Sep CS301 laboratory session; RFID gateway
                    CAM-GATE-09 recorded no swipe.
                  </p>
                  <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
                    #ATT-V1-2026-0903-HIST · SHA-256 4f8b91…a12c
                  </p>
                </div>
                <div className="border-t border-outline-variant/25 pt-space-md">
                  <div className="flex flex-wrap items-center justify-between gap-space-sm">
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      SIS academic records v3.2
                    </p>
                    <StatusChip tone="urgent" icon="priority_high" label="Midterm deficit 42.0%" />
                  </div>
                  <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                    Cumulative GPA 2.74. CS301 midterm recorded at 42.0 / 100, triggering early
                    alert rule R-CS-04.
                  </p>
                </div>
                <div className="border-t border-outline-variant/25 pt-space-md">
                  <div className="flex flex-wrap items-center justify-between gap-space-sm">
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      Special circumstance token
                    </p>
                    <StatusChip tone="sealed" icon="lock" label="ATTR-FIN-001 (masked)" />
                  </div>
                  <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                    Financial circumstance verified under CONS-001. No payload was exposed to the
                    academic advising record, then or now.
                  </p>
                </div>
              </div>
            </section>

            <section className="rounded-md border border-outline-variant/40">
              <header className="flex flex-wrap items-center justify-between gap-space-sm border-b border-outline-variant/30 bg-surface-container-low/50 px-space-base py-space-md">
                <p className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
                  <Icon name="fact_check" className="text-[18px]" />
                  {t("currentTitle")}
                </p>
                <StatusChip tone="ontrack" icon="refresh" label="07 Sep 2026" />
              </header>
              <div className="space-y-space-md p-space-base">
                <p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                  {t("currentCaption", { timestamp: "07-SEP-2026" })}
                </p>
                <div>
                  <div className="flex flex-wrap items-center justify-between gap-space-sm">
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      Attendance telemetry (reconciled v2)
                    </p>
                    <StatusChip tone="ontrack" icon="check_circle" label="90.0% — regular standing" />
                  </div>
                  <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                    Turnstile failure verified by Facilities ticket #FAC-8821. The missed swipe is
                    marked as a mechanical fault and attendance is restored.
                  </p>
                  <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
                    #CORR-20260907-004 · reconciled 07-SEP-2026 11:15 ICT
                  </p>
                </div>
                <Notice tone="sealed" icon="verified_user" title={t("nonOverwritingTitle")}>
                  {t("nonOverwritingBody")}
                </Notice>
                <div className="border-t border-outline-variant/25 pt-space-md">
                  <div className="flex flex-wrap items-center justify-between gap-space-sm">
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      SIS academic records (unchanged)
                    </p>
                    <StatusChip tone="urgent" icon="priority_high" label="Midterm deficit 42.0%" />
                  </div>
                  <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                    No subsequent SIS correction was filed. The CS301 deficit still requires active
                    academic guidance regardless of the restored attendance.
                  </p>
                </div>
              </div>
            </section>
          </div>
        </PanelBody>
      </Panel>

      {sealedEntry ? (
        <ImmutableRecord
          title={`${sealedEntry.EntryCode} — ${sealedEntry.Title}`}
          typeLabel={`${t("retention")}: tier 1 (5-year immutable)`}
          author={`${sealedEntry.AuthorName} (${sealedEntry.AuthorRoleLabel})`}
          timestamp={sealedEntry.RecordedAt}
          hash={sealedEntry.ContentHash}
          footer={<StatusChip tone="sealed" icon="lock" label={t("sealedEntry")} />}
        >
          <h3 className="font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
            {t("section1")}
          </h3>
          <p className="mt-space-sm">{sealedEntry.BodyText}</p>
          <h3 className="mt-space-lg font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
            {t("section2")}
          </h3>
          <ul className="mt-space-sm space-y-space-sm font-body-sm text-body-sm">
            <li className="rounded border border-outline-variant/40 px-space-md py-space-sm">
              Schedule the in-person consultation session — due 07-SEP-2026 (mandatory action).
            </li>
            <li className="rounded border border-outline-variant/40 px-space-md py-space-sm">
              Prepare the CS301 project log and lab records — owner: advisee.
            </li>
            <li className="rounded border border-outline-variant/40 px-space-md py-space-sm">
              Submit the attendance reconciliation inquiry to the Registrar — owner: advisor.
            </li>
          </ul>
          <Notice tone="sealed" icon="verified" title={t("appendOnlyPolicy")} className="mt-space-lg">
            {t("appendOnlyPolicyBody")}
          </Notice>
        </ImmutableRecord>
      ) : null}

      <Panel>
        <PanelFooter>
          <div className="flex flex-wrap gap-space-sm">
            <ButtonLink
              href={`/advisor/students/${studentId}`}
              variant="tertiary"
              size="sm"
              icon="person"
            >
              {t("returnToProfile")}
            </ButtonLink>
            <ButtonLink href="/advisor/cases/1" variant="tertiary" size="sm" icon="arrow_back">
              {t("backToCase")}
            </ButtonLink>
          </div>
          <div className="flex flex-wrap items-center gap-space-sm">
            <StatusChip tone="ontrack" icon="verified_user" label={t("pkiReady")} />
            <Button size="sm" icon="picture_as_pdf">
              {t("exportPack")}
            </Button>
          </div>
        </PanelFooter>
      </Panel>
    </>
  );
}
