import type { ReactNode } from "react";
import { GlobalWorkspaceShell } from "@/components/layout/global-workspace-shell";

export const dynamic = "force-dynamic";

export default async function GlobalWorkspaceLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  return (
    <GlobalWorkspaceShell locale={locale}>
      {children}
    </GlobalWorkspaceShell>
  );
}
