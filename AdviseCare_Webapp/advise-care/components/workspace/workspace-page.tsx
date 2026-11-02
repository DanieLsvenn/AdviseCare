import { getTranslations } from "next-intl/server";
import { ApiDownloadForm } from "@/components/workspace/api-download-form";
import {
  ApiOperationForm,
  ApiWorkspace,
  type WorkspaceField,
} from "@/components/workspace/api-workspace";
import { StudentDataImportWorkflow } from "@/components/admin/student-data-import-workflow";
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

  return (
    <div className="flex flex-col gap-space-lg">
      {screen === "student.support" ? <SupportRequestForm /> : null}
      {screen === "admin.studentData" ? <StudentDataImportWorkflow /> : null}
      {screen === "admin.accounts" ? (
        <Panel className="p-space-lg">
          <h2 className="font-headline-md text-headline-md text-on-surface">
            {t("importAccounts")}
          </h2>
          <p className="mt-space-xs font-body-sm text-body-sm text-on-surface-variant">
            {t("accountImportDescription")}
          </p>
          <ApiOperationForm
            definition={{
              label: t("import"),
              method: "POST",
              path: "Accounts/import",
              fields: [
                {
                  key: "file",
                  label: t("file"),
                  type: "file",
                  required: true,
                  accept: ".xlsx",
                },
              ],
            }}
          />
        </Panel>
      ) : null}
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
          definition={definition}
          key={`${screen}:${definition.listPath}:${index}`}
        />
      ))}
    </div>
  );
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
