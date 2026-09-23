import { NextResponse } from "next/server";
import {
  administratorAccount,
  advisorAccount,
  currentTerm,
  stewardAccount,
  studentAccount,
  welfareOfficerAccount,
} from "@/lib/mock";

export async function GET() {
  return NextResponse.json({
    advisor: advisorAccount,
    steward: stewardAccount,
    administrator: administratorAccount,
    welfareOfficer: welfareOfficerAccount,
    student: studentAccount,
    term: currentTerm,
  });
}
