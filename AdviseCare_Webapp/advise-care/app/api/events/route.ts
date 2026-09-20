import { NextResponse } from "next/server";
import { campusEvents } from "@/lib/mock";

export async function GET() {
  return NextResponse.json({ events: campusEvents });
}
