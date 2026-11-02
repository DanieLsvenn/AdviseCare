import { notFound } from "next/navigation";
import { WorkspacePage } from "@/components/workspace/workspace-page";
import type { WorkspaceScreen } from "@/components/workspace/workspace-definitions";

const screens: Record<string, WorkspaceScreen> = {
  purposes: "steward.purposes",
  policies: "steward.policies",
  attributes: "steward.attributes",
  consents: "steward.consents",
  "access-records": "steward.accessRecords",
  erasure: "steward.erasure",
  "emergency-reviews": "steward.emergency",
  keys: "steward.keys",
};

export default async function StewardWorkspacePage({
  params,
}: {
  params: Promise<{ slug?: string[] }>;
}) {
  const { slug } = await params;
  const route = slug?.join("/") ?? "";
  const baseRoute = route.startsWith("erasure/") ? "erasure" : route;
  const screen = screens[baseRoute || "purposes"];
  if (!screen) notFound();
  return <WorkspacePage screen={screen} />;
}
