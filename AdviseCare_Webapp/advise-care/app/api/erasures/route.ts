import { NextResponse } from "next/server";
import { erasureExecutions } from "@/lib/mock";

export async function GET() {
  return NextResponse.json({ erasures: erasureExecutions });
}
