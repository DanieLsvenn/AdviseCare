import { NextResponse } from "next/server";
import { students } from "@/lib/mock";

export async function GET() {
  return NextResponse.json({ students });
}
