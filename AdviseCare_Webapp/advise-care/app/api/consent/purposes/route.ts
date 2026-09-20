import { NextResponse } from "next/server";
import { attributeTypes, consentPurposes } from "@/lib/mock";

export async function GET() {
  return NextResponse.json({ purposes: consentPurposes, attributes: attributeTypes });
}
