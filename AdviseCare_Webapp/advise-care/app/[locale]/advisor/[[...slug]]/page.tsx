import { notFound } from "next/navigation";
import { OutreachForm } from "@/components/advisor/outreach-form";
import { getCurrentUser } from "@/lib/auth";
import { WorkspacePage } from "@/components/workspace/workspace-page";
import type { WorkspaceScreen } from "@/components/workspace/workspace-definitions";

export default async function AdvisorWorkspacePage({
  params,
}: {
  params: Promise<{ slug?: string[] }>;
}) {
  const { slug = [] } = await params;
  const route = slug.join("/");
  let screen: WorkspaceScreen;
  let pathValues: Record<string, unknown> | undefined;

  if (route === "outreach") {
    const { roles } = await getCurrentUser();
    const allowedTypes = [
      ...(roles.includes("Academic Advisor") ? (["academic"] as const) : []),
      ...(roles.includes("Wellbeing Advisor") ? (["wellbeing"] as const) : []),
    ];
    return <OutreachForm allowedTypes={allowedTypes} />;
  }

  if (route === "" || route === "caseload" || route === "cases") {
    screen = "advisor.caseload";
  } else if (route === "consent-requests" || route === "consent-requests/new") {
    screen = "advisor.consentRequests";
  } else if (route === "corrections" || /^corrections\/[^/]+$/.test(route)) {
    screen = "advisor.corrections";
  } else if (
    route === "emergency-access" ||
    route === "emergency-access/active" ||
    route === "emergency-access/request"
  ) {
    screen = "advisor.emergency";
  } else if (route === "referrals" || route === "referrals/new") {
    screen = "advisor.referrals";
  } else if (/^cases\/[^/]+$/.test(route) || /^cases\/[^/]+\/notes\/(new|review)$/.test(route)) {
    screen = "advisor.caseDetail";
    pathValues = { caseId: slug[1] };
  } else if (/^students\/[^/]+(\/current|\/attendance)?$/.test(route)) {
    screen = "advisor.studentData";
    pathValues = { studentId: slug[1] };
  } else if (/^students\/[^/]+\/historical(\/erased)?$/.test(route)) {
    screen = "advisor.historical";
    pathValues = { studentId: slug[1] };
  } else if (/^students\/[^/]+\/compare$/.test(route)) {
    screen = "advisor.compare";
    pathValues = { studentId: slug[1] };
  } else if (/^cases\/[^/]+\/history$/.test(route)) {
    screen = "advisor.caseHistory";
    pathValues = { caseId: slug[1] };
  } else {
    notFound();
  }

  return <WorkspacePage pathValues={pathValues} screen={screen} />;
}
