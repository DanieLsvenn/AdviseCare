import { notFound } from "next/navigation";
import { OutreachForm } from "@/components/advisor/outreach-form";
import { AcademicResultsView } from "@/components/academic/academic-results";
import { getCurrentUser } from "@/lib/auth";
import { WorkspacePage } from "@/components/workspace/workspace-page";
import type { WorkspaceScreen } from "@/components/workspace/workspace-definitions";

export default async function AdvisorWorkspacePage({
  params,
  searchParams,
}: {
  params: Promise<{ slug?: string[] }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const { slug = [] } = await params;
  // Opened from a case page (?caseId=...): the student's data is read for that case, so the case is fixed and not asked for
  const { caseId } = await searchParams;
  const fromCase = typeof caseId === "string" && caseId ? { caseId } : {};
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
    // The student's academic results (only the consented part) above the data attribute values
    return (
      <div className="flex flex-col gap-space-lg">
        <AcademicResultsView audience="staff" studentId={slug[1]} caseId={typeof caseId === "string" && caseId ? caseId : undefined} />
        <WorkspacePage pathValues={{ studentId: slug[1], ...fromCase }} screen="advisor.studentData" />
      </div>
    );
  } else if (/^students\/[^/]+\/historical(\/erased)?$/.test(route)) {
    screen = "advisor.historical";
    pathValues = { studentId: slug[1], ...fromCase };
  } else if (/^students\/[^/]+\/compare$/.test(route)) {
    screen = "advisor.compare";
    pathValues = { studentId: slug[1], ...fromCase };
  } else if (/^cases\/[^/]+\/history$/.test(route)) {
    screen = "advisor.caseHistory";
    pathValues = { caseId: slug[1] };
  } else {
    notFound();
  }

  return <WorkspacePage pathValues={pathValues} screen={screen} />;
}
