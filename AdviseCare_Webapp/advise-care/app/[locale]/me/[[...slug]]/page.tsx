import { notFound } from "next/navigation";
import { StudentCaseList } from "@/components/student/student-case-list";
import { WorkspacePage } from "@/components/workspace/workspace-page";
import type { WorkspaceScreen } from "@/components/workspace/workspace-definitions";

export default async function StudentWorkspacePage({
  params,
}: {
  params: Promise<{ slug?: string[] }>;
}) {
  const { slug = [] } = await params;
  const route = slug.join("/");
  let screen: WorkspaceScreen;
  let pathValues: Record<string, unknown> | undefined;

  if (route === "" || route === "profile") {
    screen = "student.profile";
  } else if (route === "cases") {
    return <StudentCaseList />;
  } else if (route === "support-request") {
    screen = "student.support";
  } else if (route === "consent") {
    screen = "student.consent";
  } else if (/^consent\/([^/]+)(?:\/withdraw)?$/.test(route)) {
    screen = "student.consent";
    pathValues = { consentId: slug[1] };
  } else if (route === "consent/requests") {
    screen = "student.consentRequests";
  } else if (
    route === "corrections" ||
    route === "corrections/new"
  ) {
    screen = "student.corrections";
  } else if (route === "access-history") {
    screen = "student.access";
  } else if (route === "events" || /^events\/[^/]+$/.test(route)) {
    screen = "student.events";
  } else if (route === "export") {
    screen = "student.export";
  } else {
    notFound();
  }

  return <WorkspacePage pathValues={pathValues} screen={screen} />;
}
