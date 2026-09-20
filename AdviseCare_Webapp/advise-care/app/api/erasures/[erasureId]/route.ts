import { NextResponse } from "next/server";
import { erasureExecutions } from "@/lib/mock";

export async function GET(
  _request: Request,
  { params }: { params: Promise<{ erasureId: string }> },
) {
  const { erasureId } = await params;
  const erasure = erasureExecutions.find((item) => item.ErasureCode === erasureId);

  if (!erasure) {
    return NextResponse.json({ error: "Erasure not found" }, { status: 404 });
  }

  return NextResponse.json({ erasure });
}
