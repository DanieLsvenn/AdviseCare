import { getTranslations, setRequestLocale } from "next-intl/server";
import {
  Button,
  DataTable,
  Icon,
  KeyValue,
  KeyValueGrid,
  Notice,
  PageHeader,
  Panel,
  PanelBody,
  PanelHeader,
  StatusChip,
  Td,
  Th,
  Tr,
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type { ExportJob } from "@/lib/types";

/** S08 — Export my record. */
export default async function ExportRecordPage({ params }: PageProps<"/[locale]/me/export">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("student.export");
  const tc = await getTranslations("common");

  const { exports } = await apiGet<{ exports: ExportJob[] }>("/api/exports");

  const schemas = [
    {
      tag: "SCHEMA::IDENTITY",
      icon: "badge",
      title: t("schema1"),
      body: t("schema1Body"),
      rows: [
        { label: "Legal identity", value: "Nguyen Van An" },
        { label: "Academic programme", value: "B.Sc. Computing & Systems" },
        { label: "Cumulative GPA", value: "2.74 / 4.00" },
        { label: "Degree attainment", value: "74 / 120 ECTS" },
      ],
      footer: "4 course modules active (semester 1, 2024–2025)",
      tone: "ontrack" as const,
    },
    {
      tag: "SCHEMA::TELEMETRY",
      icon: "sensors",
      title: t("schema2"),
      body: t("schema2Body"),
      rows: [
        { label: "Reconciled attendance", value: "90.0% verified" },
        { label: "Physical ingestion point", value: "CAM-GATE-09" },
        { label: "Reconciliation ticket", value: "#FAC-8821" },
        { label: "LMS sync streams", value: "Canvas / Moodle core" },
      ],
      footer: "Last automated ingestion: 08-Sep-2026 09:45 ICT",
      tone: "ontrack" as const,
    },
    {
      tag: "SCHEMA::CONSENT",
      icon: "policy",
      title: t("schema3"),
      body: t("schema3Body"),
      rows: [
        { label: "CONS-001 (ATTR-FIN-001)", value: "ERASED_NULL — revoked 08-Sep 10:00 ICT" },
        { label: "Withdrawal receipt", value: "RCPT-WDR-89012" },
        { label: "CONS-082 (housing support)", value: "ACTIVE" },
      ],
      footer: "Zero residual financial bytes transmitted",
      tone: "urgent" as const,
    },
    {
      tag: "SCHEMA::ADVISING",
      icon: "history_edu",
      title: t("schema4"),
      body: t("schema4Body"),
      rows: [
        { label: "Sealed entry", value: "NOTE-001 (03-Sep 10:00)" },
        { label: "Authorising signatory", value: "Demo Advisor 01" },
        { label: "Ledger anchor block", value: "#BLK-20260907-8812" },
        { label: "Linked correction", value: "REQ-CORR-20260906-04" },
      ],
      footer: "Append-only mode · no deletions permitted",
      tone: "sealed" as const,
    },
    {
      tag: "SCHEMA::INSPECTION",
      icon: "visibility",
      title: t("schema5"),
      body: t("schema5Body"),
      rows: [
        { label: "Total audited events", value: "6 logged invocations" },
        { label: "Enforced denial log", value: "08-Sep 10:00:14 ICT" },
        { label: "Target field", value: "ATTR-FIN-001 (blocked)" },
        { label: "Data leak prevention", value: "0 bytes read executed" },
      ],
      footer: "Unabridged — denied attempts are included by design",
      tone: "referral" as const,
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow={["Data portability", "S08 · Export"]}
        title={t("title")}
        description={t("intro")}
        meta={
          <div className="flex flex-wrap items-center gap-space-sm">
            <StatusChip tone="sealed" icon="data_object" label="RFC-8259 / JSON-LD v1.1" />
            <StatusChip tone="sealed" icon="lock_clock" label="LEDGER: #BLK-20260908-8846" />
          </div>
        }
        actions={
          <div className="flex flex-wrap gap-space-sm">
            <Button size="sm" icon="cached">
              {t("prepare")}
            </Button>
            <Button variant="secondary" size="sm" icon="verified">
              {t("verifyMerkle")}
            </Button>
          </div>
        }
      />

      <Notice tone="sealed" icon="gavel">
        Vietnam PDPD Decree 13/2023 Art. 10 · {t("compliance")}
      </Notice>

      <Notice tone="attention" icon="verified_user" title={t("guaranteeTitle")}>
        {t("guaranteeBody")}
      </Notice>

      <div className="flex flex-wrap items-center justify-between gap-space-sm">
        <h2 className="font-headline-sm text-headline-sm text-on-surface">{t("components")}</h2>
        <div className="flex flex-wrap items-center gap-space-sm">
          <span className="font-body-sm text-body-sm text-on-surface-variant">
            {t("componentsCaption", { count: schemas.length })}
          </span>
          <StatusChip
            tone="ontrack"
            icon="done_all"
            label={t("reconciled", { done: schemas.length, total: schemas.length })}
          />
        </div>
      </div>

      <div className="grid gap-space-md xl:grid-cols-2">
        {schemas.map((schema) => (
          <Panel key={schema.tag}>
            <PanelHeader
              icon={schema.icon}
              title={schema.title}
              subtitle={schema.body}
              actions={<StatusChip tone={schema.tone} icon="code" label={schema.tag} />}
            />
            <PanelBody>
              <KeyValueGrid columns={2}>
                {schema.rows.map((row) => (
                  <KeyValue key={row.label} label={row.label} value={row.value} />
                ))}
              </KeyValueGrid>
              <p className="mt-space-base flex items-center gap-1.5 border-t border-outline-variant/30 pt-space-md font-body-sm text-body-sm text-on-surface-variant">
                <Icon name="info" className="text-[14px]" />
                {schema.footer}
              </p>
            </PanelBody>
          </Panel>
        ))}
      </div>

      <Panel>
        <PanelHeader icon="history" title={t("history")} />
        <DataTable className="min-w-[760px]">
          <thead>
            <Tr className="hover:bg-transparent">
              <Th>{t("colExport")}</Th>
              <Th>{t("colScope")}</Th>
              <Th>{t("colFormat")}</Th>
              <Th>{t("colRequested")}</Th>
              <Th>{t("colStatus")}</Th>
              <Th align="right">{t("download")}</Th>
            </Tr>
          </thead>
          <tbody>
            {exports.map((job) => (
              <Tr key={job.ExportId}>
                <Td>
                  <p className="font-code-audit text-code-audit text-on-surface">
                    {job.ExportCode}
                  </p>
                  {job.PayloadHash ? (
                    <p className="font-code-audit text-code-audit text-on-surface-variant">
                      {job.PayloadHash.slice(0, 10)}…
                    </p>
                  ) : null}
                </Td>
                <Td>{job.ScopeLabels.join(", ")}</Td>
                <Td>{job.Format}</Td>
                <Td>
                  <span className="font-code-audit text-code-audit text-on-surface-variant">
                    {job.RequestedAt}
                  </span>
                </Td>
                <Td>
                  <StatusChip
                    tone={
                      job.Status === "READY"
                        ? "ontrack"
                        : job.Status === "EXPIRED"
                          ? "sealed"
                          : "attention"
                    }
                    icon={job.Status === "READY" ? "check_circle" : "schedule"}
                    label={job.Status}
                  />
                </Td>
                <Td align="right">
                  <Button
                    variant="secondary"
                    size="sm"
                    icon="download"
                    disabled={job.Status !== "READY"}
                  >
                    {t("download")}
                  </Button>
                </Td>
              </Tr>
            ))}
          </tbody>
        </DataTable>
      </Panel>

      <p className="font-body-sm text-body-sm text-on-surface-variant">{tc("auditMessage")}</p>
    </>
  );
}
