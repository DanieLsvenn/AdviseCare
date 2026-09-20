import { NextResponse } from "next/server";
import { correctionRequests } from "@/lib/mock";

export async function GET() {
  return NextResponse.json({ corrections: correctionRequests });
}
