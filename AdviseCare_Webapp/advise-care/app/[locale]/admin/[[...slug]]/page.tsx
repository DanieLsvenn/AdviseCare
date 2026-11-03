import { notFound } from "next/navigation";
import { WorkspacePage } from "@/components/workspace/workspace-page";
import type { WorkspaceScreen } from "@/components/workspace/workspace-definitions";

const screens: Record<string, WorkspaceScreen> = {
  m01: "admin.accounts",
  m02: "admin.masterData",
  m03: "admin.studentData",
  m04: "admin.categories",
  m05: "admin.activity",
  m06: "admin.roles",
  m07: "admin.sessions",
  m08: "admin.assignments",
};

export default async function AdminWorkspacePage({
  params,
}: {
  params: Promise<{ slug?: string[] }>;
}) {
  const { slug } = await params;
  const screen = screens[slug?.join("/") ?? "m01"];
  if (!screen) notFound();
  return <WorkspacePage screen={screen} />;
}
