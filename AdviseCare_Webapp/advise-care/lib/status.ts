import type { StatusTone } from "./types";

/**
 * One place that decides how a domain value (case stage, priority, consent
 * state, account status…) is shown. The tone carries meaning; the label always
 * travels with it (see StatusChip).
 */
const tones: Record<string, StatusTone> = {
  // Case pipeline
  pending: "attention",
  assigned: "info",
  contacted: "info",
  followup: "info",
  referred: "referral",
  closed: "sealed",
  open: "info",
  reopened: "attention",
  // Priority
  low: "sealed",
  medium: "sealed",
  high: "attention",
  urgent: "urgent",
  // Decisions and lifecycle
  accepted: "ontrack",
  returned: "sealed",
  granted: "ontrack",
  withdrawn: "sealed",
  denied: "urgent",
  approved: "ontrack",
  rejected: "urgent",
  active: "ontrack",
  inactive: "sealed",
  banned: "urgent",
  draft: "sealed",
  revoked: "sealed",
  retired: "sealed",
  expired: "sealed",
  executed: "ontrack",
  completed: "ontrack",
  finalized: "ontrack",
  inprogress: "info",
  escalated: "urgent",
  overdue: "urgent",
  flagged: "urgent",
  reviewed: "ontrack",
  destroyed: "sealed",
  // Accounts and terms
  linked: "ontrack",
  notlinked: "attention",
  emailsent: "info",
  enrolled: "ontrack",
  onleave: "attention",
  graduated: "sealed",
  archived: "sealed",
  upcoming: "info",
  current: "ontrack",
  past: "sealed",
};

/** Columns and fields whose values are workflow states rather than free text. */
export const statusFields = new Set([
  "currentStage",
  "caseCurrentStage",
  "stage",
  "fromStage",
  "toStage",
  "status",
  "priority",
  "casePriority",
  "linkStatus",
  "academicStatus",
  "purposeStatus",
  "consentStatus",
  "keyStatus",
  "decision",
  "newStatus",
  "previousStatus",
]);

/** Stage-like fields get a leading dot so a pipeline position reads at a glance. */
export const stageFields = new Set([
  "currentStage",
  "caseCurrentStage",
  "stage",
  "fromStage",
  "toStage",
]);

export function normalizeStatus(value: string): string {
  return value.replace(/[\s_-]/g, "").toLowerCase();
}

export function statusTone(value: string): StatusTone | undefined {
  return tones[normalizeStatus(value)];
}

/** Message key under the `ui.status` namespace, e.g. "followUp" → "followup". */
export function statusKey(value: string): string {
  return normalizeStatus(value);
}

/** Last-resort label for a value the dictionary does not know yet. */
export function humanize(value: string): string {
  const spaced = value
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/[_-]+/g, " ")
    .trim();
  return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
}

/** Expected states shown as quiet text, not a chip (colour only where it means something). */
export const QUIET_STATES = new Set(["active", "linked", "enrolled", "low"]);
