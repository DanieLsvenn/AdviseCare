"use client";

import { useEffect, useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import { isRecord } from "@/lib/api-contract";
import { clientApiGet } from "@/lib/client-api";
import { controlClass } from "@/components/ui/field";
import { cn } from "@/lib/cn";
import {
  FIRST_DATA_LINE,
  ImportEditor,
  postFile,
  previewShape,
  rowProblems,
  toCsvFile,
  type ImportRow,
  type RowProblem,
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
type Layout = "long" | "wide";

/** In the one-row-per-student layout the term and date can come from a column or be one value for the whole file. */
const FIXED = "__fixed__";
const wideKeys = ["studentIdentifier", "term", "validFrom"] as const;
type WideKey = (typeof wideKeys)[number];

const emptyMapping: ColumnMapping = {
  studentIdentifier: "",
  dataAttribute: "",
  value: "",
  term: "",
  validFrom: "",
};

/* Common header spellings, so a usual file maps itself. */
const guesses: Record<MappingKey, string[]> = {
  studentIdentifier: ["studentidentifier", "studentcode", "student", "email", "username", "mssv", "masinhvien"],
  dataAttribute: ["dataattribute", "attribute", "attributename", "indicator"],
  value: ["value", "giatri"],
  term: ["term", "termname", "semester", "hocky"],
  validFrom: ["validfrom", "date", "effectivedate", "ngay"],
};

/** The columns the API reads, in the one-row-per-value file sent after reshaping. */
const LONG_HEADERS = ["studentIdentifier", "dataAttribute", "value", "term", "validFrom"];
const LONG_MAPPING: ColumnMapping = {
  studentIdentifier: "studentIdentifier",
  dataAttribute: "dataAttribute",
  value: "value",
  term: "term",
  validFrom: "validFrom",
};

const messagesOf = (item: Record<string, unknown>) =>
  Array.isArray(item.messages) ? item.messages.filter((m): m is string => typeof m === "string") : [];

const squash = (text: string) => text.toLowerCase().replace(/[\s_-]/g, "");

interface Reshaped {
  csv: File;
  /** For each sent row: the table row it came from and the data attribute column it holds. */
  origin: Array<{ rowIndex: number; column: string }>;
}

/**
 * Student data import: upload, choose the file layout, map the fields to file
 * columns, fix rows in the editable preview, check, import.
 *
 * Two layouts: one row per value (student, attribute, value, term, date), or
 * the usual dataset of one row per student with a column per data attribute
 * (GPA, AbsentSlots, …). The second is reshaped here into the first before
 * it is sent, and the API's row problems are pointed back at the student's
 * row and the attribute column they belong to.
 */
export function StudentDataImportWorkflow() {
  const t = useTranslations("workspace");
  const [layout, setLayout] = useState<Layout>("wide");
  const [mapping, setMapping] = useState<ColumnMapping>(emptyMapping);
  const [fixedTerm, setFixedTerm] = useState("");
  const [fixedDate, setFixedDate] = useState("");
  const [attributeColumns, setAttributeColumns] = useState<string[]>([]);
  const [attributes, setAttributes] = useState<string[]>([]);
  const [terms, setTerms] = useState<string[]>([]);

  // Active data attributes name the columns of the wide template and are ticked automatically.
  useEffect(() => {
    let active = true;
    void Promise.all([
      clientApiGet<unknown>("DataAttributes").catch(() => []),
      clientApiGet<unknown>("AcademicTerms").catch(() => []),
    ]).then(([attributePayload, termPayload]) => {
      if (!active) return;
      const list = (payload: unknown) => (Array.isArray(payload) ? payload.filter(isRecord) : []);
      setAttributes(
        list(attributePayload)
          .filter((item) => item.status === undefined || item.status === "active")
          .map((item) => String(item.attributeName ?? ""))
          .filter(Boolean),
      );
      setTerms(list(termPayload).map((item) => String(item.termName ?? "")).filter(Boolean));
    });
    return () => {
      active = false;
    };
  }, []);

  const attributeKeys = useMemo(() => new Set(attributes.map(squash)), [attributes]);

  const wideReady =
    mapping.studentIdentifier !== "" &&
    (mapping.term === FIXED ? fixedTerm !== "" : mapping.term !== "") &&
    (mapping.validFrom === FIXED ? fixedDate !== "" : mapping.validFrom !== "") &&
    attributeColumns.length > 0;
  const longReady = mappingKeys.every((key) => mapping[key] !== "" && mapping[key] !== FIXED);
  const mappingComplete = layout === "wide" ? wideReady : longReady;

  function guessMapping(headers: string[]) {
    const found = Object.fromEntries(
      mappingKeys.map((key) => [key, headers.find((header) => guesses[key].includes(squash(header))) ?? ""]),
    ) as ColumnMapping;
    setMapping(found);
    // A file with a data-attribute column and a value column is one row per value.
    const nextLayout: Layout = found.dataAttribute && found.value ? "long" : "wide";
    setLayout(nextLayout);
    const used = new Set([found.studentIdentifier, found.term, found.validFrom].filter(Boolean));
    setAttributeColumns(headers.filter((header) => !used.has(header) && attributeKeys.has(squash(header))));
  }

  /** One row per student → one row per value, remembering where each came from. */
  function reshape(rows: ImportRow[], source: File): Reshaped {
    const origin: Reshaped["origin"] = [];
    const longRows: ImportRow[] = [];
    rows.forEach((row, rowIndex) => {
      const term = mapping.term === FIXED ? fixedTerm : (row.values[mapping.term] ?? "");
      const validFrom = mapping.validFrom === FIXED ? fixedDate : (row.values[mapping.validFrom] ?? "");
      for (const column of attributeColumns) {
        const value = (row.values[column] ?? "").trim();
        if (!value) continue; // an empty cell means "no value for this student", not an error
        origin.push({ rowIndex, column });
        longRows.push({
          id: longRows.length,
          values: {
            studentIdentifier: row.values[mapping.studentIdentifier] ?? "",
            dataAttribute: column,
            value,
            term,
            validFrom,
          },
        });
      }
    });
    return { csv: toCsvFile(LONG_HEADERS, longRows, source), origin };
  }

  /** The API's problems name rows of the reshaped file: point them at the student's row and column. */
  function pointBack(items: unknown, origin: Reshaped["origin"]): RowProblem[] {
    if (!Array.isArray(items)) return [];
    return items.filter(isRecord).map((item) => {
      const rowNumber = typeof item.rowNumber === "number" ? item.rowNumber : 0;
      const from = rowNumber >= FIRST_DATA_LINE ? origin[rowNumber - FIRST_DATA_LINE] : undefined;
      const messages = messagesOf(item);
      return from
        ? { rowIndex: from.rowIndex, messages: messages.map((message) => `${from.column}: ${message}`) }
        : { rowIndex: null, messages };
    });
  }

  async function send(path: string, csv: File, rows: ImportRow[]) {
    if (layout === "long") {
      return { payload: await postFile(path, csv, { columnMapping: JSON.stringify(mapping) }), origin: null };
    }
    const reshaped = reshape(rows, csv);
    if (reshaped.origin.length === 0) return { payload: null, origin: reshaped.origin };
    return {
      payload: await postFile(path, reshaped.csv, { columnMapping: JSON.stringify(LONG_MAPPING) }),
      origin: reshaped.origin,
    };
  }

  const problemsFrom = (items: unknown, origin: Reshaped["origin"] | null) =>
    origin ? pointBack(items, origin) : rowProblems(items, messagesOf);

  const templateColumns =
    layout === "wide"
      ? ["studentCode", "term", "validFrom", ...(attributes.length ? attributes : ["GPA", "AbsentSlots"])]
      : ["studentCode", "dataAttribute", "value", "term", "validFrom"];

  return (
    <ImportEditor
      title={t("studentDataImport")}
      description={t("importWorkflowDescriptionLayouts")}
      accept=".xlsx,.csv"
      templateColumns={templateColumns}
      templateFileName={layout === "wide" ? "advisecare-student-dataset-template.csv" : "advisecare-student-data-template.csv"}
      canCheck={mappingComplete}
      preview={async (file) => {
        const shape = previewShape(await postFile("StudentData/import/preview", file), t("unexpectedResponse"));
        guessMapping(shape.headers);
        return shape;
      }}
      check={async (csv, _headers, rows) => {
        const { payload, origin } = await send("StudentData/import/validate", csv, rows);
        if (payload === null) {
          return { ok: false, problems: [{ rowIndex: null, messages: [t("importNoValues")] }] };
        }
        const record = isRecord(payload) ? payload : {};
        const valid = typeof record.validRowCount === "number" ? record.validRowCount : 0;
        return {
          ok: record.isValid === true && valid > 0,
          problems: problemsFrom(record.errors, origin),
          warnings: problemsFrom(record.rejectedRows, origin),
          summary: t("importStudentDataReady", { count: valid }),
        };
      }}
      runImport={async (csv, _headers, rows) => {
        const { payload, origin } = await send("StudentData/import", csv, rows);
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
          problems: problemsFrom(record.errors, origin),
        };
      }}
      beforeTable={(headers, invalidate) => {
        const change = (update: () => void) => {
          update();
          invalidate();
        };
        const used = new Set([mapping.studentIdentifier, mapping.term, mapping.validFrom]);
        return (
          <section aria-labelledby="student-import-mapping" className="flex flex-col gap-space-md rounded-lg border border-rule p-space-md">
            <fieldset>
              <legend className="text-sm font-semibold text-text">{t("importLayout")}</legend>
              <div role="radiogroup" className="mt-space-sm grid gap-space-sm sm:grid-cols-2">
                {(["wide", "long"] as const).map((value) => (
                  <label
                    key={value}
                    className={cn(
                      "flex cursor-pointer flex-col rounded-md border px-space-base py-space-sm text-sm transition-colors",
                      layout === value ? "border-ink bg-ink-wash text-text" : "border-rule text-text-muted hover:border-rule-strong",
                    )}
                  >
                    <span className="flex items-center gap-2 font-semibold">
                      <input
                        type="radio"
                        name="student-import-layout"
                        className="size-4 accent-[#2142b8]"
                        checked={layout === value}
                        onChange={() => change(() => setLayout(value))}
                      />
                      {value === "wide" ? t("importLayoutWide") : t("importLayoutLong")}
                    </span>
                    <span className="mt-0.5 pl-6 text-[0.8125rem] text-text-faint">
                      {value === "wide" ? t("importLayoutWideHint") : t("importLayoutLongHint")}
                    </span>
                  </label>
                ))}
              </div>
            </fieldset>

            <div>
              <h3 id="student-import-mapping" className="text-sm font-semibold text-text">
                {t("columnMappingTitle")}
              </h3>
              <p className="mt-1 text-[0.8125rem] text-text-faint">
                {layout === "wide" ? t("mappingDescriptionWide") : t("mappingDescription")}
              </p>
            </div>

            {layout === "long" ? (
              <div className="grid gap-space-md sm:grid-cols-3 lg:grid-cols-5">
                {mappingKeys.map((key) => (
                  <label className="block" key={key}>
                    <span className="mb-1 block text-[0.8125rem] font-semibold text-text">{t(key)}</span>
                    <select
                      className={cn(controlClass, "h-9 w-full pr-8")}
                      onChange={(event) => change(() => setMapping((current) => ({ ...current, [key]: event.target.value })))}
                      required
                      value={mapping[key] === FIXED ? "" : mapping[key]}
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
            ) : (
              <>
                <div className="grid gap-space-md sm:grid-cols-3">
                  {wideKeys.map((key: WideKey) => (
                    <div key={key}>
                      <label className="block">
                        <span className="mb-1 block text-[0.8125rem] font-semibold text-text">{t(key)}</span>
                        <select
                          className={cn(controlClass, "h-9 w-full pr-8")}
                          onChange={(event) => {
                            const next = event.target.value;
                            change(() => {
                              setMapping((current) => ({ ...current, [key]: next }));
                              // A column used for the student, term or date is not a data attribute.
                              setAttributeColumns((current) => current.filter((column) => column !== next));
                            });
                          }}
                          required
                          value={mapping[key]}
                        >
                          <option value="">—</option>
                          {key !== "studentIdentifier" ? (
                            <option value={FIXED}>{key === "term" ? t("importSameTerm") : t("importSameDate")}</option>
                          ) : null}
                          {headers.map((header) => (
                            <option key={header} value={header}>
                              {header}
                            </option>
                          ))}
                        </select>
                      </label>
                      {key === "term" && mapping.term === FIXED ? (
                        <select
                          aria-label={t("term")}
                          className={cn(controlClass, "mt-1.5 h-9 w-full pr-8")}
                          value={fixedTerm}
                          onChange={(event) => change(() => setFixedTerm(event.target.value))}
                          required
                        >
                          <option value="">{t("selectOption")}</option>
                          {terms.map((term) => (
                            <option key={term} value={term}>
                              {term}
                            </option>
                          ))}
                        </select>
                      ) : null}
                      {key === "validFrom" && mapping.validFrom === FIXED ? (
                        <input
                          aria-label={t("validFrom")}
                          type="date"
                          className={cn(controlClass, "mt-1.5 h-9 w-full")}
                          value={fixedDate}
                          onChange={(event) => change(() => setFixedDate(event.target.value))}
                          required
                        />
                      ) : null}
                    </div>
                  ))}
                </div>

                <fieldset>
                  <legend className="mb-1 text-[0.8125rem] font-semibold text-text">
                    {t("importAttributeColumns", { count: attributeColumns.length })}
                  </legend>
                  <div className="flex flex-wrap gap-1.5">
                    {headers
                      .filter((header) => !used.has(header))
                      .map((header) => {
                        const checked = attributeColumns.includes(header);
                        const known = attributeKeys.has(squash(header));
                        return (
                          <label
                            key={header}
                            title={known ? undefined : t("importUnknownAttribute")}
                            className={cn(
                              "flex min-h-8 cursor-pointer items-center gap-1.5 rounded-md border px-2 font-mono text-[0.8125rem] transition-colors",
                              checked ? "border-ink bg-ink-wash text-text" : "border-rule text-text-faint hover:border-rule-strong",
                            )}
                          >
                            <input
                              type="checkbox"
                              className="size-3.5 accent-[#2142b8]"
                              checked={checked}
                              onChange={(event) =>
                                change(() =>
                                  setAttributeColumns((current) =>
                                    event.target.checked ? [...current, header] : current.filter((column) => column !== header),
                                  ),
                                )
                              }
                            />
                            {header}
                            {!known ? <span aria-hidden className="text-status-attention-text">?</span> : null}
                          </label>
                        );
                      })}
                  </div>
                  <p className="mt-1 text-[0.75rem] text-text-faint">{t("importAttributeColumnsHint")}</p>
                </fieldset>
              </>
            )}
          </section>
        );
      }}
    />
  );
}
