import {
  ButtonLink,
  DataTable,
  Field,
  Panel,
  PanelBody,
  PanelHeader,
  SearchInput,
  Select,
  StatusChip,
  Td,
  Th,
  Tr,
} from "@/components/ui";

const referrals = [
  {
    code: "REF-001",
    date: "07-Sep-2026 · 14:35 ICT",
    student: "Nguyen Van An",
    id: "DEMO-001 / STU-2024-001",
    faculty: "Computing & Software Systems",
    category: "Housing & living stability friction",
    priority: "P2 Moderate · 48h SLA",
    scope: "Consented (Housing)",
    worker: "Demo Officer 01",
    state: "In Triage",
  },
  {
    code: "REF-002",
    date: "07-Sep-2026 · 11:20 ICT",
    student: "Tran Thi Bich",
    id: "STU-2023-088",
    faculty: "Faculty of Engineering",
    category: "Commute & hardship relief",
    priority: "P1 Urgent · 4h SLA",
    scope: "Full welfare scope",
    worker: "Unassigned",
    state: "Action required",
  },
  {
    code: "REF-003",
    date: "06-Sep-2026 · 16:15 ICT",
    student: "Le Hoang Nam",
    id: "STU-2024-142",
    faculty: "School of Business",
    category: "Mental wellbeing peer check",
    priority: "P2 Moderate · 48h SLA",
    scope: "Consented (Wellbeing)",
    worker: "Officer 02",
    state: "Casework open",
  },
  {
    code: "REF-004",
    date: "05-Sep-2026 · 09:20 ICT",
    student: "Pham Minh Duc",
    id: "STU-2022-119",
    faculty: "Faculty of Science",
    category: "Crisis grant coordination",
    priority: "P3 Standard · 5d SLA",
    scope: "Incomplete scope",
    worker: "Officer 03",
    state: "Clarification",
  },
];

const queueTabs = [
  { label: "All referrals", count: "12", active: false },
  { label: "Unassigned", count: "3", active: false },
  { label: "In triage", count: "4", active: true },
  { label: "Casework open", count: "5", active: false },
  { label: "Closed / archived", count: "18", active: false },
];

export default function WelfareReferralsPage() {
  return (
    <>
      <header className="flex flex-wrap items-start justify-between gap-space-lg">
        <div>
          <h1 className="mt-space-xs font-display-sm text-display-sm text-on-surface">
            Welfare Referrals Inbox
          </h1>
          <p className="mt-space-xs max-w-3xl font-body-sm text-body-sm text-on-surface-variant">
            Institutional triage and caseworker allocation for academic
            referrals dispatched under the welfare support purpose.
          </p>
        </div>
        <div className="flex flex-wrap gap-space-sm">
          <ButtonLink
            href="/welfare/statistics"
            variant="secondary"
            size="sm"
            icon="query_stats"
          >
            View statistics
          </ButtonLink>
          <ButtonLink
            href="/welfare/cases/REF-001"
            size="sm"
            icon="arrow_forward"
          >
            Open selected case
          </ButtonLink>
        </div>
      </header>

      <Panel>
        <PanelBody className="space-y-space-md">
          <div className="grid gap-space-md lg:grid-cols-[minmax(0,1fr)_auto] lg:items-end">
            <Field label="Search referral, student, or faculty">
              <SearchInput placeholder="REF-001, student identifier, or referring faculty" />
            </Field>
            <div className="flex flex-wrap gap-space-sm">
              <Field label="Consent scope">
                <Select defaultValue="all">
                  <option value="all">All consent scopes</option>
                  <option>Full welfare scope</option>
                  <option>Partial scope</option>
                </Select>
              </Field>
              <Field label="Faculty">
                <Select defaultValue="all">
                  <option value="all">All faculties</option>
                  <option>Computing & Software Systems</option>
                  <option>Faculty of Engineering</option>
                </Select>
              </Field>
            </div>
          </div>
          <div className="flex flex-wrap gap-space-xs border-t border-outline-variant/30 pt-space-md">
            {queueTabs.map((tab) => (
              <button
                key={tab.label}
                type="button"
                className={
                  tab.active
                    ? "bg-primary px-space-md py-space-sm font-label-md text-label-md font-semibold text-on-primary"
                    : "border border-outline-variant/40 px-space-md py-space-sm font-label-md text-label-md text-on-surface-variant"
                }
              >
                {tab.label}{" "}
                <span className="ml-1 font-code-audit text-code-audit">
                  {tab.count}
                </span>
              </button>
            ))}
          </div>
        </PanelBody>
      </Panel>

      <Panel>
        <PanelHeader
          icon="inbox"
          title="Referral ledger"
          subtitle="Showing active inbound queue for the Welfare Directorate"
          actions={
            <StatusChip
              tone="urgent"
              icon="priority_high"
              label="4 action required"
            />
          }
        />
        <div className="overflow-x-auto">
          <DataTable className="min-w-245">
            <thead>
              <Tr className="hover:bg-transparent">
                <Th>Ref & date</Th>
                <Th>Student & faculty</Th>
                <Th>Classification & SLA</Th>
                <Th>Consent scope</Th>
                <Th>Caseworker</Th>
                <Th>Action</Th>
              </Tr>
            </thead>
            <tbody>
              {referrals.map((referral, index) => (
                <Tr
                  key={referral.code}
                  className={index === 0 ? "bg-secondary-fixed/20" : undefined}
                >
                  <Td>
                    <span className="font-code-audit text-code-audit font-semibold text-primary">
                      {referral.code}
                    </span>
                    <p className="mt-1 text-on-surface-variant">
                      {referral.date}
                    </p>
                    <p className="font-caption text-caption text-secondary">
                      Origin: advising case
                    </p>
                  </Td>
                  <Td>
                    <p className="font-label-md text-label-md font-semibold">
                      {referral.student}
                    </p>
                    <p className="font-code-audit text-code-audit text-on-surface-variant">
                      {referral.id}
                    </p>
                    <p className="mt-1 text-on-surface-variant">
                      {referral.faculty}
                    </p>
                  </Td>
                  <Td>
                    <p className="font-label-md text-label-md font-medium">
                      {referral.category}
                    </p>
                    <StatusChip
                      tone={
                        referral.priority.startsWith("P1")
                          ? "urgent"
                          : referral.priority.startsWith("P2")
                            ? "attention"
                            : "sealed"
                      }
                      icon="schedule"
                      label={referral.priority}
                    />
                  </Td>
                  <Td>
                    <StatusChip
                      tone={
                        referral.scope.startsWith("Incomplete")
                          ? "attention"
                          : "ontrack"
                      }
                      icon={
                        referral.scope.startsWith("Incomplete")
                          ? "warning"
                          : "verified"
                      }
                      label={referral.scope}
                    />
                    <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
                      CONS-082 verified
                    </p>
                  </Td>
                  <Td>
                    <p className="font-label-md text-label-md font-semibold">
                      {referral.worker}
                    </p>
                    <StatusChip
                      tone={
                        referral.state === "Action required"
                          ? "urgent"
                          : "referral"
                      }
                      label={referral.state}
                    />
                  </Td>
                  <Td>
                    <ButtonLink
                      href="/welfare/cases/REF-001"
                      size="sm"
                      iconAfter="arrow_forward"
                    >
                      Inspect
                    </ButtonLink>
                  </Td>
                </Tr>
              ))}
            </tbody>
          </DataTable>
        </div>
      </Panel>
    </>
  );
}
