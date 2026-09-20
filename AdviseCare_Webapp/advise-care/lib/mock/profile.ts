import type { StatusTone } from "../types";

export interface EvidenceSource {
  kind: "USED" | "MISSING";
  label: string;
}

export interface ProfileMetric {
  label: string;
  value: string;
  unit?: string;
  caption: string;
  tone?: StatusTone;
}

export interface ProfileFlag {
  title: string;
  detail: string;
  chipLabel: string;
  tone: StatusTone;
}

export interface ProfileSection {
  id: string;
  index: number;
  icon: string;
  title: string;
  headlineChip: { label: string; tone: StatusTone; icon: string };
  metrics: ProfileMetric[];
  listTitle: string;
  flags: ProfileFlag[];
  note?: string;
  sources: EvidenceSource[];
  nodeLabel: string;
  nodeRef: string;
  evidenceHref: string;
}

/**
 * Bốn khối chứng cứ của hồ sơ sinh viên. Mỗi khối tự công bố nguồn đã dùng và
 * nguồn còn thiếu — đây là chỗ "78% Moderate" không được phép trông giống một
 * con số đã hoàn chỉnh khi nó chưa hoàn chỉnh.
 */
export const profileSections: ProfileSection[] = [
  {
    id: "academic",
    index: 1,
    icon: "auto_stories",
    title: "Academic Progress & Course Performance",
    headlineChip: { label: "2 Courses Need Review", tone: "urgent", icon: "report_problem" },
    metrics: [
      {
        label: "Cumulative GPA",
        value: "2.74",
        unit: "/ 4.00",
        caption: "Down from 3.12 (previous term)",
        tone: "attention",
      },
      {
        label: "Completed credits",
        value: "74",
        unit: "/ 120 ECTS",
        caption: "61.6% of degree requirement met",
      },
    ],
    listTitle: "Flagged modules requiring intervention",
    flags: [
      {
        title: "CS301 · Advanced Algorithms",
        detail:
          "Midterm score 42 / 100 (class median 71 / 100). Flagged for formal review.",
        chipLabel: "Grade risk",
        tone: "urgent",
      },
      {
        title: "CS304 · Database Systems",
        detail:
          "Practical coursework milestone #2 incomplete; deadline passed 3 Sep 2026.",
        chipLabel: "Assessment pending",
        tone: "attention",
      },
    ],
    sources: [
      {
        kind: "USED",
        label: "SIS Academic Records (v3.2) — synced today 08:30, 100% complete",
      },
      {
        kind: "MISSING",
        label:
          "Continuous assessment marks for CS304 — awaiting departmental grade submission",
      },
    ],
    nodeLabel: "Audit node",
    nodeRef: "SIS-VAL-4011",
    evidenceHref: "/advisor/students/1/attendance",
  },
  {
    id: "attendance",
    index: 2,
    icon: "co_present",
    title: "Attendance & In-Person Engagement",
    headlineChip: { label: "90.0% Current Standing", tone: "ontrack", icon: "verified" },
    metrics: [
      {
        label: "Attendance rate",
        value: "90.0%",
        unit: "overall",
        caption: "Above institutional threshold (80.0%)",
        tone: "ontrack",
      },
      {
        label: "Telemetry freshness",
        value: "7 Sep 2026",
        caption: "Recorded via turnstile RFID",
      },
    ],
    listTitle: "Recent in-person absences (past 3 weeks)",
    flags: [
      {
        title: "CS301 Lab session #2 · 4 Sep 2026",
        detail: "No turnstile record; no excusal filed.",
        chipLabel: "Unexcused",
        tone: "urgent",
      },
      {
        title: "CS301 Lab session #1 · 28 Aug 2026",
        detail: "No turnstile record; no excusal filed.",
        chipLabel: "Unexcused",
        tone: "urgent",
      },
    ],
    note: "Two consecutive missed labs coincide with the drop in the CS301 midterm assessment score. Correlation is displayed, not asserted as cause.",
    sources: [
      {
        kind: "USED",
        label: "Campus RFID card access & biometric readers — synced 7 Sep 2026, 09:15",
      },
      { kind: "MISSING", label: "Off-campus seminar attendance log — not integrated" },
    ],
    nodeLabel: "Reader node",
    nodeRef: "CAM-GATE-09",
    evidenceHref: "/advisor/students/1/attendance",
  },
  {
    id: "participation",
    index: 3,
    icon: "laptop_mac",
    title: "Virtual Learning Environment Participation",
    headlineChip: { label: "78% Moderate Participation", tone: "ontrack", icon: "cloud_upload" },
    metrics: [
      {
        label: "Weekly VLE frequency",
        value: "4.2",
        unit: "days / week",
        caption: "Cohort average 5.6 days / week",
      },
      {
        label: "Last active session",
        value: "3 hrs ago",
        caption: "Canvas session duration 24 minutes",
      },
    ],
    listTitle: "LMS & forum interaction metrics",
    flags: [
      {
        title: "Forum views: 12 · Forum posts: 0",
        detail: "Last submission 3 days ago.",
        chipLabel: "Read-only engagement",
        tone: "referral",
      },
    ],
    sources: [
      { kind: "USED", label: "Canvas LMS / Moodle VLE activity stream — synced 2 hrs ago" },
      {
        kind: "MISSING",
        label: "External GitHub Classroom lab repository telemetry — feed disconnected",
      },
    ],
    nodeLabel: "LMS hook",
    nodeRef: "CANVAS-PROD-REST",
    evidenceHref: "/advisor/students/1/attendance",
  },
  {
    id: "circumstance",
    index: 4,
    icon: "shield_person",
    title: "Consented Circumstances & Welfare Evidence",
    headlineChip: { label: "Active consent: CONS-001", tone: "ontrack", icon: "vpn_key" },
    metrics: [
      {
        label: "Authorisation scope",
        value: "Education-related financial support review only",
        caption: "Token CONS-001 · explicit written consent",
        tone: "ontrack",
      },
      {
        label: "Consent expiry",
        value: "15 October 2026",
        caption: "38 days remaining at time of read",
      },
    ],
    listTitle: "Disclosed attributes",
    flags: [
      {
        title: "Financial circumstances attribute",
        detail:
          "Temporary education-related support requested. Self-declaration recorded via the student portal on 2 Sep 2026; the student identified difficulty meeting semester laboratory resource costs.",
        chipLabel: "Consented value",
        tone: "ontrack",
      },
    ],
    note: "Attributes outside the purpose ‘Academic advising’ are withheld: they carry no visible value and no reveal action. This screen renders nothing at all for them.",
    sources: [
      {
        kind: "USED",
        label:
          "Student self-declaration portal — verified by Student Welfare Officer on 2 Sep 2026",
      },
      {
        kind: "MISSING",
        label: "Independent institutional hardship panel corroboration — pending review",
      },
    ],
    nodeLabel: "Verification key",
    nodeRef: "GOV-CONS-771",
    evidenceHref: "/advisor/consent-requests/new?student=1",
  },
];

export interface LedgerBlock {
  timestamp: string;
  icon: string;
  typeLabel: string;
  author: string;
  body: string;
  sealNote: string;
  blockRef: string;
}

export const profileLedgerBlocks: LedgerBlock[] = [
  {
    timestamp: "7 September 2026, 11:30 ICT",
    icon: "note",
    typeLabel: "Formal case note (CASE-001)",
    author: "Demo Advisor 01 (Academic Advisor)",
    body: "Pre-withdrawal consultation booked following the midterm academic indicator warning on CS301. Student acknowledged study workload stress and requested advice regarding a financial support application (CONS-001). Agreed to initiate structured weekly check-ins and to refer for departmental tutoring support before the withdrawal decision window closes.",
    sealNote: "Committed to ledger • SHA-256 verified • no edit or delete action permitted",
    blockRef: "#BLK-20260907-8812",
  },
  {
    timestamp: "2 September 2026, 14:00 ICT",
    icon: "shield",
    typeLabel: "Consented welfare notification",
    author: "Student Welfare Services (automated sync via CONS-001)",
    body: "Student submitted an education-related financial support request. Advising access scope granted for the Academic Advisor. Unrelated housing circumstances withheld under purpose-scoping regulations.",
    sealNote: "Committed to ledger • cryptographically anchored • institutional read scope only",
    blockRef: "#BLK-20260902-3094",
  },
  {
    timestamp: "31 August 2026, 09:15 ICT",
    icon: "smart_toy",
    typeLabel: "System case trigger",
    author: "Institutional Monitoring System",
    body: "CASE-001 initiated automatically due to cumulative course alerts (CS301, CS304) and a sudden attendance inflection detected during the term week 2 monitoring cycle. Case assigned to the primary academic advisor.",
    sealNote: "Committed to ledger • automated system record",
    blockRef: "#BLK-20260831-0112",
  },
];
