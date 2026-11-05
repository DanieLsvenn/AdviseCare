import { getTranslations } from "next-intl/server";
import { ApiDownloadForm } from "@/components/workspace/api-download-form";
import {
  ApiWorkspace,
  type WorkspaceField,
} from "@/components/workspace/api-workspace";
import { StudentDataImportWorkflow } from "@/components/admin/student-data-import-workflow";
import { AccountImportWorkflow } from "@/components/admin/account-import-workflow";
import { WorkspaceTabs } from "@/components/workspace/workspace-tabs";
import { SupportRequestForm } from "@/components/student/support-request-form";
import { PageHeader } from "@/components/ui/page-header";
import { Panel } from "@/components/ui/panel";
import {
  getWorkspaceDefinitions,
  type WorkspaceScreen,
} from "@/components/workspace/workspace-definitions";

export async function WorkspacePage({
  screen,
  pathValues,
}: {
  screen: WorkspaceScreen;
  pathValues?: Record<string, unknown>;
}) {
  const t = await getTranslations("workspace");
  const definitions = getWorkspaceDefinitions(screen, t).map((definition) => {
    if (screen === "student.consent" && pathValues?.consentId) {
      return {
        ...definition,
        listPath: "Consents/me",
        create: undefined,
        recordFilter: {
          key: "consentId",
          value: String(pathValues.consentId),
        },
        pathValues,
      };
    }
    return { ...definition, pathValues };
  });

  // Consent history and the requests received sit behind tabs, so the second
  // list does not wait below the whole first one.
  if (screen === "steward.consents") {
    const tabLabels = [t("consentHistoryTab"), t("receivedRequestsTab")];
    return (
      <WorkspaceTabs
        title={definitions[0]?.title ?? t("consent")}
        tabs={definitions.map((definition, index) => ({
          label: tabLabels[index] ?? definition.title,
          definition,
        }))}
      />
    );
  }

  return (
    <div className="flex flex-col gap-space-lg">
      {screen === "student.support" ? <SupportRequestForm /> : null}
      {screen === "student.export" ? (
        <ApiDownloadForm
          title={t("export")}
          path="StudentData/me/export"
          fileName="advisecare-own-record.json"
        />
      ) : null}
      {screen === "steward.accessRecords" ? (
        <ApiDownloadForm
          title={t("export")}
          path="AccessLogs/processing-record/export"
          fileName="advisecare-processing-record"
          extensionFromQuery={{
            queryKey: "format",
            extensions: { csv: "csv" },
            fallback: "xlsx",
          }}
          fields={[
            textField(t, "studentId"),
            textField(t, "fromDate", "date"),
            textField(t, "toDate", "date"),
            {
              key: "format",
              label: t("format"),
              type: "select",
              required: true,
              options: [
                { label: t("csv"), value: "csv" },
                { label: t("xlsx"), value: "xlsx" },
              ],
            },
          ]}
        />
      ) : null}
      {screen === "head.reports" ? (
        <>
          <ApiDownloadForm
            title={t("caseReports")}
            path="Reports/cases/export"
            fileName="advisecare-cases"
            extensionFromQuery={{
              queryKey: "format",
              extensions: { pdf: "pdf" },
              fallback: "xlsx",
            }}
            fields={reportFields(t)}
          />
          <ApiDownloadForm
            title={t("advisorWorkload")}
            path="Reports/caseload-closure/export"
            fileName="advisecare-caseload-closure"
            extensionFromQuery={{
              queryKey: "format",
              extensions: { pdf: "pdf" },
              fallback: "xlsx",
            }}
            fields={reportFields(t)}
          />
        </>
      ) : null}
      {definitions.map((definition, index) => (
        <ApiWorkspace
          definition={index > 0 ? { ...definition, embedded: true } : definition}
          key={`${screen}:${definition.listPath}:${index}`}
          beforeList={index === 0 ? beforeList(screen) : undefined}
        />
      ))}
    </div>
  );
}

/** The import of a list's records sits between the list's title and the list. */
function beforeList(screen: WorkspaceScreen) {
  if (screen === "admin.accounts") return <AccountImportWorkflow />;
  if (screen === "admin.studentData") return <StudentDataImportWorkflow />;
  return undefined;
}

export async function UnavailableFeaturePage() {
  const t = await getTranslations("workspace");
  return (
    <div className="flex flex-col gap-space-lg">
      <PageHeader
        title={t("statistics")}
        description={t("statisticsUnavailable")}
      />
      <Panel className="p-space-lg">
        <p className="font-body-md text-body-md text-on-surface-variant">
          {t("noBackendFeature")}
        </p>
      </Panel>
    </div>
  );
}

function textField(
  t: Awaited<ReturnType<typeof getTranslations<"workspace">>>,
  key: "studentId" | "fromDate" | "toDate",
  type: WorkspaceField["type"] = "text",
): WorkspaceField {
  return { key, label: t(key), type };
}

function reportFields(
  t: Awaited<ReturnType<typeof getTranslations<"workspace">>>,
): WorkspaceField[] {
  return [
    { key: "from", label: t("from"), type: "date", required: true },
    { key: "to", label: t("to"), type: "date", required: true },
    {
      key: "format",
      label: t("format"),
      type: "select",
      required: true,
      options: [
        { label: t("xlsx"), value: "xlsx" },
        { label: t("pdf"), value: "pdf" },
      ],
    },
  ];
}
