import { AdvisorCase } from "@/components/advisor/advisor-case";
import { getCurrentUser } from "@/lib/auth";

export default async function AdvisorCasePage({
  params,
}: {
  params: Promise<{ caseId: string }>;
}) {
  const { caseId } = await params;
  // The case's current advisor can write messages and read the shared data; other staff on the case only read
  const user = await getCurrentUser();
  return <AdvisorCase caseId={caseId} viewerId={user.accountId} />;
}
