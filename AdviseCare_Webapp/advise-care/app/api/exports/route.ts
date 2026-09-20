import { NextResponse } from "next/server";
import { exportJobs } from "@/lib/mock";

export async function GET() {
  return NextResponse.json({ exports: exportJobs });
}
