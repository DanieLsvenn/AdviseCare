"use client";

import { useState } from "react";
import { Link } from "@/i18n/navigation";
import { Icon } from "@/components/ui/icon";
import { cn } from "@/lib/cn";

type HeadView = "overview" | "workload" | "assignments" | "reports";

const navigation: {
  view: HeadView;
  label: string;
  icon: string;
  href: string;
}[] = [
  {
    view: "overview",
    label: "Cohort overview",
    icon: "analytics",
    href: "/head/programme-overview",
  },
  {
    view: "workload",
    label: "Advisor workload",
    icon: "balance",
    href: "/head/advisor-workload",
  },
  {
    view: "assignments",
    label: "Assignment changes",
    icon: "assignment_ind",
    href: "/head/assignment-changes",
  },
  {
    view: "reports",
    label: "Term reports",
    icon: "description",
    href: "/head/reports",
  },
];

const advisors = [
  {
    name: "Dr. Tran Huu Duc",
    focus: "Software Engineering & Systems",
    total: 80,
    active: 12,
    closed: 8,
    tone: "primary",
  },
  {
    name: "Demo Advisor 01",
    focus: "Foundations & Transition",
    total: 80,
    active: 14,
    closed: 5,
    tone: "attention",
  },
  {
    name: "Dr. Nguyen Mai Lan",
    focus: "Algorithms & Theoretical CS",
    total: 80,
    active: 11,
    closed: 7,
    tone: "primary",
  },
  {
    name: "Dr. Pham Minh Tu",
    focus: "Networks & Cloud Systems",
    total: 80,
    active: 11,
    closed: 6,
    tone: "primary",
  },
];

const requisitions = [
  {
    id: "REQ-REALLOC-2026-04",
    source: "Demo Advisor 01",
    recipient: "Dr. Tran Huu Duc",
    amount: 6,
    status: "Pending head review",
    detail:
      "Elevated active care queue; module-clash cases need balanced capacity.",
    tone: "attention",
  },
  {
    id: "REQ-REALLOC-2026-03",
    source: "Dr. Nguyen Mai Lan",
    recipient: "Dr. Pham Minh Tu",
    amount: 4,
    status: "Approved · pending M02",
    detail: "Final-year capstone intake equalisation.",
    tone: "primary",
  },
  {
    id: "REQ-REALLOC-2026-01",
    source: "Dr. Pham Minh Tu",
    recipient: "Demo Advisor 01",
    amount: 4,
    status: "Applied in SIS",
    detail: "Term intake balancing completed and ledger verified.",
    tone: "secondary",
  },
  {
    id: "REQ-REALLOC-2026-02",
    source: "Dr. Tran Huu Duc",
    recipient: "Dr. Nguyen Mai Lan",
    amount: 2,
    status: "Rejected · locked",
    detail: "Quota ceiling would exceed the statutory tolerance band.",
    tone: "urgent",
  },
];

function Surface({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <section
      className={cn(
        "rounded border border-outline-variant/40 bg-surface-container-lowest",
        className,
      )}
    >
      {children}
    </section>
  );
}

function Header({ view }: { view: HeadView }) {
  return (
    <div className="flex flex-col gap-space-md border-b border-outline-variant/30 pb-space-md">
      <div className="flex flex-wrap items-center justify-between gap-space-sm">
        <div className="flex flex-wrap gap-space-xs">
          {navigation
            .filter((item) => item.view !== view)
            .map((item) => (
              <Link
                key={item.view}
                href={item.href}
                className="inline-flex items-center gap-space-xs rounded border border-outline-variant/50 bg-surface-container-lowest px-space-sm py-space-xs font-label-sm text-label-sm text-on-surface hover:bg-surface-container-high"
              >
                <Icon name={item.icon} className="text-[16px] text-primary" />
                {item.label}
              </Link>
            ))}
        </div>
      </div>
      <div className="flex flex-wrap items-end justify-between gap-space-sm">
        <div>
          <p className="font-caption text-caption font-semibold tracking-wider text-primary">
            Programme Head Portal
          </p>
          <h1 className="font-headline-lg text-headline-lg font-semibold tracking-tight text-on-surface">
            {view === "overview"
              ? "Programme strategic cohort analytics"
              : view === "workload"
                ? "Faculty advising workload ledger"
                : view === "assignments"
                  ? "Faculty caseload reallocation & assignment approvals"
                  : "Term strategic governance & aggregate report exporter"}
          </h1>
          <p className="mt-space-2xs font-body-sm text-body-sm text-on-surface-variant">
            Fall 2026 · Academic Year 2026–2027 · Aggregate oversight scope
          </p>
        </div>
        <span className="inline-flex items-center gap-space-xs rounded bg-surface-container-high px-space-sm py-space-xs font-code-audit text-code-audit font-semibold text-primary">
          <Icon name="verified" className="text-[16px]" /> K-anonymity verified
        </span>
      </div>
    </div>
  );
}

function PrivacyNotice({ reports = false }: { reports?: boolean }) {
  return (
    <div className="flex items-start gap-space-md rounded border border-outline-variant/40 border-l-4 border-l-primary bg-surface-container-low px-space-md py-space-base">
      <span className="flex size-9 shrink-0 items-center justify-center rounded bg-primary/10 text-primary">
        <Icon
          name={reports ? "verified_user" : "shield"}
          className="text-[20px]"
        />
      </span>
      <div>
        <div className="flex flex-wrap items-center gap-space-sm">
          <p className="font-label-md text-label-md font-semibold tracking-wide text-primary">
            Statutory privacy &amp; aggregation safeguard protocol
          </p>
          <span className="rounded bg-surface-container-high px-space-xs py-space-2xs font-code-audit text-caption text-on-surface-variant">
            Rule 5 · SEC-09 compliant
          </span>
        </div>
        <p className="mt-space-2xs max-w-5xl font-body-sm text-body-sm leading-relaxed text-on-surface-variant">
          This workspace renders aggregate cohort and capacity metrics only.
          Student names, student IDs, personal case narratives, and micro-data
          drill-downs are excluded. Small cells are suppressed as{" "}
          <strong className="text-on-surface">Insufficient group size</strong>{" "}
          to prevent re-identification under Decree 13/2023.
        </p>
      </div>
    </div>
  );
}

function Kpis({
  items,
}: {
  items: {
    label: string;
    value: string;
    detail: string;
    icon: string;
    tone?: string;
  }[];
}) {
  return (
    <div className="grid grid-cols-1 gap-space-md sm:grid-cols-2 xl:grid-cols-4">
      {items.map((item) => (
        <Surface
          key={item.label}
          className={cn(
            "border-t-2 p-space-md",
            item.tone === "urgent"
              ? "border-t-error"
              : item.tone === "attention"
                ? "border-t-status-attention-text"
                : "border-t-primary",
          )}
        >
          <div className="flex items-start justify-between">
            <span className="font-caption text-caption font-semibold uppercase tracking-wider text-outline">
              {item.label}
            </span>
            <Icon
              name={item.icon}
              className={cn(
                "text-[20px]",
                item.tone === "urgent" ? "text-error" : "text-primary",
              )}
            />
          </div>
          <p className="mt-space-sm font-display-sm text-display-sm font-bold tabular text-on-surface">
            {item.value}
          </p>
          <p className="font-body-sm text-body-sm text-on-surface-variant">
            {item.detail}
          </p>
        </Surface>
      ))}
    </div>
  );
}

function Bar({
  label,
  value,
  count,
  color = "bg-primary",
}: {
  label: string;
  value: number;
  count: string;
  color?: string;
}) {
  return (
    <div className="space-y-space-xs">
      <div className="flex justify-between gap-space-sm font-label-sm text-label-sm">
        <span>{label}</span>
        <span className="font-code-audit text-code-audit text-on-surface-variant">
          {count}
        </span>
      </div>
      <div className="h-2 rounded-full bg-surface-container-high">
        <div
          className={cn("h-2 rounded-full", color)}
          style={{ width: `${value}%` }}
        />
      </div>
    </div>
  );
}

function Overview() {
  return (
    <div className="flex flex-col gap-space-lg">
      <Kpis
        items={[
          {
            label: "Total enrolled cohort",
            value: "320",
            detail: "100.0% census verified",
            icon: "groups",
          },
          {
            label: "Good standing",
            value: "242",
            detail: "75.6% of total cohort",
            icon: "check_circle",
          },
          {
            label: "Advising intervention",
            value: "48",
            detail: "15.0% targeted coaching",
            icon: "support_agent",
          },
          {
            label: "Specialist review",
            value: "30",
            detail: "9.4% escalated panel",
            icon: "flag",
            tone: "urgent",
          },
        ]}
      />
      <div className="grid grid-cols-1 gap-space-lg xl:grid-cols-2">
        <Surface className="p-space-lg">
          <div className="mb-space-md flex items-start justify-between gap-space-sm">
            <div>
              <h2 className="font-headline-sm text-headline-sm font-semibold">
                Academic performance &amp; grade risk
              </h2>
              <p className="font-body-sm text-body-sm text-on-surface-variant">
                Statutory cumulative GPA stratification across four curriculum
                years.
              </p>
            </div>
            <Icon name="school" className="text-primary" />
          </div>
          <div className="space-y-space-md">
            <Bar
              label="First class honours · GPA 3.60–4.00"
              value={21.5}
              count="68 · 21.5%"
              color="bg-secondary"
            />
            <Bar
              label="Upper second · GPA 3.00–3.59"
              value={38.4}
              count="122 · 38.4%"
              color="bg-primary"
            />
            <Bar
              label="Lower second · GPA 2.50–2.99"
              value={27.2}
              count="86 · 27.2%"
              color="bg-tertiary"
            />
            <Bar
              label="Academic attention required"
              value={8.9}
              count="28 · 8.9%"
              color="bg-error"
            />
          </div>
          <div className="mt-space-lg rounded bg-surface-container-low px-space-md py-space-sm font-caption text-caption text-on-surface-variant">
            Coverage: 98.8% (316/320) · four records await formal credit
            transfer verification.
          </div>
        </Surface>
        <Surface className="p-space-lg">
          <div className="mb-space-md flex items-start justify-between gap-space-sm">
            <div>
              <h2 className="font-headline-sm text-headline-sm font-semibold">
                Attendance &amp; in-person telemetry
              </h2>
              <p className="font-body-sm text-body-sm text-on-surface-variant">
                Campus RFID, laboratory and session participation bands.
              </p>
            </div>
            <Icon name="sensor_door" className="text-primary" />
          </div>
          <div className="space-y-space-md">
            <Bar
              label="High regular attendance · ≥ 90.0%"
              value={60.6}
              count="194 · 60.6%"
              color="bg-secondary"
            />
            <Bar
              label="Compliant institutional range"
              value={25.6}
              count="82 · 25.6%"
              color="bg-primary"
            />
            <Bar
              label="Moderate attendance drift"
              value={10}
              count="32 · 10.0%"
              color="bg-tertiary"
            />
            <Bar
              label="Critical absence pattern"
              value={3.8}
              count="12 · 3.8%"
              color="bg-error"
            />
          </div>
          <div className="mt-space-lg rounded bg-surface-container-low px-space-md py-space-sm font-caption text-caption text-on-surface-variant">
            Telemetry completeness audit: zero missing streams across the active
            cohort.
          </div>
        </Surface>
      </div>
      <Surface className="p-space-lg">
        <div className="mb-space-md flex items-center justify-between gap-space-sm">
          <div>
            <h2 className="font-headline-sm text-headline-sm font-semibold">
              Virtual learning environment &amp; code repository telemetry
            </h2>
            <p className="font-body-sm text-body-sm text-on-surface-variant">
              LMS activity clusters and repository commit rates with
              suppression.
            </p>
          </div>
          <span className="font-code-audit text-code-audit text-primary">
            Coverage 96.9%
          </span>
        </div>
        <div className="grid grid-cols-1 gap-space-md sm:grid-cols-3">
          <Bar
            label="High digital activity"
            value={53.2}
            count="165"
            color="bg-tertiary"
          />
          <Bar
            label="Consistent participation"
            value={33.5}
            count="104"
            color="bg-primary"
          />
          <Bar
            label="Low online engagement"
            value={10.6}
            count="33"
            color="bg-surface-dim"
          />
        </div>
      </Surface>
    </div>
  );
}

function Workload() {
  return (
    <div className="flex flex-col gap-space-lg">
      <Kpis
        items={[
          {
            label: "Assigned advisees",
            value: "320",
            detail: "Target 80 per advisor",
            icon: "groups",
          },
          {
            label: "Active advising cases",
            value: "48",
            detail: "15.0% structured care load",
            icon: "pending_actions",
          },
          {
            label: "Completed cases",
            value: "26",
            detail: "+6 versus previous week",
            icon: "task_alt",
          },
          {
            label: "Median closure time",
            value: "14.5d",
            detail: "Nominal operational range",
            icon: "timer",
          },
        ]}
      />
      <Surface className="p-space-lg">
        <div className="mb-space-md flex items-center justify-between">
          <div>
            <h2 className="font-headline-sm text-headline-sm font-semibold">
              Caseload distribution &amp; operational balance
            </h2>
            <p className="font-body-sm text-body-sm text-on-surface-variant">
              Aggregate capacity tracking across four designated faculty
              advisors.
            </p>
          </div>
          <span className="font-code-audit text-code-audit text-secondary">
            Target ratio 1:80
          </span>
        </div>
        <div className="grid grid-cols-1 gap-space-md lg:grid-cols-2">
          {advisors.map((advisor) => (
            <div
              key={advisor.name}
              className="rounded border border-outline-variant/30 bg-surface-container-low p-space-md"
            >
              <div className="flex items-start justify-between gap-space-sm">
                <div>
                  <h3 className="font-label-md text-label-md font-semibold">
                    {advisor.name}
                  </h3>
                  <p className="font-caption text-caption text-on-surface-variant">
                    {advisor.focus}
                  </p>
                </div>
                <span
                  className={cn(
                    "rounded px-space-xs py-space-2xs font-caption text-caption",
                    advisor.tone === "attention"
                      ? "bg-status-attention-surface text-status-attention-text"
                      : "bg-status-ontrack-surface text-status-ontrack-text",
                  )}
                >
                  {advisor.tone === "attention"
                    ? "Elevated active queue"
                    : "Balanced workload"}
                </span>
              </div>
              <div className="mt-space-md grid grid-cols-3 gap-space-sm text-center">
                <div>
                  <p className="font-headline-md text-headline-md font-semibold">
                    {advisor.total}
                  </p>
                  <p className="font-caption text-caption text-on-surface-variant">
                    Total
                  </p>
                </div>
                <div>
                  <p className="font-headline-md text-headline-md font-semibold text-primary">
                    {advisor.active}
                  </p>
                  <p className="font-caption text-caption text-on-surface-variant">
                    Active
                  </p>
                </div>
                <div>
                  <p className="font-headline-md text-headline-md font-semibold">
                    {advisor.closed}
                  </p>
                  <p className="font-caption text-caption text-on-surface-variant">
                    Closed
                  </p>
                </div>
              </div>
              <div className="mt-space-md h-2 rounded-full bg-surface-container-high">
                <div
                  className={cn(
                    "h-2 rounded-full",
                    advisor.tone === "attention"
                      ? "bg-status-attention-text"
                      : "bg-primary",
                  )}
                  style={{ width: `${(advisor.active / 20) * 100}%` }}
                />
              </div>
            </div>
          ))}
        </div>
      </Surface>
      <Surface className="p-space-lg">
        <div className="mb-space-md flex items-center justify-between">
          <h2 className="font-headline-sm text-headline-sm font-semibold">
            Faculty workload equity audit ledger
          </h2>
          <span className="font-code-audit text-code-audit text-on-surface-variant">
            N=4 active rows
          </span>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full min-w-170 text-left">
            <thead>
              <tr className="border-b border-outline-variant/40 font-caption text-caption uppercase tracking-wider text-outline">
                <th className="pb-space-sm">Advisor</th>
                <th className="pb-space-sm">Cohort scope</th>
                <th className="pb-space-sm">Active cases</th>
                <th className="pb-space-sm">Median closure</th>
                <th className="pb-space-sm">Equity index</th>
              </tr>
            </thead>
            <tbody>
              {advisors.map((advisor, index) => (
                <tr
                  key={advisor.name}
                  className="border-b border-outline-variant/20 font-body-sm text-body-sm"
                >
                  <td className="py-space-sm font-semibold">{advisor.name}</td>
                  <td className="py-space-sm text-on-surface-variant">
                    Cohorts 2023–2026
                  </td>
                  <td className="py-space-sm tabular">
                    {advisor.active} ({((advisor.active / 80) * 100).toFixed(1)}
                    %)
                  </td>
                  <td className="py-space-sm tabular">
                    {[12, 18.2, 13.5, 15][index]}d
                  </td>
                  <td className="py-space-sm">
                    <span
                      className={cn(
                        "rounded px-space-xs py-space-2xs font-caption",
                        advisor.tone === "attention"
                          ? "bg-status-attention-surface text-status-attention-text"
                          : "bg-status-ontrack-surface text-status-ontrack-text",
                      )}
                    >
                      {advisor.tone === "attention"
                        ? "1.17 · elevated"
                        : "0.9{index + 2} · balanced"}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Surface>
    </div>
  );
}

function Assignments() {
  const [selected, setSelected] = useState(requisitions[0].id);
  const [approved, setApproved] = useState(false);
  const selectedRequest = requisitions.find((item) => item.id === selected)!;
  return (
    <div className="flex flex-col gap-space-lg">
      <Kpis
        items={[
          {
            label: "Pending head review",
            value: approved ? "0" : "1",
            detail: "Requisition · impacts 6 advisees",
            icon: "pending_actions",
            tone: "attention",
          },
          {
            label: "Approved · pending M02",
            value: "1",
            detail: "Awaiting administrative execution",
            icon: "sync_saved_locally",
          },
          {
            label: "Applied in AY26 SIS",
            value: "2",
            detail: "14 reallocations executed",
            icon: "task_alt",
          },
          {
            label: "Caseload equity target",
            value: "1:80",
            detail: "Tolerance ±10% · zero over-cap",
            icon: "balance",
          },
        ]}
      />
      <div className="grid grid-cols-1 items-start gap-space-lg xl:grid-cols-5">
        <Surface className="p-space-md xl:col-span-2">
          <div className="mb-space-md flex items-center justify-between">
            <h2 className="font-headline-sm text-headline-sm font-semibold">
              Requisition queue
            </h2>
            <span className="font-caption text-caption uppercase text-outline">
              Sorted by priority
            </span>
          </div>
          <div className="flex flex-col gap-space-sm">
            {requisitions.map((item) => (
              <button
                type="button"
                key={item.id}
                onClick={() => setSelected(item.id)}
                className={cn(
                  "rounded border p-space-md text-left transition-colors",
                  selected === item.id
                    ? "border-2 border-primary bg-surface-container-low"
                    : "border-outline-variant/30 hover:bg-surface-container-low",
                )}
              >
                <div className="flex items-start justify-between gap-space-sm">
                  <span className="font-code-audit text-code-audit font-semibold text-primary">
                    {item.id}
                  </span>
                  <span
                    className={cn(
                      "rounded px-space-xs py-space-2xs font-caption text-caption",
                      item.tone === "urgent"
                        ? "bg-status-urgent-surface text-status-urgent-text"
                        : item.tone === "attention"
                          ? "bg-status-attention-surface text-status-attention-text"
                          : item.tone === "secondary"
                            ? "bg-status-ontrack-surface text-status-ontrack-text"
                            : "bg-status-referral-surface text-status-referral-text",
                    )}
                  >
                    {item.status}
                  </span>
                </div>
                <p className="mt-space-sm font-label-md text-label-md font-semibold">
                  {item.source} <span className="text-primary">→</span>{" "}
                  {item.recipient}
                </p>
                <p className="mt-space-xs font-caption text-caption text-on-surface-variant">
                  {item.amount} advisees · Effective 15 Sep 2026
                </p>
              </button>
            ))}
          </div>
        </Surface>
        <div className="flex flex-col gap-space-lg xl:col-span-3">
          <Surface className="p-space-lg">
            <div className="flex items-start justify-between gap-space-md">
              <div>
                <p className="font-code-audit text-code-audit text-primary">
                  {selectedRequest.id}
                </p>
                <h2 className="mt-space-xs font-headline-sm text-headline-sm font-semibold">
                  Caseload change inspection
                </h2>
                <p className="mt-space-xs font-body-sm text-body-sm text-on-surface-variant">
                  {selectedRequest.detail}
                </p>
              </div>
              <Icon name="fact_check" className="text-primary" />
            </div>
            <div className="mt-space-lg grid grid-cols-1 gap-space-sm sm:grid-cols-2">
              <div className="rounded bg-surface-container-low p-space-md">
                <p className="font-caption text-caption uppercase text-outline">
                  Source faculty
                </p>
                <p className="mt-space-xs font-label-md text-label-md font-semibold">
                  {selectedRequest.source}
                </p>
                <p className="font-code-audit text-code-audit text-on-surface-variant">
                  80 → {80 - selectedRequest.amount} advisees
                </p>
              </div>
              <div className="rounded bg-surface-container-low p-space-md">
                <p className="font-caption text-caption uppercase text-outline">
                  Recipient faculty
                </p>
                <p className="mt-space-xs font-label-md text-label-md font-semibold">
                  {selectedRequest.recipient}
                </p>
                <p className="font-code-audit text-code-audit text-on-surface-variant">
                  80 → {80 + selectedRequest.amount} advisees
                </p>
              </div>
            </div>
            <div className="mt-space-lg rounded border border-outline-variant/30 bg-surface-container-low p-space-md">
              <p className="font-label-sm text-label-sm font-semibold uppercase text-primary">
                Programme head adjudication protocol
              </p>
              <p className="mt-space-xs font-body-sm text-body-sm text-on-surface-variant">
                Approval generates a certified decision ledger receipt. No
                student-level identifiers are available in this aggregate
                inspection view.
              </p>
            </div>
            <div className="mt-space-lg flex flex-wrap justify-end gap-space-sm">
              <button
                type="button"
                onClick={() => setApproved(false)}
                className="rounded border border-error px-space-md py-space-sm font-label-sm text-label-sm font-semibold text-error hover:bg-status-urgent-surface"
              >
                Reject requisition
              </button>
              <button
                type="button"
                onClick={() => setApproved(true)}
                className="inline-flex items-center gap-space-xs rounded bg-primary px-space-md py-space-sm font-label-sm text-label-sm font-semibold text-on-primary hover:bg-primary-container"
              >
                <Icon name="verified" className="text-[16px]" />{" "}
                {approved
                  ? "Decision receipt generated"
                  : "Approve reallocation"}
              </button>
            </div>
          </Surface>
          {approved ? (
            <Surface className="border-primary bg-surface-container-low p-space-lg">
              <p className="font-code-audit text-code-audit font-semibold text-primary">
                STATUTORY DECISION RECEIPT · DEC-REALLOC-20260908-H03
              </p>
              <p className="mt-space-sm font-headline-sm text-headline-sm font-semibold">
                Approved · ready to apply in M02
              </p>
              <p className="mt-space-xs font-body-sm text-body-sm text-on-surface-variant">
                Aggregate transfer of {selectedRequest.amount} advisee
                allocations recorded for the Fall 2026 cohort. Ledger hash:
                SIS-REALLOC-2026-F.
              </p>
            </Surface>
          ) : null}
        </div>
      </div>
    </div>
  );
}

function Reports() {
  const [prepared, setPrepared] = useState(false);
  const [format, setFormat] = useState("pdf");
  const [modules, setModules] = useState({
    academic: true,
    attendance: true,
    vle: true,
    workload: true,
    velocity: true,
  });
  const enabled = Object.values(modules).filter(Boolean).length;
  return (
    <div className="flex flex-col gap-space-lg">
      <div className="grid grid-cols-1 items-start gap-space-lg xl:grid-cols-5">
        <Surface className="p-space-lg xl:col-span-2">
          <div className="mb-space-md flex items-center justify-between border-b border-outline-variant/30 pb-space-md">
            <h2 className="font-headline-sm text-headline-sm font-semibold">
              Report scope &amp; parameter selection
            </h2>
            <span className="font-code-audit text-code-audit text-on-surface-variant">
              CONFIG_PARAM
            </span>
          </div>
          <div className="flex flex-col gap-space-md">
            <label className="flex flex-col gap-space-xs font-label-sm text-label-sm font-semibold">
              Academic reporting period
              <select
                className="h-10 rounded border border-outline-variant bg-surface px-space-sm font-body-sm"
                defaultValue="fall"
              >
                <option value="fall">Fall 2026 (Statutory AY26–27)</option>
                <option value="spring">Spring 2026 (Archive)</option>
              </select>
            </label>
            <label className="flex flex-col gap-space-xs font-label-sm text-label-sm font-semibold">
              Programme governance &amp; registry
              <select
                className="h-10 rounded border border-outline-variant bg-surface px-space-sm font-body-sm"
                defaultValue="computing"
              >
                <option value="computing">
                  B.Sc. Computing &amp; Software Systems
                </option>
                <option value="ai">B.Eng. AI &amp; Data Science</option>
              </select>
            </label>
            <div>
              <p className="mb-space-xs font-label-sm text-label-sm font-semibold">
                Modular aggregate indicators
              </p>
              <div className="space-y-space-xs">
                {Object.entries({
                  academic: "Academic indicators & progression bands",
                  attendance: "Attendance & in-person telemetry",
                  vle: "VLE & digital engagement",
                  workload: "Faculty advisor caseload & capacity",
                  velocity: "Case closure-time velocity metrics",
                }).map(([key, label]) => (
                  <label
                    key={key}
                    className="flex items-center gap-space-sm rounded border border-outline-variant/30 bg-surface-container-low p-space-sm font-body-sm text-body-sm"
                  >
                    <input
                      type="checkbox"
                      checked={modules[key as keyof typeof modules]}
                      onChange={() =>
                        setModules((current) => ({
                          ...current,
                          [key]: !current[key as keyof typeof modules],
                        }))
                      }
                    />
                    {label}
                  </label>
                ))}
              </div>
            </div>
            <fieldset>
              <legend className="mb-space-xs font-label-sm text-label-sm font-semibold">
                Format &amp; packaging
              </legend>
              <div className="flex flex-col gap-space-xs">
                {[
                  ["pdf", "Executive PDF/A-3 · ISO 19005-3"],
                  ["csv", "Sanitized tabular CSV data"],
                ].map(([value, label]) => (
                  <label
                    key={value}
                    className="flex items-center gap-space-sm rounded border border-outline-variant/30 p-space-sm font-body-sm text-body-sm"
                  >
                    <input
                      type="radio"
                      name="format"
                      value={value}
                      checked={format === value}
                      onChange={() => setFormat(value)}
                    />
                    {label}
                  </label>
                ))}
              </div>
            </fieldset>
            <button
              type="button"
              onClick={() => setPrepared(true)}
              className="inline-flex items-center justify-center gap-space-xs rounded bg-primary px-space-md py-space-sm font-label-md text-label-md font-semibold text-on-primary hover:bg-primary-container"
            >
              <Icon name="build" className="text-[18px]" />{" "}
              {prepared ? "Artifact prepared" : "Prepare report artifact"}
            </button>
          </div>
        </Surface>
        <div className="flex flex-col gap-space-lg xl:col-span-3">
          <Surface className="p-space-lg">
            <div className="flex items-center justify-between">
              <h2 className="font-headline-sm text-headline-sm font-semibold">
                Executive document preview
              </h2>
              <span className="rounded bg-status-ontrack-surface px-space-sm py-space-xs font-code-audit text-code-audit text-status-ontrack-text">
                {prepared ? "Ready for download" : "Draft preview"}
              </span>
            </div>
            <div className="mt-space-md grid grid-cols-3 gap-space-sm">
              {[
                ["Academic records", "98.8%"],
                ["Campus RFID", "100.0%"],
                ["Canvas & Git", "96.9%"],
              ].map(([label, value]) => (
                <div
                  key={label}
                  className="rounded bg-surface-container-low p-space-sm"
                >
                  <p className="font-caption text-caption text-on-surface-variant">
                    {label}
                  </p>
                  <p className="mt-space-xs font-headline-md text-headline-md font-semibold text-primary">
                    {value}
                  </p>
                </div>
              ))}
            </div>
            <div className="mt-space-md rounded bg-surface-container-low p-space-md">
              <p className="font-label-sm text-label-sm font-semibold uppercase text-primary">
                Synthesized stratification sample
              </p>
              <div className="mt-space-sm space-y-space-xs">
                {[
                  ["GPA band 3.60–4.00", "84 students", "25.0% target"],
                  ["GPA band 2.00–3.59", "228 students", "70.0% target"],
                  ["GPA below 2.00", "Insufficient group size", "Suppressed"],
                ].map(([label, count, target]) => (
                  <div
                    key={label}
                    className="grid grid-cols-3 gap-space-sm border-b border-outline-variant/20 py-space-sm font-body-sm text-body-sm"
                  >
                    <span>{label}</span>
                    <span className="font-code-audit text-code-audit">
                      {count}
                    </span>
                    <span className="text-on-surface-variant">{target}</span>
                  </div>
                ))}
              </div>
            </div>
            <div className="mt-space-md flex flex-wrap items-center justify-between gap-space-sm rounded border border-outline-variant/30 px-space-md py-space-sm font-caption text-caption text-on-surface-variant">
              <span>
                {enabled} modules enabled ·{" "}
                {format === "pdf" ? "PDF/A-3" : "Sanitized CSV"}
              </span>
              <span>Audit ID: AUD-PDPD-2026-F98</span>
            </div>
          </Surface>
          <Surface className="p-space-lg">
            <div className="flex items-center justify-between">
              <h2 className="font-headline-sm text-headline-sm font-semibold">
                Export job lifecycle monitor
              </h2>
              <Icon name="sync" className="text-primary" />
            </div>
            <div className="mt-space-md space-y-space-sm">
              {[
                [
                  "JOB-EXP-20260908-01",
                  "Ready for download",
                  "Aggregate term summary",
                ],
                ["JOB-EXP-20260908-02", "Processing · 45%", "Cohort breakdown"],
                ["JOB-EXP-20260908-03", "Queued", "Caseload equity matrix"],
              ].map(([id, status, detail]) => (
                <div
                  key={id}
                  className="flex flex-wrap items-center justify-between gap-space-sm rounded bg-surface-container-low p-space-sm"
                >
                  <div>
                    <p className="font-code-audit text-code-audit text-primary">
                      {id}
                    </p>
                    <p className="font-body-sm text-body-sm">{detail}</p>
                  </div>
                  <span className="font-label-sm text-label-sm font-semibold text-secondary">
                    {status}
                  </span>
                </div>
              ))}
            </div>
          </Surface>
        </div>
      </div>
    </div>
  );
}

export function HeadWorkspace({ view }: { view: HeadView }) {
  return (
    <div className="flex flex-col gap-space-lg">
      <Header view={view} />
      <PrivacyNotice reports={view === "reports"} />
      {view === "overview" ? (
        <Overview />
      ) : view === "workload" ? (
        <Workload />
      ) : view === "assignments" ? (
        <Assignments />
      ) : (
        <Reports />
      )}
      <footer className="flex flex-wrap items-center justify-between gap-space-sm border-t border-outline-variant/30 pt-space-md font-caption text-caption text-on-surface-variant">
        <span>
          AdviseCare Programme Head Subsystem · SEC-09 governance enforced
        </span>
        <span className="font-code-audit">Audit ledger v4.18.0-rel</span>
      </footer>
    </div>
  );
}
