import { notFound } from "next/navigation";
import { MatchingQueue } from "@/components/head/matching-queue";
import { WorkspacePage } from "@/components/workspace/workspace-page";
import type { WorkspaceScreen } from "@/components/workspace/workspace-definitions";

const screens: Record<string, WorkspaceScreen> = {
  "": "head.matching",
  matching: "head.matching",
  "advisor-workload": "head.workload",
  "assignment-changes": "head.changeRequests",
  escalations: "head.escalations",
  "programme-overview": "head.cases",
  cases: "head.cases",
  reports: "head.reports",
};

export default async function HeadWorkspacePage({
  params,
}: {
  params: Promise<{ slug?: string[] }>;
}) {
  const { slug } = await params;
  const route = slug?.join("/") ?? "";
  if (route === "" || route === "matching") return <MatchingQueue />;
  const screen = screens[route];
  if (!screen) notFound();
  return <WorkspacePage screen={screen} />;
}
