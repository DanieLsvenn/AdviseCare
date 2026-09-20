import { NextResponse } from "next/server";
import { correctionRequests, ledgerEntries } from "@/lib/mock";

export async function GET(
  _request: Request,
  { params }: { params: Promise<{ correctionId: string }> },
) {
  const { correctionId } = await params;
  const correction = correctionRequests.find(
    (item) => item.CorrectionCode === correctionId,
  );

  if (!correction) {
    return NextResponse.json({ error: "Correction not found" }, { status: 404 });
  }

  return NextResponse.json({
    correction,
    addendum:
      ledgerEntries.find(
        (entry) => entry.SupersedesEntryCode === correction.TargetEntryCode,
      ) ?? null,
  });
}
