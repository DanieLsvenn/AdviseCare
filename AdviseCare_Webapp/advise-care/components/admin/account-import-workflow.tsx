"use client";

import { useTranslations } from "next-intl";
import { isRecord } from "@/lib/api-contract";
import {
  ImportEditor,
  postFile,
  previewShape,
  rowProblems,
  type CheckOutcome,
} from "@/components/admin/import-editor";

const ACCOUNT_COLUMNS = [
  "email",
  "fullName",
  "userName",
  "facultyCode",
  "role",
  "studentCode",
  "academicStatus",
];

/** Account import: the API's preview runs every import check, so one call both reads and checks. */
export function AccountImportWorkflow() {
  const t = useTranslations("workspace");

  const outcomeOf = (payload: unknown, raw = false): CheckOutcome => {
    const record = isRecord(payload) ? payload : {};
    const problems = rowProblems(
      record.errors,
      (item) => [typeof item.message === "string" ? item.message : t("actionFailed")],
      raw ? (rowNumber) => (rowNumber < 2 ? null : rowNumber) : undefined,
    );
    const valid = typeof record.validRowCount === "number" ? record.validRowCount : 0;
    return {
      ok: record.isValid === true,
      problems,
      summary: t("importAccountsReady", { count: valid }),
    };
  };

  return (
    <ImportEditor
      collapsible
      title={t("importAccounts")}
      description={t("accountImportDescription")}
      accept=".xlsx,.csv"
      templateColumns={ACCOUNT_COLUMNS}
      templateFileName="advisecare-accounts-template.csv"
      preview={async (file) => {
        const shape = previewShape(await postFile("Accounts/import/preview", file), t("unexpectedResponse"));
        return { ...shape, outcome: outcomeOf(shape.payload, true) };
      }}
      check={async (csv) => outcomeOf(await postFile("Accounts/import/preview", csv))}
      runImport={async (csv) => {
        const payload = await postFile("Accounts/import", csv);
        const record = isRecord(payload) ? payload : {};
        if (record.isSuccess === true) {
          const count = typeof record.createdCount === "number" ? record.createdCount : 0;
          return { ok: true, message: t("importAccountsDone", { count }) };
        }
        return {
          ok: false,
          message: t("importRefused"),
          problems: outcomeOf(payload).problems,
        };
      }}
    />
  );
}
