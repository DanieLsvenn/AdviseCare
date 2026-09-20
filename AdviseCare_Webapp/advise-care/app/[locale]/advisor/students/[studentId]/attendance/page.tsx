import { notFound } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import {
  ButtonLink,
  DataTable,
  Icon,
  KeyValue,
  KeyValueGrid,
  Notice,
  Panel,
  PanelBody,
  PanelFooter,
  PanelHeader,
  ProvenanceList,
  StatusChip,
  Td,
  Th,
  Tr,
} from "@/components/ui";
import { Link } from "@/i18n/navigation";
import { apiGet } from "@/lib/api";
import type {
  AttendanceSession,
  IndicatorProvenance,
  Student,
} from "@/lib/types";

const statusTone = {
  PRESENT: { tone: "ontrack", label: "Present (100%)", icon: "check_circle" },
  LATE: { tone: "attention", label: "Late arrival", icon: "schedule" },
  ABSENT: { tone: "urgent", label: "Unexcused absence (-5.0%)", icon: "cancel" },
  EXCUSED: { tone: "referral", label: "Excused (neutral)", icon: "verified" },
} as const;

/** A03 — Attendance evidence & audit. */
export default async function AttendanceEvidencePage({
  params,
}: PageProps<"/[locale]/advisor/students/[studentId]/attendance">) {
  const { locale, studentId } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.evidence");
  const tc = await getTranslations("common");

  const detail = await apiGet<{
    student: Student;
    attendance: AttendanceSession[];
    attendanceProvenance: IndicatorProvenance[];
  }>(`/api/students/${studentId}`).catch(() => null);

  if (!detail) notFound();
  const { student, attendance, attendanceProvenance } = detail;

  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-space-md">
        <div className="flex items-center gap-space-sm">
          <Icon name="fact_check" className="text-[22px] text-primary" />
          <h1 className="font-headline-md text-headline-md text-on-surface">{t("title")}</h1>
        </div>
        <div className="flex flex-wrap items-center gap-space-sm">
          <StatusChip
            tone="ontrack"
            icon="verified"
            label={`${tc("activePurpose")}: ${tc("academicAdvising")}`}
          />
          <Link
            href={`/advisor/students/${student.StudentId}`}
            className="flex items-center gap-1.5 font-label-md text-label-md text-on-surface-variant hover:text-on-surface"
          >
            <Icon name="close" className="text-[18px]" />
            {t("close")}
          </Link>
        </div>
      </div>

      <Panel>
        <PanelHeader
          icon="co_present"
          title={t("subtitle")}
          subtitle={`${student.FullName} (${student.StudentCode}) · ${t("scopeHash")}: #ATT-V1-2026-0907-1400`}
          actions={<StatusChip tone="ontrack" icon="verified" label="90.0% Current standing" />}
        />
        <PanelBody className="space-y-space-lg">
          <Notice tone="referral" icon="info" title={t("noticeTitle")}>
            {t("noticeBody")}
          </Notice>

          <div>
            <h2 className="font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
              {t("metricSummary")}
            </h2>
            <KeyValueGrid columns={3} className="mt-space-md">
              <KeyValue label={t("currentStanding")} value="90.0% overall" />
              <KeyValue label={t("telemetryFreshness")} value="7 Sep 2026, 14:00 ICT" mono />
              <KeyValue label={t("coveredRange")} value="18 Aug – 7 Sep 2026 (weeks 1–3)" />
              <KeyValue label={t("calculationEngine")} value={t("calculationEngineValue")} />
              <KeyValue label={t("primarySource")} value={t("primarySourceValue")} />
              <KeyValue
                label={t("completeness")}
                value={
                  <span className="flex items-center gap-space-sm">
                    <span>88.5%</span>
                    <span className="h-1.5 w-20 overflow-hidden rounded-full bg-surface-container-high">
                      <span className="block h-full w-[88.5%] bg-secondary" />
                    </span>
                  </span>
                }
              />
            </KeyValueGrid>
            <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
              {t("completenessCaption")}
            </p>
          </div>

          <Notice tone="attention" icon="assignment_turned_in" title={t("correctionTitle")}>
            <p>{t("correctionBody")}</p>
            <p className="mt-space-sm font-code-audit text-code-audit">
              {t("authority")}: Faculty Office of Computing · {t("auditRef")}: #CORR-2026-0907-01
            </p>
          </Notice>
        </PanelBody>
      </Panel>

      <Panel>
        <PanelHeader
          icon="check_circle"
          title={t("sourcesUsedTitle")}
          actions={
            <StatusChip
              tone="sealed"
              icon="list"
              label={t("eventRecords", { count: attendance.length })}
            />
          }
        />
        <DataTable className="min-w-[840px]">
          <thead>
            <Tr className="hover:bg-transparent">
              <Th>{t("colDate")}</Th>
              <Th>{t("colModule")}</Th>
              <Th>{t("colFeed")}</Th>
              <Th>{t("colEvent")}</Th>
              <Th>{t("colWeight")}</Th>
            </Tr>
          </thead>
          <tbody>
            {attendance.map((session) => {
              const meta = statusTone[session.Status];
              return (
                <Tr key={session.SessionId}>
                  <Td>
                    <p className="font-code-audit text-code-audit text-on-surface">
                      {session.SessionDate}
                    </p>
                    {session.AddendumCode ? (
                      <StatusChip
                        tone="referral"
                        icon="published_with_changes"
                        size="sm"
                        label={t("correctedOn")}
                        className="mt-1"
                      />
                    ) : null}
                  </Td>
                  <Td>{session.SessionLabel}</Td>
                  <Td>
                    <p>{session.SourceLabel}</p>
                    <p className="font-code-audit text-code-audit text-on-surface-variant">
                      {session.GateRef}
                    </p>
                  </Td>
                  <Td>
                    <p className="font-code-audit text-code-audit text-on-surface-variant">
                      {tc("recorded")} {session.RecordedAt}
                    </p>
                  </Td>
                  <Td>
                    <StatusChip tone={meta.tone} icon={meta.icon} label={meta.label} />
                  </Td>
                </Tr>
              );
            })}
          </tbody>
        </DataTable>
        <PanelFooter>
          <span className="font-code-audit text-code-audit">
            {t("ledgerScope")}: AUD-ATT-DEMO001-2026
          </span>
          <span>ISO/IEC 27001 certified read</span>
        </PanelFooter>
      </Panel>

      <Panel>
        <PanelHeader
          icon="block"
          title={t("unavailableTitle")}
          subtitle={t("scopeEnforcement")}
        />
        <PanelBody>
          <ProvenanceList items={attendanceProvenance} />
          <p className="mt-space-md font-body-sm text-body-sm text-on-surface-variant">
            {tc("unavailableExplainer")}
          </p>
        </PanelBody>
      </Panel>

      <div className="flex flex-wrap items-center justify-between gap-space-sm">
        <ButtonLink
          href={`/advisor/students/${student.StudentId}`}
          variant="tertiary"
          size="sm"
          icon="arrow_back"
        >
          {t("close")}
        </ButtonLink>
        <ButtonLink
          href={`/advisor/students/${student.StudentId}/historical`}
          variant="secondary"
          size="sm"
          icon="history"
        >
          {t("viewHistorical")}
        </ButtonLink>
      </div>
    </>
  );
}
