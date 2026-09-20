import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { accessLog } from "@/lib/mock";

export async function GET(request: NextRequest) {
  const studentCode = request.nextUrl.searchParams.get("studentCode");
  const entries = studentCode
    ? accessLog.filter((entry) => entry.StudentCode === studentCode)
    : accessLog;

  return NextResponse.json({ entries });
}
