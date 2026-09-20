import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { attributeTypes, consentGrants } from "@/lib/mock";

export async function GET(request: NextRequest) {
  const studentId = request.nextUrl.searchParams.get("studentId");
  const grants = studentId
    ? consentGrants.filter((grant) => grant.StudentId === Number(studentId))
    : consentGrants;

  return NextResponse.json({ grants, attributes: attributeTypes });
}
