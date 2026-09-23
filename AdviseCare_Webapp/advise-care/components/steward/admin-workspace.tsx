import {
  Button,
  ButtonLink,
  DataTable,
  KeyValue,
  KeyValueGrid,
  Notice,
  PageHeader,
  Panel,
  PanelBody,
  PanelHeader,
  PanelFooter,
  StatusChip,
  Td,
  Th,
  Tr,
} from "@/components/ui";

export type AdminModule = "m01" | "m02" | "m03" | "m04" | "m05" | "m06" | "m07";

const modules: Record<
  AdminModule,
  { code: string; title: string; subtitle: string; icon: string }
> = {
  m01: {
    code: "M01 // IAM-RBAC",
    title: "Accounts and roles",
    subtitle:
      "Central institutional identity directory and role-based access control governance",
    icon: "manage_accounts",
  },
  m02: {
    code: "M02 // IAM-REGISTRY",
    title: "Faculties and assignments",
    subtitle:
      "Faculty programme governance, advisor quotas and institutional student assignment matrix",
    icon: "account_tree",
  },
  m03: {
    code: "M03 // ETL-INGESTION",
    title: "Data source connections",
    subtitle:
      "Institutional feed connectors, ingestion pipeline governance and schema field mapping",
    icon: "hub",
  },
  m04: {
    code: "M04 // REFERENCE",
    title: "Reference data",
    subtitle:
      "Canonical catalogs, academic period schedules and event taxonomies",
    icon: "menu_book",
  },
  m05: {
    code: "M05 // TELEMETRY",
    title: "System health",
    subtitle:
      "Service availability, ingestion freshness, batch processing telemetry and diagnostics",
    icon: "monitoring",
  },
  m06: {
    code: "M06 // RESTORE",
    title: "Restore and integrity verification",
    subtitle:
      "Bitemporal snapshot validation, cryptographic ledger replay and post-erasure invariance",
    icon: "restore",
  },
  m07: {
    code: "M07 // INDICATORS",
    title: "Indicator configuration",
    subtitle:
      "Versioned academic, attendance and participation indicator governance",
    icon: "tune",
  },
};

const moduleLinks: Record<AdminModule, { href: string; label: string }> = {
  m01: { href: "/admin/m02", label: "Open faculty assignments (M02)" },
  m02: { href: "/admin/m03", label: "View data sources (M03)" },
  m03: { href: "/admin/m05", label: "View ingestion health (M05)" },
  m04: { href: "/admin/m05", label: "View system health (M05)" },
  m05: { href: "/admin/m06", label: "Verify backup integrity (M06)" },
  m06: { href: "/admin/m05", label: "Back to system health (M05)" },
  m07: { href: "/admin/m04", label: "Open reference data (M04)" },
};

const metricSets: Record<
  AdminModule,
  Array<[string, string, string, string]>
> = {
  m01: [
    ["Total accounts", "86", "82 active · 4 disabled", "groups"],
    ["Academic advisors", "42", "1,280 advisees · 4 faculties", "school"],
    ["Welfare officers", "6", "Hardware token MFA", "health_and_safety"],
    [
      "Governance",
      "18",
      "12 heads · 4 stewards · 2 admins",
      "admin_panel_settings",
    ],
  ],
  m02: [
    ["Enrolled students", "320", "100% census verified", "groups"],
    ["Assigned advisees", "314", "6 pending allocation", "how_to_reg"],
    ["Active advisors", "42", "Across 4 faculties", "school"],
    ["Quota target", "1:80", "Zero-overload target", "balance"],
  ],
  m03: [
    ["Connected feeds", "3", "All health verified", "hub"],
    ["Daily ingestion", "14,280", "Records / 24h", "swap_vert"],
    ["Pipeline SLA", "99.94%", "Target ≥ 99.50%", "schedule"],
    ["Key vault", "HSM", "Zero plaintext secrets", "lock"],
  ],
  m04: [
    ["Academic terms", "3", "18 archived", "calendar_month"],
    ["Event taxonomies", "14", "Canonical bilingual codes", "category"],
    ["Downstream feeds", "3", "Active in M03", "account_tree"],
    ["Catalog integrity", "SHA-256", "Verified root", "fingerprint"],
  ],
  m05: [
    [
      "Service availability",
      "99.98%",
      "SLA pass · rolling 30 days",
      "cloud_done",
    ],
    ["Ingestion freshness", "2m 14s", "Drift minimal", "schedule"],
    ["Queued jobs", "18", "4 workers online · 0 stalled", "reorder"],
    ["Failures (24h)", "1", "Retry eligible", "warning"],
  ],
  m06: [
    ["Stored snapshots", "4", "3 verified · 1 pending", "database"],
    ["Ledger blocks", "441,895", "Bitemporal witness chain", "link"],
    ["Restore target", "VLAN-910", "Isolated sandbox", "lan"],
    ["Invariant checks", "3 / 4", "One evaluation in progress", "fact_check"],
  ],
  m07: [
    ["Academic indicators", "3", "Production active", "school"],
    ["Attendance indicators", "2", "Selected configuration", "event_available"],
    ["Participation", "2", "VLE and submission cadence", "data_usage"],
    ["Rule status", "PASS", "No composite risk score", "policy"],
  ],
};

function Metrics({
  items,
}: {
  items: Array<[string, string, string, string]>;
}) {
  return (
    <div className="grid gap-space-md sm:grid-cols-2 xl:grid-cols-4">
      {items.map(([label, value, detail, icon]) => (
        <Panel key={label} className="p-space-base">
          <div className="flex items-center justify-between gap-space-sm">
            <span className="font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
              {label}
            </span>
            <span className="material-symbols-outlined text-[20px] text-primary">
              {icon}
            </span>
          </div>
          <p className="mt-space-sm font-display-sm text-display-sm font-semibold text-on-surface">
            {value}
          </p>
          <p className="mt-1 font-caption text-caption text-on-surface-variant">
            {detail}
          </p>
        </Panel>
      ))}
    </div>
  );
}

function Directory({ module }: { module: AdminModule }) {
  const rows =
    module === "m01"
      ? [
          [
            "ACC-STAFF-0104",
            "Dr. Tran Huu Duc",
            "duc.th@university.edu.vn",
            "Academic Advisor",
            "Active",
          ],
          [
            "ACC-STAFF-0082",
            "Demo Advisor 01",
            "advisor01@university.edu.vn",
            "Academic Advisor",
            "Active",
          ],
          [
            "ACC-STAFF-0055",
            "Dr. Le Thao Nguyen",
            "nguyen.l@university.edu.vn",
            "Student Welfare Officer",
            "Active",
          ],
          [
            "ACC-STAFF-0012",
            "Prof. Vu Thi Kim Oanh",
            "oanh.vtk@university.edu.vn",
            "Programme Head",
            "Active",
          ],
        ]
      : [
          [
            "BSC-CS",
            "Computing & Software",
            "80 advisors",
            "80 / 80",
            "Balanced",
          ],
          [
            "BSC-SE",
            "Software Engineering",
            "76 advisees",
            "76 / 80",
            "Balanced",
          ],
          [
            "BSC-IS",
            "Information Security",
            "80 advisees",
            "80 / 80",
            "Review",
          ],
          ["BSC-DS", "Data Science", "78 advisees", "78 / 80", "Balanced"],
        ];
  return (
    <Panel>
      <PanelHeader
        icon={module === "m01" ? "badge" : "account_tree"}
        title={
          module === "m01"
            ? "Staff identity ledger and role bindings"
            : "Advisor workload and quota matrix"
        }
        subtitle={
          module === "m01"
            ? "Read-synchronized with University Active Directory / Azure AD SAML endpoint"
            : "Institutional student assignment coverage for AY 2026–2027"
        }
        actions={
          <Button
            size="sm"
            icon={module === "m01" ? "person_add" : "add_circle"}
          >
            {module === "m01"
              ? "Add institutional account"
              : "Create assignment"}
          </Button>
        }
      />
      <div className="overflow-x-auto">
        <DataTable className="min-w-[760px]">
          <thead>
            <Tr className="hover:bg-transparent">
              <Th>Reference</Th>
              <Th>Identity / faculty</Th>
              <Th>Scope</Th>
              <Th>Capacity</Th>
              <Th>Status</Th>
            </Tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <Tr key={row[0]}>
                {row.map((cell, index) => (
                  <Td
                    key={`${row[0]}-${index}`}
                    className={
                      index === 0
                        ? "font-code-audit text-code-audit text-primary"
                        : undefined
                    }
                  >
                    {index === row.length - 1 ? (
                      <StatusChip
                        tone={cell === "Review" ? "attention" : "ontrack"}
                        icon={cell === "Review" ? "warning" : "check_circle"}
                        label={cell}
                      />
                    ) : (
                      cell
                    )}
                  </Td>
                ))}
              </Tr>
            ))}
          </tbody>
        </DataTable>
      </div>
      <PanelFooter>
        <span className="font-code-audit text-code-audit">
          Showing {rows.length} of{" "}
          {module === "m01"
            ? "86 institutional accounts"
            : "320 student allocations"}
        </span>
        <StatusChip tone="sealed" icon="lock" label="Audit synchronized" />
      </PanelFooter>
    </Panel>
  );
}

function Operations({ module }: { module: AdminModule }) {
  const content: Record<
    AdminModule,
    {
      title: string;
      icon: string;
      subtitle: string;
      body: string;
      rows: Array<[string, string, string]>;
    }
  > = {
    m03: {
      title: "Configured ingestion feeds",
      icon: "hub",
      subtitle: "Sanitized gateway connectors and zero-PII schema contracts",
      body: "Credentials are represented by hardware references only. Synthetic connection tests never expose raw payloads or secrets in this console.",
      rows: [
        ["FEED-ATTEND-001", "Demo attendance feed", "Connected / healthy"],
        ["FEED-SIS-GRADE-002", "Demo grade feed", "Connected / idle"],
        [
          "FEED-EVENT-CHECKIN-003",
          "Demo event check-in",
          "Connected / scheduled",
        ],
      ],
    },
    m04: {
      title: "Academic term catalog",
      icon: "calendar_month",
      subtitle:
        "Bitemporal reference records remain preserved after retirement",
      body: "Reference records linked to committed advising cases cannot be hard-deleted. Retirement preserves foreign keys for historical lookup and institutional audit.",
      rows: [
        [
          "TERM-AY2627-FALL",
          "Fall 2026 (Statutory AY 2026–2027)",
          "Active / current census",
        ],
        [
          "TERM-AY2627-SPRING",
          "Spring 2027 (Statutory AY 2026–2027)",
          "Active / upcoming",
        ],
        [
          "TERM-AY2526-SUMMER",
          "Summer 2026 (AY 2025–2026)",
          "Concluded / archived",
        ],
      ],
    },
    m05: {
      title: "Ingestion batch execution log",
      icon: "monitoring",
      subtitle:
        "Operational telemetry only; no student attributes are rendered",
      body: "Health metrics describe platform operation and infrastructure throughput. Automated retries require cryptographic operator authorization before replay.",
      rows: [
        [
          "BATCH-ATT-184",
          "Demo attendance feed",
          "Partial failure · retry eligible",
        ],
        ["BATCH-EVT-088", "Demo event check-in", "Succeeded · 100%"],
        ["BATCH-SIS-041", "Demo grade feed", "Succeeded · 100%"],
      ],
    },
    m06: {
      title: "Snapshot catalog and verification manifest",
      icon: "database",
      subtitle:
        "Cold-storage witness ledgers for isolated restore verification",
      body: "Restore tests execute strictly inside an ephemeral sandbox and must not revive erased records, invalidated keys or redacted welfare disclosures.",
      rows: [
        ["SNAP-20260908-0300", "441,895 ledger blocks", "Pending check"],
        ["SNAP-20260907-0300", "439,120 ledger blocks", "Verified 100%"],
        ["SNAP-20260906-0300", "436,812 ledger blocks", "Verified 100%"],
      ],
    },
    m07: {
      title: "Indicator specification: Biweekly attendance rate",
      icon: "tune",
      subtitle: "IND-ATT-01 · Production v1.2 · immutable after publication",
      body: "Indicators are discrete factual observations. The platform prohibits combining cross-domain signals into a singular student risk score or automated academic judgment.",
      rows: [
        ["Required stream", "FEED-ATTEND-001.checkin_timestamp", "Compulsory"],
        ["Evaluation horizon", "Rolling 14 days", "Active"],
        ["Review threshold", "Attendance below 75.0%", "Governance value"],
      ],
    },
    m01: {
      title: "System administration",
      icon: "admin_panel_settings",
      subtitle: "Institutional root trust",
      body: "Select an administration module from the left navigation to inspect its operational registry.",
      rows: [],
    },
    m02: {
      title: "Assignment governance",
      icon: "account_tree",
      subtitle: "Quota changes require programme-head adjudication",
      body: "Student assignment changes are routed through the immutable governance ledger.",
      rows: [],
    },
  };
  const selected = content[module];
  return (
    <Panel>
      <PanelHeader
        icon={selected.icon}
        title={selected.title}
        subtitle={selected.subtitle}
        actions={
          <StatusChip
            tone="sealed"
            icon="verified_user"
            label="Governance enforced"
          />
        }
      />
      <PanelBody>
        <p className="max-w-4xl font-body-sm text-body-sm text-on-surface-variant">
          {selected.body}
        </p>
        {selected.rows.length ? (
          <div className="mt-space-lg space-y-space-sm">
            {selected.rows.map(([label, value, status]) => (
              <div
                key={label}
                className="flex flex-wrap items-center justify-between gap-space-md rounded border border-outline-variant/40 bg-surface-container-low/50 px-space-base py-space-md"
              >
                <div>
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    {label}
                  </p>
                  <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
                    {value}
                  </p>
                </div>
                <StatusChip
                  tone={
                    status.includes("failure") || status.includes("Pending")
                      ? "attention"
                      : "ontrack"
                  }
                  icon={
                    status.includes("failure") || status.includes("Pending")
                      ? "warning"
                      : "check_circle"
                  }
                  label={status}
                />
              </div>
            ))}
          </div>
        ) : null}
      </PanelBody>
    </Panel>
  );
}

export function AdminWorkspace({ module }: { module: AdminModule }) {
  const current = modules[module];
  const adjacent = moduleLinks[module];
  return (
    <>
      <PageHeader
        eyebrow={[current.code, "Institutional root trust"]}
        title={`M${module.slice(1)} — ${current.title}`}
        description={current.subtitle}
        meta={
          <StatusChip
            tone="sealed"
            icon="verified_user"
            label="Decree 13/2023 compliant"
          />
        }
        actions={
          <ButtonLink href={adjacent.href} size="sm" iconAfter="arrow_forward">
            {adjacent.label}
          </ButtonLink>
        }
      />
      <Notice
        tone="sealed"
        icon={current.icon}
        title="Statutory administration protocol"
      >
        This demonstration console is restricted to operational metadata,
        configuration and audit evidence. Student personal circumstances,
        confidential narratives and raw connector secrets are never rendered
        here. Every committed change is recorded in the institutional ledger.
      </Notice>
      <Metrics items={metricSets[module]} />
      {module === "m01" || module === "m02" ? (
        <Directory module={module} />
      ) : (
        <Operations module={module} />
      )}
      <div className="grid gap-space-md xl:grid-cols-2">
        <Panel>
          <PanelHeader
            icon="security"
            title="Demonstration administrator session"
            subtitle="The current workspace is rendered as System Administrator"
          />
          <PanelBody>
            <KeyValueGrid columns={2}>
              <KeyValue label="Account" value="System Administrator" />
              <KeyValue label="Role code" value="SYSTEM_ADMIN" mono />
              <KeyValue label="Directory" value="Institution-wide" />
              <KeyValue label="Session state" value="SSO + MFA verified" />
            </KeyValueGrid>
          </PanelBody>
        </Panel>
        <Panel>
          <PanelHeader
            icon="policy"
            title="Control boundary"
            subtitle="Operational actions remain explicitly scoped"
          />
          <PanelBody>
            <p className="font-body-sm text-body-sm text-on-surface-variant">
              Configuration changes require an institutional identity, an
              explicit purpose and an immutable audit receipt. Destructive
              actions are represented as retirement or revocation rather than
              hard deletion.
            </p>
            <div className="mt-space-base">
              <StatusChip
                tone="ontrack"
                icon="check_circle"
                label="Audit trail active"
              />
            </div>
          </PanelBody>
        </Panel>
      </div>
    </>
  );
}
