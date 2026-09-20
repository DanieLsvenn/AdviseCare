import { NextResponse } from "next/server";
import {
  advisingCases,
  agreedActions,
  caseTransitions,
  ledgerEntries,
  referrals,
} from "@/lib/mock";

export async function GET(
  _request: Request,
  { params }: { params: Promise<{ caseId: string }> },
) {
  const { caseId } = await params;
  const id = Number(caseId);
  const advisingCase = advisingCases.find((item) => item.CaseId === id);

  if (!advisingCase) {
    return NextResponse.json({ error: "Case not found" }, { status: 404 });
  }

  return NextResponse.json({
    case: advisingCase,
    transitions: caseTransitions[id] ?? [],
    entries: ledgerEntries.filter((entry) => entry.CaseId === id),
    actions: id === 1 ? agreedActions : [],
    referrals: referrals.filter((item) => item.CaseNumber === advisingCase.CaseNumber),
  });
}
