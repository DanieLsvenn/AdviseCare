import { notFound } from "next/navigation";
import {
  UnavailableFeaturePage,
  WorkspacePage,
} from "@/components/workspace/workspace-page";
import type { WorkspaceScreen } from "@/components/workspace/workspace-definitions";

export default async function WelfareWorkspacePage({
  params,
}: {
  params: Promise<{ slug?: string[] }>;
}) {
  const { slug = [] } = await params;
  const route = slug.join("/");

  if (route === "statistics") return <UnavailableFeaturePage />;

  if (route === "" || route === "cases" || route === "cases/intervention") {
    return <WorkspacePage screen="welfare.interventions" />;
  }
  if (route === "referrals") {
    return <WorkspacePage screen="welfare.referrals" />;
  }
  if (route === "escalations") {
    return <WorkspacePage screen="welfare.escalations" />;
  }
  if (route === "external-services") {
    return <WorkspacePage screen="welfare.externalServices" />;
  }
  if (route === "external-referrals") {
    return <WorkspacePage screen="welfare.externalReferrals" />;
  }
  const caseMatch = /^cases\/([^/]+)(?:\/(follow-up|intervention))?$/.exec(route);
  if (caseMatch) {
    const screen: WorkspaceScreen =
      caseMatch[2] === "follow-up"
        ? "welfare.externalReferrals"
        : caseMatch[2] === "intervention"
          ? "welfare.interventions"
          : "welfare.caseDetail";
    return (
      <WorkspacePage
        pathValues={{ caseId: caseMatch[1] }}
        screen={screen}
      />
    );
  }
  notFound();
}
