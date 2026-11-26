"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { isRecord } from "@/lib/api-contract";
import { cn } from "@/lib/cn";
import { controlClass } from "@/components/ui/field";
import { Icon } from "@/components/ui/icon";
import {
  ImportEditor,
  postFile,
  previewShape,
  rowProblems,
  type ImportRow,
} from "@/components/admin/import-editor";

/** The fields the API reads from a one-row-per-value file (the columnMapping keys). */
const mappingKeys = ["studentIdentifier", "dataAttribute", "value", "term", "validFrom", "subject"] as const;

/** The subject column is optional: only rows of per-subject data attributes (e.g. Attendance rate) need a subject code. */
const optionalKeys: ReadonlySet<MappingKey> = new Set(["subject"]);

type MappingKey = (typeof mappingKeys)[number];
type ColumnMapping = Record<MappingKey, string>;

const emptyMapping: ColumnMapping = {
  studentIdentifier: "",
  dataAttribute: "",
  value: "",
  term: "",
  validFrom: "",
  subject: "",
};

/* Common header spellings, so a usual file maps itself ("indicator" is kept for old files). */
const guesses: Record<MappingKey, string[]> = {
  studentIdentifier: ["studentidentifier", "studentcode", "student", "email", "username", "mssv", "masinhvien"],
  dataAttribute: ["dataattribute", "attribute", "attributename", "indicator"],
  value: ["value", "giatri"],
  term: ["term", "termname", "semester", "hocky"],
  validFrom: ["validfrom", "date", "effectivedate", "ngay"],
  subject: ["subject", "subjectcode", "course", "coursecode", "monhoc", "mamon"],
};

const messagesOf = (item: Record<string, unknown>) =>
  Array.isArray(item.messages) ? item.messages.filter((m): m is string => typeof m === "string") : [];

const squash = (text: string) => text.toLowerCase().replace(/[\s_-]/g, "");

/**
 * Student data import: upload a one-row-per-value file (student, data
 * attribute, value, term, valid from, subject), say which file column feeds
 * each field, fix rows in the preview (grouped by student), check, import.
 *
 * The mapping is read from the file's side (QC-282): one row per file column
 * with two sample values and a "maps to" choice. A field can take only one
 * column; choosing it for another column moves it there. Columns mapped to
 * nothing are ignored.
 */
export function StudentDataImportWorkflow() {
  const t = useTranslations("workspace");
  const [mapping, setMapping] = useState<ColumnMapping>(emptyMapping);

  const fieldLabel = (key: MappingKey) => t(`mapField.${key}`);
  const missing = mappingKeys.filter((key) => !optionalKeys.has(key) && mapping[key] === "");

  function guessMapping(headers: string[]) {
    const used = new Set<string>();
    const found = { ...emptyMapping };
    for (const key of mappingKeys) {
      const header = headers.find((item) => !used.has(item) && guesses[key].includes(squash(item)));
      if (header) {
        found[key] = header;
        used.add(header);
      }
    }
    setMapping(found);
  }

  /** Give the column a field (or none): the field leaves the column that had it, the column leaves its old field. */
  function mapColumn(header: string, key: MappingKey | "") {
    setMapping((current) => {
      const next = { ...current };
      for (const field of mappingKeys) if (next[field] === header) next[field] = "";
      if (key) next[key] = header;
      return next;
    });
  }

  const send = (path: string, csv: File) => postFile(path, csv, { columnMapping: JSON.stringify(mapping) });

  return (
    <ImportEditor
      title={t("studentDataImport")}
      description={t("importWorkflowDescription")}
      accept=".xlsx,.csv"
      templateColumns={["studentCode", "dataAttribute", "value", "term", "validFrom", "subject"]}
      templateFileName="advisecare-student-data-template.csv"
      canCheck={missing.length === 0}
      groupBy={mapping.studentIdentifier || undefined}
      preview={async (file) => {
        const shape = previewShape(await postFile("StudentData/import/preview", file), t("unexpectedResponse"));
        guessMapping(shape.headers);
        return shape;
      }}
      check={async (csv) => {
        const record = (await send("StudentData/import/validate", csv)) as unknown;
        const result = isRecord(record) ? record : {};
        const valid = typeof result.validRowCount === "number" ? result.validRowCount : 0;
        return {
          ok: result.isValid === true && valid > 0,
          problems: rowProblems(result.errors, messagesOf),
          warnings: rowProblems(result.rejectedRows, messagesOf),
          summary: t("importStudentDataReady", { count: valid }),
        };
      }}
      runImport={async (csv) => {
        const payload = await send("StudentData/import", csv);
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
        return { ok: false, message: t("importRefused"), problems: rowProblems(record.errors, messagesOf) };
      }}
      beforeTable={(headers, invalidate, rows) => (
        <MappingTable
          headers={headers}
          rows={rows}
          mapping={mapping}
          missing={missing}
          fieldLabel={fieldLabel}
          onMap={(header, key) => {
            mapColumn(header, key);
            invalidate();
          }}
        />
      )}
    />
  );
}

/** One row per file column: its header, two sample values and the field it maps to. */
function MappingTable({
  headers,
  rows,
  mapping,
  missing,
  fieldLabel,
  onMap,
}: {
  headers: string[];
  rows: ImportRow[];
  mapping: ColumnMapping;
  missing: MappingKey[];
  fieldLabel: (key: MappingKey) => string;
  onMap: (header: string, key: MappingKey | "") => void;
}) {
  const t = useTranslations("workspace");
  const fieldOf = (header: string) => mappingKeys.find((key) => mapping[key] === header) ?? "";

  return (
    <section aria-labelledby="student-import-mapping" className="flex flex-col gap-space-sm rounded-lg border border-rule p-space-md">
      <div>
        <h3 id="student-import-mapping" className="text-sm font-semibold text-text">
          {t("columnMappingTitle")}
        </h3>
        <p className="mt-1 text-[0.8125rem] text-text-faint">{t("mappingDescription")}</p>
      </div>

      {missing.length ? (
        <p className="flex items-start gap-1.5 text-[0.8125rem] font-semibold text-status-attention-text">
          <Icon name="warning" className="mt-px text-[16px]" />
          {t("mappingMissing", { fields: missing.map(fieldLabel).join(", ") })}
        </p>
      ) : (
        <p className="flex items-center gap-1.5 text-[0.8125rem] text-status-ontrack-text">
          <Icon name="check_circle" className="text-[16px]" />
          {t("mappingComplete")}
        </p>
      )}

      <div className="overflow-x-auto rounded-md border border-rule">
        <table className="w-full min-w-[560px] border-collapse text-left text-sm">
          <thead className="bg-paper-tint">
            <tr className="border-b border-rule text-[0.75rem] font-semibold text-text-faint">
              <th scope="col" className="h-9 px-space-md">{t("mappingFileColumn")}</th>
              <th scope="col" className="h-9 px-space-sm">{t("mappingSample")}</th>
              <th scope="col" className="h-9 w-72 px-space-md">{t("mappingMapsTo")}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-rule">
            {headers.map((header) => {
              const field = fieldOf(header);
              const samples = rows
                .map((row) => (row.values[header] ?? "").trim())
                .filter(Boolean)
                .slice(0, 2);
              return (
                <tr key={header} className={cn("align-middle", !field && "text-text-faint")}>
                  <td className="px-space-md py-space-sm font-mono text-[0.8125rem] font-semibold">{header}</td>
                  <td className="px-space-sm py-space-sm font-mono text-[0.8125rem] text-text-muted">
                    {samples.length ? samples.join(" · ") : "—"}
                  </td>
                  <td className="px-space-md py-1.5">
                    <select
                      aria-label={t("mappingMapsToColumn", { column: header })}
                      className={cn(controlClass, "h-9 w-full pr-8", !field && "text-text-faint")}
                      value={field}
                      onChange={(event) => onMap(header, event.target.value as MappingKey | "")}
                    >
                      <option value="">{t("mappingIgnored")}</option>
                      {mappingKeys.map((key) => (
                        <option key={key} value={key}>
                          {mapping[key] && mapping[key] !== header
                            ? t("mappingMovesFrom", { field: fieldLabel(key), column: mapping[key] })
                            : fieldLabel(key)}
                        </option>
                      ))}
                    </select>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </section>
  );
}
