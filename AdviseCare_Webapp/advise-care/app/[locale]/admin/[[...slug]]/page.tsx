import { notFound } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { TranscriptImportWorkflow } from "@/components/admin/transcript-import-workflow";
import { PageHeader } from "@/components/ui/page-header";
import { WorkspacePage } from "@/components/workspace/workspace-page";
import type { WorkspaceScreen } from "@/components/workspace/workspace-definitions";

const screens: Record<string, WorkspaceScreen> = {
  m01: "admin.accounts",
  m02: "admin.masterData",
  m03: "admin.studentData",
  m04: "admin.categories",
  m05: "admin.activity",
  m07: "admin.sessions",
  m08: "admin.assignments",
};

export default async function AdminWorkspacePage({
  params,
}: {
  params: Promise<{ slug?: string[] }>;
}) {
  const { slug } = await params;
  // Academic results import has its own page (batch upload of transcript PDFs), not a workspace list
  if (slug?.join("/") === "m09") {
    const t = await getTranslations("nav");
    return (
      <div className="flex flex-col gap-space-lg">
        <PageHeader title={t("administrator.academicResults")} />
        <TranscriptImportWorkflow />
      </div>
    );
  }
  const screen = screens[slug?.join("/") ?? "m01"];
  if (!screen) notFound();
  return <WorkspacePage screen={screen} />;
}
