import { str, listOf } from "@/components/case/case-api";
import { ApiError } from "@/lib/api-contract";

/** One advisor's request to take on a case or a flag (TakeOnRequestResponse). */
export interface TakeOnRequestRecord {
  takeOnRequestId: string;
  advisorId: string;
  caseId: string | null;
  indicatorFlagId: string | null;
  studentId: string;
  advisorFullName: string;
  advisorEmail: string;
  categoryName: string;
  decidedByFullName: string;
  declineReason: string;
  indicatorName: string;
  note: string;
  studentCode: string;
  studentFullName: string;
  subjectCode: string;
  termName: string;
  advisorOpenCaseCount: number;
  createdAt: string;
  decidedAt: string | null;
  priority: string;
  severity: string;
  supportType: string;
  targetType: "case" | "flag";
  status: string;
}

/** An early-support flag (IndicatorFlagResponse): directory data and the rule, never the student's value. */
export interface FlagRecord {
  indicatorFlagId: string;
  caseId: string | null;
  myTakeOnRequestId: string | null;
  studentId: string;
  attributeName: string;
  categoryName: string;
  dismissReason: string;
  facultyName: string;
  indicatorDescription: string;
  indicatorName: string;
  myTakeOnRequestStatus: string;
  resolvedByFullName: string;
  studentCode: string;
  studentFullName: string;
  subjectCode: string;
  subjectName: string;
  termName: string;
  thresholdValue: string;
  comparisonOperator: string;
  pendingTakeOnRequestCount: number;
  flaggedAt: string;
  resolvedAt: string | null;
  supportType: string;
  priority: string;
  severity: string;
  status: string;
}

/** An unassigned student case an advisor may ask to take on (GetTakeOnCaseResponse). */
export interface TakeOnCaseRecord {
  caseId: string;
  myTakeOnRequestId: string | null;
  studentId: string;
  categoryName: string;
  facultyName: string;
  myTakeOnRequestStatus: string;
  requestReason: string;
  studentCode: string;
  studentFullName: string;
  subjectCode: string;
  subjectName: string;
  sharedAttributeNames: string[];
  openedAt: string;
  supportType: string;
  priority: string;
  severity: string;
}

const num = (value: unknown) => (typeof value === "number" ? value : 0);

export function toTakeOnRequest(record: Record<string, unknown>): TakeOnRequestRecord {
  return {
    takeOnRequestId: str(record.takeOnRequestId),
    advisorId: str(record.advisorId),
    caseId: str(record.caseId) || null,
    indicatorFlagId: str(record.indicatorFlagId) || null,
    studentId: str(record.studentId),
    advisorFullName: str(record.advisorFullName),
    advisorEmail: str(record.advisorEmail),
    categoryName: str(record.categoryName),
    decidedByFullName: str(record.decidedByFullName),
    declineReason: str(record.declineReason),
    indicatorName: str(record.indicatorName),
    note: str(record.note),
    studentCode: str(record.studentCode),
    studentFullName: str(record.studentFullName),
    subjectCode: str(record.subjectCode),
    termName: str(record.termName),
    advisorOpenCaseCount: num(record.advisorOpenCaseCount),
    createdAt: str(record.createdAt),
    decidedAt: str(record.decidedAt) || null,
    priority: str(record.priority),
    severity: str(record.severity),
    supportType: str(record.supportType),
    targetType: record.targetType === "flag" ? "flag" : "case",
    status: str(record.status),
  };
}

export function toFlag(record: Record<string, unknown>): FlagRecord {
  return {
    indicatorFlagId: str(record.indicatorFlagId),
    caseId: str(record.caseId) || null,
    myTakeOnRequestId: str(record.myTakeOnRequestId) || null,
    studentId: str(record.studentId),
    attributeName: str(record.attributeName),
    categoryName: str(record.categoryName),
    dismissReason: str(record.dismissReason),
    facultyName: str(record.facultyName),
    indicatorDescription: str(record.indicatorDescription),
    indicatorName: str(record.indicatorName),
    myTakeOnRequestStatus: str(record.myTakeOnRequestStatus),
    resolvedByFullName: str(record.resolvedByFullName),
    studentCode: str(record.studentCode),
    studentFullName: str(record.studentFullName),
    subjectCode: str(record.subjectCode),
    subjectName: str(record.subjectName),
    termName: str(record.termName),
    thresholdValue: str(record.thresholdValue),
    comparisonOperator: str(record.comparisonOperator),
    pendingTakeOnRequestCount: num(record.pendingTakeOnRequestCount),
    flaggedAt: str(record.flaggedAt),
    resolvedAt: str(record.resolvedAt) || null,
    supportType: str(record.supportType),
    priority: str(record.priority),
    severity: str(record.severity),
    status: str(record.status),
  };
}

export function toTakeOnCase(record: Record<string, unknown>): TakeOnCaseRecord {
  return {
    caseId: str(record.caseId),
    myTakeOnRequestId: str(record.myTakeOnRequestId) || null,
    studentId: str(record.studentId),
    categoryName: str(record.categoryName),
    facultyName: str(record.facultyName),
    myTakeOnRequestStatus: str(record.myTakeOnRequestStatus),
    requestReason: str(record.requestReason),
    studentCode: str(record.studentCode),
    studentFullName: str(record.studentFullName),
    subjectCode: str(record.subjectCode),
    subjectName: str(record.subjectName),
    sharedAttributeNames: Array.isArray(record.sharedAttributeNames) ? record.sharedAttributeNames.map(str) : [],
    openedAt: str(record.openedAt),
    supportType: str(record.supportType),
    priority: str(record.priority),
    severity: str(record.severity),
  };
}

export const takeOnRequests = (payload: unknown) => listOf(payload).map(toTakeOnRequest);
export const flags = (payload: unknown) => listOf(payload).map(toFlag);
export const takeOnCases = (payload: unknown) => listOf(payload).map(toTakeOnCase);

/** The API's own message for a refused request (4xx), or the given fallback. */
export function apiMessage(cause: unknown, fallback: string): string {
  return cause instanceof ApiError && cause.status < 500 && cause.message ? cause.message : fallback;
}

/** "Attendance rate < 75" from the rule's operator and threshold. */
export function ruleText(flag: Pick<FlagRecord, "attributeName" | "comparisonOperator" | "thresholdValue">): string {
  const symbols: Record<string, string> = {
    lessThan: "<",
    lessOrEqual: "≤",
    greaterThan: ">",
    greaterOrEqual: "≥",
    equal: "=",
    notEqual: "≠",
  };
  return `${flag.attributeName} ${symbols[flag.comparisonOperator] ?? flag.comparisonOperator} ${flag.thresholdValue}`;
}
