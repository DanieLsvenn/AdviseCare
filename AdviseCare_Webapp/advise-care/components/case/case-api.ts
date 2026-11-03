import { isRecord } from "@/lib/api-contract";

/** Fields of CaseResponse (AdviseCare.Application/Contracts/Responses/Case). */
export interface CaseRecord {
  caseId: string;
  studentId: string;
  studentName: string;
  studentCode: string;
  studentEmail: string;
  advisorId: string | null;
  advisorName: string;
  advisorEmail: string;
  openedByName: string;
  subjectCode: string;
  subjectName: string;
  supportCategoryId: string;
  categoryName: string;
  supportType: string;
  requestReason: string;
  openedAt: string;
  closedAt: string | null;
  currentStage: string;
  priority: string;
  status: string;
}

export interface StageHistoryRecord {
  caseStageHistoryId: string;
  changedByName: string;
  reason: string;
  changedAt: string;
  fromStage: string;
  toStage: string;
}

export interface CaseNoteRecord {
  caseNoteId: string;
  authorId: string;
  authorName: string;
  noteText: string;
  createdAt: string;
  editedAt: string | null;
  isFinalized: boolean;
}

export function str(value: unknown): string {
  return typeof value === "string" ? value : typeof value === "number" ? String(value) : "";
}

/** The API may wrap a record ({ data: … }) or return it bare. */
export function unwrap(payload: unknown): Record<string, unknown> | null {
  if (!isRecord(payload)) return null;
  for (const key of ["data", "result", "value"]) {
    if (isRecord(payload[key])) return payload[key] as Record<string, unknown>;
  }
  return payload;
}

export function listOf(payload: unknown): Record<string, unknown>[] {
  if (Array.isArray(payload)) return payload.filter(isRecord);
  if (!isRecord(payload)) return [];
  for (const key of ["items", "data", "results", "records", "value"]) {
    if (Array.isArray(payload[key])) return (payload[key] as unknown[]).filter(isRecord);
  }
  return [];
}

export function toCase(record: Record<string, unknown>): CaseRecord {
  return {
    caseId: str(record.caseId),
    studentId: str(record.studentId),
    studentName: str(record.studentName),
    studentCode: str(record.studentCode),
    studentEmail: str(record.studentEmail),
    advisorId: str(record.advisorId) || null,
    advisorName: str(record.advisorName),
    advisorEmail: str(record.advisorEmail),
    openedByName: str(record.openedByName),
    subjectCode: str(record.subjectCode),
    subjectName: str(record.subjectName),
    supportCategoryId: str(record.supportCategoryId),
    categoryName: str(record.categoryName),
    supportType: str(record.supportType),
    requestReason: str(record.requestReason),
    openedAt: str(record.openedAt),
    closedAt: str(record.closedAt) || null,
    currentStage: str(record.currentStage),
    priority: str(record.priority),
    status: str(record.status),
  };
}

export function toHistory(record: Record<string, unknown>): StageHistoryRecord {
  return {
    caseStageHistoryId: str(record.caseStageHistoryId),
    changedByName: str(record.changedByName),
    reason: str(record.reason),
    changedAt: str(record.changedAt),
    fromStage: str(record.fromStage),
    toStage: str(record.toStage),
  };
}

export function toNote(record: Record<string, unknown>): CaseNoteRecord {
  return {
    caseNoteId: str(record.caseNoteId),
    authorId: str(record.authorId),
    authorName: str(record.authorName),
    noteText: str(record.noteText),
    createdAt: str(record.createdAt),
    editedAt: str(record.editedAt) || null,
    isFinalized: record.isFinalized === true,
  };
}

/** Oldest first, as a contact book is written. */
export function byDate<T>(items: T[], at: (item: T) => string): T[] {
  return [...items].sort((a, b) => at(a).localeCompare(at(b)));
}
