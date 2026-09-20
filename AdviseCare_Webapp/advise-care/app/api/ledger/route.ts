import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { ledgerEntries } from "@/lib/mock";

export async function GET(request: NextRequest) {
  const studentId = request.nextUrl.searchParams.get("studentId");
  const asOf = request.nextUrl.searchParams.get("asOf");

  let entries = studentId
    ? ledgerEntries.filter((entry) => entry.StudentId === Number(studentId))
    : ledgerEntries;

  /**
   * Bộ lọc transaction-time: tái dựng đúng màn hình tại thời điểm `asOf`,
   * không phải trạng thái hiện tại (xem `ledger.fn_LedgerAsOf` trong ERD).
   */
  if (asOf) {
    entries = entries.filter((entry) => entry.RecordedAt <= asOf);
  }

  return NextResponse.json({
    entries: [...entries].sort((a, b) => (a.RecordedAt < b.RecordedAt ? 1 : -1)),
  });
}
