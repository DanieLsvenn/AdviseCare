import { NextResponse } from "next/server";
import { advisorAccount, stewardAccount, studentAccount, currentTerm } from "@/lib/mock";

export async function GET() {
  return NextResponse.json({
    advisor: advisorAccount,
    steward: stewardAccount,
    student: studentAccount,
    term: currentTerm,
  });
}
