import { str } from "@/components/case/case-api";

/**
 * Meet-ups (MEETUPREQUESTS, MEETUPSCHEDULES). Same rules as the API
 * (MeetupRequestService): Vietnam time, Monday to Friday, inside
 * 08:30-11:30 or 13:30-18:00, 15-minute steps, at least 15 minutes.
 */
export const SLOT_MINUTES = 15;
export const TIME_WINDOWS = [
  { start: 8 * 60 + 30, end: 11 * 60 + 30 },
  { start: 13 * 60 + 30, end: 18 * 60 },
] as const;
/** The day the schedule screen draws: from the first window's start to the last window's end. */
export const DAY_START = TIME_WINDOWS[0].start;
export const DAY_END = TIME_WINDOWS[TIME_WINDOWS.length - 1].end;

export interface MeetupRequestRecord {
  meetupRequestId: string;
  caseId: string;
  advisorId: string;
  studentId: string;
  awaitingResponseFrom: string;
  proposedBy: string;
  requestedBy: string;
  advisorFullName: string;
  studentFullName: string;
  proposedByFullName: string;
  requestedByFullName: string;
  categoryName: string;
  reason: string;
  location: string;
  room: string;
  declineReason: string;
  localDate: string;
  localStartTime: string;
  localEndTime: string;
  durationMinutes: number;
  rescheduleCount: number;
  createdAt: string;
  respondedAt: string;
  status: string;
}

export interface MeetupScheduleRecord {
  meetupScheduleId: string;
  meetupRequestId: string;
  caseId: string;
  advisorId: string;
  studentId: string;
  advisorFullName: string;
  studentFullName: string;
  cancelledByFullName: string;
  categoryName: string;
  reason: string;
  location: string;
  room: string;
  cancelReason: string;
  localDate: string;
  localStartTime: string;
  localEndTime: string;
  durationMinutes: number;
  scheduledStartAt: string;
  status: string;
}

function num(value: unknown): number {
  return typeof value === "number" ? value : 0;
}

export function toMeetupRequest(record: Record<string, unknown>): MeetupRequestRecord {
  return {
    meetupRequestId: str(record.meetupRequestId),
    caseId: str(record.caseId),
    advisorId: str(record.advisorId),
    studentId: str(record.studentId),
    awaitingResponseFrom: str(record.awaitingResponseFrom),
    proposedBy: str(record.proposedBy),
    requestedBy: str(record.requestedBy),
    advisorFullName: str(record.advisorFullName),
    studentFullName: str(record.studentFullName),
    proposedByFullName: str(record.proposedByFullName),
    requestedByFullName: str(record.requestedByFullName),
    categoryName: str(record.categoryName),
    reason: str(record.reason),
    location: str(record.location),
    room: str(record.room),
    declineReason: str(record.declineReason),
    localDate: str(record.localDate),
    localStartTime: str(record.localStartTime),
    localEndTime: str(record.localEndTime),
    durationMinutes: num(record.durationMinutes),
    rescheduleCount: num(record.rescheduleCount),
    createdAt: str(record.createdAt),
    respondedAt: str(record.respondedAt),
    status: str(record.status),
  };
}

export function toMeetupSchedule(record: Record<string, unknown>): MeetupScheduleRecord {
  return {
    meetupScheduleId: str(record.meetupScheduleId),
    meetupRequestId: str(record.meetupRequestId),
    caseId: str(record.caseId),
    advisorId: str(record.advisorId),
    studentId: str(record.studentId),
    advisorFullName: str(record.advisorFullName),
    studentFullName: str(record.studentFullName),
    cancelledByFullName: str(record.cancelledByFullName),
    categoryName: str(record.categoryName),
    reason: str(record.reason),
    location: str(record.location),
    room: str(record.room),
    cancelReason: str(record.cancelReason),
    localDate: str(record.localDate),
    localStartTime: str(record.localStartTime),
    localEndTime: str(record.localEndTime),
    durationMinutes: num(record.durationMinutes),
    scheduledStartAt: str(record.scheduledStartAt),
    status: str(record.status),
  };
}

/** "14:30" -> 870 */
export function toMinutes(time: string): number {
  const [hours, minutes] = time.split(":").map(Number);
  return (hours || 0) * 60 + (minutes || 0);
}

/** 870 -> "14:30" */
export function fromMinutes(total: number): string {
  return `${String(Math.floor(total / 60)).padStart(2, "0")}:${String(total % 60).padStart(2, "0")}`;
}

/** Every bookable start time, e.g. 08:30, 08:45 … 11:15, 13:30 … 17:45. */
export function startTimeOptions(): string[] {
  const options: string[] = [];
  for (const window of TIME_WINDOWS) {
    for (let minute = window.start; minute + SLOT_MINUTES <= window.end; minute += SLOT_MINUTES) {
      options.push(fromMinutes(minute));
    }
  }
  return options;
}

/** The durations that still end inside the window of the chosen start time. */
export function durationOptions(startTime: string): number[] {
  const start = toMinutes(startTime);
  const window = TIME_WINDOWS.find((item) => start >= item.start && start < item.end);
  if (!window) return [];
  const options: number[] = [];
  for (let minutes = SLOT_MINUTES; start + minutes <= window.end; minutes += SLOT_MINUTES) options.push(minutes);
  return options;
}

/** Today's date in Vietnam (yyyy-MM-dd). */
export function vietnamToday(now = new Date()): string {
  return new Date(now.getTime() + 7 * 60 * 60 * 1000).toISOString().slice(0, 10);
}

/** yyyy-MM-dd plus some days. */
export function addDays(date: string, days: number): string {
  const value = new Date(`${date}T00:00:00Z`);
  value.setUTCDate(value.getUTCDate() + days);
  return value.toISOString().slice(0, 10);
}

/** 0 = Sunday … 6 = Saturday for a yyyy-MM-dd date. */
export function weekday(date: string): number {
  return new Date(`${date}T00:00:00Z`).getUTCDay();
}

/** The Monday of the week a date is in. */
export function mondayOf(date: string): string {
  const day = weekday(date);
  return addDays(date, day === 0 ? -6 : 1 - day);
}

/** 2026-12-01 -> 01/12/2026 */
export function formatLocalDate(date: string): string {
  const [year, month, day] = date.split("-");
  return year && month && day ? `${day}/${month}/${year}` : date;
}

export function formatDuration(minutes: number): string {
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;
  return hours ? (rest ? `${hours} h ${rest} min` : `${hours} h`) : `${rest} min`;
}
