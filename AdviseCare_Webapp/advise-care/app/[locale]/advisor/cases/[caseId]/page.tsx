import { AdvisorCase } from "@/components/advisor/advisor-case";

export default async function AdvisorCasePage({
  params,
}: {
  params: Promise<{ caseId: string }>;
}) {
  const { caseId } = await params;
  return <AdvisorCase caseId={caseId} />;
}
