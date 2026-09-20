import { NextResponse } from "next/server";
import {
  attendanceProvenance,
  attendanceSessions,
  consentGrants,
  moduleRecords,
  profileLedgerBlocks,
  profileSections,
  students,
} from "@/lib/mock";

export async function GET(
  _request: Request,
  { params }: { params: Promise<{ studentId: string }> },
) {
  const { studentId } = await params;
  const id = Number(studentId);
  const student = students.find((item) => item.StudentId === id);

  if (!student) {
    return NextResponse.json({ error: "Student not found" }, { status: 404 });
  }

  return NextResponse.json({
    student,
    modules: moduleRecords[id] ?? [],
    attendance: attendanceSessions[id] ?? [],
    attendanceProvenance,
    grants: consentGrants.filter((grant) => grant.StudentId === id),
    sections: id === 1 ? profileSections : [],
    ledgerBlocks: id === 1 ? profileLedgerBlocks : [],
  });
}
