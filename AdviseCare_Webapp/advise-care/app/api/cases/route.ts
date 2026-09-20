import { NextResponse } from "next/server";
import { advisingCases } from "@/lib/mock";

export async function GET() {
  return NextResponse.json({ cases: advisingCases });
}
