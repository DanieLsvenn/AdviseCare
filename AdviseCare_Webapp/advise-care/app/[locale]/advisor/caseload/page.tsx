import { getTranslations, setRequestLocale } from "next-intl/server";
import { IndicatorCell } from "@/components/advisor/indicator-cell";
import {
  AuditFooter,
  Avatar,
  Button,
  DataTable,
  Icon,
  MetaPill,
  Notice,
  PageHeader,
  Pagination,
  Panel,
  SearchInput,
  Select,
  SourceStatusBar,
  StatusChip,
  Td,
  Th,
  Tr,
} from "@/components/ui";
import { Link } from "@/i18n/navigation";
import { apiGet } from "@/lib/api";
import type { AcademicTerm, CaseloadRow, DataSourceStatus } from "@/lib/types";

/** A01 — Advisor caseload. */
export default async function CaseloadPage({
  params,
}: PageProps<"/[locale]/advisor/caseload">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("advisor.caseload");
  const tc = await getTranslations("common");

  const data = await apiGet<{
    term: AcademicTerm;
    sources: DataSourceStatus[];
    totalAssigned: number;
    rows: CaseloadRow[];
  }>("/api/caseload");

  return (
    <>
      <PageHeader
        eyebrow={[t("eyebrow1"), t("eyebrow2"), t("eyebrow3")]}
        title={t("title")}
        badge={
          <span className="rounded bg-surface-container-high px-space-md py-1 font-headline-sm text-headline-sm text-primary">
            {t("assignedBadge", { count: data.totalAssigned })}
          </span>
        }
        meta={
          <div className="flex flex-wrap items-center gap-space-sm">
            <MetaPill icon="calendar_month" label={`${tc("term")}:`} value={data.term.Label} />
            <MetaPill icon="apartment" label={`${tc("faculty")}:`} value="Computing" />
          </div>
        }
        actions={<MetaPill icon="lock" label={t("scopePill")} tone="primary" />}
      />

      <SourceStatusBar sources={data.sources} />

      <Notice tone="sealed" icon="info" title={t("independentTitle")}>
        <p>{t("independentBody")}</p>
        <p className="mt-space-sm font-code-audit text-code-audit uppercase">
          {t("complianceCode")}: AUDIT-SEC-2026-FOC
        </p>
      </Notice>

      <Panel>
        <div className="flex flex-wrap items-center gap-space-md border-b border-outline-variant/30 px-space-lg py-space-base">
          <SearchInput
            className="min-w-[260px] flex-1"
            placeholder={t("searchPlaceholder")}
            aria-label={t("searchPlaceholder")}
            defaultValue=""
          />
          <Select aria-label={t("filterTerm")} defaultValue="current" className="w-auto">
            <option value="current">{`${t("filterTerm")}: ${data.term.Label}`}</option>
          </Select>
          <Select aria-label={t("filterStage")} defaultValue="all" className="w-auto">
            <option value="all">{`${t("filterStage")}: ${t("allStages")}`}</option>
          </Select>
          <Select aria-label={t("filterCompleteness")} defaultValue="all" className="w-auto">
            <option value="all">{`${t("filterCompleteness")}: ${t("allLevels")}`}</option>
          </Select>
          <Select aria-label={tc("sortBy")} defaultValue="academic" className="w-auto">
            <option value="academic">{`${tc("sortBy")}: ${t("sortAcademicDesc")}`}</option>
          </Select>
          <Button variant="secondary" icon="filter_alt_off">
            {tc("reset")}
          </Button>
        </div>

        <div className="flex flex-wrap items-center justify-between gap-space-md px-space-lg py-space-md">
          <p className="font-label-md text-label-md text-on-surface">
            {t("showingSummary", { shown: data.rows.length, total: data.totalAssigned })}
            <span className="ml-space-sm text-secondary">
              • {t("criteriaMatches", { count: data.rows.length })}
            </span>
          </p>
          <button
            type="button"
            className="flex items-center gap-1.5 font-body-sm text-body-sm text-on-surface-variant hover:text-on-surface"
          >
            <Icon name="sync_alt" className="text-[16px]" />
            {t("toggleSkeleton")}
          </button>
        </div>

        <DataTable className="min-w-[1080px]">
          <thead>
            <Tr className="hover:bg-transparent">
              <Th className="w-[240px]">{t("colStudent")}</Th>
              <Th>{t("colAcademic")}</Th>
              <Th>{t("colAttendance")}</Th>
              <Th>{t("colParticipation")}</Th>
              <Th>{t("colCircumstance")}</Th>
              <Th>{t("colFeed")}</Th>
            </Tr>
          </thead>
          <tbody>
            {data.rows.map((row) => (
              <Tr key={row.StudentId} highlighted={row.Flagged}>
                <Td>
                  <div className="flex items-center gap-space-md">
                    <Avatar
                      initials={row.Initials}
                      tone={row.Academic.Tone === "urgent" ? "urgent" : "primary"}
                    />
                    <div className="min-w-0">
                      <Link
                        href={`/advisor/students/${row.StudentId}`}
                        className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface hover:text-primary"
                      >
                        <span className="truncate">{row.FullName}</span>
                        {row.Flagged ? (
                          <Icon
                            name="flag"
                            className="text-[14px] text-status-urgent-text"
                            label={t("openProfile")}
                          />
                        ) : null}
                      </Link>
                      <p className="font-code-audit text-code-audit text-on-surface-variant">
                        {row.StudentCode}
                      </p>
                      <p className="truncate font-body-sm text-body-sm text-on-surface-variant">
                        {row.ProgrammeLabel}
                      </p>
                    </div>
                  </div>
                </Td>
                <Td>
                  <IndicatorCell cell={row.Academic} />
                </Td>
                <Td>
                  <IndicatorCell cell={row.Attendance} />
                </Td>
                <Td>
                  <IndicatorCell cell={row.Participation} />
                </Td>
                <Td>
                  <IndicatorCell cell={row.Circumstance} />
                </Td>
                <Td>
                  <IndicatorCell cell={row.FeedSync} />
                </Td>
              </Tr>
            ))}
          </tbody>
        </DataTable>

        <Pagination
          page={1}
          totalPages={19}
          perPage={8}
          summary={t("paginationSummary", {
            shown: data.rows.length,
            total: data.totalAssigned,
          })}
        />
      </Panel>

      <div className="flex flex-wrap items-center gap-space-sm">
        <StatusChip
          tone="sealed"
          icon="lock"
          label={tc("unavailableForPurpose")}
        />
        <p className="max-w-3xl font-body-sm text-body-sm text-on-surface-variant">
          {tc("unavailableExplainer")}
        </p>
      </div>

      <AuditFooter
        message={`Data queried under Role: Academic Advisor (Faculty of Computing). ${tc("auditMessage")}`}
        sessionHash="#LOG-2026-FOC-8812"
        timestamp="2026-10-18 09:14 ICT"
      />
    </>
  );
}
