import { NextResponse } from "next/server";
import { campusEvents } from "@/lib/mock";

export async function GET(
  _request: Request,
  { params }: { params: Promise<{ eventId: string }> },
) {
  const { eventId } = await params;
  const event = campusEvents.find((item) => item.EventCode === eventId);

  if (!event) {
    return NextResponse.json({ error: "Event not found" }, { status: 404 });
  }

  return NextResponse.json({ event });
}
