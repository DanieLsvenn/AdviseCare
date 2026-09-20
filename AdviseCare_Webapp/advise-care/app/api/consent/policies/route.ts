import { NextResponse } from "next/server";
import { accessPolicies, consentPurposes } from "@/lib/mock";

export async function GET() {
  return NextResponse.json({ policies: accessPolicies, purposes: consentPurposes });
}
