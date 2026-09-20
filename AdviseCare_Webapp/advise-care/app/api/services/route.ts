import { NextResponse } from "next/server";
import { referrals, supportServices } from "@/lib/mock";

export async function GET() {
  return NextResponse.json({ services: supportServices, referrals });
}
