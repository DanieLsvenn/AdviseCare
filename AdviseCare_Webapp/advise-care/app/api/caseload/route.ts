import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { caseloadRows, currentTerm, dataSources } from "@/lib/mock";

export async function GET(request: NextRequest) {
  const query = request.nextUrl.searchParams.get("q")?.toLowerCase().trim();
  const rows = query
    ? caseloadRows.filter(
        (row) =>
          row.FullName.toLowerCase().includes(query) ||
          row.StudentCode.toLowerCase().includes(query) ||
          row.ProgrammeLabel.toLowerCase().includes(query),
      )
    : caseloadRows;

  return NextResponse.json({
    term: currentTerm,
    sources: dataSources,
    totalAssigned: 150,
    rows,
  });
}
