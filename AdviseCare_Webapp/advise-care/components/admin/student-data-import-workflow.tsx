"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { isRecord } from "@/lib/api-contract";
import { controlClass } from "@/components/ui/field";
import { cn } from "@/lib/cn";
import {
  ImportEditor,
  postFile,
  previewShape,
  rowProblems,
} from "@/components/admin/import-editor";

const mappingKeys = [
  "studentIdentifier",
  "dataAttribute",
  "value",
  "term",
  "validFrom",
] as const;

type MappingKey = (typeof mappingKeys)[number];
type ColumnMapping = Record<MappingKey, string>;

const emptyMapping: ColumnMapping = {
  studentIdentifier: "",
  dataAttribute: "",
  value: "",
  term: "",
  validFrom: "",
};

/* Common header spellings, so a usual file maps itself. */
const guesses: Record<MappingKey, string[]> = {
  studentIdentifier: ["studentidentifier", "studentcode", "student", "email", "username"],
  dataAttribute: ["dataattribute", "attribute", "attributename", "indicator"],
  value: ["value", "giatri"],
  term: ["term", "termname", "semester", "hocky"],
  validFrom: ["validfrom", "date", "effectivedate", "ngay"],
};

const messagesOf = (item: Record<string, unknown>) =>
  Array.isArray(item.messages) ? item.messages.filter((m): m is string => typeof m === "string") : [];

/**
 * Student data import: upload, map the five fields to file columns, fix rows
 * in the editable preview, check, import. The API checks rows only with a
 * mapping, so checking waits until every field is mapped.
 */
export function StudentDataImportWorkflow() {
  const t = useTranslations("workspace");
  const [mapping, setMapping] = useState<ColumnMapping>(emptyMapping);
  const mappingComplete = mappingKeys.every((key) => mapping[key] !== "");

  return (
    <ImportEditor
      title={t("studentDataImport")}
      description={t("importWorkflowDescription")}
      accept=".xlsx,.csv"
      templateColumns={["studentCode", "dataAttribute", "value", "term", "validFrom"]}
      templateFileName="advisecare-student-data-template.csv"
      canCheck={mappingComplete}
      preview={async (file) => {
        const shape = previewShape(await postFile("StudentData/import/preview", file), t("unexpectedResponse"));
        setMapping(
          Object.fromEntries(
            mappingKeys.map((key) => [
              key,
              shape.headers.find((header) => guesses[key].includes(header.toLowerCase().replace(/[\s_-]/g, ""))) ?? "",
            ]),
          ) as ColumnMapping,
        );
        return shape;
      }}
      check={async (csv) => {
        const payload = await postFile("StudentData/import/validate", csv, {
          columnMapping: JSON.stringify(mapping),
        });
        const record = isRecord(payload) ? payload : {};
        const valid = typeof record.validRowCount === "number" ? record.validRowCount : 0;
        return {
          ok: record.isValid === true && valid > 0,
          problems: rowProblems(record.errors, messagesOf),
          warnings: rowProblems(record.rejectedRows, messagesOf),
          summary: t("importStudentDataReady", { count: valid }),
        };
      }}
      runImport={async (csv) => {
        const payload = await postFile("StudentData/import", csv, {
          columnMapping: JSON.stringify(mapping),
        });
        const record = isRecord(payload) ? payload : {};
        if (record.isSuccess === true) {
          return {
            ok: true,
            message: t("importStudentDataDone", {
              inserted: typeof record.insertedCount === "number" ? record.insertedCount : 0,
              missing: typeof record.missingCount === "number" ? record.missingCount : 0,
            }),
          };
        }
        return {
          ok: false,
          message: t("importRefused"),
          problems: rowProblems(record.errors, messagesOf),
        };
      }}
      beforeTable={(headers, invalidate) => (
        <section aria-labelledby="student-import-mapping" className="rounded-lg border border-rule p-space-md">
          <h3 id="student-import-mapping" className="text-sm font-semibold text-text">
            {t("columnMapping")}
          </h3>
          <p className="mb-space-md mt-1 text-[0.8125rem] text-text-faint">{t("mappingDescription")}</p>
          <div className="grid gap-space-md sm:grid-cols-3 lg:grid-cols-5">
            {mappingKeys.map((key) => (
              <label className="block" key={key}>
                <span className="mb-1 block text-[0.8125rem] font-semibold text-text">{t(key)}</span>
                <select
                  className={cn(controlClass, "h-9 w-full pr-8")}
                  onChange={(event) => {
                    setMapping((current) => ({ ...current, [key]: event.target.value }));
                    invalidate();
                  }}
                  required
                  value={mapping[key]}
                >
                  <option value="">—</option>
                  {headers.map((header) => (
                    <option key={header} value={header}>
                      {header}
                    </option>
                  ))}
                </select>
              </label>
            ))}
          </div>
        </section>
      )}
    />
  );
}
