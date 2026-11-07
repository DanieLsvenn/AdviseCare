import { StudentCase } from "@/components/student/student-case";

export default async function StudentCasePage({
  params,
}: {
  params: Promise<{ caseId: string }>;
}) {
  const { caseId } = await params;
  return <StudentCase caseId={caseId} />;
}
