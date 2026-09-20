/**
 * Kiểu dữ liệu domain của AdviseCare.
 * Bám sát ERD trong `claude/database-erd-mvp.md`: consent là một chuỗi bản ghi
 * append-only, ledger là bitemporal, và mọi chỉ số đều mang provenance.
 * Tên trường giữ nguyên PascalCase của schema để khi cắm .NET API vào thì
 * payload khớp 1-1, không cần lớp ánh xạ.
 */

/* ── core ────────────────────────────────────────────────────────────────── */

export type RoleCode =
  | "SYSTEM_ADMIN"
  | "DATA_STEWARD"
  | "ADVISOR"
  | "WELFARE_OFFICER"
  | "STUDENT"
  | "PROGRAMME_HEAD";

export type StudentStatus = "ACTIVE" | "SUSPENDED" | "GRADUATED" | "WITHDRAWN";

export interface Account {
  AccountId: number;
  FullName: string;
  Email: string;
  PreferredLanguage: "vi" | "en";
  RoleCode: RoleCode;
  RoleLabel: string;
  FacultyName: string;
  Initials: string;
}

export interface Student {
  StudentId: number;
  StudentCode: string;
  FullName: string;
  Initials: string;
  ProgrammeCode: string;
  ProgrammeName: string;
  CohortLabel: string;
  YearOfStudy: number;
  Status: StudentStatus;
  FacultyName: string;
  PhotoUrl?: string;
  EnrolmentTrack: string;
}

export interface AcademicTerm {
  TermId: number;
  Code: string;
  Label: string;
  StartDate: string;
  EndDate: string;
  IsCurrent: boolean;
}

/* ── insight: chỉ số + provenance ────────────────────────────────────────── */

export type IndicatorDimension =
  | "ACADEMIC"
  | "ATTENDANCE"
  | "PARTICIPATION"
  | "CIRCUMSTANCE";

export type IndicatorBand = "LOW" | "MEDIUM" | "HIGH" | "INSUFFICIENT_DATA";

export type StatusTone =
  | "ontrack"
  | "attention"
  | "urgent"
  | "referral"
  | "sealed";

/**
 * Một ô chỉ số như nó xuất hiện trên lưới caseload.
 * `Tone` + `Label` đi cùng nhau: Design Rule §4.1 cấm chỉ dùng màu.
 */
export interface IndicatorCell {
  Dimension: IndicatorDimension;
  Label: string;
  Detail: string;
  Tone: StatusTone;
  Icon: string;
  Band?: IndicatorBand;
  CompletenessRatio?: number;
}

export type ContributionKind = "USED" | "MISSING" | "STALE" | "DENIED_BY_CONSENT";

export interface IndicatorProvenance {
  SourceCode: string;
  SourceLabel: string;
  ContributionKind: ContributionKind;
  RecordCount: number;
  CoverageRatio: number;
  Note?: string;
}

export interface DataSourceStatus {
  Code: string;
  Label: string;
  StateLabel: string;
  Tone: StatusTone;
  Icon: string;
  SyncedAtLabel: string;
}

/* ── caseload ────────────────────────────────────────────────────────────── */

export interface CaseloadRow {
  StudentId: number;
  StudentCode: string;
  FullName: string;
  Initials: string;
  ProgrammeLabel: string;
  Flagged: boolean;
  Academic: IndicatorCell;
  Attendance: IndicatorCell;
  Participation: IndicatorCell;
  /** Chỉ hiện khi có consent. Nếu không có: không placeholder, không nút xin quyền. */
  Circumstance: IndicatorCell;
  FeedSync: IndicatorCell;
}

/* ── consent ─────────────────────────────────────────────────────────────── */

export type ConsentState = "GRANTED" | "NARROWED" | "WITHDRAWN" | "EXPIRED";

export interface ConsentPurpose {
  PurposeId: number;
  Code: string;
  Label: string;
  Description: string;
  LegalBasis: string;
  DefaultRetentionDays: number;
  Version: number;
  AttributeCodes: string[];
  GrantsActive: number;
}

export interface SensitiveAttributeType {
  AttributeTypeId: number;
  Code: string;
  Label: string;
  Category:
    | "FINANCIAL"
    | "HEALTH"
    | "FAMILY"
    | "ACADEMIC"
    | "ATTENDANCE"
    | "CONTACT";
  IsSensitive: boolean;
  IsEncryptedAtRest: boolean;
}

export interface ConsentGrant {
  ConsentGrantId: number;
  StudentId: number;
  PurposeCode: string;
  PurposeLabel: string;
  State: ConsentState;
  ScopeLabel: string;
  SharedWith: string[];
  AttributeCodes: string[];
  EffectiveFrom: string;
  ExpiresAt: string | null;
  RecordedAt: string;
  SupersededAt: string | null;
  SupersedesGrantId: number | null;
  EvidenceHash: string;
}

export interface AccessPolicy {
  PolicyId: number;
  RoleCode: RoleCode;
  RoleLabel: string;
  PurposeCode: string;
  AttributeCode: string | null;
  AttributeLabel: string;
  Effect: "PERMIT" | "DENY";
  RequiresConsent: boolean;
  RequiresApproval: boolean;
  Version: number;
}

export interface AccessLogEntry {
  AccessLogId: number;
  OccurredAt: string;
  AccountName: string;
  AccountRoleLabel: string;
  StudentCode: string;
  PurposeLabel: string;
  AttributeLabel: string;
  Decision: "PERMIT" | "DENY";
  DecisionReason: string;
  PolicyRef: string;
  ConsentGrantRef: string | null;
}

export type AccessRequestType = "EMERGENCY_ACCESS" | "CONSENT_SCOPE";
export type AccessRequestState = "PENDING" | "APPROVED" | "REJECTED" | "EXPIRED";

export interface AccessRequest {
  AccessRequestId: number;
  RequestCode: string;
  RequestType: AccessRequestType;
  RequesterName: string;
  RequesterRoleLabel: string;
  StudentId: number;
  StudentCode: string;
  StudentName: string;
  PurposeLabel: string;
  AttributeLabels: string[];
  Justification: string;
  State: AccessRequestState;
  RaisedAt: string;
  ExpiresAt: string | null;
  DecidedAt: string | null;
  DecidedBy: string | null;
  ResultingConsentGrantId: number | null;
}

export type ErasureStatus = "PENDING" | "EXECUTED" | "VERIFIED" | "FAILED";

export interface ErasureExecution {
  ErasureId: number;
  ErasureCode: string;
  StudentCode: string;
  AttributeLabel: string;
  PurposeLabel: string;
  TriggeredByGrantId: number;
  ValuesAffected: number;
  KeyRef: string;
  Status: ErasureStatus;
  RequestedAt: string;
  ExecutedAt: string | null;
  VerifiedAt: string | null;
  VerificationHash: string | null;
  DestructionEvidenceHash: string | null;
}

/* ── ledger ──────────────────────────────────────────────────────────────── */

export type CaseStateCode =
  | "TRIAGE"
  | "CONTACT"
  | "REFERRAL"
  | "FOLLOW_UP"
  | "CLOSED";

export type CasePriority = "LOW" | "NORMAL" | "HIGH" | "URGENT";

export interface AdvisingCase {
  CaseId: number;
  CaseNumber: string;
  StudentId: number;
  StudentCode: string;
  StudentName: string;
  AdvisorName: string;
  TermLabel: string;
  CurrentState: CaseStateCode;
  Priority: CasePriority;
  Title: string;
  OpenedAt: string;
  ClosedAt: string | null;
  OpenActions: number;
}

export interface CaseStateTransition {
  TransitionId: number;
  FromState: CaseStateCode | null;
  ToState: CaseStateCode;
  OccurredAt: string;
  RecordedAt: string;
  ByAccountName: string;
  Note?: string;
}

export type LedgerEntryTypeCode =
  | "NOTE"
  | "CONTACT_ATTEMPT"
  | "DECISION"
  | "REFERRAL"
  | "AGREED_ACTION"
  | "CORRECTION";

export type LedgerVisibility = "ADVISOR_ONLY" | "CARE_TEAM" | "STUDENT_VISIBLE";

export interface LedgerEntryCitation {
  CitationId: number;
  CitedObjectType:
    | "INDICATOR_VALUE"
    | "ACADEMIC_RECORD"
    | "ATTENDANCE_RECORD"
    | "SENSITIVE_ATTRIBUTE"
    | "CONSENT_GRANT"
    | "LEDGER_ENTRY";
  CitedObjectRef: string;
  Label: string;
  /** Đúng chuỗi đã hiển thị trên màn hình lúc ra quyết định. */
  DisplayedValue: string;
  AsOfValidTime: string;
  AsOfTransactionTime: string;
  Redacted?: boolean;
}

export interface LedgerEntry {
  LedgerEntryId: number;
  EntryCode: string;
  StudentId: number;
  CaseId: number | null;
  EntryType: LedgerEntryTypeCode;
  TypeLabel: string;
  Title: string;
  AuthorName: string;
  AuthorRoleLabel: string;
  BodyText: string;
  Visibility: LedgerVisibility;
  RequiredPurposeLabel: string | null;
  ValidFrom: string;
  RecordedAt: string;
  SupersededAt: string | null;
  SupersedesEntryCode: string | null;
  ContentHash: string;
  PreviousEntryHash: string;
  Citations: LedgerEntryCitation[];
  Sealed: boolean;
  LocationLabel?: string;
}

export interface AgreedAction {
  ActionId: number;
  ActionCode: string;
  Description: string;
  OwnerName: string;
  DueDate: string;
  Status: "OPEN" | "DONE" | "CANCELLED";
}

export interface SupportService {
  ServiceId: number;
  Code: string;
  Label: string;
  Category: string;
  IsExternal: boolean;
  Description: string;
}

export interface Referral {
  ReferralId: number;
  ReferralCode: string;
  ServiceLabel: string;
  CaseNumber: string;
  StudentCode: string;
  ConsentGrantRef: string;
  State: "RAISED" | "ACKNOWLEDGED" | "IN_PROGRESS" | "CLOSED" | "DECLINED";
  RaisedAt: string;
}

/* ── student self-service ────────────────────────────────────────────────── */

export interface CorrectionRequest {
  CorrectionId: number;
  CorrectionCode: string;
  StudentCode: string;
  StudentName: string;
  TargetEntryCode: string;
  TargetLabel: string;
  DisputedValue: string;
  ProposedValue: string;
  StudentStatement: string;
  State: "SUBMITTED" | "UNDER_REVIEW" | "ADDENDUM_ADDED" | "DECLINED";
  SubmittedAt: string;
  ReviewerName: string | null;
  ReviewedAt: string | null;
  ReviewerOutcome: string | null;
}

export interface CampusEvent {
  EventId: number;
  EventCode: string;
  Title: string;
  Category: string;
  StartsAt: string;
  EndsAt: string;
  LocationLabel: string;
  HostLabel: string;
  Summary: string;
  SeatsTotal: number;
  SeatsTaken: number;
  Registered: boolean;
  ConsentNote: string;
}

export interface ExportJob {
  ExportId: number;
  ExportCode: string;
  ScopeLabels: string[];
  Format: "PDF" | "JSON" | "CSV";
  RequestedAt: string;
  Status: "QUEUED" | "READY" | "EXPIRED";
  PayloadHash: string | null;
  ExpiresAt: string | null;
}

/* ── module học vụ ───────────────────────────────────────────────────────── */

export interface ModuleRecord {
  CourseCode: string;
  CourseName: string;
  FacultyLead: string;
  Score: number | null;
  MaxScore: number;
  StatusLabel: string;
  Tone: StatusTone;
  Detail: string;
  ProgressLabel: string;
}

export interface AttendanceSession {
  SessionId: number;
  SessionDate: string;
  CourseCode: string;
  SessionLabel: string;
  Status: "PRESENT" | "ABSENT" | "LATE" | "EXCUSED";
  SourceLabel: string;
  GateRef: string;
  RecordedAt: string;
  SupersededAt: string | null;
  AddendumCode?: string;
}
