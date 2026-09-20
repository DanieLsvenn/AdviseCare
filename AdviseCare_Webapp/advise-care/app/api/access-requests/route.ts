import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { accessRequests } from "@/lib/mock";

export async function GET(request: NextRequest) {
  const type = request.nextUrl.searchParams.get("type");
  const requests = type
    ? accessRequests.filter((item) => item.RequestType === type)
    : accessRequests;

  return NextResponse.json({ requests });
}
